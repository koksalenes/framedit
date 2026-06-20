using Framedit.Models.ImageOptimizer;
using Framedit.Services.ImageOptimizer;
using Microsoft.AspNetCore.Mvc;

namespace Framedit.Controllers.ImageOptimizer;

[Route("image-optimizer")]
public class ImageOptimizerController : Controller
{
    private const int MaxFileCount  = 50;
    private const long MaxTotalSize = 1_000_000_000;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif", ".webp"
    };

    private static readonly HashSet<string> AllowedFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "keep", "jpg", "png", "webp"
    };

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
    [RequestSizeLimit(1_073_741_824)]
    [RequestFormLimits(MultipartBodyLengthLimit = 1_073_741_824, ValueCountLimit = 60)]
    public async Task<IActionResult> Optimize([FromForm] ImageOptimizeRequestModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Invalid request." });

        var files = model.Files;
        if (files == null || files.Count == 0)
            return BadRequest(new { error = "Please select at least one image." });

        if (files.Count > MaxFileCount)
            return BadRequest(new { error = $"Maximum {MaxFileCount} images allowed at once." });

        if (files.Sum(f => f.Length) > MaxTotalSize)
            return BadRequest(new { error = "Total size exceeds 1 GB." });

        var invalid = files.FirstOrDefault(f => !AllowedExtensions.Contains(Path.GetExtension(f.FileName)));
        if (invalid != null)
            return BadRequest(new { error = $"Unsupported file type '{Path.GetExtension(invalid.FileName)}'." });

        if (!AllowedFormats.Contains(model.OutputFormat))
            return BadRequest(new { error = "Invalid output format." });

        string? zipPath = null;
        try
        {
            zipPath = await _optimizer.OptimizeAsync(
                files,
                model.OutputFormat,
                model.Quality,
                model.StripMetadata,
                ct);

            var zipBytes = await System.IO.File.ReadAllBytesAsync(zipPath, ct);
            return File(zipBytes, "application/zip", "framedit-optimized.zip");
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
