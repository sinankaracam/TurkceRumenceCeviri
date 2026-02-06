using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TurkceRumenceCeviri.Services;
using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;
using TurkceRumenceCeviri.Utilities;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Linq;
using System.Net.NetworkInformation; // Already present, but ensure for device checks

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
    private ITranslationService _translationService;
    private ISpeechRecognitionService _speechService;
    private ITextToSpeechService _ttsService;
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

    private readonly System.Text.StringBuilder _sttBuffer = new();
    private DateTime _lastSttCommit = DateTime.MinValue;

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
    public RelayCommand DeployAzureResourcesCommand { get; }
    public RelayCommand GetGroqKeyCommand { get; }
    public RelayCommand ShowRomanianPhoneticCommand { get; }
    public RelayCommand ShowTranslationPhoneticCommand { get; }
    public RelayCommand ShowAssistantPhoneticCommand { get; }
    public RelayCommand OpenRomanianListeningCommand { get; }

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
        // Activation / license
        ActivateCommand = new RelayCommand(async _ => await ActivateAsync(), _ => !IsLicensed);
        SaveSettingsCommand = new RelayCommand(async _ => await SaveSettingsAsync()); // YENİ: Her zaman aktif

        // generate device code
        try
        {
            DeviceCode = TurkceRumenceCeviri.Utilities.DeviceCodeGenerator.CreateDeviceCode(TurkceRumenceCeviri.Utilities.HardwareIdProvider.GetHardwareId());
        }
        catch { DeviceCode = string.Empty; }

        // attempt to load persisted license
        try
        {
            var persisted = TurkceRumenceCeviri.Utilities.LicenseStorage.Load();
            if (persisted != null && !string.IsNullOrWhiteSpace(persisted.LicenseKey))
            {
                var act = new TurkceRumenceCeviri.Services.ActivationService();
                var deviceMatches = string.Equals(persisted.DeviceCode, DeviceCode, StringComparison.Ordinal);
                if (deviceMatches && !string.IsNullOrEmpty(DeviceCode) && act.ValidateActivationKey(DeviceCode, persisted.LicenseKey))
                {
                    IsLicensed = true;
                    TranslatorKeyInput = persisted.TranslatorKey ?? string.Empty;
                    SpeechKeyInput = persisted.SpeechKey ?? string.Empty;
                    GroqKeyInput = persisted.GroqKey ?? string.Empty;
                    LicenseKeyInput = persisted.LicenseKey ?? string.Empty;
                    TranslatorRegionInput = persisted.TranslatorRegion ?? "eastus";
                    SpeechRegionInput = persisted.SpeechRegion ?? "eastus";
                    LicenseStatusMessage = "Lisans yüklendi";

                    try
                    {
                        if (!string.IsNullOrEmpty(TranslatorKeyInput)) Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_KEY", TranslatorKeyInput);
                        if (!string.IsNullOrEmpty(SpeechKeyInput)) Environment.SetEnvironmentVariable("AZURE_SPEECH_KEY", SpeechKeyInput);
                        if (!string.IsNullOrEmpty(GroqKeyInput)) Environment.SetEnvironmentVariable("GROQ_API_KEY", GroqKeyInput);
                        if (!string.IsNullOrEmpty(TranslatorRegionInput)) Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_REGION", TranslatorRegionInput);
                        if (!string.IsNullOrEmpty(SpeechRegionInput)) Environment.SetEnvironmentVariable("AZURE_SPEECH_REGION", SpeechRegionInput);
                    }
                    catch { }
                }
            }
        }
        catch { }

        TestTranslatorKeyCommand = new RelayCommand(async _ => await TestTranslatorKeyAsync());
        TestSpeechKeyCommand = new RelayCommand(async _ => await TestSpeechKeyAsync());
        
        DeployAzureResourcesCommand = new RelayCommand(_ => 
        {
            try
            {
                // BURADA DEĞİŞİKLİK YAPIYORUZ:
                // Ham (Raw) JSON linki:
                string rawTemplateUrl = "https://raw.githubusercontent.com/sinankaracam/TurkceRumenceCeviri/master/TurkceRumenceCeviri/Assets/azuredeploy.json";
                
                // URL'i "URL Encode" işlemi ile güvenli hale getiriyoruz (C# kütüphanesi ile)
                string encodedUrl = Uri.EscapeDataString(rawTemplateUrl);
                
                // Azure Portal Şablon Dağıtım Linkini oluşturuyoruz:
                string deployToAzureLink = $"https://portal.azure.com/#create/Microsoft.Template/uri/{encodedUrl}";
                
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = deployToAzureLink,
                    UseShellExecute = true
                });
            }
            catch { }
        });

        GetGroqKeyCommand = new RelayCommand(_ => 
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://console.groq.com/keys",
                    UseShellExecute = true
                });
            }
            catch { }
        });

        ShowRomanianPhoneticCommand = new RelayCommand(_ => ShowPhoneticWindow(RomanianText));
        ShowTranslationPhoneticCommand = new RelayCommand(_ => ShowPhoneticWindow(TranslatedRomanian));
        ShowAssistantPhoneticCommand = new RelayCommand(_ => ShowPhoneticWindow(AssistantResponse));
        OpenRomanianListeningCommand = new RelayCommand(_ => OpenRomanianListeningWindow(), _ => IsLicensed);
    }

    // Activation / license fields and properties
    private string _deviceCode = "";
    private string _translatorKeyInput = "";
    private string _groqKeyInput = "";
    private string _speechKeyInput = "";
    private string _licenseKeyInput = "";
    private string _licenseStatusMessage = "";
    private bool _isLicensed = false;
    private string _translatorRegionInput = "westeurope";
    private string _speechRegionInput = "westeurope";
    private bool _isLayoutLocked = false;

    public string DeviceCode { get => _deviceCode; set => SetProperty(ref _deviceCode, value); }
    public string TranslatorKeyInput { get => _translatorKeyInput; set => SetProperty(ref _translatorKeyInput, value); }
    public string GroqKeyInput { get => _groqKeyInput; set => SetProperty(ref _groqKeyInput, value); }
    public string SpeechKeyInput { get => _speechKeyInput; set => SetProperty(ref _speechKeyInput, value); }
    public string LicenseKeyInput { get => _licenseKeyInput; set => SetProperty(ref _licenseKeyInput, value); }
    public string LicenseStatusMessage { get => _licenseStatusMessage; set => SetProperty(ref _licenseStatusMessage, value); }
    public bool IsLicensed { get => _isLicensed; set { if (SetProperty(ref _isLicensed, value)) System.Windows.Input.CommandManager.InvalidateRequerySuggested(); } }
    public string TranslatorRegionInput { get => _translatorRegionInput; set => SetProperty(ref _translatorRegionInput, value); }
    public string SpeechRegionInput { get => _speechRegionInput; set => SetProperty(ref _speechRegionInput, value); }
    
    public bool IsLayoutLocked 
    { 
        get => _isLayoutLocked; 
        set 
        {
            if (SetProperty(ref _isLayoutLocked, value))
            {
                OnPropertyChanged(nameof(IsLayoutUnlocked));
            }
        }
    }
    
    public bool IsLayoutUnlocked => !IsLayoutLocked;

    public RelayCommand ActivateCommand { get; }
    public RelayCommand SaveSettingsCommand { get; } // YENİ
    public RelayCommand TestTranslatorKeyCommand { get; }
    public RelayCommand TestSpeechKeyCommand { get; }


    private async Task ActivateAsync()
    {
        try
        {
            LicenseStatusMessage = "Doğrulanıyor...";
            await Task.Delay(10); // allow UI update
            var svc = new TurkceRumenceCeviri.Services.ActivationService();
            if (string.IsNullOrWhiteSpace(DeviceCode))
            {
                LicenseStatusMessage = "Cihaz kodu alınamadı.";
                return;
            }

            if (svc.ValidateActivationKey(DeviceCode, LicenseKeyInput))
            {
                svc.PersistActivation(DeviceCode, LicenseKeyInput, TranslatorKeyInput, SpeechKeyInput, GroqKeyInput, TranslatorRegionInput, SpeechRegionInput);
                IsLicensed = true;
                LicenseStatusMessage = "Lisans aktifleştirildi.";
                // Apply to current process environment so restart/other code can pick them up
                try
                {
                    if (!string.IsNullOrEmpty(TranslatorKeyInput)) Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_KEY", TranslatorKeyInput);
                    if (!string.IsNullOrEmpty(SpeechKeyInput)) Environment.SetEnvironmentVariable("AZURE_SPEECH_KEY", SpeechKeyInput);
                    if (!string.IsNullOrEmpty(GroqKeyInput)) Environment.SetEnvironmentVariable("GROQ_API_KEY", GroqKeyInput);
                    if (!string.IsNullOrEmpty(TranslatorRegionInput)) Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_REGION", TranslatorRegionInput);
                    if (!string.IsNullOrEmpty(SpeechRegionInput)) Environment.SetEnvironmentVariable("AZURE_SPEECH_REGION", SpeechRegionInput);
                }
                catch { }
                // Reinitialize runtime services so keys take effect without restart
                try
                {
                    ReinitializeServices();
                    LicenseStatusMessage = "Lisans aktifleştirildi ve servisler yeniden başlatıldı.";
                }
                catch { LicenseStatusMessage = "Lisans kaydedildi, servis yeniden başlatılamadı."; }
            }
            else
            {
                IsLicensed = false;
                LicenseStatusMessage = "Geçersiz lisans anahtarı.";
            }
        }
        catch (Exception ex)
        {
            LicenseStatusMessage = $"Aktivasyon hatası: {ex.Message}";
        }
    }

    private async Task SaveSettingsAsync()
    {
        LicenseStatusMessage = "Kaydediliyor ve kontrol ediliyor...";
        await Task.Delay(50); // UI thread update

        var svc = new TurkceRumenceCeviri.Services.ActivationService();
        
        // Lisans anahtarı ve Cihaz kodu kontrolü
        bool isLicenseValid = !string.IsNullOrWhiteSpace(DeviceCode) && svc.ValidateActivationKey(DeviceCode, LicenseKeyInput);

        if (isLicenseValid)
        {
            // 1. Ayarları diske kaydet
            svc.PersistActivation(DeviceCode, LicenseKeyInput, TranslatorKeyInput, SpeechKeyInput, GroqKeyInput, TranslatorRegionInput, SpeechRegionInput);
            IsLicensed = true;

            // 2. Runtime ortam değişkenlerini güncelle (Uygulamayı kapatıp açmaya gerek kalmadan)
            try
            {
                if (!string.IsNullOrEmpty(TranslatorKeyInput)) Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_KEY", TranslatorKeyInput);
                if (!string.IsNullOrEmpty(SpeechKeyInput)) Environment.SetEnvironmentVariable("AZURE_SPEECH_KEY", SpeechKeyInput);
                if (!string.IsNullOrEmpty(GroqKeyInput)) Environment.SetEnvironmentVariable("GROQ_API_KEY", GroqKeyInput);
                if (!string.IsNullOrEmpty(TranslatorRegionInput)) Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_REGION", TranslatorRegionInput);
                if (!string.IsNullOrEmpty(SpeechRegionInput)) Environment.SetEnvironmentVariable("AZURE_SPEECH_REGION", SpeechRegionInput);
            }
            catch { }

            // 3. Servisleri yeni anahtarlarla yeniden başlat
            ReinitializeServices();

            var sb = new StringBuilder("Kaydedildi. ");

            // 4. Çeviri Servisi Kontrolü
            try
            {
                if (!string.IsNullOrWhiteSpace(TranslatorKeyInput))
                {
                    var endpoint = $"https://{TranslatorRegionInput}.api.cognitive.microsofttranslator.com";
                    var uri = $"{endpoint}/translate?api-version=3.0&to=tr";
                    using var client = new HttpClient();
                    var req = new HttpRequestMessage(HttpMethod.Post, uri);
                    req.Headers.Add("Ocp-Apim-Subscription-Key", TranslatorKeyInput);
                    req.Headers.Add("Ocp-Apim-Subscription-Region", TranslatorRegionInput);
                    req.Content = new StringContent(JsonConvert.SerializeObject(new object[] { new { Text = "test" } }), Encoding.UTF8, "application/json");
                    var resp = await client.SendAsync(req);
                    if (resp.IsSuccessStatusCode) sb.Append("Çeviri: ✔ ");
                    else sb.Append($"Çeviri: ❌ ({resp.StatusCode}) ");
                }
                else sb.Append("Çeviri: - ");
            }
            catch { sb.Append("Çeviri: ❌ "); }

            // 5. Ses Servisi Kontrolü (Sessiz)
            try
            {
                if (!string.IsNullOrWhiteSpace(SpeechKeyInput))
                {
                    var config = SpeechConfig.FromSubscription(SpeechKeyInput, SpeechRegionInput);
                    // Ses çalmaması için dummy output stream kullanıyoruz
                    using var audioOut = AudioConfig.FromStreamOutput(AudioOutputStream.CreatePullStream());
                    using var synth = new SpeechSynthesizer(config, audioOut);
                    var res = await synth.SpeakTextAsync("test");
                    if (res.Reason == ResultReason.SynthesizingAudioCompleted) sb.Append("Ses: ✔ ");
                    else sb.Append($"Ses: ❌ ({res.Reason}) ");
                }
                else sb.Append("Ses: - ");
            }
            catch { sb.Append("Ses: ❌ "); }

            LicenseStatusMessage = sb.ToString();
        }
        else
        {
            IsLicensed = false;
            LicenseStatusMessage = "Geçersiz Lisans! Ayarların kaydedilmesi unsuccessful.";
        }
    }

    private async void StartListening()
    {
        DebugHelper.LogMessage("StartListening invoked");

        // Check for microphone permissions/devices
        if (!HasMicrophoneAccess())
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TurkceRumenceCeviri_SpeechLog.txt");
            File.AppendAllText(logPath, $"{DateTime.Now}: Microphone access denied or no device available. Please check Windows Privacy settings.\n");
            System.Windows.MessageBox.Show("Mikrofon erişimi reddedildi veya cihaz bulunamadı. Lütfen Windows Gizlilikayarlarını kontrol edin.", "Hata", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        IsListening = true;
        _listeningCts = new CancellationTokenSource();
        _sessionManager.IsSessionActive = true;

        try
        {
            while (IsListening && !_listeningCts.Token.IsCancellationRequested)
            {
                var recognizedText = await _speechService.RecognizeAsync(_listeningCts.Token);
                DebugHelper.LogMessage($"STT result: '{recognizedText}'", "STT");

                if (!string.IsNullOrWhiteSpace(recognizedText) && !recognizedText.StartsWith("[") && recognizedText != "İleri" && recognizedText != "Geri")
                {
                    // Buffer by pause/punctuation to reduce word-by-word fragmentation
                    if (_sttBuffer.Length > 0)
                        _sttBuffer.Append(' ');
                    _sttBuffer.Append(recognizedText.Trim());

                    var now = DateTime.UtcNow;
                    var bufferText = _sttBuffer.ToString();
                    bool hasEndPunct = bufferText.EndsWith(".") || bufferText.EndsWith("!") || bufferText.EndsWith("?");
                    bool timedCommit = _lastSttCommit == DateTime.MinValue || (now - _lastSttCommit) >= TimeSpan.FromSeconds(2);

                    if (hasEndPunct || timedCommit)
                    {
                        var segment = bufferText.Trim();
                        _sttBuffer.Clear();
                        _lastSttCommit = now;

                        // Fast language inference: prefer STT auto-detect, then heuristic normalizer
                        var detectedLang = _speechService.LastDetectedLanguage;
                        detectedLang = (detectedLang ?? "").Trim().ToLowerInvariant();
                        if (detectedLang.StartsWith("tr")) detectedLang = "tr";
                        else if (detectedLang.StartsWith("ro")) detectedLang = "ro";
                        else
                        {
                            detectedLang = TurkceRumenceCeviri.Services.Implementations.RomanianNormalizer.DetectLanguageForText(segment);
                        }

                        DebugHelper.LogMessage($"Detected language: {detectedLang}", "STT");
                        _sessionManager.UpdateLanguage(detectedLang);
                        DetectedLanguage = detectedLang.ToUpperInvariant();

                        if (detectedLang == "ro")
                        {
                            RomanianText += (string.IsNullOrEmpty(RomanianText) ? "" : " ") + segment;
                            await PerformTranslation("ro");
                        }
                        else
                        {
                            TurkishText += (string.IsNullOrEmpty(TurkishText) ? "" : " ") + segment;
                            await PerformTranslation("tr");
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            DebugHelper.LogWarning("Listening canceled");
        }
        catch (Exception ex)
        {
            // Log the unexpected error to the file and show a user message
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TurkceRumenceCeviri_SpeechLog.txt");
            File.AppendAllText(logPath, $"{DateTime.Now}: Unexpected error in StartListening: {ex.ToString()}\n");
            System.Windows.MessageBox.Show($"Dinleme sırasında beklenmedik bir hata oluştu. Lütfen masaüstündeki 'TurkceRumenceCeviri_SpeechLog.txt' dosyasını kontrol edin ve geliştiriciye gönderin.\nHata: {ex.Message}", "Hata", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
        
        // DEĞİŞİKLİK:
        // Eski kod: var context = DetectedLanguage == "TR" ? TurkishText : RomanianText;
        // Yeni kod: Yapay zeka artık SADECE Rumence giriş metnini (RomanianText) dikkate alacak.
        var context = RomanianText; 
        
        // Dil olarak da metnin dili olan 'ro' gönderiyoruz ki AI içeriği doğru anlasın.
        // Sorunuz (AssistantQuestion) Türkçe olsa bile, AI Rumence metni analiz edip Türkçe cevap verecektir.
        var language = "ro"; 

        try
        {
            var original = await _aiService.AnswerQuestionAsync(AssistantQuestion ?? string.Empty, context, language);
            AssistantResponse = original;
            
            // Cevabı her iki dile de çevirip göster (Mevcut yapı korunuyor)
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
            var roChars = new HashSet<char>(new[] { 'ă','Ă','â','Â','î','Î','ş','Ș','ț','Ț' });

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

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

    private async Task TestTranslatorKeyAsync()
    {
        try
        {
            var key = TranslatorKeyInput;
            var region = TranslatorRegionInput;
            if (string.IsNullOrWhiteSpace(key))
            {
                LicenseStatusMessage = "Translator key boş";
                return;
            }

            var endpoint = $"https://{region}.api.cognitive.microsofttranslator.com";
            var uri = $"{endpoint}/translate?api-version=3.0&to=tr";
            using var client = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Post, uri);
            req.Headers.Add("Ocp-Apim-Subscription-Key", key);
            req.Headers.Add("Ocp-Apim-Subscription-Region", region);
            req.Content = new StringContent(JsonConvert.SerializeObject(new object[] { new { Text = "test" } }), Encoding.UTF8, "application/json");
            var resp = await client.SendAsync(req).ConfigureAwait(false);
            var text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (resp.IsSuccessStatusCode)
            {
                LicenseStatusMessage = "Translator test başarılı";
            }
            else
            {
                LicenseStatusMessage = $"Translator test başarısız: {(int)resp.StatusCode} {resp.ReasonPhrase}";
            }
        }
        catch (Exception ex)
        {
            LicenseStatusMessage = $"Translator test hatası: {ex.Message}";
        }
    }

    private async Task TestSpeechKeyAsync()
    {
        try
        {
            var key = SpeechKeyInput;
            var region = SpeechRegionInput;
            if (string.IsNullOrWhiteSpace(key))
            {
                LicenseStatusMessage = "Speech key boş";
                return;
            }

            try
            {
                var config = SpeechConfig.FromSubscription(key, region);
                using var audio = AudioConfig.FromDefaultSpeakerOutput();
                using var synth = new SpeechSynthesizer(config, audio);
                var res = await synth.SpeakTextAsync("test").ConfigureAwait(false);
                if (res.Reason == ResultReason.SynthesizingAudioCompleted)
                {
                    LicenseStatusMessage = "Speech (TTS) test başarılı";
                }
                else
                {
                    LicenseStatusMessage = $"Speech test başarısız: {res.Reason}";
                }
            }
            catch (Exception ex)
            {
                LicenseStatusMessage = $"Speech test hatası: {ex.Message}";
            }
        }
        catch (Exception ex)
        {
            LicenseStatusMessage = $"Speech test hatası: {ex.Message}";
        }
    }

    private void ReinitializeServices()
    {
        try
        {
            // Recreate translation, speech and tts services using current keys
            var cfg = TurkceRumenceCeviri.Configuration.AzureConfig.LoadFromEnvironment();

            _translationService = new TurkceRumenceCeviri.Services.Implementations.AzureTranslationService(TranslatorKeyInput ?? cfg.TranslatorKey, TranslatorRegionInput ?? cfg.TranslatorRegion);
            _speechService = new TurkceRumenceCeviri.Services.Implementations.AzureSpeechRecognitionService(SpeechKeyInput ?? cfg.SpeechKey, SpeechRegionInput ?? cfg.SpeechRegion);
            _ttsService = new TurkceRumenceCeviri.Services.Implementations.AzureTextToSpeechService(SpeechKeyInput ?? cfg.SpeechKey, SpeechRegionInput ?? cfg.SpeechRegion);
        }
        catch (Exception ex)
        {
            TurkceRumenceCeviri.Utilities.DebugHelper.LogWarning($"Failed to reinitialize services: {ex.Message}");
        }
    }

    private void ShowPhoneticWindow(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // Pencere açılır açılmaz fonetik dönüştürme otomatik uygulanır.
        System.Windows.Application.Current.Dispatcher.Invoke(() => 
        {
            var win = new TurkceRumenceCeviri.Views.PhoneticResultWindow(text, true);
            if (System.Windows.Application.Current.MainWindow != null)
            {
                win.Owner = System.Windows.Application.Current.MainWindow;
            }
            win.ShowDialog();
        });
    }

    private void OpenRomanianListeningWindow()
    {
        var cfg = TurkceRumenceCeviri.Configuration.AzureConfig.LoadFromEnvironment();
        var speechKey = !string.IsNullOrWhiteSpace(SpeechKeyInput) ? SpeechKeyInput : cfg.SpeechKey;
        var speechRegion = !string.IsNullOrWhiteSpace(SpeechRegionInput) ? SpeechRegionInput : cfg.SpeechRegion;

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var win = new TurkceRumenceCeviri.Views.RomanianListeningWindow(speechKey, speechRegion);
            if (System.Windows.Application.Current.MainWindow != null)
            {
                win.Owner = System.Windows.Application.Current.MainWindow;
            }
            win.Show();
        });
    }

    // Add this helper method to check microphone access
    private bool HasMicrophoneAccess()
    {
        // Simplified: Assume access is available (relies on SDK logging for actual issues)
        // For a proper check, install NAudio and use NAudio.CoreAudioApi.MMDeviceEnumerator
           return true;
   }
}
