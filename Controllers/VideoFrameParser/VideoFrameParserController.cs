using System.Text.RegularExpressions;
using Framedit.Constants;
using Framedit.Models.VideoFrameParser;
using Framedit.Services.VideoFrameParser;
using Microsoft.AspNetCore.Mvc;

namespace Framedit.Controllers.VideoFrameParser;

[Route("video-frame-parser")]
public class VideoFrameParserController : Controller
{
    private static readonly Regex BaseNameRegex =
        new(AppConstants.VideoFrameParser.BaseNamePattern, RegexOptions.Compiled);

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
    [RequestSizeLimit(AppConstants.Upload.HttpRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = AppConstants.Upload.HttpRequestBytes)]
    public async Task<IActionResult> Parse([FromForm] ParseRequestModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Invalid request." });

        if (model.VideoFile == null || model.VideoFile.Length == 0)
            return BadRequest(new { error = "Please select a video file." });

        var ext = Path.GetExtension(model.VideoFile.FileName);
        if (!AppConstants.VideoFrameParser.AllowedExtensions.Contains(ext))
            return BadRequest(new { error = $"Unsupported file type '{ext}'." });

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

            var zipBytes     = await System.IO.File.ReadAllBytesAsync(zipPath, ct);
            var downloadName = $"{baseName}.zip";

            return File(zipBytes, AppConstants.Upload.ZipMimeType, downloadName);
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
            if (zipPath != null) _frameParser.Cleanup(zipPath);
        }
    }
}
