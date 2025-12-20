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
        var envPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
        // DotEnv.Core kullanımı: .env dosyasını yükle ve değerleri Environment değişkenlerine uygula
        var loader = new EnvLoader();
        loader.AddEnvFile(envPath)
              .Load();
        DebugHelper.LogSuccess($".env yüklendi: {envPath}");
        DebugHelper.LogMessage($"AZURE_TRANSLATOR_KEY present: {(!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_KEY")))}");
        DebugHelper.LogMessage($"AZURE_SPEECH_KEY present: {(!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY")))}");


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

