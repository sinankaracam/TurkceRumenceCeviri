using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace TurkceRumenceCeviri.Services.Implementations;

public class AzureTextToSpeechService : ITextToSpeechService
{
    private SpeechSynthesizer _synthesizer;
    private readonly SpeechConfig _config;
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;

    public AzureTextToSpeechService(string speechKey, string speechRegion)
    {
        _config = SpeechConfig.FromSubscription(speechKey, speechRegion);
        var audio = AudioConfig.FromDefaultSpeakerOutput();
        _synthesizer = new SpeechSynthesizer(_config, audio);
        _isPlaying = false;
    }

    public async Task SpeakAsync(string text, string language)
    {
        try
        {
            _isPlaying = true;

            var voiceName = language?.ToLower() switch
            {
                "tr" => "tr-TR-AysuNeural",
                "ro" => "ro-RO-AlinaNeural",
                _ => "tr-TR-AysuNeural"
            };
            _config.SpeechSynthesisLanguage = GetLocale(language);
            _config.SpeechSynthesisVoiceName = voiceName;
            // update voice and speak
            if (_synthesizer is null)
            {
                var audio = AudioConfig.FromDefaultSpeakerOutput();
                _synthesizer = new SpeechSynthesizer(_config, audio);
            }
            SpeechSynthesisResult result;
            if ((language ?? "").ToLower() == "ro")
            {
                var ssml = $"<speak version='1.0' xml:lang='{GetLocale(language)}'><voice name='{voiceName}'>{System.Net.WebUtility.HtmlEncode(text)}</voice></speak>";
                result = await _synthesizer.SpeakSsmlAsync(ssml);
            }
            else
            {
                result = await _synthesizer.SpeakTextAsync(text);
            }


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
        if (_synthesizer is not null)
        {
            await _synthesizer.StopSpeakingAsync();
        }
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
