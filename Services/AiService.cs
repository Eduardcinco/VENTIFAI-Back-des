using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace VentifyAPI.Services
{
    public class AiService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _defaultModel;

        public AiService(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("openrouter");
            _apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
                     ?? config["OpenRouter:ApiKey"]
                     ?? string.Empty;
            _baseUrl = config["OpenRouter:BaseUrl"] ?? "https://openrouter.ai/api/v1";
            _defaultModel = config["OpenRouter:DefaultModel"] ?? "openrouter/auto";
        }

        public async Task<string> ChatAsync(IEnumerable<AiMessage> messages, string? model = null, string? referer = null, string? title = null)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException("OpenRouter API key no configurada. Define OPENROUTER_API_KEY en el entorno o en appsettings.");
            }
            var url = _baseUrl.TrimEnd('/') + "/chat/completions";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            if (!string.IsNullOrWhiteSpace(referer)) req.Headers.Add("HTTP-Referer", referer);
            if (!string.IsNullOrWhiteSpace(title)) req.Headers.Add("X-Title", title);

            var payload = new
            {
                model = string.IsNullOrWhiteSpace(model) ? _defaultModel : model,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
                temperature = 0.2
            };
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            if (!res.IsSuccessStatusCode)
            {
                var errText = await res.Content.ReadAsStringAsync();
                throw new HttpRequestException($"OpenRouter error {(int)res.StatusCode}: {errText}");
            }
            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            return content ?? string.Empty;
        }
    }

    public class AiMessage
    {
        public string Role { get; set; } = "user"; // system|user|assistant
        public string Content { get; set; } = string.Empty;
    }
}
