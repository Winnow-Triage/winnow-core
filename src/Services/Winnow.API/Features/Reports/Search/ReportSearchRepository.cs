using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Reports.Search;

public class ReportSearchRepository : IReportSearchRepository
{
    private readonly WinnowDbContext _dbContext;

    public ReportSearchRepository(WinnowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedSearchList<ReportSearchDto>> GetRecentlyUpdatedReportsAsync(
        Guid projectId,
        int pageNumber,
        int pageSize,
        ReportSearchFilters filters,
        CancellationToken cancellationToken = default)
    {
        var take = pageSize;
        var skip = (pageNumber - 1) * pageSize;

        var (whereClause, parameters) = BuildFilterClause(projectId, filters);
        parameters.Add("Take", take);
        parameters.Add("Skip", skip);

        // Sorting
        var allowedSortFields = new[] { "Title", "Status", "CreatedAt" };
        var sortBy = allowedSortFields.Contains(filters.SortBy) ? $"\"{filters.SortBy}\"" : "\"CreatedAt\"";
        var sortOrder = filters.SortOrder.Equals("Asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

        var sql = $@"
            SELECT
                ""Id"", 
                ""Title"", 
                ""Message"" AS Description,
                ""Status"",
                ""CreatedAt"" AS UpdatedAt,
                ""ClusterId"",
                ""IsOverage"",
                ""IsLocked""
            FROM ""Reports""
            WHERE {whereClause}
            ORDER BY {sortBy} {sortOrder}
            LIMIT @Take OFFSET @Skip;";

        var countSql = $@"SELECT COUNT(*) FROM ""Reports"" WHERE {whereClause};";

        var items = await _dbContext.Database.GetDbConnection().QueryAsync<ReportSearchDto>(sql, parameters);
        var totalCount = await _dbContext.Database.GetDbConnection().ExecuteScalarAsync<int>(countSql, parameters);

        return new PaginatedSearchList<ReportSearchDto>(items.AsList(), totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedSearchList<ReportSearchDto>> HybridSearchReportsAsync(
        Guid projectId,
        string searchText,
        float[] searchVector,
        int pageNumber,
        int pageSize,
        ReportSearchFilters filters,
        CancellationToken cancellationToken = default)
    {
        var take = pageSize;
        var skip = (pageNumber - 1) * pageSize;

        // Ensure proper vector string format for pgvector, e.g., '[0.1, 0.2, ...]'
        var vectorString = "[" + string.Join(", ", searchVector) + "]";

        var (whereClause, parameters) = BuildFilterClause(projectId, filters);
        parameters.Add("SearchText", searchText);
        parameters.Add("SearchVector", vectorString);
        parameters.Add("Take", take);
        parameters.Add("Skip", skip);

        // Sorting
        var allowedSortFields = new[] { "Title", "Status", "CreatedAt" };
        string orderByClause;
        if (allowedSortFields.Contains(filters.SortBy))
        {
            var sortOrder = filters.SortOrder.Equals("Asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
            orderByClause = $"ORDER BY r.\"{filters.SortBy}\" {sortOrder}";
        }
        else
        {
            orderByClause = "ORDER BY rrf.relevance_score DESC";
        }

        const string keywordSql = @"
            SELECT
                ""Id"",
                ROW_NUMBER() OVER(ORDER BY (
                    ts_rank(to_tsvector('english', ""Title"" || ' ' || ""Message""), websearch_to_tsquery('english', @SearchText)) * 1.0 +
                    ts_rank(to_tsvector('english', ""Title"" || ' ' || ""Message""), phraseto_tsquery('english', @SearchText)) * 2.0
                ) DESC) AS rank
            FROM ""Reports""
            WHERE {0}
              AND (
                to_tsvector('english', ""Title"" || ' ' || ""Message"") @@ websearch_to_tsquery('english', @SearchText) OR
                to_tsvector('english', ""Title"" || ' ' || ""Message"") @@ phraseto_tsquery('english', @SearchText)
              )";

        const string vectorSql = @"
            SELECT
                ""Id"", 
                ROW_NUMBER() OVER(ORDER BY ""Embedding"" <=> @SearchVector::vector ASC) AS rank
            FROM ""Reports""
            WHERE {0}
            LIMIT 100";

        var sql = $@"
            WITH keyword_search AS({string.Format(keywordSql, whereClause)}),
            vector_search AS ({string.Format(vectorSql, whereClause)}),
            rrf AS(
                SELECT
                    COALESCE(k.""Id"", v.""Id"") AS report_id,
                    (COALESCE(3.0 / (60 + k.rank), 0) + COALESCE(4.0 / (60 + v.rank), 0)) AS relevance_score
                FROM keyword_search k
                FULL OUTER JOIN vector_search v ON k.""Id"" = v.""Id""
            )
            SELECT
                r.""Id"", 
                r.""Title"", 
                r.""Message"" AS Description,
                r.""Status"",
                r.""CreatedAt"" AS UpdatedAt,
                r.""ClusterId"",
                r.""IsOverage"",
                r.""IsLocked"",
                rrf.relevance_score AS RelevanceScore
            FROM rrf
            JOIN ""Reports"" r ON rrf.report_id = r.""Id""
            {orderByClause}
            LIMIT @Take OFFSET @Skip;";

        var countSql = $@"
            SELECT COUNT(*) 
            FROM ""Reports"" 
            WHERE {whereClause} 
            AND (
                to_tsvector('english', ""Title"" || ' ' || ""Message"") @@ plainto_tsquery('english', @SearchText)
                OR ""Id"" IN (SELECT ""Id"" FROM ({string.Format(vectorSql, whereClause)}) v)
            );";

        var items = await _dbContext.Database.GetDbConnection().QueryAsync<ReportSearchDto>(sql, parameters);
        var totalCount = await _dbContext.Database.GetDbConnection().ExecuteScalarAsync<int>(countSql, parameters);

        return new PaginatedSearchList<ReportSearchDto>(items.AsList(), totalCount, pageNumber, pageSize);
    }

    private (string WhereClause, DynamicParameters Parameters) BuildFilterClause(Guid projectId, ReportSearchFilters filters)
    {
        var conditions = new List<string> { "\"ProjectId\" = @ProjectId", "\"IsSanitized\" = true" };
        var parameters = new DynamicParameters();
        parameters.Add("ProjectId", projectId);

        if (filters.Statuses != null && filters.Statuses.Length > 0)
        {
            conditions.Add("\"Status\" = ANY(@Statuses)");
            parameters.Add("Statuses", filters.Statuses);
        }

        if (filters.ClusterId.HasValue)
        {
            conditions.Add("\"ClusterId\" = @ClusterId");
            parameters.Add("ClusterId", filters.ClusterId.Value);
        }

        if (filters.IsOverage.HasValue)
        {
            conditions.Add("\"IsOverage\" = @IsOverage");
            parameters.Add("IsOverage", filters.IsOverage.Value);
        }

        if (filters.IsLocked.HasValue)
        {
            conditions.Add("\"IsLocked\" = @IsLocked");
            parameters.Add("IsLocked", filters.IsLocked.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.AssignedTo))
        {
            conditions.Add("\"AssignedTo\" = @AssignedTo");
            parameters.Add("AssignedTo", filters.AssignedTo);
        }

        return (string.Join(" AND ", conditions), parameters);
    }
}
