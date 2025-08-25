using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Se.FeatureFlags.Api.Abstractions;
using Se.FeatureFlags.Api.Application.DTOs;
using Se.FeatureFlags.Api.Application.Helpers;
using Se.FeatureFlags.Api.Domain;
using HttpRequest = Microsoft.AspNetCore.Http.HttpRequest;
using HttpResponse = Microsoft.AspNetCore.Http.HttpResponse;
using Results = Microsoft.AspNetCore.Http.Results;

namespace Se.FeatureFlags.Api.Endpoints;

public static class FlagEndpoints
{
    public static void MapFlagEndpoints(this WebApplication app)
    {
        app.MapGet("/flags", GetFlags);
        app.MapGet("/flags/{key}", GetFlag);
        app.MapPost("/flags", CreateFlag);
        app.MapPatch("/flags/{key}", UpdateFlag);
        app.MapDelete("/flags/{key}", DeleteFlag);
    }

    private static async Task<IResult> GetFlags(
        [FromServices] IFlagRepository repo,
        [FromQuery] string app,
        [FromQuery] string? env)
    {
        if (string.IsNullOrWhiteSpace(app))
        {
            return Results.BadRequest("Query parameter 'app' is required.");
        }

        var flags = await repo.GetFlagsAsync(app, env);
        var result = flags.Select(f => new
        {
            f.Key,
            f.Enabled,
            f.Value,
            f.Version,
            f.UpdatedAt,
            ETag = FlagHelpers.ComputeETag(f)
        }).ToList();

        return Results.Ok(result);
    }

    private static async Task<IResult> GetFlag(
        [FromServices] IFlagRepository repo,
        [FromQuery] string app,
        [FromRoute] string key,
        [FromQuery] string? env,
        HttpResponse response)
    {
        if (string.IsNullOrWhiteSpace(app) || string.IsNullOrWhiteSpace(key))
        {
            return Results.BadRequest("'app' and 'key' are required.");
        }

        var flag = await repo.GetFlagAsync(app, key, env);
        if (flag is null)
        {
            return Results.NotFound();
        }

        var etag = FlagHelpers.ComputeETag(flag);
        response.Headers["ETag"] = etag;

        return Results.Ok(flag);
    }

    private static async Task<IResult> CreateFlag(
        [FromServices] IFlagRepository repo,
        [FromBody] Flag flag,
        HttpRequest req,
        HttpResponse response)
    {
        if (string.IsNullOrWhiteSpace(flag.App) || string.IsNullOrWhiteSpace(flag.Key))
        {
            return Results.BadRequest("'App' and 'Key' are required.");
        }

        var keyRegex = new Regex(@"^[a-zA-Z0-9_\-\.]{3,64}$");
        var appRegex = new Regex(@"^[a-zA-Z0-9_\-]{2,32}$");
        var envRegex = new Regex(@"^[a-zA-Z0-9_\-]{2,16}$");

        if (!keyRegex.IsMatch(flag.Key))
        {
            return Results.BadRequest("Invalid flag key format.");
        }

        if (!appRegex.IsMatch(flag.App))
        {
            return Results.BadRequest("Invalid app format.");
        }

        if (!string.IsNullOrEmpty(flag.Env) && !envRegex.IsMatch(flag.Env))
        {
            return Results.BadRequest("Invalid env format.");
        }

        if (req.Headers.TryGetValue("If-Match", out var ifMatch))
        {
            var existing = await repo.GetFlagAsync(flag.App, flag.Key, flag.Env);
            if (existing != null)
            {
                var currentEtag = FlagHelpers.ComputeETag(existing);
                if (ifMatch != currentEtag)
                {
                    return Results.StatusCode(412);
                }
            }
        }

        var upserted = await repo.UpsertFlagAsync(flag);
        var newEtag = FlagHelpers.ComputeETag(upserted);
        response.Headers["ETag"] = newEtag;

        return Results.Ok(upserted);
    }

    private static async Task<IResult> UpdateFlag(
        [FromServices] IFlagRepository repo,
        [FromQuery] string app,
        [FromRoute] string key,
        [FromQuery] string? env,
        [FromBody] FlagPatchDto patch,
        HttpRequest req,
        HttpResponse response)
    {
        if (string.IsNullOrWhiteSpace(app) || string.IsNullOrWhiteSpace(key))
        {
            return Results.BadRequest("'app' and 'key' are required.");
        }

        var existing = await repo.GetFlagAsync(app, key, env);
        if (existing is null)
        {
            return Results.NotFound();
        }

        if (req.Headers.TryGetValue("If-Match", out var ifMatch))
        {
            var currentEtag = FlagHelpers.ComputeETag(existing);
            if (ifMatch != currentEtag)
            {
                return Results.StatusCode(412);
            }
        }

        if (patch.Enabled.HasValue)
        {
            existing.Enabled = patch.Enabled.Value;
        }

        if (patch.Value != null)
        {
            existing.Value = patch.Value;
        }

        var updated = await repo.UpsertFlagAsync(existing);
        var newEtag = FlagHelpers.ComputeETag(updated);
        response.Headers["ETag"] = newEtag;

        return Results.Ok(updated);
    }

    private static async Task<IResult> DeleteFlag(
        [FromServices] IFlagRepository repo,
        [FromQuery] string app,
        [FromRoute] string key,
        [FromQuery] string? env)
    {
        if (string.IsNullOrWhiteSpace(app) || string.IsNullOrWhiteSpace(key))
        {
            return Results.BadRequest("'app' and 'key' are required.");
        }

        var deleted = await repo.DeleteFlagAsync(app, key, env);

        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
