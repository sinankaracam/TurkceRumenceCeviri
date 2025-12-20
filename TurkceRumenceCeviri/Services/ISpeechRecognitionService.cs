namespace TurkceRumenceCeviri.Services;

public interface ISpeechRecognitionService
{
    Task<string> RecognizeAsync(CancellationToken cancellationToken);
    Task StopRecognitionAsync();
    bool IsListening { get; }
    string? LastDetectedLanguage { get; }
}
