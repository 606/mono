namespace Se.FeatureFlags.Api.Domain;

public class FlagCondition
{
    public string Target { get; set; } = default!; // user|segment|percent
    public string Op { get; set; } = default!; // in|not_in|eq|lt|gt|between|match
    public List<object> Args { get; set; } = [];
}
