using System;
using System.Collections.Generic;
using System.Linq;
using cmsContentManagement.Application.Common.ErrorCodes;
using cmsContentManagement.Application.Common.Settings;
using cmsContentManagement.Application.DTO;
using cmsContentManagement.Application.Interfaces;
using cmsContentManagement.Domain.Entities;
using cmsContentManagment.Infrastructure.Persistance;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace cmsContentManagment.Infrastructure.Repositories;

public class ContentManagmentService : IContentManagmentService
{
    private readonly AppDbContext _dbContext;
    private readonly ElasticsearchClient _elasticClient;
    private readonly ElasticSettings _elasticSettings;
    private readonly ILogger<ContentManagmentService> _logger;

    public ContentManagmentService(
        AppDbContext dbContext,
        ElasticsearchClient elasticClient,
        IOptions<ElasticSettings> elasticOptions,
        ILogger<ContentManagmentService> logger)
    {
        _dbContext = dbContext;
        _elasticClient = elasticClient;
        _logger = logger;
        _elasticSettings = elasticOptions.Value;
    }

    public async Task<Guid> GenerateNewContentId(Guid userId)
    {
        var content = await _dbContext.Contents.FirstOrDefaultAsync(e => e.UserId == userId && e.Status == "New");
        if (content != null) return content.ContentId;

        content = new Content
        {
            UserId = userId
        };

        await _dbContext.Contents.AddAsync(content);
        await _dbContext.SaveChangesAsync();
        await IndexContentAsync(content);

        return content.ContentId;
    }

    public async Task<Content> getContentById(Guid userId, Guid contentId)
    {
        var content = await _dbContext.Contents.FirstOrDefaultAsync(e => e.UserId == userId && e.ContentId == contentId);
        if (content == null) throw GeneralErrorCodes.NotFound;
        return content;
    }

    public async Task<List<Content>> getAllContents(Guid userId)
    {
        return await _dbContext.Contents.Where(e => e.UserId == userId).ToListAsync();
    }

    public async Task DeleteContent(Guid userId, Guid contentId)
    {
        var content = await _dbContext.Contents.FirstOrDefaultAsync(e => e.UserId == userId && e.ContentId == contentId);
        if (content == null) throw GeneralErrorCodes.NotFound;

        _dbContext.Contents.Remove(content);
        await _dbContext.SaveChangesAsync();
        await RemoveContentFromIndexAsync(contentId);
    }

    public async Task UpdateContent(ContentDTO content)
    {
        var contentToBeUpdated = await _dbContext.Contents.FirstOrDefaultAsync(e => e.UserId == content.UserId && e.ContentId == content.ContentId);
        if (contentToBeUpdated == null) throw GeneralErrorCodes.NotFound;

        contentToBeUpdated.Title = content.Title;
        contentToBeUpdated.RichContent = content.RichContent;

        await _dbContext.SaveChangesAsync();
        await IndexContentAsync(contentToBeUpdated);
    }

    public async Task AddAssetUrlToContent(Guid userId, Guid contentId, string assetUrl)
    {
        var content = await _dbContext.Contents.FirstOrDefaultAsync(e => e.UserId == userId && e.ContentId == contentId);
        if (content == null) throw GeneralErrorCodes.NotFound;

        content.AssetUrl = assetUrl;

        await _dbContext.SaveChangesAsync();
        await IndexContentAsync(content);
    }

    public async Task<IReadOnlyCollection<Content>> SearchContents(string query, int size = 25)
    {
        size = size <= 0 ? 25 : Math.Min(size, 100);

        try
        {
            Query queryContainer = string.IsNullOrWhiteSpace(query)
                ? new MatchAllQuery()
                : new SimpleQueryStringQuery
                {
                    Query = $"{query}*",
                    Fields = new[] { "title", "richContent", "assetUrl" },
                    DefaultOperator = Operator.And
                };

            var request = new SearchRequest<Content>(_elasticSettings.DefaultIndex)
            {
                Size = size,
                Query = queryContainer
            };

            var response = await _elasticClient.SearchAsync<Content>(request);

            if (!response.IsValidResponse || response.Documents is null)
            {
                _logger.LogWarning("Elasticsearch search failed with query '{Query}'.", query);
                return Array.Empty<Content>();
            }

            return response.Documents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while searching content with query '{Query}'", query);
            return Array.Empty<Content>();
        }
    }

    private async Task IndexContentAsync(Content content)
    {
        try
        {
            var response = await _elasticClient.IndexAsync(content, i => i
                .Index(_elasticSettings.DefaultIndex)
                .Id(content.ContentId.ToString()));

            if (!response.IsValidResponse)
            {
                _logger.LogWarning("Failed to index content {ContentId}: {Reason}", content.ContentId, response.DebugInformation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while indexing content {ContentId}", content.ContentId);
        }
    }

    private async Task RemoveContentFromIndexAsync(Guid contentId)
    {
        try
        {
            var response = await _elasticClient.DeleteAsync<Content>(contentId.ToString(), d => d.Index(_elasticSettings.DefaultIndex));

            if (!response.IsValidResponse)
            {
                _logger.LogWarning("Failed to remove content {ContentId} from index: {Reason}", contentId, response.DebugInformation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting content {ContentId} from index", contentId);
        }
    }
}