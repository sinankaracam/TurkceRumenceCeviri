namespace TurkceRumenceCeviri.Services;

public interface ITranslationService
{
    Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage);
    Task<string> DetectLanguageAsync(string text);
}
