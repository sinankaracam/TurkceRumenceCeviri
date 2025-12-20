using TurkceRumenceCeviri.Configuration;

namespace TurkceRumenceCeviri.Services;

public interface IConfigService
{
    AzureConfig Current { get; }
    void Reload();
    void Validate();
}
