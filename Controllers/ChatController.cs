using Microsoft.AspNetCore.Mvc;
using ollama_gpt.Helpers;
using System.Text;
using System.Text.Json;

namespace ollama_gpt.Controllers;

// Todos los endpoints aquí aceptan cualquier JSON y devuelven lo que Ollama responda,
// sin perder campos ni nombres. Ideal para soportar TODOS los parámetros.
[ApiController]
[Route("api/ollama")]
public class OllamaPassthroughController : ControllerBase {
    private readonly HttpClient _http;

    public OllamaPassthroughController(IHttpClientFactory f) {
        _http = f.CreateClient("ollama");
    }

    // -----------------------------
    //         NO STREAM
    // -----------------------------

    // POST /api/ollama/generate
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] JsonElement body, CancellationToken ct) {
        var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
        using var res = await _http.PostAsync("/api/generate", content, ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        return StatusCode((int)res.StatusCode, text);
    }

    // POST /api/ollama/chat
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] JsonElement body, CancellationToken ct) {
        var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
        using var res = await _http.PostAsync("/api/chat", content, ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        return StatusCode((int)res.StatusCode, text);
    }

    // POST /api/ollama/embeddings
    [HttpPost("embeddings")]
    public async Task<IActionResult> Embeddings([FromBody] JsonElement body, CancellationToken ct) {
        var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
        using var res = await _http.PostAsync("/api/embeddings", content, ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        return StatusCode((int)res.StatusCode, text);
    }

    // -----------------------------
    //           STREAM
    // -----------------------------

    // POST /api/ollama/generate/stream  (fuerza stream=true y reenvía línea a línea)
    [HttpPost("generate/stream")]
    public async Task StreamGenerate([FromBody] JsonElement body, CancellationToken ct) {
        Response.SetSseHeaders();
        await Response.WriteAsync(":\n\n", ct);

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/generate") {
            Content = WrapStreamTrue(body)
        };

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        await PipeLinesAsSse(resp, ct);
    }

    // POST /api/ollama/chat/stream (fuerza stream=true)
    [HttpPost("chat/stream")]
    public async Task StreamChat([FromBody] JsonElement body, CancellationToken ct) {
        Response.SetSseHeaders();
        await Response.WriteAsync(":\n\n", ct);

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/chat") {
            Content = WrapStreamTrue(body)
        };

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        await PipeLinesAsSse(resp, ct);
    }

    // -----------------------------
    //       GESTIÓN DE MODELOS
    // -----------------------------

    [HttpGet("version")]
    public async Task<IActionResult> Version(CancellationToken ct) {
        using var res = await _http.GetAsync("/api/version", ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        return StatusCode((int)res.StatusCode, text);
    }

    [HttpGet("tags")]
    public async Task<IActionResult> Tags(CancellationToken ct) {
        using var res = await _http.GetAsync("/api/tags", ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        return StatusCode((int)res.StatusCode, text);
    }

    // POST /api/ollama/pull   body: { "name":"gpt-oss:20b" , "insecure"?: true , ... }
    [HttpPost("pull")]
    public async Task<IActionResult> Pull([FromBody] JsonElement body, CancellationToken ct) {
        var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
        using var res = await _http.PostAsync("/api/pull", content, ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        return StatusCode((int)res.StatusCode, text);
    }

    // POST /api/ollama/show   body: { "name":"gpt-oss:20b" }
    [HttpPost("show")]
    public async Task<IActionResult> Show([FromBody] JsonElement body, CancellationToken ct) {
        var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
        using var res = await _http.PostAsync("/api/show", content, ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        return StatusCode((int)res.StatusCode, text);
    }

    // POST /api/ollama/create body: { "name":"mi-modelo", "modelfile":"..." }
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] JsonElement body, CancellationToken ct) {
        var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
        using var res = await _http.PostAsync("/api/create", content, ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        return StatusCode((int)res.StatusCode, text);
    }

    // POST /api/ollama/copy   body: { "source":"gpt-oss:20b", "destination":"alias" }
    [HttpPost("copy")]
    public async Task<IActionResult> Copy([FromBody] JsonElement body, CancellationToken ct) {
        var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
        using var res = await _http.PostAsync("/api/copy", content, ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        return StatusCode((int)res.StatusCode, text);
    }

    // DELETE /api/ollama/delete body: { "name":"alias" }
    [HttpDelete("delete")]
    public async Task<IActionResult> Delete([FromBody] JsonElement body, CancellationToken ct) {
        using var req = new HttpRequestMessage(HttpMethod.Delete, "/api/delete") {
            Content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json")
        };
        using var res = await _http.SendAsync(req, ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        return StatusCode((int)res.StatusCode, text);
    }

    // POST /api/ollama/unload body: { "name":"modelo" }
    [HttpPost("unload")]
    public async Task<IActionResult> Unload([FromBody] JsonElement body, CancellationToken ct) {
        var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
        using var res = await _http.PostAsync("/api/unload", content, ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        return StatusCode((int)res.StatusCode, text);
    }

    // -----------------------------
    //        UTILIDADES
    // -----------------------------

    private static StringContent WrapStreamTrue(JsonElement body) {
        // Asegura stream=true sin destruir el resto del JSON
        using var doc = JsonDocument.Parse(body.GetRawText());
        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms)) {
            writer.WriteStartObject();
            foreach (var prop in doc.RootElement.EnumerateObject()) {
                prop.WriteTo(writer);
            }
            writer.WriteBoolean("stream", true);
            writer.WriteEndObject();
        }
        return new StringContent(Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8, "application/json");
    }

    private async Task PipeLinesAsSse(HttpResponseMessage upstream, CancellationToken ct) {
        // Si Ollama devolvió error, forward tal cual
        if (!upstream.IsSuccessStatusCode) {
            var err = await upstream.Content.ReadAsStringAsync(ct);
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { error = err })}\n\n", ct);
            return;
        }

        await using var stream = await upstream.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested) {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // reenviamos la línea sin alterar (o con "data: ..." si preferís)
            await Response.WriteAsync($"data: {line}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        await Response.WriteAsync("data: [DONE]\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}