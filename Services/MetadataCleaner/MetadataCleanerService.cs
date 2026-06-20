using System.Diagnostics;
using System.IO.Compression;

namespace Framedit.Services.MetadataCleaner;

public class MetadataCleanerService : IMetadataCleanerService
{
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".avi", ".mkv", ".webm", ".m4v", ".flv", ".wmv", ".mpeg", ".mpg"
    };

    private readonly ILogger<MetadataCleanerService> _logger;

    public MetadataCleanerService(ILogger<MetadataCleanerService> logger)
    {
        _logger = logger;
    }

    public async Task<string> CleanAsync(IEnumerable<IFormFile> files, CancellationToken ct = default)
    {
        var sessionDir = Path.Combine(Path.GetTempPath(), "framedit", "metadata", Guid.NewGuid().ToString());
        var tmpDir     = Path.Combine(sessionDir, "tmp");
        var outDir     = Path.Combine(sessionDir, "out");
        Directory.CreateDirectory(tmpDir);
        Directory.CreateDirectory(outDir);

        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();

            var safeName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(safeName)) continue;

            if (!usedNames.Add(safeName))
            {
                var stem = Path.GetFileNameWithoutExtension(safeName);
                var ext  = Path.GetExtension(safeName);
                var i = 2;
                do { safeName = $"{stem}-{i++}{ext}"; } while (!usedNames.Add(safeName));
            }

            var fileExt  = Path.GetExtension(safeName);
            var tmpPath  = Path.Combine(tmpDir, Guid.NewGuid() + fileExt);
            var outPath  = Path.Combine(outDir, safeName);

            await using (var fs = File.Create(tmpPath))
                await file.OpenReadStream().CopyToAsync(fs, ct);

            var codecArgs = VideoExtensions.Contains(fileExt) ? "-c copy" : "";
            var args = $"-y -i \"{tmpPath}\" -map_metadata -1 {codecArgs} \"{outPath}\"";

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
                _logger.LogError("FFmpeg failed for {File} (exit {Code}): {Stderr}", safeName, process.ExitCode, stderr);
                throw new InvalidOperationException($"Failed to clean '{safeName}'. Ensure it is a supported format.");
            }

            File.Delete(tmpPath);
        }

        var zipPath = Path.Combine(sessionDir, "cleaned.zip");
        await using var zipStream = File.Create(zipPath);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: false);

        foreach (var outFile in Directory.GetFiles(outDir).OrderBy(f => f))
            await archive.CreateEntryFromFileAsync(outFile, $"framedit-metadata-cleaner/{Path.GetFileName(outFile)}", CompressionLevel.Fastest, ct);

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
}
