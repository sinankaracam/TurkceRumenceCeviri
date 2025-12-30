using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Threading;

namespace TurkceRumenceCeviri.Services.Implementations;

public class AzureSpeechRecognitionService : ISpeechRecognitionService
{
    private readonly string? _speechKey;
    private readonly string? _speechRegion;
    private bool _isListening;
    private readonly bool _enabled;
    private SpeechRecognizer? _continuousRecognizer;
    private TaskCompletionSource<string>? _currentRecognitionTask;
    public string? LastDetectedLanguage { get; private set; }

    public bool IsListening => _isListening;

    public AzureSpeechRecognitionService(string speechKey, string speechRegion)
    {
        // If speechKey is missing, do not attempt to create Azure Speech SDK objects.
        if (string.IsNullOrWhiteSpace(speechKey))
        {
            TurkceRumenceCeviri.Utilities.DebugHelper.LogWarning("Azure Speech key not provided - speech recognition disabled.");
            _speechKey = null;
            _speechRegion = null;
            _enabled = false;
            _isListening = false;
            return;
        }

        try
        {
            // store keys and enable; create recognizer per-call to avoid invalid-state transitions
            _speechKey = speechKey;
            _speechRegion = speechRegion;
            _enabled = true;
            _isListening = false;
        }
        catch (Exception ex)
        {
            TurkceRumenceCeviri.Utilities.DebugHelper.LogWarning($"Failed to initialize AzureSpeechRecognitionService: {ex.Message}");
            _speechKey = null;
            _speechRegion = null;
            _enabled = false;
            _isListening = false;
        }
    }

    public async Task<string> RecognizeAsync(CancellationToken cancellationToken)
    {
        if (!_enabled || string.IsNullOrEmpty(_speechKey) || string.IsNullOrEmpty(_speechRegion))
        {
            // Speech disabled: return empty so UI can continue
            return string.Empty;
        }

        // If already listening, return current result
        if (_isListening && _currentRecognitionTask != null)
        {
            return await _currentRecognitionTask.Task;
        }

        _currentRecognitionTask = new TaskCompletionSource<string>();

        try
        {
            _isListening = true;
            // create a fresh recognizer for this one-shot call to avoid SDK invalid state transitions
            var config = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
            var autoDetectConfig = AutoDetectSourceLanguageConfig.FromLanguages(new[] { "tr-TR", "ro-RO" });
            using var recognizer = new SpeechRecognizer(config, autoDetectConfig);
            var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
                return string.Empty;

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

            var recognizedText = result.Reason switch
            {
                ResultReason.RecognizedSpeech => result.Text,
                ResultReason.NoMatch => string.Empty,
                ResultReason.Canceled =>
                    (Microsoft.CognitiveServices.Speech.CancellationDetails.FromResult(result) is var cd && cd != null
                        ? $"[STT Canceled: {cd.Reason} {cd.ErrorCode} {cd.ErrorDetails}]"
                        : string.Empty),
                _ => string.Empty
            };

            _currentRecognitionTask.SetResult(recognizedText);
            return recognizedText;
        }
        catch (Exception ex)
        {
            _currentRecognitionTask.SetException(ex);
            throw;
        }
        finally
        {
            _isListening = false;
            _currentRecognitionTask = null;
        }
    }

    public async Task StopRecognitionAsync()
    {
        // For one-shot recognition (RecognizeOnceAsync) there is no continuous stop to call.
        // Calling StopContinuousRecognitionAsync while not in continuous mode causes invalid state transitions.
        _isListening = false;
        if (_currentRecognitionTask != null)
        {
            _currentRecognitionTask.TrySetCanceled();
        }
        await Task.CompletedTask;
    }
}

