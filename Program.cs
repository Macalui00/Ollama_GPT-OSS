using ollama_gpt.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal class clsProgram {
    private static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        // Controllers
        builder.Services.AddControllers();

        // Tu cliente para streaming generate clásico
        builder.Services.AddSingleton<clsOllamaClient>();

        // HttpClient nombrado para passthrough universal a Ollama
        builder.Services.AddHttpClient("ollama", c => {
            c.BaseAddress = new Uri("http://127.0.0.1:11434");
            c.Timeout = Timeout.InfiniteTimeSpan; // streaming largo
        });

        // CORS (dev: allow all)
        builder.Services.AddCors(options => {
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader()
            );
        });

        var app = builder.Build();

        // CORS
        app.UseCors();

        // Archivos estáticos (wwwroot) + default files (sirve index.html en /)
        app.UseDefaultFiles();   // busca index.html en wwwroot
        app.UseStaticFiles();

        // Mapear controllers (api/*)
        app.MapControllers();

        // Endpoints auxiliares
        app.MapGet("/ping", () => Results.Text("pong"));
        app.MapGet("/_routes", (Microsoft.AspNetCore.Routing.EndpointDataSource es) =>
            Results.Json(es.Endpoints.Select(e => e.DisplayName)));

        // Opcional: si querés que cualquier ruta desconocida devuelva el index (SPA)
        // app.MapFallbackToFile("index.html");

        app.Run();
    }
}