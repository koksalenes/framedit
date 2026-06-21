using System.Diagnostics;
using System.IO.Compression;
using Mediaration.Constants;

namespace Mediaration.Services.ImageOptimizer;

public class ImageOptimizerService : IImageOptimizerService
{
    private readonly ILogger<ImageOptimizerService> _logger;

    public ImageOptimizerService(ILogger<ImageOptimizerService> logger)
    {
        _logger = logger;
    }

    public async Task<string> OptimizeAsync(
        IEnumerable<IFormFile> files,
        string outputFormat,
        int quality,
        bool stripMetadata,
        CancellationToken ct = default)
    {
        var sessionDir = Path.Combine(Path.GetTempPath(), "mediaration", "optimizer", Guid.NewGuid().ToString());
        var tmpDir     = Path.Combine(sessionDir, "tmp");
        var outDir     = Path.Combine(sessionDir, "out");
        Directory.CreateDirectory(tmpDir);
        Directory.CreateDirectory(outDir);

        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();

            var originalName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(originalName)) continue;

            var inputExt  = Path.GetExtension(originalName).ToLowerInvariant();
            var stem      = Path.GetFileNameWithoutExtension(originalName);
            var outputExt = ResolveOutputExt(inputExt, outputFormat);

            var outName = stem + outputExt;
            if (!usedNames.Add(outName))
            {
                var i = 2;
                do { outName = $"{stem}-{i++}{outputExt}"; } while (!usedNames.Add(outName));
            }

            var tmpPath = Path.Combine(tmpDir, Guid.NewGuid() + inputExt);
            var outPath = Path.Combine(outDir, outName);

            await using (var fs = File.Create(tmpPath))
                await file.OpenReadStream().CopyToAsync(fs, ct);

            var codecArgs = BuildCodecArgs(outputExt, quality);
            var metaArgs  = stripMetadata ? "-map_metadata -1" : "";
            var args      = $"-y -i \"{tmpPath}\" {codecArgs} {metaArgs} \"{outPath}\"".Trim();

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
                _logger.LogError("FFmpeg failed for {File} (exit {Code}): {Stderr}", outName, process.ExitCode, stderr);
                throw new InvalidOperationException($"Failed to optimize '{originalName}'. Ensure it is a supported image format.");
            }

            File.Delete(tmpPath);
        }

        var zipPath = Path.Combine(sessionDir, "optimized.zip");
        await using var zipStream = File.Create(zipPath);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: false);

        foreach (var outFile in Directory.GetFiles(outDir).OrderBy(f => f))
            await archive.CreateEntryFromFileAsync(outFile, $"{AppConstants.ImageOptimizer.ZipFolder}/{Path.GetFileName(outFile)}", CompressionLevel.Fastest, ct);

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

    private static string ResolveOutputExt(string inputExt, string outputFormat)
    {
        if (outputFormat.Equals(AppConstants.Ext.Jpg.Name,  StringComparison.OrdinalIgnoreCase)) return AppConstants.Ext.Jpg.Extension;
        if (outputFormat.Equals(AppConstants.Ext.Png.Name,  StringComparison.OrdinalIgnoreCase)) return AppConstants.Ext.Png.Extension;
        if (outputFormat.Equals(AppConstants.Ext.Webp.Name, StringComparison.OrdinalIgnoreCase)) return AppConstants.Ext.Webp.Extension;
        return AppConstants.ImageOptimizer.ExtToOutputExt.TryGetValue(inputExt, out var mapped) ? mapped : inputExt;
    }

    private static string BuildCodecArgs(string outputExt, int quality)
    {
        var jpegQ = Math.Max(1, (int)Math.Round(1 + (100 - quality) * 30.0 / 99));

        if (outputExt.Equals(AppConstants.Ext.Jpg.Extension,  StringComparison.OrdinalIgnoreCase)) return $"-q:v {jpegQ}";
        if (outputExt.Equals(AppConstants.Ext.Png.Extension,  StringComparison.OrdinalIgnoreCase)) return "-compression_level 6";
        if (outputExt.Equals(AppConstants.Ext.Webp.Extension, StringComparison.OrdinalIgnoreCase)) return $"-c:v libwebp -q:v {quality}";
        return "";
    }
}
