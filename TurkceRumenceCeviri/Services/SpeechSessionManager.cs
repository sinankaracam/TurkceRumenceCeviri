namespace TurkceRumenceCeviri.Services;

/// <summary>
/// Konuþma oturumu bilgisini takip eder
/// Birden fazla konuþmada hangi dilde olduðunu hatýrlar
/// </summary>
public class SpeechSessionManager
{
    private string _currentLanguage = "unknown";
    private DateTime _lastDetectionTime = DateTime.MinValue;
    private const int LanguageSwitchThresholdSeconds = 5;

    public string CurrentLanguage => _currentLanguage;
    public bool IsSessionActive { get; set; }

    public event EventHandler<LanguageDetectedEventArgs>? LanguageDetected;

    /// <summary>
    /// Konuþmada dil deðiþikliðini kaydeder
    /// </summary>
    public void UpdateLanguage(string detectedLanguage)
    {
        var now = DateTime.Now;
        var timeSinceLastDetection = (now - _lastDetectionTime).TotalSeconds;

        // Eðer dil deðiþtiyse ve yeterli zaman geçtiyse, yeni dil olarak kaydet
        if (detectedLanguage != _currentLanguage && timeSinceLastDetection > LanguageSwitchThresholdSeconds)
        {
            _currentLanguage = detectedLanguage;
            _lastDetectionTime = now;
            
            LanguageDetected?.Invoke(this, new LanguageDetectedEventArgs 
            { 
                Language = detectedLanguage,
                Timestamp = now
            });
        }
        else if (_currentLanguage == "unknown")
        {
            _currentLanguage = detectedLanguage;
            _lastDetectionTime = now;
            
            LanguageDetected?.Invoke(this, new LanguageDetectedEventArgs 
            { 
                Language = detectedLanguage,
                Timestamp = now
            });
        }
    }

    /// <summary>
    /// Konuþma oturumunu sýfýrla
    /// </summary>
    public void ResetSession()
    {
        _currentLanguage = "unknown";
        _lastDetectionTime = DateTime.MinValue;
        IsSessionActive = false;
    }
}

public class LanguageDetectedEventArgs : EventArgs
{
    public required string Language { get; set; }
    public DateTime Timestamp { get; set; }
}
