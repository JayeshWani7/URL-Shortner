using Microsoft.EntityFrameworkCore;
using UrlShortener.Contracts;
using UrlShortener.Data;
using UrlShortener.Models;
using UrlShortener.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=urls.db"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/api/url/shorten", async (ShortenRequest request, AppDbContext dbContext, HttpContext httpContext) =>
{
    if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
    {
        return Results.BadRequest(new { message = "Please provide a valid absolute http/https URL." });
    }

    string code;
    do
    {
        code = ShortCodeGenerator.Generate();
    }
    while (await dbContext.Urls.AnyAsync(url => url.ShortCode == code));

    var shortUrl = new ShortUrl
    {
        OriginalUrl = request.Url,
        ShortCode = code
    };

    dbContext.Urls.Add(shortUrl);
    await dbContext.SaveChangesAsync();

    var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
    return Results.Ok(new
    {
        shortUrl = $"{baseUrl}/{code}",
        code,
        originalUrl = request.Url
    });
})
.WithName("ShortenUrl");

app.MapGet("/{code}", async (string code, AppDbContext dbContext) =>
{
    var shortUrl = await dbContext.Urls.FirstOrDefaultAsync(url => url.ShortCode == code);
    if (shortUrl is null)
    {
        return Results.NotFound(new { message = "Short URL not found." });
    }

    shortUrl.Clicks++;
    await dbContext.SaveChangesAsync();

    return Results.Redirect(shortUrl.OriginalUrl);
})
.WithName("RedirectToOriginal");

app.Run();
