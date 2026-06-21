using Framedit.Constants;
using Framedit.Services.MetadataCleaner;
using Microsoft.AspNetCore.Mvc;

namespace Framedit.Controllers.MetadataCleaner;

[Route("metadata-cleaner")]
public class MetadataCleanerController : Controller
{
    private readonly IMetadataCleanerService _cleaner;
    private readonly ILogger<MetadataCleanerController> _logger;

    public MetadataCleanerController(IMetadataCleanerService cleaner, ILogger<MetadataCleanerController> logger)
    {
        _cleaner = cleaner;
        _logger  = logger;
    }

    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpPost("clean")]
    [RequestSizeLimit(AppConstants.Upload.HttpRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = AppConstants.Upload.HttpRequestBytes, ValueCountLimit = AppConstants.Upload.MaxFormValueCount)]
    public async Task<IActionResult> Clean(IFormFileCollection files, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Invalid request." });

        if (files == null || files.Count == 0)
            return BadRequest(new { error = "Please select at least one file." });

        if (files.Count > AppConstants.Upload.MaxFileCount)
            return BadRequest(new { error = $"Maximum {AppConstants.Upload.MaxFileCount} files allowed at once." });

        if (files.Sum(f => f.Length) > AppConstants.Upload.MaxTotalBytes)
            return BadRequest(new { error = "Total size exceeds the 1 GB limit." });

        var invalid = files.FirstOrDefault(f => !AppConstants.MetadataCleaner.AllowedExtensions.Contains(Path.GetExtension(f.FileName)));
        if (invalid != null)
            return BadRequest(new { error = $"Unsupported file type '{Path.GetExtension(invalid.FileName)}'." });

        string? zipPath = null;
        try
        {
            zipPath = await _cleaner.CleanAsync(files, ct);
            var zipBytes = await System.IO.File.ReadAllBytesAsync(zipPath, ct);
            return File(zipBytes, AppConstants.Upload.ZipMimeType, AppConstants.MetadataCleaner.DownloadName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during metadata cleaning");
            return StatusCode(500, new { error = "An unexpected error occurred. Please try again." });
        }
        finally
        {
            if (zipPath != null) _cleaner.Cleanup(zipPath);
        }
    }
}
