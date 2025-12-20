using DotEnv.Core;
using TurkceRumenceCeviri.Configuration;
using TurkceRumenceCeviri.Utilities;

namespace TurkceRumenceCeviri.Services.Implementations;

public class ConfigService : IConfigService
{
    public AzureConfig Current { get; private set; }

    public ConfigService()
    {
        LoadEnv();
        Current = AzureConfig.LoadFromEnvironment();
        DebugHelper.LogMessage(Current.ToString(), "CONFIG");
    }

    public void Reload()
    {
        LoadEnv();
        Current = AzureConfig.LoadFromEnvironment();
        DebugHelper.LogMessage("Configuration reloaded", "CONFIG");
    }

    public void Validate()
    {
        // AzureConfig.LoadFromEnvironment çaðrýsý zaten validate tetikliyor
        // Burada ek kontrolleri koyabilirsiniz
    }

    private void LoadEnv()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var candidates = new List<string>
        {
            System.IO.Path.Combine(baseDir, ".env"),
            // Project directory
            System.IO.Path.Combine(baseDir, "..", "..", "..", ".env"),
            // Solution root (one more up)
            System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".env"),
        };

        string? existing = candidates.Select(p => System.IO.Path.GetFullPath(p))
                                     .FirstOrDefault(System.IO.File.Exists);

        if (existing is null)
        {
            DebugHelper.LogWarning(".env dosyasý bulunamadý. Ortam deðiþkenleri OS üzerinden ayarlanmýþ olmalý.");
            return;
        }

        var loader = new EnvLoader();
        loader.AddEnvFile(existing).Load();
        DebugHelper.LogSuccess($".env yüklendi: {existing}");
    }
}
