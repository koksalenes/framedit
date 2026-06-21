using System.Diagnostics;
using System.IO.Compression;
using Mediaration.Constants;

namespace Mediaration.Services.VideoFrameParser;

public class FrameParserService : IFrameParserService
{
    private readonly ILogger<FrameParserService> _logger;

    public FrameParserService(ILogger<FrameParserService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractFramesAsync(
        Stream videoStream,
        string fileName,
        double fps,
        string format,
        string baseName,
        bool stripMetadata,
        CancellationToken ct = default)
    {
        var sessionDir = Path.Combine(Path.GetTempPath(), "mediaration", Guid.NewGuid().ToString());
        var framesDir  = Path.Combine(sessionDir, "frames");
        Directory.CreateDirectory(framesDir);

        var inputExt  = Path.GetExtension(fileName).ToLowerInvariant();
        var videoPath = Path.Combine(sessionDir, $"input{inputExt}");

        await using (var fs = File.Create(videoPath))
            await videoStream.CopyToAsync(fs, ct);

        var (ext, codecArgs) = ResolveFrameFormat(format);

        var vfArgs     = fps > 0 ? $"-vf \"fps={fps}\"" : "";
        var metaArgs   = stripMetadata ? "-map_metadata -1" : "";
        var tmpPattern = Path.Combine(framesDir, $"tmp_%08d{ext}");
        var args       = $"-i \"{videoPath}\" {vfArgs} {codecArgs} {metaArgs} \"{tmpPattern}\"";

        if (_logger.IsEnabled(LogLevel.Information))
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
            throw new InvalidOperationException(
                $"FFmpeg failed with exit code {process.ExitCode}. Ensure the file is a valid video.");
        }

        var tmpFiles = Directory.GetFiles(framesDir, $"tmp_*{ext}")
                                .OrderBy(f => f)
                                .ToArray();

        if (tmpFiles.Length == 0)
            throw new InvalidOperationException(
                "No frames were extracted. The video may be empty or in an unsupported format.");

        int digits = (int)Math.Floor(Math.Log10(tmpFiles.Length)) + 1;

        for (int i = 0; i < tmpFiles.Length; i++)
        {
            var num     = (i + 1).ToString().PadLeft(digits, '0');
            var newPath = Path.Combine(framesDir, $"{baseName}-{num}{ext}");
            File.Move(tmpFiles[i], newPath);
        }

        var zipPath = Path.Combine(sessionDir, "frames.zip");
        await ZipFile.CreateFromDirectoryAsync(framesDir, zipPath, ct);

        return zipPath;
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

    private static (string ext, string codecArgs) ResolveFrameFormat(string format)
    {
        if (format.Equals(AppConstants.Ext.Png.Name,  StringComparison.OrdinalIgnoreCase))
            return (AppConstants.Ext.Png.Extension,  "-compression_level 3");
        if (format.Equals(AppConstants.Ext.Webp.Name, StringComparison.OrdinalIgnoreCase))
            return (AppConstants.Ext.Webp.Extension, "-c:v libwebp -q:v 85");
        return (AppConstants.Ext.Jpg.Extension, "-q:v 2");
    }
}
