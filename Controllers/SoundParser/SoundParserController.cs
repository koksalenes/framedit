using Framedit.Models.SoundParser;
using Framedit.Services.SoundParser;
using Microsoft.AspNetCore.Mvc;

namespace Framedit.Controllers.SoundParser;

[Route("sound-parser")]
public class SoundParserController : Controller
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".avi", ".mkv", ".webm", ".flv", ".wmv", ".m4v", ".mpeg", ".mpg"
    };

    private static readonly HashSet<string> AllowedFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "mp3", "aac", "wav", "flac", "ogg"
    };

    private readonly ISoundParserService _soundParser;
    private readonly ILogger<SoundParserController> _logger;

    public SoundParserController(ISoundParserService soundParser, ILogger<SoundParserController> logger)
    {
        _soundParser = soundParser;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpPost("extract")]
    [RequestSizeLimit(1_000_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 1_000_000_000)]
    public async Task<IActionResult> Extract([FromForm] SoundParseRequestModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Invalid request." });

        if (model.VideoFile == null || model.VideoFile.Length == 0)
            return BadRequest(new { error = "Please select a video file." });

        var ext = Path.GetExtension(model.VideoFile.FileName);
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { error = $"Unsupported file type '{ext}'." });

        if (!AllowedFormats.Contains(model.Format))
            return BadRequest(new { error = "Invalid output format." });

        string? outputPath = null;
        try
        {
            await using var stream = model.VideoFile.OpenReadStream();
            outputPath = await _soundParser.ExtractAudioAsync(stream, model.VideoFile.FileName, model.Format, ct);

            var audioBytes   = await System.IO.File.ReadAllBytesAsync(outputPath, ct);
            var baseName     = Path.GetFileNameWithoutExtension(model.VideoFile.FileName);
            var downloadName = $"{baseName}.{model.Format}";
            var mimeType     = MimeType(model.Format);

            return File(audioBytes, mimeType, downloadName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during audio extraction");
            return StatusCode(500, new { error = "An unexpected error occurred. Please try again." });
        }
        finally
        {
            if (outputPath != null) _soundParser.Cleanup(outputPath);
        }
    }

    private static string MimeType(string format) => format switch
    {
        "aac"  => "audio/aac",
        "wav"  => "audio/wav",
        "flac" => "audio/flac",
        "ogg"  => "audio/ogg",
        _      => "audio/mpeg",
    };
}
