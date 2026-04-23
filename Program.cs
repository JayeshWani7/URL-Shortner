using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

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

app.MapPost("/api/url/{code}/rules", async (string code, CreateRuleRequest request, AppDbContext dbContext) =>
{
    var shortUrl = await dbContext.Urls.FirstOrDefaultAsync(url => url.ShortCode == code);
    if (shortUrl is null)
    {
        return Results.NotFound(new { message = "Short URL not found." });
    }

    if (!Uri.TryCreate(request.TargetUrl, UriKind.Absolute, out var targetUri) ||
        (targetUri.Scheme != Uri.UriSchemeHttp && targetUri.Scheme != Uri.UriSchemeHttps))
    {
        return Results.BadRequest(new { message = "TargetUrl must be a valid absolute http/https URL." });
    }

    var normalizedDevice = (request.DeviceType ?? "any").Trim().ToLowerInvariant();
    if (normalizedDevice is not ("any" or "mobile" or "desktop"))
    {
        return Results.BadRequest(new { message = "DeviceType must be one of: any, mobile, desktop." });
    }

    string? normalizedCountry = null;
    if (!string.IsNullOrWhiteSpace(request.CountryCode))
    {
        normalizedCountry = request.CountryCode.Trim().ToUpperInvariant();
        if (!Regex.IsMatch(normalizedCountry, "^[A-Z]{2}$"))
        {
            return Results.BadRequest(new { message = "CountryCode must be a 2-letter ISO code (example: IN, US)." });
        }
    }

    string? normalizedLanguage = null;
    if (!string.IsNullOrWhiteSpace(request.LanguagePrefix))
    {
        normalizedLanguage = request.LanguagePrefix.Trim().ToLowerInvariant();
    }

    if (request.ActiveFromUtc.HasValue && request.ActiveUntilUtc.HasValue && request.ActiveFromUtc > request.ActiveUntilUtc)
    {
        return Results.BadRequest(new { message = "ActiveFromUtc must be less than or equal to ActiveUntilUtc." });
    }

    if (request.BucketStart.HasValue != request.BucketEnd.HasValue)
    {
        return Results.BadRequest(new { message = "BucketStart and BucketEnd must be provided together." });
    }

    if (request.BucketStart is < 0 or > 99 || request.BucketEnd is < 0 or > 99)
    {
        return Results.BadRequest(new { message = "BucketStart/BucketEnd must be between 0 and 99." });
    }

    if (request.BucketStart.HasValue && request.BucketEnd.HasValue && request.BucketStart > request.BucketEnd)
    {
        return Results.BadRequest(new { message = "BucketStart must be less than or equal to BucketEnd." });
    }

    var rule = new ShortUrlRule
    {
        ShortUrlId = shortUrl.Id,
        TargetUrl = targetUri.AbsoluteUri,
        Priority = request.Priority,
        DeviceType = normalizedDevice,
        CountryCode = normalizedCountry,
        LanguagePrefix = normalizedLanguage,
        ActiveFromUtc = request.ActiveFromUtc,
        ActiveUntilUtc = request.ActiveUntilUtc,
        BucketStart = request.BucketStart,
        BucketEnd = request.BucketEnd
    };

    dbContext.UrlRules.Add(rule);
    await dbContext.SaveChangesAsync();

    return Results.Ok(new
    {
        message = "Rule added successfully.",
        shortCode = code,
        rule = new
        {
            rule.Id,
            rule.TargetUrl,
            rule.Priority,
            rule.DeviceType,
            rule.CountryCode,
            rule.LanguagePrefix,
            rule.ActiveFromUtc,
            rule.ActiveUntilUtc,
            rule.BucketStart,
            rule.BucketEnd,
            rule.HitCount
        }
    });
})
.WithName("AddSmartRule");

app.MapGet("/api/url/{code}/rules", async (string code, AppDbContext dbContext) =>
{
    var shortUrl = await dbContext.Urls.FirstOrDefaultAsync(url => url.ShortCode == code);
    if (shortUrl is null)
    {
        return Results.NotFound(new { message = "Short URL not found." });
    }

    var rules = await dbContext.UrlRules
        .Where(rule => rule.ShortUrlId == shortUrl.Id)
        .OrderBy(rule => rule.Priority)
        .ThenBy(rule => rule.Id)
        .Select(rule => new
        {
            rule.Id,
            rule.TargetUrl,
            rule.Priority,
            rule.DeviceType,
            rule.CountryCode,
            rule.LanguagePrefix,
            rule.ActiveFromUtc,
            rule.ActiveUntilUtc,
            rule.BucketStart,
            rule.BucketEnd,
            rule.HitCount
        })
        .ToListAsync();

    return Results.Ok(new
    {
        shortCode = code,
        fallbackUrl = shortUrl.OriginalUrl,
        rules
    });
})
.WithName("ListSmartRules");

app.MapGet("/api/url/{code}/resolve-preview", async (
    string code,
    string? device,
    string? country,
    string? language,
    int? bucket,
    DateTime? atUtc,
    AppDbContext dbContext) =>
{
    var shortUrl = await dbContext.Urls.FirstOrDefaultAsync(url => url.ShortCode == code);
    if (shortUrl is null)
    {
        return Results.NotFound(new { message = "Short URL not found." });
    }

    var context = new RequestContext(
        NormalizeDevice(device) ?? "desktop",
        string.IsNullOrWhiteSpace(country) ? null : country.Trim().ToUpperInvariant(),
        string.IsNullOrWhiteSpace(language) ? null : language.Trim().ToLowerInvariant(),
        bucket ?? 0,
        atUtc ?? DateTime.UtcNow);

    var rules = await dbContext.UrlRules
        .Where(rule => rule.ShortUrlId == shortUrl.Id)
        .OrderBy(rule => rule.Priority)
        .ThenBy(rule => rule.Id)
        .ToListAsync();

    var matchedRule = rules.FirstOrDefault(rule => RuleMatches(rule, context));

    return Results.Ok(new
    {
        shortCode = code,
        context = new
        {
            context.Device,
            context.Country,
            context.Language,
            context.Bucket,
            context.NowUtc
        },
        matchedRuleId = matchedRule?.Id,
        matchedTarget = matchedRule?.TargetUrl,
        fallbackTarget = shortUrl.OriginalUrl,
        finalTarget = matchedRule?.TargetUrl ?? shortUrl.OriginalUrl
    });
})
.WithName("PreviewSmartResolution");

app.MapGet("/{code}", async (string code, AppDbContext dbContext, HttpContext httpContext) =>
{
    var shortUrl = await dbContext.Urls.FirstOrDefaultAsync(url => url.ShortCode == code);
    if (shortUrl is null)
    {
        return Results.NotFound(new { message = "Short URL not found." });
    }

    var requestContext = BuildRequestContext(httpContext, code);

    var rules = await dbContext.UrlRules
        .Where(rule => rule.ShortUrlId == shortUrl.Id)
        .OrderBy(rule => rule.Priority)
        .ThenBy(rule => rule.Id)
        .ToListAsync();

    var matchedRule = rules.FirstOrDefault(rule => RuleMatches(rule, requestContext));
    var destination = matchedRule?.TargetUrl ?? shortUrl.OriginalUrl;

    shortUrl.Clicks++;
    if (matchedRule is not null)
    {
        matchedRule.HitCount++;
    }

    await dbContext.SaveChangesAsync();

    return Results.Redirect(destination);
})
.WithName("RedirectToOriginal");

app.Run();

static RequestContext BuildRequestContext(HttpContext httpContext, string code)
{
    var userAgent = httpContext.Request.Headers.UserAgent.ToString();
    var device = IsMobileUserAgent(userAgent) ? "mobile" : "desktop";

    var country = httpContext.Request.Headers["CF-IPCountry"].ToString();
    if (string.IsNullOrWhiteSpace(country))
    {
        country = httpContext.Request.Headers["X-Country-Code"].ToString();
    }

    country = string.IsNullOrWhiteSpace(country) ? null : country.Trim().ToUpperInvariant();

    var acceptLanguage = httpContext.Request.Headers.AcceptLanguage.ToString();
    var language = ParseLanguagePrefix(acceptLanguage);

    var key = $"{code}|{httpContext.Connection.RemoteIpAddress}|{userAgent}";
    var bucket = GetDeterministicBucket(key);

    return new RequestContext(device, country, language, bucket, DateTime.UtcNow);
}

static bool RuleMatches(ShortUrlRule rule, RequestContext context)
{
    if (rule.ActiveFromUtc.HasValue && context.NowUtc < rule.ActiveFromUtc.Value)
    {
        return false;
    }

    if (rule.ActiveUntilUtc.HasValue && context.NowUtc > rule.ActiveUntilUtc.Value)
    {
        return false;
    }

    var requiredDevice = NormalizeDevice(rule.DeviceType) ?? "any";
    if (requiredDevice != "any" && requiredDevice != context.Device)
    {
        return false;
    }

    if (!string.IsNullOrWhiteSpace(rule.CountryCode) &&
        !string.Equals(rule.CountryCode, context.Country, StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    if (!string.IsNullOrWhiteSpace(rule.LanguagePrefix))
    {
        if (string.IsNullOrWhiteSpace(context.Language) ||
            !context.Language.StartsWith(rule.LanguagePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
    }

    if (rule.BucketStart.HasValue && rule.BucketEnd.HasValue)
    {
        if (context.Bucket < rule.BucketStart.Value || context.Bucket > rule.BucketEnd.Value)
        {
            return false;
        }
    }

    return true;
}

static int GetDeterministicBucket(string key)
{
    var bytes = Encoding.UTF8.GetBytes(key);
    var hash = SHA256.HashData(bytes);
    return hash[0] % 100;
}

static bool IsMobileUserAgent(string userAgent)
{
    if (string.IsNullOrWhiteSpace(userAgent))
    {
        return false;
    }

    return Regex.IsMatch(userAgent, "Mobile|Android|iPhone|iPad", RegexOptions.IgnoreCase);
}

static string? ParseLanguagePrefix(string acceptLanguage)
{
    if (string.IsNullOrWhiteSpace(acceptLanguage))
    {
        return null;
    }

    var first = acceptLanguage
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .FirstOrDefault();

    if (string.IsNullOrWhiteSpace(first))
    {
        return null;
    }

    var language = first.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .FirstOrDefault();

    return string.IsNullOrWhiteSpace(language) ? null : language.ToLowerInvariant();
}

static string? NormalizeDevice(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw))
    {
        return null;
    }

    var value = raw.Trim().ToLowerInvariant();
    return value is "any" or "mobile" or "desktop" ? value : null;
}

record RequestContext(string Device, string? Country, string? Language, int Bucket, DateTime NowUtc);
