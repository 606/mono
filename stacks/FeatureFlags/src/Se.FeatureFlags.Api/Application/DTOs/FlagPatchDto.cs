namespace Se.FeatureFlags.Api.Application.DTOs;

public class FlagPatchDto
{
    public bool? Enabled { get; set; }
    public object? Value { get; set; }
}
