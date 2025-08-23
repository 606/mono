using System;
using System.Collections.Generic;
using System.Linq;

namespace Se.Aws.Cost.Guard.Lambda.Domain.Options;

public sealed record CostGuardOptions(
    bool DryRun,
    string Mode,
    IReadOnlyList<string> Regions,
    int LogRetentionDays,
    IReadOnlySet<string> KeepInstanceTypes,
    string? SkipTagKey,
    string? SkipTagValue,
    int SnapshotRetentionDays)
{
    public static CostGuardOptions FromEnv()
    {
        bool.TryParse(Environment.GetEnvironmentVariable("DRY_RUN"), out var dry);
        var mode = Environment.GetEnvironmentVariable("MODE") ?? "SOFT";

        var regions = (Environment.GetEnvironmentVariable("REGIONS") ?? "us-east-1")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var keep = (Environment.GetEnvironmentVariable("KEEP_INSTANCE_TYPES") ?? "t2.micro,t3.micro,t4g.micro")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var skipKey = Environment.GetEnvironmentVariable("SKIP_TAG_KEY");
        var skipVal = Environment.GetEnvironmentVariable("SKIP_TAG_VALUE");

        var retention = int.TryParse(Environment.GetEnvironmentVariable("LOG_RETENTION_DAYS"), out var r) ? r : 3;
        var snapRet = int.TryParse(Environment.GetEnvironmentVariable("SNAPSHOT_RETENTION_DAYS"), out var d) ? d : 7;

        return new CostGuardOptions(
            DryRun: dry,
            Mode: mode,
            Regions: regions,
            LogRetentionDays: retention,
            KeepInstanceTypes: keep,
            SkipTagKey: string.IsNullOrWhiteSpace(skipKey) ? null : skipKey,
            SkipTagValue: string.IsNullOrWhiteSpace(skipVal) ? null : skipVal,
            SnapshotRetentionDays: Math.Max(0, snapRet));
    }

    public bool IsHard => string.Equals(Mode, "HARD", StringComparison.OrdinalIgnoreCase);
}
