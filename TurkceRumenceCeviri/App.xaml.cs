using System.Windows;
using TurkceRumenceCeviri.Configuration;
using TurkceRumenceCeviri.Services;
using TurkceRumenceCeviri.Services.Implementations;
using TurkceRumenceCeviri.ViewModels;
using DotEnv.Core;
using TurkceRumenceCeviri.Utilities;

namespace TurkceRumenceCeviri;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Try to load persisted settings early so environment variables can be applied before services are constructed
        DebugHelper.LogMessage("Attempting to load persisted settings (user.settings.dat) before configuration.", "BOOT");
        try
        {
            var persisted = LicenseStorage.Load();
            if (persisted != null)
            {
                DebugHelper.LogMessage("Applying persisted settings to environment (early load).", "BOOT");
                if (!string.IsNullOrEmpty(persisted.TranslatorKey)) Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_KEY", persisted.TranslatorKey);
                if (!string.IsNullOrEmpty(persisted.SpeechKey)) Environment.SetEnvironmentVariable("AZURE_SPEECH_KEY", persisted.SpeechKey);
                if (!string.IsNullOrEmpty(persisted.GroqKey)) Environment.SetEnvironmentVariable("GROQ_API_KEY", persisted.GroqKey);
                if (!string.IsNullOrEmpty(persisted.LicenseKey)) Environment.SetEnvironmentVariable("LISANS_KEY", persisted.LicenseKey);
                if (!string.IsNullOrEmpty(persisted.TranslatorRegion)) Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_REGION", persisted.TranslatorRegion);
                if (!string.IsNullOrEmpty(persisted.SpeechRegion)) Environment.SetEnvironmentVariable("AZURE_SPEECH_REGION", persisted.SpeechRegion);
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogWarning($"Early load of persisted settings failed: {ex.Message}");
        }

        // ConfigService will load persisted settings (user.settings.dat) and apply environment variables
        DebugHelper.LogMessage("ConfigService will load persisted settings if available.", "BOOT");

        // Azure konfigürasyonunu yükle (.env dosyasından veya environment variables'dan)
        // NOT: Artık sadece 2 Azure hizmeti kullanılıyor!
        // 1. Azure Translator (Çeviri + Dil Algılama)
        // 2. Azure Speech Services (STT/TTS)
        var configService = new ConfigService();
        var config = configService.Current;
        DebugHelper.LogMessage(config.ToString());

        // Tüm anahtarlar .env veya environment üzerinden AzureConfig ile alınır.
        // Eksik anahtarlar için AzureConfig.ValidateConfiguration uyarı verir.

        // Servisler oluştur (Dependency Injection)
        // Language Service artık ihtiyaç yok - Translator detect endpoint'ini kullanıyoruz
        var translationService = new AzureTranslationService(
            config.TranslatorKey, 
            config.TranslatorRegion,
            null, // Language Key artık gerekli değil
            null  // Language Endpoint artık gerekli değil
        );

        var speechService = new AzureSpeechRecognitionService(
            config.SpeechKey, 
            config.SpeechRegion);
            
        var ttsService = new AzureTextToSpeechService(
            config.SpeechKey, 
            config.SpeechRegion);
            
        var pythonService = new PythonBackendService(config.PythonApiUrl);
        // Use local Tesseract OCR from project folder if available
        var tesseractExe = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tesseract-OCR", "tesseract.exe");
        IOcrService ocrService = System.IO.File.Exists(tesseractExe)
            ? new TesseractOcrService(tesseractExe, config.OcrLanguage)
            : pythonService; // fallback to python OCR

        // AI Assistant via Groq
        IAIAssistantService aiService = new GroqAssistantService(config.GroqKey);

        // ViewModel oluştur (MVVM Pattern)
        var viewModel = new MainViewModel(
            translationService,
            speechService,
            ttsService,
            ocrService,
            aiService
        );

        // MainWindow oluştur ve DataContext ayarla
        var mainWindow = new MainWindow();
        mainWindow.SetDataContext(viewModel);
        mainWindow.Show();
    }
}

