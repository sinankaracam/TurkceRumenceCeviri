namespace TurkceRumenceCeviri.Services;

public interface IOcrService
{
    Task<(string Text, string DetectedLanguage)> ExtractTextAsync(string imagePath);
    Task<IReadOnlyList<OcrLine>> ExtractLinesAsync(string imagePath);
}
