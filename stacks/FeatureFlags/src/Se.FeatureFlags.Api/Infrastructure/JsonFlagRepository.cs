using System.Text.Json;
using Se.FeatureFlags.Api.Abstractions;
using Se.FeatureFlags.Api.Domain;

namespace Se.FeatureFlags.Api.Infrastructure;

public class JsonFlagRepository : IFlagRepository
{
    private readonly string filePath;
    private readonly SemaphoreSlim lockObj = new(1, 1);
    private readonly TimeSpan cacheTtl;
    private List<Flag> cache = [];
    private DateTime lastLoad = DateTime.MinValue;

    public JsonFlagRepository(string filePath, int cacheTtlSeconds = 30)
    {
        this.filePath = filePath;
        cacheTtl = TimeSpan.FromSeconds(cacheTtlSeconds);
    }

    public async Task<List<Flag>> GetFlagsAsync(string app, string? env = null)
    {
        await EnsureCacheAsync();
        return cache.Where(f => f.App == app && (env == null || f.Env == env)).ToList();
    }

    public async Task<Flag?> GetFlagAsync(string app, string key, string? env = null)
    {
        await EnsureCacheAsync();
        return cache.FirstOrDefault(f => f.App == app && f.Key == key && (env == null || f.Env == env));
    }

    public async Task<Flag> UpsertFlagAsync(Flag flag)
    {
        await lockObj.WaitAsync();
        try
        {
            await EnsureCacheAsync(force: true);
            var idx = cache.FindIndex(f => f.App == flag.App && f.Key == flag.Key && f.Env == flag.Env);
            if (idx >= 0)
            {
                flag.Version = cache[idx].Version + 1;
                flag.CreatedAt = cache[idx].CreatedAt;
                flag.UpdatedAt = DateTime.UtcNow;
                cache[idx] = flag;
            }
            else
            {
                flag.Version = 1;
                flag.CreatedAt = DateTime.UtcNow;
                flag.UpdatedAt = DateTime.UtcNow;
                cache.Add(flag);
            }

            await SaveAsync();
            return flag;
        }
        finally
        {
            lockObj.Release();
        }
    }

    public async Task<bool> DeleteFlagAsync(string app, string key, string? env = null)
    {
        await lockObj.WaitAsync();
        try
        {
            await EnsureCacheAsync(force: true);
            var removed = cache.RemoveAll(f => f.App == app && f.Key == key && (env == null || f.Env == env));
            if (removed > 0)
            {
                await SaveAsync();
                return true;
            }

            return false;
        }
        finally
        {
            lockObj.Release();
        }
    }

    private async Task EnsureCacheAsync(bool force = false)
    {
        if (!force && (DateTime.UtcNow - lastLoad) < cacheTtl && cache.Count > 0)
        {
            return;
        }

        if (!File.Exists(filePath))
        {
            cache = [];
            lastLoad = DateTime.UtcNow;
            return;
        }

        var json = await File.ReadAllTextAsync(filePath);
        cache = JsonSerializer.Deserialize<List<Flag>>(json) ?? [];
        lastLoad = DateTime.UtcNow;
    }

    private async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
        lastLoad = DateTime.UtcNow;
    }
}
