namespace UrlShortener.Models;

public class ShortUrl
{
    public int Id { get; set; }
    public required string OriginalUrl { get; set; }
    public required string ShortCode { get; set; }
    public int Clicks { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ShortUrlRule> Rules { get; set; } = new List<ShortUrlRule>();
}
