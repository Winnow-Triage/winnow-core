using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.Search;

public class ReportSearchRepository : IReportSearchRepository
{
    private readonly WinnowDbContext _dbContext;

    public ReportSearchRepository(WinnowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedSearchList<ReportSearchDto>> GetRecentlyUpdatedReportsAsync(Guid projectId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var take = pageSize;
        var skip = (pageNumber - 1) * pageSize;

        const string sql = @"
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
            WHERE ""ProjectId"" = @ProjectId
            ORDER BY ""CreatedAt"" DESC
            LIMIT @Take OFFSET @Skip;";

        const string countSql = @"SELECT COUNT(*) FROM ""Reports"" WHERE ""ProjectId"" = @ProjectId;";

        var items = await _dbContext.Database.GetDbConnection().QueryAsync<ReportSearchDto>(sql, new { ProjectId = projectId, Take = take, Skip = skip });
        var totalCount = await _dbContext.Database.GetDbConnection().ExecuteScalarAsync<int>(countSql, new { ProjectId = projectId });

        return new PaginatedSearchList<ReportSearchDto>(items.AsList(), totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedSearchList<ReportSearchDto>> HybridSearchReportsAsync(Guid projectId, string searchText, float[] searchVector, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
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
                    ts_rank(to_tsvector('english', ""Title"" || ' ' || ""Message""), plainto_tsquery('english', @SearchText)) AS rank_score
                FROM ""Reports""
                WHERE ""ProjectId"" = @ProjectId
                  AND to_tsvector('english', ""Title"" || ' ' || ""Message"") @@ plainto_tsquery('english', @SearchText)
            ),
            vector_search AS(
                SELECT
                    ""Id"", 
                    1 - (""Embedding"" <=> @SearchVector::vector) AS vector_score
                FROM ""Reports""
                WHERE ""ProjectId"" = @ProjectId
                ORDER BY ""Embedding"" <=> @SearchVector::vector
                LIMIT 100
            ),
            rrf AS(
                SELECT
                    COALESCE(k.""Id"", v.""Id"") AS report_id,
                    COALESCE(1.0 / (60 + ROW_NUMBER() OVER(ORDER BY k.rank_score DESC)), 0) +
                    COALESCE(1.0 / (60 + ROW_NUMBER() OVER(ORDER BY v.vector_score DESC)), 0) AS relevance_score
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
            ORDER BY rrf.relevance_score DESC
            LIMIT @Take OFFSET @Skip;";

        var items = await _dbContext.Database.GetDbConnection().QueryAsync<ReportSearchDto>(sql, new
        {
            ProjectId = projectId,
            SearchText = searchText,
            SearchVector = vectorString,
            Take = take,
            Skip = skip
        });

        // For simplicity in search, total count might just be the retrieved items depending on requirements
        return new PaginatedSearchList<ReportSearchDto>(items.AsList(), items.Count(), pageNumber, pageSize);
    }
}
