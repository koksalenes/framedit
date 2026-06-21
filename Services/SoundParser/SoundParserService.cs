using System.Diagnostics;
using Mediaration.Constants;

namespace Mediaration.Services.SoundParser;

public class SoundParserService : ISoundParserService
{
    private readonly ILogger<SoundParserService> _logger;

    public SoundParserService(ILogger<SoundParserService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractAudioAsync(Stream videoStream, string fileName, string format, CancellationToken ct = default)
    {
        var sessionDir = Path.Combine(Path.GetTempPath(), "mediaration", "sound", Guid.NewGuid().ToString());
        Directory.CreateDirectory(sessionDir);

        var inputExt  = Path.GetExtension(fileName).ToLowerInvariant();
        var inputPath = Path.Combine(sessionDir, $"input{inputExt}");

        await using (var fs = File.Create(inputPath))
            await videoStream.CopyToAsync(fs, ct);

        var (outputExt, codecArgs) = ResolveAudioFormat(format);

        var outputPath = Path.Combine(sessionDir, $"audio{outputExt}");
        var args = $"-y -i \"{inputPath}\" {codecArgs} \"{outputPath}\"";

        _logger.LogInformation("Running ffmpeg: {Args}", args);

        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = args,
            RedirectStandardError  = true,
            RedirectStandardOutput = true,
            UseShellExecute  = false,
            CreateNoWindow   = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            _logger.LogError("FFmpeg failed (exit {Code}): {Stderr}", process.ExitCode, stderr);
            throw new InvalidOperationException("Failed to extract audio. The video may have no audio track or be in an unsupported format.");
        }

        return outputPath;
    }

    public void Cleanup(string sessionPath)
    {
        try
        {
            var dir = Path.GetDirectoryName(sessionPath);
            if (dir != null && Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup session at {SessionPath}", sessionPath);
        }
    }

    private static (string ext, string codecArgs) ResolveAudioFormat(string format)
    {
        if (format.Equals(AppConstants.Ext.Aac.Name,  StringComparison.OrdinalIgnoreCase))
            return (AppConstants.Ext.Aac.Extension,  "-vn -c:a aac -b:a 192k");
        if (format.Equals(AppConstants.Ext.Wav.Name,  StringComparison.OrdinalIgnoreCase))
            return (AppConstants.Ext.Wav.Extension,  "-vn -c:a pcm_s16le");
        if (format.Equals(AppConstants.Ext.Flac.Name, StringComparison.OrdinalIgnoreCase))
            return (AppConstants.Ext.Flac.Extension, "-vn -c:a flac");
        if (format.Equals(AppConstants.Ext.Ogg.Name,  StringComparison.OrdinalIgnoreCase))
            return (AppConstants.Ext.Ogg.Extension,  "-vn -c:a libvorbis -q:a 6");
        return (AppConstants.Ext.Mp3.Extension, "-vn -c:a libmp3lame -q:a 2");
    }
}
