namespace Mediaration.Services.VideoFrameParser;

public interface IFrameParserService
{
    Task<string> ExtractFramesAsync(
        Stream videoStream,
        string fileName,
        double fps,
        string format,
        string baseName,
        bool stripMetadata,
        CancellationToken ct = default);

    void Cleanup(string sessionPath);
}
