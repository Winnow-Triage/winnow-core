
using Dapper;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Clusters.Search;

public class ClusterSearchRepository(WinnowDbContext dbContext) : IClusterSearchRepository
{
    private readonly WinnowDbContext _dbContext = dbContext;

    public async Task<PaginatedClusterSearchList<ClusterSearchDto>> GetRecentClustersAsync(Guid projectId, ClusterSearchFilters filters, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var take = pageSize;
        var skip = (pageNumber - 1) * pageSize;

        var (whereClause, parameters) = BuildFilterClause(projectId, filters);
        parameters.Add("Take", take);
        parameters.Add("Skip", skip);

        var orderByClause = "ORDER BY c.\"CreatedAt\" DESC";
        if (!string.IsNullOrEmpty(filters.SortBy) && !filters.SortBy.Equals("relevanceScore", StringComparison.OrdinalIgnoreCase))
        {
            var sortOrder = filters.SortOrder.Equals("Asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

            // Map frontend sort keys to database columns
            var sortColumn = filters.SortBy switch
            {
                "title" => "Title",
                "status" => "Status",
                "createdAt" => "CreatedAt",
                "criticalityScore" => "CriticalityScore",
                "reportCount" => "ReportCount",
                _ => "CreatedAt"
            };

            orderByClause = sortColumn == "ReportCount"
                ? $"ORDER BY (SELECT COUNT(*) FROM \"Reports\" r WHERE r.\"ClusterId\" = c.\"Id\") {sortOrder}"
                : $"ORDER BY c.\"{sortColumn}\" {sortOrder}";
        }

        var sql = $@"
            SELECT
                c.""Id"", 
                c.""Title"", 
                c.""Summary"",
                c.""Status"",
                c.""CreatedAt"",
                c.""CriticalityScore"",
                (c.""IsSummarizing"" = TRUE AND (c.""SummarizationStartedAt"" IS NULL OR c.""SummarizationStartedAt"" > NOW() - INTERVAL '10 minutes')) AS IsSummarizing,
                (SELECT COUNT(*) FROM ""Reports"" r WHERE r.""ClusterId"" = c.""Id"") AS ReportCount,
                EXISTS (SELECT 1 FROM ""Reports"" r WHERE r.""ClusterId"" = c.""Id"" AND r.""IsLocked"" = TRUE) AS IsLocked,
                EXISTS (SELECT 1 FROM ""Reports"" r WHERE r.""ClusterId"" = c.""Id"" AND r.""IsOverage"" = TRUE) AS IsOverage
            FROM ""Clusters"" c
            WHERE {whereClause}
            {orderByClause}
            LIMIT @Take OFFSET @Skip;";

        var countSql = $@"SELECT COUNT(*) FROM ""Clusters"" c WHERE {whereClause};";

        var items = await _dbContext.Database.GetDbConnection().QueryAsync<ClusterSearchDto>(sql, parameters);
        var totalCount = await _dbContext.Database.GetDbConnection().ExecuteScalarAsync<int>(countSql, parameters);

        return new PaginatedClusterSearchList<ClusterSearchDto>(items.AsList(), totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedClusterSearchList<ClusterSearchDto>> HybridSearchClustersAsync(Guid projectId, string searchText, float[] searchVector, ClusterSearchFilters filters, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var take = pageSize;
        var skip = (pageNumber - 1) * pageSize;

        var (whereClause, parameters) = BuildFilterClause(projectId, filters);
        parameters.Add("SearchText", searchText);
        parameters.Add("SearchVector", "[" + string.Join(", ", searchVector) + "]");
        parameters.Add("Take", take);
        parameters.Add("Skip", skip);

        var orderByClause = "ORDER BY rrf.relevance_score DESC";
        if (!string.IsNullOrEmpty(filters.SortBy) && !filters.SortBy.Equals("relevanceScore", StringComparison.OrdinalIgnoreCase))
        {
            var sortOrder = filters.SortOrder.Equals("Asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
            var sortColumn = filters.SortBy switch
            {
                "title" => "Title",
                "status" => "Status",
                "createdAt" => "CreatedAt",
                "criticalityScore" => "CriticalityScore",
                "reportCount" => "ReportCount",
                _ => null
            };

            if (sortColumn != null)
            {
                orderByClause = sortColumn == "ReportCount"
                    ? $"ORDER BY (SELECT COUNT(*) FROM \"Reports\" r WHERE r.\"ClusterId\" = c.\"Id\") {sortOrder}"
                    : $"ORDER BY c.\"{sortColumn}\" {sortOrder}";
            }
        }

        const string keywordSql = @"
            SELECT
                ""Id"",
                ROW_NUMBER() OVER(ORDER BY (
                    ts_rank(to_tsvector('english', COALESCE(""Title"", '') || ' ' || COALESCE(""Summary"", '')), websearch_to_tsquery('english', @SearchText)) * 1.0 +
                    ts_rank(to_tsvector('english', COALESCE(""Title"", '') || ' ' || COALESCE(""Summary"", '')), phraseto_tsquery('english', @SearchText)) * 2.0
                ) DESC) AS rank
            FROM ""Clusters"" c
            WHERE {0}
              AND (
                to_tsvector('english', COALESCE(""Title"", '') || ' ' || COALESCE(""Summary"", '')) @@ websearch_to_tsquery('english', @SearchText) OR
                to_tsvector('english', COALESCE(""Title"", '') || ' ' || COALESCE(""Summary"", '')) @@ phraseto_tsquery('english', @SearchText)
              )";

        const string vectorSql = @"
            SELECT
                ""Id"", 
                ROW_NUMBER() OVER(ORDER BY ""Centroid"" <=> @SearchVector::vector ASC) AS rank
            FROM ""Clusters"" c
            WHERE {0} AND ""Centroid"" IS NOT NULL
            LIMIT 100";

        var sql = $@"
            WITH keyword_search AS({string.Format(keywordSql, whereClause)}),
            vector_search AS ({string.Format(vectorSql, whereClause)}),
            rrf AS(
                SELECT
                    COALESCE(k.""Id"", v.""Id"") AS cluster_id,
                    (COALESCE(3.0 / (60 + k.rank), 0) + COALESCE(4.0 / (60 + v.rank), 0)) AS relevance_score
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
                (c.""IsSummarizing"" = TRUE AND (c.""SummarizationStartedAt"" IS NULL OR c.""SummarizationStartedAt"" > NOW() - INTERVAL '10 minutes')) AS IsSummarizing,
                (SELECT COUNT(*) FROM ""Reports"" r WHERE r.""ClusterId"" = c.""Id"") AS ReportCount,
                EXISTS (SELECT 1 FROM ""Reports"" r WHERE r.""ClusterId"" = c.""Id"" AND r.""IsLocked"" = TRUE) AS IsLocked,
                EXISTS (SELECT 1 FROM ""Reports"" r WHERE r.""ClusterId"" = c.""Id"" AND r.""IsOverage"" = TRUE) AS IsOverage,
                rrf.relevance_score AS RelevanceScore
            FROM rrf
            JOIN ""Clusters"" c ON rrf.cluster_id = c.""Id""
            {orderByClause}
            LIMIT @Take OFFSET @Skip;";

        var items = await _dbContext.Database.GetDbConnection().QueryAsync<ClusterSearchDto>(sql, parameters);

        // Count for hybrid search is tricky because of the RRF join, but we can approximate or use the CTE count
        var countSql = $@"
            SELECT COUNT(*) 
            FROM ""Clusters"" c 
            WHERE {whereClause} 
            AND (
                to_tsvector('english', COALESCE(""Title"", '') || ' ' || COALESCE(""Summary"", '')) @@ websearch_to_tsquery('english', @SearchText)
                OR ""Id"" IN (SELECT ""Id"" FROM ({string.Format(vectorSql, whereClause)}) v)
            );";
        var totalCount = await _dbContext.Database.GetDbConnection().ExecuteScalarAsync<int>(countSql, parameters);

        return new PaginatedClusterSearchList<ClusterSearchDto>(items.AsList(), totalCount, pageNumber, pageSize);
    }

    private (string WhereClause, DynamicParameters Parameters) BuildFilterClause(Guid projectId, ClusterSearchFilters filters)
    {
        var conditions = new List<string> { "c.\"ProjectId\" = @ProjectId" };
        var parameters = new DynamicParameters();
        parameters.Add("ProjectId", projectId);

        if (filters.Statuses != null && filters.Statuses.Length > 0)
        {
            conditions.Add("c.\"Status\" = ANY(@Statuses)");
            parameters.Add("Statuses", filters.Statuses);
        }

        if (filters.IsOverage.HasValue)
        {
            if (filters.IsOverage.Value)
                conditions.Add("EXISTS (SELECT 1 FROM \"Reports\" r WHERE r.\"ClusterId\" = c.\"Id\" AND r.\"IsOverage\" = TRUE)");
            else
                conditions.Add("NOT EXISTS (SELECT 1 FROM \"Reports\" r WHERE r.\"ClusterId\" = c.\"Id\" AND r.\"IsOverage\" = TRUE)");
        }

        if (filters.IsLocked.HasValue)
        {
            if (filters.IsLocked.Value)
                conditions.Add("EXISTS (SELECT 1 FROM \"Reports\" r WHERE r.\"ClusterId\" = c.\"Id\" AND r.\"IsLocked\" = TRUE)");
            else
                conditions.Add("NOT EXISTS (SELECT 1 FROM \"Reports\" r WHERE r.\"ClusterId\" = c.\"Id\" AND r.\"IsLocked\" = TRUE)");
        }

        return (string.Join(" AND ", conditions), parameters);
    }
}
