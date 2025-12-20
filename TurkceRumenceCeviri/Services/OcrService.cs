using System;
using System.IO;
using System.Threading.Tasks;
using Tesseract;
using System.Collections.Generic;

namespace TurkceRumenceCeviri.Services;

public record OcrLine(string Text, string Language);
public class OcrService : IOcrService
{
    public async Task<(string Text, string DetectedLanguage)> ExtractTextAsync(string imagePath)
    {
        var baseDir = AppContext.BaseDirectory;
        var tessDataPath = Path.Combine(baseDir, "Resources", "tessdata");
        var lang = "tur+ron";

        if (!Directory.Exists(tessDataPath))
            throw new DirectoryNotFoundException($"tessdata not found at '{tessDataPath}' (BaseDir='{baseDir}').");

        var turPath = Path.Combine(tessDataPath, "tur.traineddata");
        var ronPath = Path.Combine(tessDataPath, "ron.traineddata");
        if (!File.Exists(turPath) || !File.Exists(ronPath))
            throw new FileNotFoundException($"Missing traineddata. tur: {File.Exists(turPath)}, ron: {File.Exists(ronPath)}.");

        if (!File.Exists(imagePath))
            throw new FileNotFoundException($"OCR image not found: '{imagePath}'.");

        try
        {
            using var engine = new TesseractEngine(tessDataPath, lang, EngineMode.Default);
            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);

            var text = page.GetText() ?? string.Empty;
            await Task.Yield();

            var detected = DetectLanguageFromText(text);
            return (text, detected);
        }
        catch (TesseractException ex)
        {
            throw new InvalidOperationException($"Tesseract failed: {ex.Message}", ex);
        }
    }

    private static string DetectLanguageFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "unknown";
        if (TurkceRumenceCeviri.Services.Implementations.RomanianNormalizer.IsLikelyTurkish(text))
            return "tr";
        if (TurkceRumenceCeviri.Services.Implementations.RomanianNormalizer.IsLikelyRomanian(text))
            return "ro";
        return TurkceRumenceCeviri.Services.Implementations.RomanianNormalizer.DetectLanguageForText(text);
    }

    public async Task<IReadOnlyList<OcrLine>> ExtractLinesAsync(string imagePath)
    {
        var (text, _) = await ExtractTextAsync(imagePath);
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<OcrLine>();

        var lines = text.Replace("\r\n", "\n")
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var detected = DetectLanguageFromText(text);
        var result = new List<OcrLine>(lines.Length);
        foreach (var l in lines)
            result.Add(new OcrLine(l, detected));
        return result;
    }
}