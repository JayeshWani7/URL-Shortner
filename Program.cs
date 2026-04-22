using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
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

    if (!app.Environment.IsProduction())
    {
        app.UseHttpsRedirection();
    }

app.MapPost("/api/url/shorten", async (ShortenRequest request, AppDbContext dbContext, HttpContext httpContext) =>
{
    if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
    {
        return Results.BadRequest(new { message = "Please provide a valid absolute http/https URL." });
    }

    var normalizedOriginalUrl = uri.AbsoluteUri;
    var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
    var alias = string.IsNullOrWhiteSpace(request.Alias) ? null : request.Alias.Trim();

    if (alias is not null && !Regex.IsMatch(alias, "^[a-zA-Z0-9_-]{3,32}$"))
    {
        return Results.BadRequest(new
        {
            message = "Alias must be 3-32 characters and contain only letters, numbers, underscore, or hyphen."
        });
    }

    var existingShortUrl = await dbContext.Urls.FirstOrDefaultAsync(url => url.OriginalUrl == normalizedOriginalUrl);
    if (existingShortUrl is not null)
    {
        if (alias is not null && !string.Equals(existingShortUrl.ShortCode, alias, StringComparison.Ordinal))
        {
            return Results.BadRequest(new
            {
                message = "URL is already shortened with a different code.",
                shortUrl = $"{baseUrl}/{existingShortUrl.ShortCode}",
                code = existingShortUrl.ShortCode,
                originalUrl = existingShortUrl.OriginalUrl,
                alreadyShortened = true
            });
        }

        return Results.Ok(new
        {
            message = "URL is already shortened.",
            shortUrl = $"{baseUrl}/{existingShortUrl.ShortCode}",
            code = existingShortUrl.ShortCode,
            originalUrl = existingShortUrl.OriginalUrl,
            alreadyShortened = true
        });
    }

    string code;
    if (alias is not null)
    {
        var aliasExists = await dbContext.Urls.AnyAsync(url => url.ShortCode == alias);
        if (aliasExists)
        {
            return Results.BadRequest(new { message = "Alias is already in use. Please choose another one." });
        }

        code = alias;
    }
    else
    {
        do
        {
            code = ShortCodeGenerator.Generate();
        }
        while (await dbContext.Urls.AnyAsync(url => url.ShortCode == code));
    }

    var shortUrl = new ShortUrl
    {
        OriginalUrl = normalizedOriginalUrl,
        ShortCode = code
    };

    dbContext.Urls.Add(shortUrl);
    await dbContext.SaveChangesAsync();

    return Results.Ok(new
    {
        message = "Short URL created successfully.",
        shortUrl = $"{baseUrl}/{code}",
        code,
        originalUrl = normalizedOriginalUrl,
        alreadyShortened = false
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
