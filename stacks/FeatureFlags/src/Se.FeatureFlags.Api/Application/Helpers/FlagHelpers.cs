using System.Security.Cryptography;
using System.Text;
using Se.FeatureFlags.Api.Domain;

namespace Se.FeatureFlags.Api.Application.Helpers;

public static class FlagHelpers
{
    public static string ComputeETag(Flag flag)
    {
        var data = $"{flag.App}:{flag.Key}:{flag.Env}:{flag.Version}:{flag.UpdatedAt:O}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}
