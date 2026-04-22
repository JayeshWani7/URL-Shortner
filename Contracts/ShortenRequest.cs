namespace UrlShortener.Contracts;

public class ShortenRequest
{
    public required string Url { get; set; }
    public string? Alias { get; set; }
}
