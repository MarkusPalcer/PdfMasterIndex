using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PdfMasterIndex.Service.Domain.Index;
using PdfMasterIndex.Service.Infrastructure.Persistence;

namespace PdfMasterIndex.Service.Presentation.v1;

[ApiController]
public class ScanPathController(IRepository repository, ISettingsRepository settingsRepository) : ControllerBase
{
    [HttpGet("/api/v1/scanpaths")]
    public async Task<ScanPathDto[]> Get() => await repository.ScanPaths
                                                              .Include(x => x.Tags)
                                                              .Select(x => new ScanPathDto(x))
                                                              .ToArrayAsync();

    [HttpGet("/api/v1/scanpaths/{id}")]
    public async Task<ActionResult<ScanPathDto>> Get(Guid id)
    {
        var result = await repository.ScanPaths.SingleOrDefaultAsync(x => x.Id == id);

        if (result == null)
        {
            return NotFound();
        }

        return new ScanPathDto(result);
    }

    [HttpPost("/api/v1/scanpaths")]
    public async Task<ActionResult<ScanPathDto>> Post(ScanPathDto scanPath, [FromServices] ILogger<ScanPathController> logger)
    {
        logger.LogInformation("Creating new ScanPath: {Path}", scanPath.Path);
        if (scanPath.Id != Guid.Empty)
        {
            var exists = await repository.ScanPaths.AnyAsync(x => x.Id == scanPath.Id);
            if (exists)
            {
                return Conflict($"A ScanPath with ID {scanPath.Id} already exists.");
            }
        }

        if (scanPath.Path.IsNullOrEmpty())
        {
            return BadRequest("Path is required.");
        }


        var newItem = new ScanPath
        {
            Id = scanPath.Id,
            Path = scanPath.Path!,
            Name = scanPath.Name ?? scanPath.Path!,
            Tags = await repository.ProcessTags(scanPath.Tags)
        };

        await repository.AddAsync(newItem);
        await repository.SaveChangesAsync();

        var settings = await settingsRepository.GetSettingsAsync();
        settings.ScanPaths[newItem.Path] = new PdfMasterIndex.Service.Domain.Settings.ScanPath(newItem);
        await settingsRepository.SaveSettingsAsync(settings);

        logger.LogInformation("ScanPath {Path} created successfully with ID {Id}", newItem.Path, newItem.Id);
        return CreatedAtAction(nameof(Get), new { id = scanPath.Id }, scanPath);
    }

    [HttpPut("/api/v1/scanpaths/{id}")]
    public async Task<IActionResult> Put(Guid id, ScanPathDto scanPath, [FromServices] ILogger<ScanPathController> logger)
    {
        logger.LogInformation("Updating ScanPath {Id}...", id);
        if (!scanPath.Tags.IsNullOrEmpty())
        {
            return BadRequest("Tags cannot be updated with this endpoint. Use the tags collection endpoints instead");
        }

        var existing = await repository.ScanPaths.SingleOrDefaultAsync(x => x.Id == id);

        if (existing == null)
        {
            return NotFound();
        }

        var settings = await settingsRepository.GetSettingsAsync();
        var settingsEntry = settings.ScanPaths.GetValueOrDefault(existing.Path);

        if (settingsEntry is not null)
        {
            settings.ScanPaths.Remove(existing.Path);
        }

        if (!scanPath.Name.IsNullOrEmpty())
        {
            existing.Name = scanPath.Name!;
        }

        if (!scanPath.Path.IsNullOrEmpty())
        {
            existing.Path = scanPath.Path!;
        }

        repository.Update(existing);
        await repository.SaveChangesAsync();

        settings.ScanPaths[existing.Path] = new Domain.Settings.ScanPath(existing);
        await settingsRepository.SaveSettingsAsync(settings);

        logger.LogInformation("ScanPath {Id} updated successfully", id);
        return NoContent();
    }

    [HttpDelete("/api/v1/scanpaths/{id}")]
    public async Task<IActionResult> Delete(Guid id, [FromServices] ILogger<ScanPathController> logger)
    {
        logger.LogInformation("Deleting ScanPath {Id}...", id);
        var existing = await repository.ScanPaths.SingleOrDefaultAsync(x => x.Id == id);

        if (existing == null) return NoContent();

        await repository.DeleteScanPathAsync(existing);

        var settings = await settingsRepository.GetSettingsAsync();
        settings.ScanPaths.Remove(existing.Path);
        await settingsRepository.SaveSettingsAsync(settings);

        logger.LogInformation("ScanPath {Id} deleted successfully from database and settings", id);
        return NoContent();
    }

    [HttpGet("/api/v1/scanpaths/{id}/tags")]
    public async Task<ActionResult<string[]>> GetTags(Guid id)
    {
        var existing = await repository.ScanPaths.SingleOrDefaultAsync(x => x.Id == id);
        if (existing == null) return NotFound("ScanPath not found");

        return Ok(existing.Tags.Select(t => t.Value).ToArray());
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public record DocumentDto(Guid Id, string Name, int Pages, int Words, string[] Tags);

    [HttpGet("/api/v1/scanpaths/{id}/documents")]
    public async Task<ActionResult<DocumentDto[]>> GetDocuments(Guid id)
    {
        var existing = await repository.ScanPaths
                            .Include(x => x.Documents)
                            .ThenInclude(x => x.Tags)
                            .SingleOrDefaultAsync(x => x.Id == id);
        if (existing == null) return NotFound("ScanPath not found");

        return Ok(existing.Documents.Select(d => new DocumentDto(d.Id, d.Name, d.PageCount, d.WordCount, d.Tags.Select(t => t.Value).ToArray())).ToArray());
    }

    [HttpPost("/api/v1/scanpaths/{id}/tags/{tag}")]
    public async Task<IActionResult> AddTag(Guid id, string tag)
    {
        var existing = await repository.ScanPaths.Include(x => x.Tags).SingleOrDefaultAsync(x => x.Id == id);
        if (existing == null) return NotFound("ScanPath not found");

        if (existing.Tags.Any(t => t.Value == tag)) return NoContent();

        var tagEntity = await repository.ProcessTags([tag]);

        existing.Tags.Add(tagEntity.First());
        await repository.SaveChangesAsync();

        var settings = await settingsRepository.GetSettingsAsync();
        settings.ScanPaths[existing.Path] = new Domain.Settings.ScanPath(existing);
        await settingsRepository.SaveSettingsAsync(settings);

        return NoContent();
    }

    [HttpDelete("/api/v1/scanpaths/{id}/tags/{tag}")]
    public async Task<IActionResult> RemoveTag(Guid id, string tag)
    {
        var existing = await repository.ScanPaths.Include(x => x.Tags).SingleOrDefaultAsync(x => x.Id == id);
        if (existing == null) return NotFound("ScanPath not found");

        var tagToRemove = existing.Tags.FirstOrDefault(t => t.Value == tag);
        if (tagToRemove == null) return NoContent();

        existing.Tags.Remove(tagToRemove);
        await repository.SaveChangesAsync();

        var settings = await settingsRepository.GetSettingsAsync();
        settings.ScanPaths[existing.Path] = new Domain.Settings.ScanPath(existing);
        await settingsRepository.SaveSettingsAsync(settings);

        return NoContent();
    }
}

public class ScanPathDto
{
    public ScanPathDto(ScanPath scanPath)
    {
        Id = scanPath.Id;
        Name = scanPath.Name;
        Path = scanPath.Path;
        Tags = scanPath.Tags.Select(t => t.Value).ToArray();
    }

    public ScanPathDto()
    {
    }

    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Path { get; set; }

    public string[]? Tags { get; set; }
}