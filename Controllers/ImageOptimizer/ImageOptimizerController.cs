using Framedit.Constants;
using Framedit.Models.ImageOptimizer;
using Framedit.Services.ImageOptimizer;
using Microsoft.AspNetCore.Mvc;

namespace Framedit.Controllers.ImageOptimizer;

[Route("image-optimizer")]
public class ImageOptimizerController : Controller
{
    private readonly IImageOptimizerService _optimizer;
    private readonly ILogger<ImageOptimizerController> _logger;

    public ImageOptimizerController(IImageOptimizerService optimizer, ILogger<ImageOptimizerController> logger)
    {
        _optimizer = optimizer;
        _logger    = logger;
    }

    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpPost("optimize")]
    [RequestSizeLimit(AppConstants.Upload.HttpRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = AppConstants.Upload.HttpRequestBytes, ValueCountLimit = AppConstants.Upload.MaxFormValueCount)]
    public async Task<IActionResult> Optimize([FromForm] ImageOptimizeRequestModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Invalid request." });

        var files = model.Files;
        if (files == null || files.Count == 0)
            return BadRequest(new { error = "Please select at least one image." });

        if (files.Count > AppConstants.Upload.MaxFileCount)
            return BadRequest(new { error = $"Maximum {AppConstants.Upload.MaxFileCount} images allowed at once." });

        if (files.Sum(f => f.Length) > AppConstants.Upload.MaxTotalBytes)
            return BadRequest(new { error = "Total size exceeds 1 GB." });

        var invalid = files.FirstOrDefault(f => !AppConstants.ImageOptimizer.AllowedExtensions.Contains(Path.GetExtension(f.FileName)));
        if (invalid != null)
            return BadRequest(new { error = $"Unsupported file type '{Path.GetExtension(invalid.FileName)}'." });

        if (!AppConstants.ImageOptimizer.AllowedFormats.Contains(model.OutputFormat))
            return BadRequest(new { error = "Invalid output format." });

        string? zipPath = null;
        try
        {
            zipPath = await _optimizer.OptimizeAsync(files, model.OutputFormat, model.Quality, model.StripMetadata, ct);
            var zipBytes = await System.IO.File.ReadAllBytesAsync(zipPath, ct);
            return File(zipBytes, AppConstants.Upload.ZipMimeType, AppConstants.ImageOptimizer.DownloadName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during image optimization");
            return StatusCode(500, new { error = "An unexpected error occurred. Please try again." });
        }
        finally
        {
            if (zipPath != null) _optimizer.Cleanup(zipPath);
        }
    }
}
