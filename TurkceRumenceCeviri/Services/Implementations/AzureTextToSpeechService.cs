using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace TurkceRumenceCeviri.Services.Implementations;

public class AzureTextToSpeechService : ITextToSpeechService
{
    private SpeechSynthesizer? _synthesizer;
    private readonly SpeechConfig? _config;
    private bool _isPlaying;
    private readonly bool _enabled;

    public bool IsPlaying => _isPlaying;

    public AzureTextToSpeechService(string speechKey, string speechRegion)
    {
        // If no key provided, disable TTS to avoid exceptions during startup.
        if (string.IsNullOrWhiteSpace(speechKey))
        {
            TurkceRumenceCeviri.Utilities.DebugHelper.LogWarning("Azure Speech synthesis key not provided - TTS disabled.");
            _config = null;
            _synthesizer = null;
            _isPlaying = false;
            _enabled = false;
            return;
        }

        try
        {
            _config = SpeechConfig.FromSubscription(speechKey, speechRegion);
            var audio = AudioConfig.FromDefaultSpeakerOutput();
            _synthesizer = new SpeechSynthesizer(_config, audio);
            _isPlaying = false;
            _enabled = true;
        }
        catch (Exception ex)
        {
            TurkceRumenceCeviri.Utilities.DebugHelper.LogWarning($"Failed to initialize AzureTextToSpeechService: {ex.Message}");
            _config = null;
            _synthesizer = null;
            _isPlaying = false;
            _enabled = false;
        }
    }

    public async Task SpeakAsync(string text, string language)
    {
        if (!_enabled || _config is null)
        {
            // TTS disabled - nothing to do
            return;
        }

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

            // ensure synthesizer exists
            if (_synthesizer is null)
            {
                try
                {
                    var audio = AudioConfig.FromDefaultSpeakerOutput();
                    _synthesizer = new SpeechSynthesizer(_config, audio);
                }
                catch (Exception ex)
                {
                    TurkceRumenceCeviri.Utilities.DebugHelper.LogWarning($"Failed to create SpeechSynthesizer at speak time: {ex.Message}");
                    return;
                }
            }

            SpeechSynthesisResult result;
            if ((language ?? "").ToLower() == "ro")
            {
                var ssml = $"<speak version='1.0' xml:lang='{GetLocale(language)}'><voice name='{voiceName}'><prosody rate='-25%'>{System.Net.WebUtility.HtmlEncode(text)}</prosody></voice></speak>";
                result = await _synthesizer.SpeakSsmlAsync(ssml).ConfigureAwait(false);
            }
            else
            {
                var ssml = $"<speak version='1.0' xml:lang='{GetLocale(language)}'><voice name='{voiceName}'><prosody rate='-25%'>{System.Net.WebUtility.HtmlEncode(text)}</prosody></voice></speak>";
                result = await _synthesizer.SpeakSsmlAsync(ssml).ConfigureAwait(false);
            }

            if (result.Reason != ResultReason.SynthesizingAudioCompleted)
            {
                TurkceRumenceCeviri.Utilities.DebugHelper.LogWarning($"Seslendir hatasý: {result.Reason}");
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
        if (!_enabled || _synthesizer is null)
        {
            return;
        }

        try
        {
            await _synthesizer.StopSpeakingAsync().ConfigureAwait(false);
        }
        catch { }
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
