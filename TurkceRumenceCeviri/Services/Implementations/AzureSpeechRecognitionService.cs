using Microsoft.CognitiveServices.Speech;

namespace TurkceRumenceCeviri.Services.Implementations;

public class AzureSpeechRecognitionService : ISpeechRecognitionService
{
    private readonly SpeechRecognizer _recognizer;
    private bool _isListening;
    public string? LastDetectedLanguage { get; private set; }

    public bool IsListening => _isListening;

    public AzureSpeechRecognitionService(string speechKey, string speechRegion)
    {
        var config = SpeechConfig.FromSubscription(speechKey, speechRegion);
        // Auto-detect between Turkish and Romanian
        var autoDetectConfig = AutoDetectSourceLanguageConfig.FromLanguages(new[] { "tr-TR", "ro-RO" });
        _recognizer = new SpeechRecognizer(config, autoDetectConfig);
        _isListening = false;
    }

    public async Task<string> RecognizeAsync(CancellationToken cancellationToken)
    {
        try
        {
            _isListening = true;
            var result = await _recognizer.RecognizeOnceAsync();

            if (cancellationToken.IsCancellationRequested)
                return "";

            // Read auto-detected language from result properties if available
            try
            {
                var prop = result.Properties.GetProperty(PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult);
                if (!string.IsNullOrWhiteSpace(prop))
                {
                    // Map full locale to short code
                    LastDetectedLanguage = prop.StartsWith("tr", StringComparison.OrdinalIgnoreCase) ? "tr" :
                                           prop.StartsWith("ro", StringComparison.OrdinalIgnoreCase) ? "ro" : prop;
                }
            }
            catch { /* ignore */ }

            return result.Reason switch
            {
                ResultReason.RecognizedSpeech => result.Text,
                ResultReason.NoMatch => "",
                ResultReason.Canceled =>
                    (Microsoft.CognitiveServices.Speech.CancellationDetails.FromResult(result) is var cd && cd != null
                        ? $"[STT Canceled: {cd.Reason} {cd.ErrorCode} {cd.ErrorDetails}]"
                        : ""),
                _ => ""
            };
        }
        finally
        {
            _isListening = false;
        }
    }

    public async Task StopRecognitionAsync()
    {
        _isListening = false;
        await Task.CompletedTask;
    }
}
