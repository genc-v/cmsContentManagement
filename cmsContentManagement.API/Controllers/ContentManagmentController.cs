using cmsContentManagement.Application.DTO;
using cmsContentManagement.Application.Interfaces;
using cmsContentManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cmsContentManagment.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ContentManagmentController : ControllerBase
{
    private readonly IContentManagmentService _contentManagmentService;

    public ContentManagmentController(IContentManagmentService contentManagmentService)
    {
        _contentManagmentService = contentManagmentService;
    }

    [HttpGet("{userId}/{contentId}")]
    public async Task<Content> GetContents(Guid userId, Guid contentId)
    {
        return await _contentManagmentService.getContentById(userId, contentId);
    }

    [HttpGet]
    public async Task<List<Content>> GetAllContents([FromQuery] Guid userId)
    {
        return await _contentManagmentService.getAllContents(userId);
    }

    [HttpGet("search")]
    public async Task<IReadOnlyCollection<Content>> SearchContents(
        [FromQuery] string? query,
        [FromQuery] int size = 25
    )
    {
        return await _contentManagmentService.SearchContents(query, size);
    }

    [HttpPut]
    public async Task CreateContent([FromBody] ContentDTO content)
    {
        await _contentManagmentService.UpdateContent(content);
    }

    [HttpDelete("{userId}/{contentId}")]
    public async Task DeleteContent(Guid userId, Guid contentId)
    {
        await _contentManagmentService.DeleteContent(userId, contentId);
    }

    [HttpGet("generate-new-id")]
    public async Task<Guid> GenerateNewContentId(Guid userId)
    {
        return await _contentManagmentService.GenerateNewContentId(userId);
    }
}
