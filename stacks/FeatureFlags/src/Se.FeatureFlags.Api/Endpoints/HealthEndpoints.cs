namespace Se.FeatureFlags.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

        app.MapGet("/version", () =>
        {
            var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
            return Results.Ok(new { version });
        });

        app.MapGet("/metrics", () =>
        {
            var metrics = "# HELP featureflags_up 1 if up\n# TYPE featureflags_up gauge\nfeatureflags_up 1";
            return Results.Text(metrics, "text/plain");
        });
    }
}
