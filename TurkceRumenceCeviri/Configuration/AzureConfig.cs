namespace TurkceRumenceCeviri.Configuration;

/// <summary>
/// Azure Cognitive Services ve diðer API credentials'ýný güvenli þekilde yönetir.
/// NEVER hardcode credentials! Ortam deðiþkenlerinden yükleyin.
/// 
/// NOT: Dil algýlama için artýk ayrý Language Service'e ihtiyaç YOK
/// Translator API'nin kendi detect endpoint'i kullanýlýyor (BEDAVA)
/// </summary>
public class AzureConfig
{
    public string TranslatorKey { get; set; } = "";
    public string TranslatorRegion { get; set; } = "";
    public string SpeechKey { get; set; } = "";
    public string SpeechRegion { get; set; } = "";
    
    // Eski properties (backward compatibility için - artýk kullanýlmýyor)
    [Obsolete("Language Service artýk kullanýlmýyor. Translator detect endpoint kullanýlýyor.")]
    public string LanguageKey { get; set; } = "";
    
    [Obsolete("Language Service artýk kullanýlmýyor. Translator detect endpoint kullanýlýyor.")]
    public string LanguageEndpoint { get; set; } = "";
    
    public string PythonApiUrl { get; set; } = "http://localhost:5000";
    
    // Ek API Keys (gelecek için)
    public string DeepLKey { get; set; } = "";
    public string GroqKey { get; set; } = "";
    public string OcrLanguage { get; set; } = "ron+tur"; // Rumence + Türkçe

    public static AzureConfig LoadFromEnvironment()
    {
        var config = new AzureConfig
        {
            // Azure Services (sadece 2 gerekli)
            TranslatorKey = GetEnvironmentVariable("AZURE_TRANSLATOR_KEY"),
            TranslatorRegion = GetEnvironmentVariable("AZURE_TRANSLATOR_REGION", "westeurope"),
            SpeechKey = GetEnvironmentVariable("AZURE_SPEECH_KEY"),
            SpeechRegion = GetEnvironmentVariable("AZURE_SPEECH_REGION", "westeurope"),
            
            // Language Service artýk OPSIYONEL
            LanguageKey = GetEnvironmentVariable("AZURE_LANGUAGE_KEY", ""),
            LanguageEndpoint = GetEnvironmentVariable("AZURE_LANGUAGE_ENDPOINT", ""),
            
            // Ek API Keys
            DeepLKey = GetEnvironmentVariable("DEEPL_API_KEY", ""),
            GroqKey = GetEnvironmentVariable("GROQ_API_KEY", ""),
            PythonApiUrl = GetEnvironmentVariable("PYTHON_API_URL", "http://localhost:5000"),
            OcrLanguage = GetEnvironmentVariable("OCR_LANGUAGE", "ron+tur")
        };

        // If environment variables are not set, try to load keys from persisted license storage
        try
        {
            if (string.IsNullOrEmpty(config.TranslatorKey) || string.IsNullOrEmpty(config.SpeechKey))
            {
                var persisted = TurkceRumenceCeviri.Utilities.LicenseStorage.Load();
                if (persisted != null)
                {
                    if (string.IsNullOrEmpty(config.TranslatorKey) && !string.IsNullOrEmpty(persisted.TranslatorKey))
                        config.TranslatorKey = persisted.TranslatorKey;
                    if (string.IsNullOrEmpty(config.SpeechKey) && !string.IsNullOrEmpty(persisted.SpeechKey))
                        config.SpeechKey = persisted.SpeechKey;
                    if (string.IsNullOrEmpty(config.GroqKey) && !string.IsNullOrEmpty(persisted.GroqKey))
                        config.GroqKey = persisted.GroqKey;
                }
            }
        }
        catch { /* ignore errors while trying to load persisted keys */ }

        // Validation (do NOT throw here so UI can start even if env vars are missing; just warn)
        ValidateConfiguration(config);
        return config;
    }

    /// <summary>
    /// Ortam deðiþkeninden deðer al, yoksa default deðer döndür
    /// </summary>
    private static string GetEnvironmentVariable(string variableName, string defaultValue = "")
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        
        if (string.IsNullOrWhiteSpace(value) && !string.IsNullOrEmpty(defaultValue))
        {
            return defaultValue;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            Console.WriteLine($"?? Warning: Environment variable '{variableName}' not configured");
            return "";
        }

        return value;
    }

    /// <summary>
    /// Konfigürasyonun geçerliliðini kontrol et
    /// </summary>
    private static void ValidateConfiguration(AzureConfig config)
    {
        var missingKeys = new List<string>();

        // Sadece Translator ve Speech gerekli
        if (string.IsNullOrEmpty(config.TranslatorKey))
            missingKeys.Add("AZURE_TRANSLATOR_KEY");
        if (string.IsNullOrEmpty(config.SpeechKey))
            missingKeys.Add("AZURE_SPEECH_KEY");

        // Language Service artýk opsiyonel
        if (string.IsNullOrEmpty(config.LanguageKey))
            Console.WriteLine("?? Info: AZURE_LANGUAGE_KEY not set (OK - using Translator detect endpoint)");

        if (missingKeys.Any())
        {
            var message = $"Missing required environment variables: {string.Join(", ", missingKeys)}";
            Console.WriteLine($"? {message}");
            Console.WriteLine("? Application will continue so you can enter keys via the UI. Some features may fail until keys are provided.");
            // Do not throw here to allow UI to start so user can provide keys through the settings panel
        }
    }

    /// <summary>
    /// Debug amaçlý (saklý deðerleri göstermez)
    /// </summary>
    public override string ToString()
    {
        return $@"
? Azure Configuration:
  - Translator Region: {TranslatorRegion}
  - Speech Region: {SpeechRegion}
  - Language Service: {(string.IsNullOrEmpty(LanguageKey) ? "NOT NEEDED (using Translator detect)" : "configured")}
  - Python API: {PythonApiUrl}
  - DeepL: {(string.IsNullOrEmpty(DeepLKey) ? "not set" : "configured")}
  - Groq: {(string.IsNullOrEmpty(GroqKey) ? "not set" : "configured")}
  
?? All sensitive keys are masked for security
";
    }
}

