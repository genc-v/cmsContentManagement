using cmsContentManagement.Application.DTO;
using cmsContentManagement.Domain.Entities;

namespace cmsContentManagement.Application.Interfaces;

public interface IContentManagmentService
{
    public Task<Guid> GenerateNewContentId(Guid userId);
    public Task<Content> getContentById(Guid userId, Guid contentId);
    public Task<List<Content>> getAllContents(Guid userId);
    public Task DeleteContent(Guid userId, Guid contentId);
    public Task UpdateContent(ContentDTO content);
    public Task AddAssetUrlToContent(Guid userId, Guid contentId, string assetUrl);
    public Task<IReadOnlyCollection<Content>> SearchContents(string query, int size = 25);
}
