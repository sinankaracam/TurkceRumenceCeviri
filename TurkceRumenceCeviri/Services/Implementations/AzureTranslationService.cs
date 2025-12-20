using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TurkceRumenceCeviri.Services.Implementations;

public class AzureTranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly string _translatorKey;
    private readonly string _translatorRegion;

    public AzureTranslationService(string translatorKey, string translatorRegion, string? unusedLanguageKey = null, string? unusedLanguageEndpoint = null)
    {
        _translatorKey = translatorKey;
        _translatorRegion = translatorRegion;
        _httpClient = new HttpClient();
        
        // Not: Language Service artýk kullanýlmýyor. 
        // Translator API'nin kendi detect endpoint'i kullanýlýyor.
        // unusedLanguageKey ve unusedLanguageEndpoint parametreleri compatibility için býrakýldý.
    }

    public async Task<string> DetectLanguageAsync(string text)
    {
        try
        {
            // Azure Translator API'nin detect endpoint'ini kullan (Language Service yerine)
            var endpoint = $"https://{_translatorRegion}.api.cognitive.microsofttranslator.com";
            var uri = $"{endpoint}/detect?api-version=3.0";

            var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _translatorKey);
            request.Headers.Add("Ocp-Apim-Subscription-Region", _translatorRegion);

            var body = new object[] { new { Text = text } };
            request.Content = new StringContent(
                JsonConvert.SerializeObject(body),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var arr = JArray.Parse(result);
            var lang = arr.First?[("language")];
            return lang?.ToString() ?? "unknown";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dil algýlama hatasý: {ex.Message}");
            return "unknown";
        }
    }

    public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        try
        {
            // Kaynak dili boþsa algýlama yap
            if (string.IsNullOrEmpty(sourceLanguage) || sourceLanguage == "auto")
            {
                sourceLanguage = await DetectLanguageAsync(text);
            }

            // Dil kodlarýný normalize et
            sourceLanguage = NormalizeLanguageCode(sourceLanguage);
            targetLanguage = NormalizeLanguageCode(targetLanguage);

            // Azure Translator REST API çaðrýsý
            var endpoint = $"https://{_translatorRegion}.api.cognitive.microsofttranslator.com";
            var uri = $"{endpoint}/translate?api-version=3.0&from={sourceLanguage}&to={targetLanguage}";

            var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _translatorKey);
            request.Headers.Add("Ocp-Apim-Subscription-Region", _translatorRegion);

            var body = new object[] { new { Text = text } };
            request.Content = new StringContent(
                JsonConvert.SerializeObject(body),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage response;
            string result;
            try
            {
                response = await _httpClient.SendAsync(request);
                result = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Translate regional endpoint failed ({ex.Message}), falling back to global endpoint.");
                endpoint = "https://api.cognitive.microsofttranslator.com";
                uri = $"{endpoint}/translate?api-version=3.0&from={sourceLanguage}&to={targetLanguage}";
                request = new HttpRequestMessage(HttpMethod.Post, uri);
                request.Headers.Add("Ocp-Apim-Subscription-Key", _translatorKey);
                request.Headers.Add("Ocp-Apim-Subscription-Region", _translatorRegion);
                request.Content = new StringContent(JsonConvert.SerializeObject(new object[] { new { Text = text } }), System.Text.Encoding.UTF8, "application/json");
                response = await _httpClient.SendAsync(request);
                result = await response.Content.ReadAsStringAsync();
            }
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Translator HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {result}");
                return $"[Çeviri Hatasý: HTTP {(int)response.StatusCode} {response.ReasonPhrase}]";
            }

            var arr = JArray.Parse(result);
            var first = arr.First as JObject;
            var translations = first?[("translations")] as JArray;
            var textToken = translations?.First?[("text")];
            return textToken?.ToString() ?? "";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Çeviri hatasý: {ex.Message}");
            return $"[Çeviri Hatasý: {ex.Message}]";
        }
    }

    private string NormalizeLanguageCode(string languageCode)
    {
        return languageCode?.ToLower() switch
        {
            "tr" or "turkish" => "tr",
            "ro" or "romanian" => "ro",
            "en" or "english" => "en",
            _ => languageCode?.ToLower() ?? "en"
        };
    }
}
