using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfMasterIndex.Service.Infrastructure.Persistence;

namespace PdfMasterIndex.Service.Presentation.v1;

[ApiController]
public class TagController(IRepository repository) : ControllerBase
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public record TagDto(Guid Id, string Value);

    [HttpGet("/api/v1/tags")]
    public async Task<TagDto[]> Get()
    {
        return await repository.Tags
                               .Include(x => x.ScanPaths)
                               .Where(x => x.ScanPaths.Any() || x.Documents.Any())
                               .Select(x => new TagDto(x.Id, x.Value))
                               .ToArrayAsync();
    }
}