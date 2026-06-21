namespace Mediaration.Services.MetadataCleaner;

public interface IMetadataCleanerService
{
    Task<string> CleanAsync(IEnumerable<IFormFile> files, CancellationToken ct = default);
    void Cleanup(string sessionPath);
}
