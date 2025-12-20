using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TurkceRumenceCeviri.Services.Implementations;

public class GroqAssistantService : IAIAssistantService
{
    private readonly string _apiKey;
    private readonly HttpClient _http;
    private readonly string _model;

    public GroqAssistantService(string apiKey, string model = "llama-3.1-8b-instant")
    {
        _apiKey = apiKey;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _model = model;
    }

    public async Task<string> AnswerQuestionAsync(string question, string context, string language)
    {
        var prompt = $"Dil: {language}\nSoru: {question}\nBaðlam:\n{context}\n\nCevabý ayný dilde, kýsa ve net ver.";
        var payload = new
        {
            model = _model,
            messages = new object[]
            {
                new { role = "system", content = "Sen bir çeviri asistanýsýn." },
                new { role = "user", content = prompt }
            }
        };
        var json = JsonSerializer.Serialize(payload);
        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return content ?? "";
    }
}
