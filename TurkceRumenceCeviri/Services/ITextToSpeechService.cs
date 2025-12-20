namespace TurkceRumenceCeviri.Services;

public interface ITextToSpeechService
{
    Task SpeakAsync(string text, string language);
    Task StopAsync();
    bool IsPlaying { get; }
}
