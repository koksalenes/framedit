using System.Text.RegularExpressions;
using Framedit.Models.VideoFrameParser;
using Framedit.Services.VideoFrameParser;
using Microsoft.AspNetCore.Mvc;

namespace Framedit.Controllers.VideoFrameParser;

[Route("video-frame-parser")]
public class VideoFrameParserController : Controller
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".avi", ".mkv", ".webm", ".flv", ".wmv", ".m4v", ".mpeg", ".mpg"
    };

    private static readonly Regex BaseNameRegex = new(@"^[a-zA-Z0-9][a-zA-Z0-9_-]*$", RegexOptions.Compiled);

    private readonly IFrameParserService _frameParser;
    private readonly ILogger<VideoFrameParserController> _logger;

    public VideoFrameParserController(IFrameParserService frameParser, ILogger<VideoFrameParserController> logger)
    {
        _frameParser = frameParser;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpPost("parse")]
    [RequestSizeLimit(1_073_741_824)]
    [RequestFormLimits(MultipartBodyLengthLimit = 1_073_741_824)]
    public async Task<IActionResult> Parse([FromForm] ParseRequestModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                             ?? "Invalid request.";
            return BadRequest(new { error = firstError });
        }

        if (model.VideoFile == null || model.VideoFile.Length == 0)
            return BadRequest(new { error = "Please select a video file." });

        var ext = Path.GetExtension(model.VideoFile.FileName);
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { error = $"Unsupported file type '{ext}'. Accepted: {string.Join(", ", AllowedExtensions)}" });

        if (model.Fps is < 0 or > 120)
            return BadRequest(new { error = "FPS must be between 0 and 120." });

        var baseName = (model.BaseName ?? "frame").Trim();
        if (!BaseNameRegex.IsMatch(baseName))
            return BadRequest(new { error = "Base name may only contain letters, numbers, hyphens and underscores, and must start with a letter or number." });

        string? zipPath = null;
        try
        {
            await using var stream = model.VideoFile.OpenReadStream();
            zipPath = await _frameParser.ExtractFramesAsync(
                stream, model.VideoFile.FileName,
                model.Fps, model.Format,
                baseName, model.StripMetadata, ct);

            var zipBytes    = await System.IO.File.ReadAllBytesAsync(zipPath, ct);
            var downloadName = $"{baseName}.zip";

            return File(zipBytes, "application/zip", downloadName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during frame extraction");
            return StatusCode(500, new { error = "An unexpected error occurred. Please try again." });
        }
        finally
        {
            if (zipPath != null)
                _frameParser.Cleanup(zipPath);
        }
    }
}
