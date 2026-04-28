namespace UrlShortener.Contracts;

public class CreateRuleRequest
{
    public required string TargetUrl { get; set; }
    public int Priority { get; set; } = 100;
    public string? DeviceType { get; set; }
    public string? CountryCode { get; set; }
    public string? LanguagePrefix { get; set; }
    public int? BucketStart { get; set; }
    public int? BucketEnd { get; set; }
}