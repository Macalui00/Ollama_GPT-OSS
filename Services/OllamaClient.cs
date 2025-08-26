using System.Text.Json;
using System.Text;

namespace ollama_gpt.Services {
    public class clsOllamaClient {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        // Mejor con 127.0.0.1 en lugar de localhost
        public clsOllamaClient(string pBaseUrl = "http://127.0.0.1:11434") {
            _httpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
            _baseUrl = pBaseUrl.TrimEnd('/');
        }

        public async IAsyncEnumerable<string> GenerateStreamAsync(
            string pModel,
            string pPrompt,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken pCancellationToken = default) {
            // 🔴 CLAVE: usar las claves que Ollama espera: "model" y "prompt"
            var wRequestBody = new {
                model = pModel,
                prompt = pPrompt,
                stream = true
                // Si preferís el endpoint /api/chat en lugar de /api/generate:
                // messages = new[] { new { role = "user", content = pPrompt } }
            };

            var wJson = JsonSerializer.Serialize(wRequestBody);

            using var wRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/generate") {
                Content = new StringContent(wJson, Encoding.UTF8, "application/json")
            };

            using var wResponse = await _httpClient.SendAsync(
                wRequest,
                HttpCompletionOption.ResponseHeadersRead,
                pCancellationToken
            );

            // En vez de arrojar excepción, logueamos el error para ver el detalle real
            if (!wResponse.IsSuccessStatusCode) {
                var errorText = await wResponse.Content.ReadAsStringAsync(pCancellationToken);
                Console.Error.WriteLine($"❌ Ollama {(int)wResponse.StatusCode} {wResponse.ReasonPhrase}: {errorText}");
                yield break;
            }

            using var wStream = await wResponse.Content.ReadAsStreamAsync(pCancellationToken);
            using var wReader = new StreamReader(wStream);

            while (!wReader.EndOfStream && !pCancellationToken.IsCancellationRequested) {
                var wLine = await wReader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(wLine))
                    continue;

                // Token (campo "response" en cada línea del stream)
                var token = TryExtractToken(wLine);
                if (!string.IsNullOrEmpty(token))
                    yield return token;

                // ¿Terminó?
                if (IsDone(wLine))
                    yield break;
            }
        }

        private static string? TryExtractToken(string line) {
            try {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("response", out var prop))
                    return prop.GetString();
            } catch (JsonException) {
                Console.Error.WriteLine($"⚠️ JSON inválido: {line}");
            }
            return null;
        }

        private static bool IsDone(string line) {
            try {
                using var doc = JsonDocument.Parse(line);
                return doc.RootElement.TryGetProperty("done", out var done) && done.GetBoolean();
            } catch { return false; }
        }
    }
}
