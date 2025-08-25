namespace Se.FeatureFlags.Api.Domain;

public class Flag
{
    public string App { get; set; } = default!;
    public string Env { get; set; } = "prod";
    public string Key { get; set; } = default!;
    public bool Enabled { get; set; }
    public object? Value { get; set; }
    public List<FlagCondition>? Conditions { get; set; }
    public int? Rollout { get; set; }
    public FlagMetadata? Metadata { get; set; }
    public int Version { get; set; } = 1;
    public string? ETag { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
