using System.Diagnostics;
using System.Text;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace TurkceRumenceCeviri.Services.Implementations;

public class TesseractOcrService : IOcrService
{
    private readonly string _tesseractExePath;
    private readonly string _language;

    public TesseractOcrService(string tesseractExePath, string language = "ron+tur")
    {
        _tesseractExePath = System.IO.Path.GetFullPath(tesseractExePath);
        _language = language;
    }

    public async Task<(string Text, string DetectedLanguage)> ExtractTextAsync(string imagePath)
    {
        try
        {
            var inputPath = System.IO.Path.GetFullPath(imagePath);
            var outputBase = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ocr_output_" + Guid.NewGuid().ToString("N"));

            var tessDataRoot = LocateTessDataRoot();
            // First pass: use combined language to get rough text
            var psi = new ProcessStartInfo
            {
                FileName = _tesseractExePath,
                Arguments = tessDataRoot != null
                    ? $"\"{inputPath}\" \"{outputBase}\" -l tur+ron --tessdata-dir \"{tessDataRoot}\""
                    : $"\"{inputPath}\" \"{outputBase}\" -l tur+ron",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null)
                throw new InvalidOperationException("Tesseract process could not be started.");

            var stdOut = await proc.StandardOutput.ReadToEndAsync();
            var stdErr = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            if (proc.ExitCode != 0)
            {
                var msg = string.IsNullOrWhiteSpace(stdErr) ? stdOut : stdErr;
                return ($"[OCR Hatasý: {msg}]", "unknown");
            }

            var txtPath = outputBase + ".txt";
            if (!System.IO.File.Exists(txtPath))
                return ("", "unknown");

            var roughText = await System.IO.File.ReadAllTextAsync(txtPath, Encoding.UTF8);

            // Decide best language using RomanianNormalizer
            string decidedLang;
            if (RomanianNormalizer.IsLikelyTurkish(roughText)) decidedLang = "tur";
            else if (RomanianNormalizer.IsLikelyRomanian(roughText)) decidedLang = "ron";
            else decidedLang = _language.Contains("ron") && !_language.Contains("tur") ? "ron" : "tur";

            // Second pass: re-run OCR with decided single language for better accuracy
            var outputFinal = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ocr_final_" + Guid.NewGuid().ToString("N"));
            var psiFinal = new ProcessStartInfo
            {
                FileName = _tesseractExePath,
                Arguments = tessDataRoot != null
                    ? $"\"{inputPath}\" \"{outputFinal}\" -l {decidedLang} --tessdata-dir \"{tessDataRoot}\""
                    : $"\"{inputPath}\" \"{outputFinal}\" -l {decidedLang}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var proc2 = Process.Start(psiFinal);
            if (proc2 == null)
                throw new InvalidOperationException("Tesseract process (final) could not be started.");
            var stdOut2 = await proc2.StandardOutput.ReadToEndAsync();
            var stdErr2 = await proc2.StandardError.ReadToEndAsync();
            await proc2.WaitForExitAsync();
            if (proc2.ExitCode != 0)
            {
                var msg2 = string.IsNullOrWhiteSpace(stdErr2) ? stdOut2 : stdErr2;
                return ($"[OCR Hatasý: {msg2}]", "unknown");
            }

            var finalTxtPath = outputFinal + ".txt";
            var finalText = System.IO.File.Exists(finalTxtPath)
                ? await System.IO.File.ReadAllTextAsync(finalTxtPath, Encoding.UTF8)
                : roughText;

            var detectedLanguage = decidedLang == "ron" ? "ro" : "tr";
            return (finalText, detectedLanguage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OCR Hatasý: {ex.Message}");
            return ($"[OCR Hatasý: {ex.Message}]", "unknown");
        }
    }

    private static string? LocateTessDataRoot()
    {
        try
        {
            // 1. Project relative path
            var projRoot = Path.Combine(AppContext.BaseDirectory);
            // Try to find project root (two levels up from bin/Debug|Release)
            var dir = new DirectoryInfo(projRoot);
            while (dir != null && dir.Name != "TurkceRumenceCeviri")
                dir = dir.Parent;
            var candidate = dir != null ? Path.Combine(dir.FullName, "Tesseract-OCR", "tessdata") : null;
            if (candidate != null && Directory.Exists(candidate))
                return candidate;

            // 2. Adjacent folder to executable
            var execCandidate = Path.Combine(AppContext.BaseDirectory, "Tesseract-OCR", "tessdata");
            if (Directory.Exists(execCandidate))
                return execCandidate;

            return null;
        }
        catch { return null; }
    }

    public async Task<IReadOnlyList<OcrLine>> ExtractLinesAsync(string imagePath)
    {
        var (text, _) = await ExtractTextAsync(imagePath);
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<OcrLine>();

        var lines = text.Replace("\r\n", "\n")
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var roChars = new[] { '?', 'â', 'î', '?', '?', '?', 'Â', 'Î', '?', '?' };
        var detected = text.IndexOfAny(roChars) >= 0 ? "ro" : "tr";
        var result = new List<OcrLine>(lines.Length);
        foreach (var l in lines)
            result.Add(new OcrLine(l, detected));
        return result;
    }
}
