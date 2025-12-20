using Newtonsoft.Json;
using System.Net.Http;
using System.IO;
using System;
using System.Text;
using System.Threading.Tasks;

namespace TurkceRumenceCeviri.Services.Implementations;

public class PythonBackendService : IOcrService, IAIAssistantService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public PythonBackendService(string baseUrl = "http://localhost:5000")
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient();
    }

    // OCR Ýþlemi
    public async Task<(string Text, string DetectedLanguage)> ExtractTextAsync(string imagePath)
    {
        try
        {
            using var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(File.OpenRead(imagePath));
            form.Add(fileContent, "image", Path.GetFileName(imagePath));

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/ocr", form);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(json);

            return (
                (string)(result?.text ?? ""),
                (string)(result?.detected_language ?? "tr")
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OCR Hatasý: {ex.Message}");
            return ("[OCR Hatasý]", "unknown");
        }
    }

    // AI Asistan Yanýtý
    public async Task<string> AnswerQuestionAsync(string question, string context, string language)
    {
        try
        {
            var payload = new
            {
                question,
                context,
                language
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/ask", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(json);

            return (string)(result?.answer ?? "");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AI Asistan Hatasý: {ex.Message}");
            return $"[Hata: {ex.Message}]";
        }
    }

    // Satýr bazlý OCR
    public async Task<IReadOnlyList<OcrLine>> ExtractLinesAsync(string imagePath)
    {
        var (text, _) = await ExtractTextAsync(imagePath);
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<OcrLine>();

        var lines = text.Replace("\r\n", "\n")
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var detected = text.IndexOfAny(new[] { '?', 'â', 'î', '?', '?', '?', 'Â', 'Î', '?', '?' }) >= 0 ? "ro" : "tr";
        var result = new List<OcrLine>(lines.Length);
        foreach (var l in lines)
            result.Add(new OcrLine(l, detected));
        return result;
    }
}
