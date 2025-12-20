using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TurkceRumenceCeviri.Services;
using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;
using TurkceRumenceCeviri.Utilities;

namespace TurkceRumenceCeviri.ViewModels;

public class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add { System.Windows.Input.CommandManager.RequerySuggested += value; }
        remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
    }

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter = null) => _execute(parameter);
}

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ITranslationService _translationService;
    private readonly ISpeechRecognitionService _speechService;
    private readonly ITextToSpeechService _ttsService;
    private readonly IOcrService _ocrService;
    private readonly IAIAssistantService _aiService;
    private readonly SpeechSessionManager _sessionManager;

    private string _romanianText = "";
    private string _turkishText = "";
    private string _translatedRomanian = "";
    private string _translatedTurkish = "";
    private string _assistantResponse = "";
    private string _detectedLanguage = "TR";
    private bool _isListening;
    private bool _isSpeaking;
    private CancellationTokenSource? _listeningCts;
    private CancellationTokenSource? _manualTranslateCts;

    public string RomanianText
    {
        get => _romanianText;
        set
        {
            if (SetProperty(ref _romanianText, value))
            {
                // Manual edit translation debounce (only when NOT listening)
                if (!IsListening && !string.IsNullOrWhiteSpace(_romanianText))
                {
                    DebounceManualTranslate("ro");
                }
            }
        }
    }

    public string TurkishText
    {
        get => _turkishText;
        set
        {
            if (SetProperty(ref _turkishText, value))
            {
                // Manual edit translation debounce (only when NOT listening)
                if (!IsListening && !string.IsNullOrWhiteSpace(_turkishText))
                {
                    DebounceManualTranslate("tr");
                }
            }
        }
    }

    public string TranslatedRomanian
    {
        get => _translatedRomanian;
        set { SetProperty(ref _translatedRomanian, value); }
    }

    public string TranslatedTurkish
    {
        get => _translatedTurkish;
        set { SetProperty(ref _translatedTurkish, value); }
    }

    public string AssistantResponse
    {
        get => _assistantResponse;
        set { SetProperty(ref _assistantResponse, value); }
    }

    public string DetectedLanguage
    {
        get => _detectedLanguage;
        set { SetProperty(ref _detectedLanguage, value); }
    }

    public bool IsListening
    {
        get => _isListening;
        set { SetProperty(ref _isListening, value); }
    }

    public bool IsSpeaking
    {
        get => _isSpeaking;
        set { SetProperty(ref _isSpeaking, value); }
    }

    // Commands
    public RelayCommand StartListeningCommand { get; }
    public RelayCommand StopListeningCommand { get; }
    public RelayCommand SpeakCommand { get; }
    public RelayCommand SpeakTurkishTranslationCommand { get; }
    public RelayCommand SpeakRomanianTranslationCommand { get; }
    public RelayCommand AskAssistantCommand { get; }
    public RelayCommand ClearCommand { get; }
    public RelayCommand ExtractFromScreenCommand { get; }

    public MainViewModel(
        ITranslationService translationService,
        ISpeechRecognitionService speechService,
        ITextToSpeechService ttsService,
        IOcrService ocrService,
        IAIAssistantService aiService)
    {
        _translationService = translationService;
        _speechService = speechService;
        _ttsService = ttsService;
        _ocrService = ocrService;
        _aiService = aiService;
        _sessionManager = new SpeechSessionManager();

        StartListeningCommand = new RelayCommand(_ => StartListening());
        StopListeningCommand = new RelayCommand(_ => StopListening());
        SpeakCommand = new RelayCommand(_ => Speak());
        SpeakTurkishTranslationCommand = new RelayCommand(_ => SpeakTurkishTranslation());
        SpeakRomanianTranslationCommand = new RelayCommand(_ => SpeakRomanianTranslation());
        AskAssistantCommand = new RelayCommand(_ => AskAssistant());
        ClearCommand = new RelayCommand(_ => Clear());
        ExtractFromScreenCommand = new RelayCommand(_ => ExtractFromScreen());
    }

    private async void StartListening()
    {
        DebugHelper.LogMessage("StartListening invoked");
        IsListening = true;
        _listeningCts = new CancellationTokenSource();
        _sessionManager.IsSessionActive = true;

        try
        {
            while (IsListening && !_listeningCts.Token.IsCancellationRequested)
            {
                var recognizedText = await _speechService.RecognizeAsync(_listeningCts.Token);
                DebugHelper.LogMessage($"STT result: '{recognizedText}'", "STT");

                if (!string.IsNullOrEmpty(recognizedText) && !recognizedText.StartsWith("[") && recognizedText != "Ýleri" && recognizedText != "Geri")
                {
                    var detectedLang = _speechService.LastDetectedLanguage ?? await _translationService.DetectLanguageAsync(recognizedText);
                    DebugHelper.LogMessage($"Detected language: {detectedLang}", "STT");

                    _sessionManager.UpdateLanguage(detectedLang);
                    DetectedLanguage = detectedLang.ToUpper();

                    if (detectedLang == "tr")
                    {
                        TurkishText += (string.IsNullOrEmpty(TurkishText) ? "" : " ") + recognizedText;
                        await PerformTranslation("tr");
                    }
                    else if (detectedLang == "ro")
                    {
                        RomanianText += (string.IsNullOrEmpty(RomanianText) ? "" : " ") + recognizedText;
                        await PerformTranslation("ro");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            DebugHelper.LogWarning("Listening canceled");
        }
        finally
        {
            IsListening = false;
            DebugHelper.LogMessage("StartListening finished");
        }
    }

    private async void StopListening()
    {
        DebugHelper.LogMessage("StopListening invoked");
        IsListening = false;
        await _speechService.StopRecognitionAsync();

        var cts = _listeningCts;
        _listeningCts = null;
        try { cts?.Cancel(); } catch (ObjectDisposedException) { }
        cts?.Dispose();

        if (!string.IsNullOrEmpty(TurkishText))
            await PerformTranslation("tr");
        else if (!string.IsNullOrEmpty(RomanianText))
            await PerformTranslation("ro");

        if (IsSpeaking)
        {
            await _ttsService.StopAsync();
            IsSpeaking = false;
        }
        DebugHelper.LogMessage("StopListening finished");
    }

    private async void Speak()
    {
        DebugHelper.LogMessage("Speak invoked");
        IsSpeaking = true;

        try
        {
            var textToSpeak = DetectedLanguage == "TR" ? TranslatedRomanian : TranslatedTurkish;
            var language = DetectedLanguage == "TR" ? "ro" : "tr";

            if (!string.IsNullOrWhiteSpace(textToSpeak))
            {
                await _ttsService.SpeakAsync(textToSpeak, language);
            }
        }
        finally
        {
            IsSpeaking = false;
            DebugHelper.LogMessage("Speak finished");
        }
    }

    private async void SpeakTurkishTranslation()
    {
        DebugHelper.LogMessage("SpeakTurkishTranslation invoked");
        if (string.IsNullOrWhiteSpace(TranslatedTurkish)) return;
        IsSpeaking = true;
        try
        {
            await _ttsService.SpeakAsync(TranslatedTurkish, "tr");
        }
        finally
        {
            IsSpeaking = false;
            DebugHelper.LogMessage("SpeakTurkishTranslation finished");
        }
    }

    private async void SpeakRomanianTranslation()
    {
        DebugHelper.LogMessage("SpeakRomanianTranslation invoked");
        if (string.IsNullOrWhiteSpace(TranslatedRomanian)) return;
        IsSpeaking = true;
        try
        {
            await _ttsService.SpeakAsync(TranslatedRomanian, "ro");
        }
        finally
        {
            IsSpeaking = false;
            DebugHelper.LogMessage("SpeakRomanianTranslation finished");
        }
    }

    private async void AskAssistant()
    {
        DebugHelper.LogMessage("AskAssistant invoked");
        var context = DetectedLanguage == "TR" ? TurkishText : RomanianText;
        var language = DetectedLanguage == "TR" ? "tr" : "ro";

        try
        {
            AssistantResponse = await _aiService.AnswerQuestionAsync("", context, language);
        }
        catch (Exception ex)
        {
            AssistantResponse = $"Hata: {ex.Message}";
        }
    }

    private async void ExtractFromScreen()
    {
        DebugHelper.LogMessage("ExtractFromScreen invoked");
        try
        {
            var screenshotPath = Path.Combine(Path.GetTempPath(), "ocr_temp.png");
            // Capture selected region to file via Snipping Tool
            var capturedPath = await TurkceRumenceCeviri.Services.Implementations.ScreenCaptureService.CaptureSelectedRegionAsync(screenshotPath);
            if (string.IsNullOrEmpty(capturedPath) || !File.Exists(capturedPath))
            {
                AssistantResponse = "Ekran görüntüsü yakalanamadý. Lütfen yeniden deneyin.";
                return;
            }

            // OCR once, detect overall language and append to the corresponding segment
            var (fullText, overallLang) = await _ocrService.ExtractTextAsync(screenshotPath);
            if (string.IsNullOrWhiteSpace(fullText))
            {
                AssistantResponse = "Seçili alandan metin okunamadý.";
                return;
            }

            // Decide language: prefer strong heuristics; then OCR detected; then heuristic detector
            string lang;
            // Prefer Romanian detection first (Romanian diacritics/words are strong signals)
            if (TurkceRumenceCeviri.Services.Implementations.RomanianNormalizer.IsLikelyRomanian(fullText))
                lang = "ro";
            else if (TurkceRumenceCeviri.Services.Implementations.RomanianNormalizer.IsLikelyTurkish(fullText))
                lang = "tr";
            else if (!string.IsNullOrWhiteSpace(overallLang))
                lang = overallLang.ToLowerInvariant();
            else
                lang = TurkceRumenceCeviri.Services.Implementations.RomanianNormalizer.DetectLanguageForText(fullText);

            if (lang == "tr")
            {
                _sessionManager.UpdateLanguage("tr");
                DetectedLanguage = "TR";
                TurkishText = string.IsNullOrEmpty(TurkishText) ? fullText : $"{TurkishText}{Environment.NewLine}{fullText}";
                await PerformTranslation("tr");
            }
            else
            {
                var normalized = TurkceRumenceCeviri.Services.Implementations.RomanianNormalizer.NormalizeRomanian(fullText);
                _sessionManager.UpdateLanguage("ro");
                DetectedLanguage = "RO";
                RomanianText = string.IsNullOrEmpty(RomanianText) ? normalized : $"{RomanianText}{Environment.NewLine}{normalized}";
                await PerformTranslation("ro");
            }

            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ekran çevirme hatasý: {ex.Message}");
        }
    }

    private async Task PerformTranslation(string sourceLanguage)
    {
        try
        {
            var sourceText = sourceLanguage == "tr" ? TurkishText : RomanianText;
            var targetLanguage = sourceLanguage == "tr" ? "ro" : "tr";

            if (string.IsNullOrWhiteSpace(sourceText))
            {
                // Clear target when source is empty
                if (sourceLanguage == "tr")
                {
                    TranslatedRomanian = "";
                }
                else
                {
                    TranslatedTurkish = "";
                }
                return;
            }

            var translation = await _translationService.TranslateAsync(sourceText, sourceLanguage, targetLanguage);

            if (sourceLanguage == "tr")
            {
                TranslatedRomanian = translation;
            }
            else
            {
                TranslatedTurkish = translation;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Çeviri hatasý: {ex.Message}");
        }
    }

    private void Clear()
    {
        RomanianText = "";
        TurkishText = "";
        TranslatedRomanian = "";
        TranslatedTurkish = "";
        AssistantResponse = "";
        _manualTranslateCts?.Cancel();
        _manualTranslateCts?.Dispose();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!Equals(storage, value))
        {
            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
        return false;
    }

    private void DebounceManualTranslate(string sourceLanguage)
    {
        try
        {
            _manualTranslateCts?.Cancel();
            _manualTranslateCts?.Dispose();
            _manualTranslateCts = new CancellationTokenSource();
            var token = _manualTranslateCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(400, token);
                    if (!token.IsCancellationRequested)
                    {
                        await PerformTranslation(sourceLanguage);
                    }
                }
                catch (OperationCanceledException) { }
            }, token);
        }
        catch { }
    }
}
