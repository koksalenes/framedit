using Framedit.Constants;
using Framedit.Models.SoundParser;
using Framedit.Services.SoundParser;
using Microsoft.AspNetCore.Mvc;

namespace Framedit.Controllers.SoundParser;

[Route("sound-parser")]
public class SoundParserController : Controller
{
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
    [RequestSizeLimit(AppConstants.Upload.HttpRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = AppConstants.Upload.HttpRequestBytes)]
    public async Task<IActionResult> Extract([FromForm] SoundParseRequestModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Invalid request." });

        if (model.VideoFile == null || model.VideoFile.Length == 0)
            return BadRequest(new { error = "Please select a video file." });

        var ext = Path.GetExtension(model.VideoFile.FileName);
        if (!AppConstants.SoundParser.AllowedExtensions.Contains(ext))
            return BadRequest(new { error = $"Unsupported file type '{ext}'." });

        if (!AppConstants.SoundParser.AllowedFormats.Contains(model.Format))
            return BadRequest(new { error = "Invalid output format." });

        string? outputPath = null;
        try
        {
            await using var stream = model.VideoFile.OpenReadStream();
            outputPath = await _soundParser.ExtractAudioAsync(stream, model.VideoFile.FileName, model.Format, ct);

            var audioBytes   = await System.IO.File.ReadAllBytesAsync(outputPath, ct);
            var baseName     = Path.GetFileNameWithoutExtension(model.VideoFile.FileName);
            var fileType     = ResolveFileType(model.Format);
            var downloadName = $"{baseName}{fileType.Extension}";

            return File(audioBytes, fileType.MimeType, downloadName);
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

    private static AppConstants.FileType ResolveFileType(string format)
    {
        if (format.Equals(AppConstants.Ext.Aac.Name,  StringComparison.OrdinalIgnoreCase)) return AppConstants.Ext.Aac;
        if (format.Equals(AppConstants.Ext.Wav.Name,  StringComparison.OrdinalIgnoreCase)) return AppConstants.Ext.Wav;
        if (format.Equals(AppConstants.Ext.Flac.Name, StringComparison.OrdinalIgnoreCase)) return AppConstants.Ext.Flac;
        if (format.Equals(AppConstants.Ext.Ogg.Name,  StringComparison.OrdinalIgnoreCase)) return AppConstants.Ext.Ogg;
        return AppConstants.Ext.Mp3;
    }
}
