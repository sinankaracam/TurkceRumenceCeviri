using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using TurkceRumenceCeviri.Services;
using System.IO;

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

    //Define a log file path for SDK and app errors
    private static readonly string LogFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TurkceRumenceCeviri_SpeechLog.txt");

    public bool IsListening => _isListening;

    public AzureSpeechRecognitionService(string speechKey, string speechRegion)
    {
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
            return string.Empty;
        }

        if (_isListening && _currentRecognitionTask != null)
        {
            return await _currentRecognitionTask.Task;
        }

        _currentRecognitionTask = new TaskCompletionSource<string>();

        try
        {
            _isListening = true;
            
            var config = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
            config.SetProperty(PropertyId.Speech_LogFilename, LogFilePath);
            
            var autoDetectConfig = AutoDetectSourceLanguageConfig.FromLanguages(new[] { "tr-TR", "ro-RO" });
            
            // Explicitly use the default microphone input configuration.
            // This ensures that we always use the active system device (whether Internal or Headset),
            // even if the default device changes between recognition sessions.
            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var recognizer = new SpeechRecognizer(config, autoDetectConfig, audioConfig);
            
            var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
                return string.Empty;

            try
            {
                var prop = result.Properties.GetProperty(PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult);
                if (!string.IsNullOrWhiteSpace(prop))
                {
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
            //Log the exception to the file for debugging
            File.AppendAllText(LogFilePath, $"{DateTime.Now}: Exception in RecognizeAsync: {ex.ToString()}\n");
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
        _isListening = false;
        if (_currentRecognitionTask != null)
        {
            _currentRecognitionTask.TrySetCanceled();
        }
        await Task.CompletedTask;
       }
   }

