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
    private string _assistantQuestion = "";
    // Assistant counterpart properties
    private string _translatedTurkishAssistant = "";
    private string _translatedRomanianAssistant = "";
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

    public string AssistantQuestion
    {
        get => _assistantQuestion;
        set { SetProperty(ref _assistantQuestion, value); }
    }

    public string TranslatedTurkishAssistant { get => _translatedTurkishAssistant; set => SetProperty(ref _translatedTurkishAssistant, value); }
    public string TranslatedRomanianAssistant { get => _translatedRomanianAssistant; set => SetProperty(ref _translatedRomanianAssistant, value); }

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
    public RelayCommand SummarizeCommand { get; }
    public RelayCommand ExtractQuestionsCommand { get; }
    public RelayCommand AnswerQuestionsCommand { get; }
    public RelayCommand WordListCommand { get; }
    public RelayCommand PronounceCommand { get; }

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
        SummarizeCommand = new RelayCommand(_ => { AssistantQuestion = "Metni özetle"; AskAssistant(); });
        ExtractQuestionsCommand = new RelayCommand(_ => { AssistantQuestion = "Soruları çıkar"; AskAssistant(); });
        AnswerQuestionsCommand = new RelayCommand(_ => { AssistantQuestion = "Sadece Soruları cevapla"; AskAssistant(); });
        WordListCommand = new RelayCommand(_ => { AssistantQuestion = "Kelime listesi"; AskAssistant(); });
        PronounceCommand = new RelayCommand(_ => { AssistantQuestion = "Telaffuz öner"; AskAssistant(); });
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

                if (!string.IsNullOrEmpty(recognizedText) && !recognizedText.StartsWith("[") && recognizedText != "İleri" && recognizedText != "Geri")
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
        // Stop mic if currently listening to avoid STT capturing TTS audio
        if (IsListening)
        {
            try { _listeningCts?.Cancel(); } catch { }
            _listeningCts?.Dispose();
            _listeningCts = null;
            try { await _speechService.StopRecognitionAsync(); } catch { }
            IsListening = false;
        }
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
        // Stop mic if currently listening to avoid STT capturing TTS audio
        if (IsListening)
        {
            try { _listeningCts?.Cancel(); } catch { }
            _listeningCts?.Dispose();
            _listeningCts = null;
            try { await _speechService.StopRecognitionAsync(); } catch { }
            IsListening = false;
        }
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
        // Stop mic if currently listening to avoid STT capturing TTS audio
        if (IsListening)
        {
            try { _listeningCts?.Cancel(); } catch { }
            _listeningCts?.Dispose();
            _listeningCts = null;
            try { await _speechService.StopRecognitionAsync(); } catch { }
            IsListening = false;
        }
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
            var original = await _aiService.AnswerQuestionAsync(AssistantQuestion ?? string.Empty, context, language);
            AssistantResponse = original;
            // translate to both languages for display
            var trText = language == "tr" ? original : await _translationService.TranslateAsync(original, language, "tr");
            var roText = language == "ro" ? original : await _translationService.TranslateAsync(original, language, "ro");
            TranslatedTurkishAssistant = trText;
            TranslatedRomanianAssistant = roText;
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
                AssistantResponse = "Ekran görüntüsü yakalanamadı. Lütfen yeniden deneyin.";
                return;
            }

            // OCR once, then trim and route lines strictly by selected region content
            var (fullText, overallLang) = await _ocrService.ExtractTextAsync(screenshotPath);
            if (string.IsNullOrWhiteSpace(fullText))
            {
                AssistantResponse = "Seçili alandan metin okunamadı.";
                return;
            }
            // Decide one side for the whole selection to preserve meaning
            // Exclude 'ş/Ş' from Turkish to avoid overlap with Romanian 'ș/Ș'
            var trChars = new HashSet<char>(new[] { 'ğ','Ğ','ı','İ','ç','Ç','ö','Ö','ü','Ü' });
            var roChars = new HashSet<char>(new[] { 'ă','Ă','â','Â','î','Î','ș','Ș','ț','Ț' });

            // Normalize OCR-confusable characters for detection (not for display)
            string detectText = fullText
                .Replace('ş','ș')
                .Replace('Ş','Ș')
                .Replace('ţ','ț')
                .Replace('Ţ','Ț');

            int trCount = 0, roCount = 0;
            foreach (var ch in detectText)
            {
                if (trChars.Contains(ch)) trCount++;
                if (roChars.Contains(ch)) roCount++;
            }

            string decidedLang;
            if (roCount > 0 && trCount == 0) decidedLang = "ro";
            else if (trCount > 0 && roCount == 0) decidedLang = "tr";
            else if (roCount > trCount) decidedLang = "ro"; // mixed: prefer side with more diacritics
            else if (trCount > roCount) decidedLang = "tr";
            else
            {
                // counts equal or zero: prefer word heuristics, then OCR overallLang, then detector
                if (TurkceRumenceCeviri.Services.Implementations.RomanianNormalizer.IsLikelyRomanian(fullText))
                    decidedLang = "ro";
                else if (TurkceRumenceCeviri.Services.Implementations.RomanianNormalizer.IsLikelyTurkish(fullText))
                    decidedLang = "tr";
                else if (!string.IsNullOrWhiteSpace(overallLang))
                    decidedLang = overallLang.ToLowerInvariant();
                else
                    decidedLang = TurkceRumenceCeviri.Services.Implementations.RomanianNormalizer.DetectLanguageForText(fullText);
            }

            // Clean lines: trim, drop empty/decoration-only
            var parts = fullText.Replace("\r\n", "\n")
                                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var sb = new System.Text.StringBuilder();
            foreach (var line in parts)
            {
                var t = line.Trim();
                if (string.IsNullOrEmpty(t)) continue;
                bool hasWordChar = false;
                foreach (var ch in t)
                {
                    if (char.IsLetterOrDigit(ch)) { hasWordChar = true; break; }
                }
                if (!hasWordChar) continue;
                sb.AppendLine(t);
            }
            var finalBlock = sb.ToString().Trim();
            if (decidedLang == "ro") finalBlock = TurkceRumenceCeviri.Services.Implementations.RomanianNormalizer.NormalizeRomanian(finalBlock);

            if (decidedLang == "tr")
            {
                _sessionManager.UpdateLanguage("tr");
                DetectedLanguage = "TR";
                TurkishText = string.IsNullOrEmpty(TurkishText) ? finalBlock : $"{TurkishText}{Environment.NewLine}{finalBlock}";
                await PerformTranslation("tr");
            }
            else
            {
                _sessionManager.UpdateLanguage("ro");
                DetectedLanguage = "RO";
                RomanianText = string.IsNullOrEmpty(RomanianText) ? finalBlock : $"{RomanianText}{Environment.NewLine}{finalBlock}";
                await PerformTranslation("ro");
            }

            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ekran çevirme hatası: {ex.Message}");
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
            Console.WriteLine($"Çeviri hatası: {ex.Message}");
        }
    }

    private void Clear()
    {
        RomanianText = "";
        TurkishText = "";
        TranslatedRomanian = "";
        TranslatedTurkish = "";
        AssistantResponse = "";
        TranslatedTurkishAssistant = "";
        TranslatedRomanianAssistant = "";
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
