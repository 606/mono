using Se.FeatureFlags.Api.Domain;

namespace Se.FeatureFlags.Api.Abstractions;

public interface IFlagRepository
{
    Task<List<Flag>> GetFlagsAsync(string app, string? env = null);
    Task<Flag?> GetFlagAsync(string app, string key, string? env = null);
    Task<Flag> UpsertFlagAsync(Flag flag);
    Task<bool> DeleteFlagAsync(string app, string key, string? env = null);
}
