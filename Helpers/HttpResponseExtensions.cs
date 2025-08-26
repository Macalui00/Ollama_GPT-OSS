using Microsoft.AspNetCore.Http;

namespace ollama_gpt.Helpers {
    public static class HttpResponseExtensions {
        /// <summary>
        /// Configura los headers para una respuesta SSE (Server-Sent Events).
        /// </summary>
        public static void SetSseHeaders(this HttpResponse response) {
            response.StatusCode = StatusCodes.Status200OK;
            response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            response.Headers.Pragma = "no-cache";
            response.Headers.Expires = "0";
            response.Headers.Connection = "keep-alive";
            response.Headers["X-Accel-Buffering"] = "no"; // desactiva buffering en proxies
            response.ContentType = "text/event-stream";
        }

        /// <summary>
        /// Configura headers básicos para streaming de texto plano.
        /// </summary>
        public static void SetPlainStreamHeaders(this HttpResponse response) {
            response.StatusCode = StatusCodes.Status200OK;
            response.Headers.CacheControl = "no-cache";
            response.Headers.Connection = "keep-alive";
            response.ContentType = "text/plain; charset=utf-8";
        }

        /// <summary>
        /// Configura headers estándar para respuestas JSON.
        /// </summary>
        public static void SetJsonHeaders(this HttpResponse response) {
            response.StatusCode = StatusCodes.Status200OK;
            response.Headers.CacheControl = "no-cache";
            response.ContentType = "application/json; charset=utf-8";
        }
    }
}
