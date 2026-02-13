using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Infrastructure.Scheduling;

public class ClusterRefinementJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ClusterRefinementJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var tenants = GetAllActiveTenants();

                foreach (var tenantId in tenants)
                {
                    logger.LogInformation("Janitor: Starting cleanup for Tenant {TenantId}", tenantId);
                    try
                    {
                        await RunRefinementForTenantAsync(tenantId, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Janitor: Refinement failed for Tenant {TenantId}", tenantId);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cluster refinement cycle failed.");
            }

            // Run every 5 minutes
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private List<string> GetAllActiveTenants()
    {
        var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        if (!Directory.Exists(dataDir)) return new List<string> { "default" };

        var files = Directory.GetFiles(dataDir, "*.db");
        var tenants = files
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .ToList();

        if (!tenants.Contains("default")) tenants.Add("default");
        return tenants;
    }

    private async Task RunRefinementForTenantAsync(string tenantId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();

        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        if (tenantContext is TenantContext concreteContext)
        {
            concreteContext.TenantId = tenantId == "default" ? null : tenantId;
        }

        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        // 1. Fetch "Active Leaders"
        // Full Sweep: We look at ALL active leaders
        var recentLeaders = await db.Tickets
            .AsNoTracking()
            .Where(t => t.ParentTicketId == null
                     && t.Status != "Duplicate")
            .OrderBy(t => t.CreatedAt) // OLDEST FIRST: Oldest master is always the stable target
            .ToListAsync(ct);

        if (recentLeaders.Count < 1) return;

        logger.LogInformation("Janitor [{TenantId}]: Scanning {Count} active leaders. (Oldest-Wins Strategy)", tenantId, recentLeaders.Count);

        // 2. Ensure all leaders have embeddings (Healing step)
        var embeddingService = scope.ServiceProvider.GetRequiredService<Winnow.Server.Services.Ai.IEmbeddingService>();
        foreach (var leader in recentLeaders.Where(l => l.Embedding == null))
        {
            try
            {
                logger.LogInformation("Janitor [{TenantId}]: Generating missing embedding for ticket {Id}", tenantId, leader.Id);
                var text = $"{leader.Title}\n{leader.Description}";
                var embeddingFloats = await embeddingService.GetEmbeddingAsync(text);
                var embeddingBytes = new byte[embeddingFloats.Length * sizeof(float)];
                Buffer.BlockCopy(embeddingFloats, 0, embeddingBytes, 0, embeddingBytes.Length);

                await db.Database.ExecuteSqlRawAsync(
                    "UPDATE Tickets SET Embedding = {0} WHERE Id = {1}",
                    [embeddingBytes, leader.Id], ct);

                await db.Database.ExecuteSqlRawAsync(
                    "INSERT OR REPLACE INTO vec_tickets(rowid, embedding) VALUES ((SELECT rowid FROM Tickets WHERE Id = {0}), {1})",
                    [leader.Id, embeddingBytes], ct);

                // Update local object for this run
                leader.Embedding = embeddingBytes;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Janitor [{TenantId}]: Failed to heal embedding for {Id}", tenantId, leader.Id);
            }
        }

        int mergeCount = 0;
        int suggestCount = 0;
        var processedIds = new HashSet<Guid>();
        var mergeMap = new Dictionary<Guid, Guid>(); // Track merges in THIS cycle to avoid loops
        
        // Resolve Singleton services from the OUTER scope (Singleton) via constructor injection if possible, 
        // or just resolve from the scope provider since they are Singletons/Scoped appropriately.
        // Actually, IDuplicateChecker is Scoped (per request/scope), INegativeMatchCache is Singleton.
        // We can resolve both from the current scope.
        var duplicateChecker = scope.ServiceProvider.GetRequiredService<Winnow.Server.Services.Ai.IDuplicateChecker>();
        var negativeCache = scope.ServiceProvider.GetRequiredService<Winnow.Server.Services.Ai.INegativeMatchCache>();

        foreach (var leaderA in recentLeaders)
        {
            if (processedIds.Contains(leaderA.Id)) continue;
            if (leaderA.Embedding == null) continue;

            const double HardMergeThreshold = 0.35;
            const double SuggestThreshold = 0.55;

            // Find similar tickets (ANY ticket, not just leaders)
            var matches = await FindMatchesInDb(db, leaderA, SuggestThreshold, ct);
            if (matches.Count == 0) continue;

            // Resolve current state of targets (they might have merged earlier in this loop)
            var matchIds = matches.Select(m => m.Id).ToList();
            var parentInfo = await db.Tickets
                .AsNoTracking()
                .Where(t => matchIds.Contains(t.Id))
                .Select(t => new { t.Id, t.ParentTicketId, t.CreatedAt })
                .ToDictionaryAsync(t => t.Id, t => new { t.ParentTicketId, t.CreatedAt }, ct);

            // Group by ultimate parent
            var clusterGroupsRaw = matches
                .Select(m =>
                {
                    var info = parentInfo[m.Id];
                    var currentParentId = info.ParentTicketId ?? m.Id;

                    // Trace forward if the parent was merged in THIS cycle
                    while (mergeMap.TryGetValue(currentParentId, out var nextParent))
                    {
                        currentParentId = nextParent;
                    }

                    return new { Match = m, UltimateParentId = currentParentId };
                })
                .Where(m => m.UltimateParentId != leaderA.Id) // Don't match self
                .GroupBy(m => m.UltimateParentId)
                .ToList();

            var clusterGroups = new List<(Guid Key, List<TicketMatch> Items)>();
            foreach (var group in clusterGroupsRaw)
            {
                // DEEP RESOLUTION: Ensure the group key itself is a true root in the DB
                var trueRootId = await db.ResolveUltimateMasterAsync(group.Key, ct);
                clusterGroups.Add((trueRootId, group.Select(g => g.Match).ToList()));
            }

            ClusterMatch? bestMatch = null;
            float[] leaderAFloats = BytesToFloats(leaderA.Embedding);

            foreach (var group in clusterGroups)
            {
                var clusterId = group.Key;

                // Fetch all members of this cluster to calculate centroid
                var members = await db.Tickets
                    .AsNoTracking()
                    .Where(t => t.Id == clusterId || t.ParentTicketId == clusterId)
                    .Select(t => t.Embedding)
                    .Where(e => e != null)
                    .ToListAsync(ct);

                if (members.Count == 0) continue;

                var centroid = CalculateCentroid(members);
                var centroidDist = CalculateCosineDistance(leaderAFloats, centroid);

                if (bestMatch == null || centroidDist < bestMatch.Distance)
                {
                    bestMatch = new ClusterMatch(clusterId, group.Items.First().Title, centroidDist);
                }
            }

            if (bestMatch != null)
            {
                // Oldest-Wins Rule: A can only merge into B if B is OLDER than A.
                // Otherwise, A remains the master and B should merge into A when it's B's turn.
                var targetTicket = await db.Tickets.AsNoTracking()
                    .Where(t => t.Id == bestMatch.Id)
                    .Select(t => new { t.Id, t.Title, t.Description, t.CreatedAt })
                    .FirstOrDefaultAsync(ct);

                if (targetTicket == null) continue;

                if (bestMatch.Distance <= HardMergeThreshold)
                {
                    // If target is younger, we skip and wait for the target's turn to merge into US
                    if (targetTicket.CreatedAt > leaderA.CreatedAt)
                    {
                        logger.LogDebug("Janitor [{TenantId}]: Possible merge {A} -> {B} skipped (Oldest Wins: {A} is older).",
                            tenantId, leaderA.Id, targetTicket.Id, leaderA.Id);
                        continue;
                    }

                    // SEMANTIC GATEKEEPER CHECK (0.15 - 0.35 range)
                    if (bestMatch.Distance > 0.15)
                    {
                        // Check Negative Cache first!
                        if (negativeCache.IsKnownMismatch(tenantId, leaderA.Id, targetTicket.Id))
                        {
                            logger.LogDebug("Janitor [{TenantId}]: Skipping known mismatch {A} -> {B} (Cache Hit).", tenantId, leaderA.Id, targetTicket.Id);
                            
                            // Skip entirely if known mismatch
                            continue;
                        }

                        // Verify with LLM
                        var areDuplicates = await duplicateChecker.AreDuplicatesAsync(
                            leaderA.Title, leaderA.Description!,
                            targetTicket.Title, targetTicket.Description!,
                            ct);

                        if (!areDuplicates)
                        {
                            logger.LogInformation("Janitor [{TenantId}]: Semantic Gatekeeper VETOED merge {A} -> {B}. Caching negative match.", tenantId, leaderA.Id, targetTicket.Id);
                            negativeCache.MarkAsMismatch(tenantId, leaderA.Id, targetTicket.Id);
                            
                            // Downgrade to Suggestion
                            goto SuggestionPath;
                        }
                    }

                    logger.LogInformation("Janitor [{TenantId}]: CLUSTER MERGE! {LeaderA} -> {ClusterLeader} (Centroid Match, Dist: {Dist:F3})",
                        tenantId, leaderA.Title, bestMatch.Title, bestMatch.Distance);

                    // 1. Hierarchical Flattening: Reassign ALL children of LeaderA to bestMatch.Id
                    await db.Database.ExecuteSqlRawAsync(
                        "UPDATE Tickets SET ParentTicketId = {0} WHERE ParentTicketId = {1}",
                        [bestMatch.Id, leaderA.Id], ct);

                    // 2. Mark LeaderA itself as a duplicate of bestMatch.Id
                    var ticketA = await db.Tickets.FindAsync([leaderA.Id], ct);
                    if (ticketA != null)
                    {
                        ticketA.ParentTicketId = bestMatch.Id;
                        ticketA.Status = "Duplicate";
                        ticketA.ConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
                        ticketA.SuggestedParentId = null;
                        ticketA.SuggestedConfidenceScore = null;
                        mergeCount++;
                        processedIds.Add(leaderA.Id);
                        mergeMap[leaderA.Id] = bestMatch.Id; // Record the merge for transitive resolution in this cycle
                    }
                    continue; // Skip suggestion path if merged
                }

                SuggestionPath:
                if (bestMatch.Distance <= SuggestThreshold)
                {
                    // Suggestions don't strictly need Oldest Wins but it helps stability
                    var ticketA = await db.Tickets.FindAsync([leaderA.Id], ct);
                    if (ticketA != null && ticketA.SuggestedParentId == null)
                    {
                        ticketA.SuggestedParentId = bestMatch.Id;
                        ticketA.SuggestedConfidenceScore = (float)Math.Max(0, 1.0 - bestMatch.Distance);
                        suggestCount++;
                    }
                }
            }
        }

        // 3. Cycle Breaker Pass
        await BreakCyclesAsync(db, tenantId, ct);

        if (mergeCount > 0 || suggestCount > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Janitor [{TenantId}]: Cleanup complete. Merged: {Merge}, Suggested: {Suggest}.",
                tenantId, mergeCount, suggestCount);
        }
    }

    private async Task BreakCyclesAsync(WinnowDbContext db, string tenantId, CancellationToken ct)
    {
        // Detect tickets where Parent points to a ticket that ALSO has a parent (or points back)
        var candidates = await db.Tickets
            .Where(t => t.ParentTicketId != null)
            .Select(t => new { t.Id, t.ParentTicketId })
            .ToListAsync(ct);

        int fixes = 0;
        foreach (var c in candidates)
        {
            var path = new List<Guid> { c.Id };
            var currentId = c.ParentTicketId;

            while (currentId != null)
            {
                if (path.Contains(currentId.Value))
                {
                    // CYCLE DETECTED! (e.g., A -> B -> A)
                    logger.LogWarning("Janitor [{TenantId}]: Circular reference detected! Breaking cycle at {Id}", tenantId, currentId.Value);

                    // Fix: Break the link by picking the oldest to be master (or just nulling current)
                    await db.Database.ExecuteSqlRawAsync(
                        "UPDATE Tickets SET ParentTicketId = NULL, Status = 'Open' WHERE Id = {0}",
                        [currentId.Value], ct);

                    fixes++;
                    break;
                }

                path.Add(currentId.Value);

                // Fetch next parent
                var nextParentId = await db.Tickets
                    .Where(t => t.Id == currentId.Value)
                    .Select(t => t.ParentTicketId)
                    .FirstOrDefaultAsync(ct);

                if (nextParentId == null) break; // Reached a root

                // If we reach a 3rd level (A -> B -> C), flatten it immediately
                if (path.Count >= 2)
                {
                    logger.LogInformation("Janitor [{TenantId}]: Flattening deep hierarchy {A} -> {B} -> {Root}",
                        tenantId, path[0], path[1], nextParentId);

                    await db.Database.ExecuteSqlRawAsync(
                        "UPDATE Tickets SET ParentTicketId = {0} WHERE Id = {1}",
                        [nextParentId, path[0]], ct);

                    fixes++;
                    break;
                }

                currentId = nextParentId;
            }
        }

        if (fixes > 0)
        {
            logger.LogInformation("Janitor [{TenantId}]: Cycle Breaker fixed {Count} hierarchies.", tenantId, fixes);
        }
    }

    private async Task<List<TicketMatch>> FindMatchesInDb(WinnowDbContext db, Ticket target, double threshold, CancellationToken ct)
    {
        var sql = @"
            SELECT t.Id, t.Title, t.CreatedAt, v.distance as Distance
            FROM vec_tickets v
            JOIN Tickets t ON v.rowid = t.rowid
            WHERE v.embedding MATCH {0}
              AND k = 20
              AND v.distance < {1}
              AND t.Id != {2}
              AND t.Status != 'Duplicate'
        ";

        return await db.Database.SqlQueryRaw<TicketMatch>(sql, target.Embedding!, threshold, target.Id)
            .ToListAsync(ct);
    }

    private float[] BytesToFloats(byte[] bytes)
    {
        float[] floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }

    private float[] CalculateCentroid(List<byte[]?> embeddings)
    {
        var validOnes = embeddings.Where(e => e != null).Cast<byte[]>().ToList();
        if (validOnes.Count == 0) return Array.Empty<float>();

        int length = validOnes[0].Length / sizeof(float);
        float[] centroid = new float[length];
        foreach (var bytes in validOnes)
        {
            for (int i = 0; i < length; i++)
            {
                centroid[i] += BitConverter.ToSingle(bytes, i * sizeof(float));
            }
        }
        for (int i = 0; i < length; i++) centroid[i] /= validOnes.Count;
        return centroid;
    }

    private double CalculateCosineDistance(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0) return 1.0;
        float dot = 0, ma = 0, mb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            ma += a[i] * a[i];
            mb += b[i] * b[i];
        }
        if (ma == 0 || mb == 0) return 1.0;
        return 1.0 - (dot / (Math.Sqrt(ma) * Math.Sqrt(mb)));
    }

    private record TicketMatch(Guid Id, string Title, DateTime CreatedAt, double Distance);
    private record ClusterMatch(Guid Id, string Title, double Distance);
}
