namespace Mediaration.Services.SoundParser;

public interface ISoundParserService
{
    Task<string> ExtractAudioAsync(Stream videoStream, string fileName, string format, CancellationToken ct = default);
    void Cleanup(string sessionPath);
}
