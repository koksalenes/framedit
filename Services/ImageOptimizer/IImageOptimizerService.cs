namespace Mediaration.Services.ImageOptimizer;

public interface IImageOptimizerService
{
    Task<string> OptimizeAsync(
        IEnumerable<IFormFile> files,
        string outputFormat,
        int quality,
        bool stripMetadata,
        CancellationToken ct = default);

    void Cleanup(string sessionPath);
}
