namespace UrlShortener.Models;

public class ShortUrlRule
{
    public int Id { get; set; }
    public int ShortUrlId { get; set; }
    public ShortUrl ShortUrl { get; set; } = null!;

    public required string TargetUrl { get; set; }
    public int Priority { get; set; } = 100;

    // Allowed values: any, mobile, desktop
    public string DeviceType { get; set; } = "any";
    public string? CountryCode { get; set; }
    public string? LanguagePrefix { get; set; }

    public DateTime? ActiveFromUtc { get; set; }
    public DateTime? ActiveUntilUtc { get; set; }

    // Optional deterministic bucket range for A/B testing, inclusive, 0-99.
    public int? BucketStart { get; set; }
    public int? BucketEnd { get; set; }

    public int HitCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}