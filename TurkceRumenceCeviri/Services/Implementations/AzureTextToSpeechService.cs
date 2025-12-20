using Microsoft.CognitiveServices.Speech;

namespace TurkceRumenceCeviri.Services.Implementations;

public class AzureTextToSpeechService : ITextToSpeechService
{
    private readonly SpeechSynthesizer _synthesizer;
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;

    public AzureTextToSpeechService(string speechKey, string speechRegion)
    {
        var config = SpeechConfig.FromSubscription(speechKey, speechRegion);
        _synthesizer = new SpeechSynthesizer(config);
        _isPlaying = false;
    }

    public async Task SpeakAsync(string text, string language)
    {
        try
        {
            _isPlaying = true;

            var voiceName = language?.ToLower() switch
            {
                "tr" => "tr-TR-AysanNeural",
                "ro" => "ro-RO-AlinaNeural",
                _ => "tr-TR-AysanNeural"
            };

            // SSML formatýnda seslendir
            var ssml = $@"<speak version='1.0' xml:lang='{GetLocale(language)}'>
                <voice name='{voiceName}'>
                    {System.Net.WebUtility.HtmlEncode(text)}
                </voice>
            </speak>";

            var result = await _synthesizer.SpeakSsmlAsync(ssml);
            
            if (result.Reason != ResultReason.SynthesizingAudioCompleted)
            {
                Console.WriteLine($"Seslendir hatasý: {result.Reason}");
            }
        }
        finally
        {
            _isPlaying = false;
        }
    }

    public async Task StopAsync()
    {
        _isPlaying = false;
        await Task.CompletedTask;
    }

    private string GetLocale(string language)
    {
        return language?.ToLower() switch
        {
            "tr" => "tr-TR",
            "ro" => "ro-RO",
            _ => "tr-TR"
        };
    }
}
