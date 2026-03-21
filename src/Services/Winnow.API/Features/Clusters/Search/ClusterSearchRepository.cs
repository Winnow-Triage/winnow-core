
using Dapper;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Clusters.Search;

public class ClusterSearchRepository(WinnowDbContext dbContext) : IClusterSearchRepository
{
    private readonly WinnowDbContext _dbContext = dbContext;

    public async Task<PaginatedClusterSearchList<ClusterSearchDto>> GetRecentClustersAsync(Guid projectId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var take = pageSize;
        var skip = (pageNumber - 1) * pageSize;

        const string sql = @"
            SELECT
                c.""Id"", 
                c.""Title"", 
                c.""Summary"",
                c.""Status"",
                c.""CreatedAt"",
                c.""CriticalityScore"",
                (SELECT COUNT(*) FROM ""Reports"" r WHERE r.""ClusterId"" = c.""Id"") AS ReportCount,
                EXISTS (SELECT 1 FROM ""Reports"" r WHERE r.""ClusterId"" = c.""Id"" AND r.""IsLocked"" = TRUE) AS IsLocked,
                EXISTS (SELECT 1 FROM ""Reports"" r WHERE r.""ClusterId"" = c.""Id"" AND r.""IsOverage"" = TRUE) AS IsOverage
            FROM ""Clusters"" c
            WHERE c.""ProjectId"" = @ProjectId
            ORDER BY c.""CreatedAt"" DESC
            LIMIT @Take OFFSET @Skip;";

        const string countSql = @"SELECT COUNT(*) FROM ""Clusters"" WHERE ""ProjectId"" = @ProjectId;";

        var items = await _dbContext.Database.GetDbConnection().QueryAsync<ClusterSearchDto>(sql, new { ProjectId = projectId, Take = take, Skip = skip });
        var totalCount = await _dbContext.Database.GetDbConnection().ExecuteScalarAsync<int>(countSql, new { ProjectId = projectId });

        return new PaginatedClusterSearchList<ClusterSearchDto>(items.AsList(), totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedClusterSearchList<ClusterSearchDto>> HybridSearchClustersAsync(Guid projectId, string searchText, float[] searchVector, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var take = pageSize;
        var skip = (pageNumber - 1) * pageSize;

        // Ensure proper vector string format for pgvector, e.g., '[0.1, 0.2, ...]'
        var vectorString = "[" + string.Join(", ", searchVector) + "]";

        // RRF (Reciprocal Rank Fusion) parameters
        // K is typically set to 60 for RRF
        const string sql = @"
            WITH keyword_search AS(
                SELECT
                    ""Id"",
                    ts_rank(to_tsvector('english', COALESCE(""Title"", '') || ' ' || COALESCE(""Summary"", '')), plainto_tsquery('english', @SearchText)) AS rank_score
                FROM ""Clusters""
                WHERE ""ProjectId"" = @ProjectId
                  AND to_tsvector('english', COALESCE(""Title"", '') || ' ' || COALESCE(""Summary"", '')) @@ plainto_tsquery('english', @SearchText)
            ),
            vector_search AS(
                SELECT
                    ""Id"", 
                    1 - (""Centroid"" <=> @SearchVector::vector) AS vector_score
                FROM ""Clusters""
                WHERE ""ProjectId"" = @ProjectId AND ""Centroid"" IS NOT NULL
                ORDER BY ""Centroid"" <=> @SearchVector::vector
                LIMIT 100
            ),
            rrf AS(
                SELECT
                    COALESCE(k.""Id"", v.""Id"") AS cluster_id,
                    COALESCE(1.0 / (60 + ROW_NUMBER() OVER(ORDER BY k.rank_score DESC)), 0) +
                    COALESCE(1.0 / (60 + ROW_NUMBER() OVER(ORDER BY v.vector_score DESC)), 0) AS relevance_score
                FROM keyword_search k
                FULL OUTER JOIN vector_search v ON k.""Id"" = v.""Id""
            )
            SELECT
                c.""Id"", 
                c.""Title"", 
                c.""Summary"",
                c.""Status"",
                c.""CreatedAt"",
                c.""CriticalityScore"",
                (SELECT COUNT(*) FROM ""Reports"" r WHERE r.""ClusterId"" = c.""Id"") AS ReportCount,
                EXISTS (SELECT 1 FROM ""Reports"" r WHERE r.""ClusterId"" = c.""Id"" AND r.""IsLocked"" = TRUE) AS IsLocked,
                EXISTS (SELECT 1 FROM ""Reports"" r WHERE r.""ClusterId"" = c.""Id"" AND r.""IsOverage"" = TRUE) AS IsOverage,
                rrf.relevance_score AS RelevanceScore
            FROM rrf
            JOIN ""Clusters"" c ON rrf.cluster_id = c.""Id""
            ORDER BY rrf.relevance_score DESC
            LIMIT @Take OFFSET @Skip;";

        var items = await _dbContext.Database.GetDbConnection().QueryAsync<ClusterSearchDto>(sql, new
        {
            ProjectId = projectId,
            SearchText = searchText,
            SearchVector = vectorString,
            Take = take,
            Skip = skip
        });

        return new PaginatedClusterSearchList<ClusterSearchDto>(items.AsList(), items.Count(), pageNumber, pageSize);
    }
}
