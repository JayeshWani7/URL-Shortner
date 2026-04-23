# UrlShortener

A minimal URL shortener API built with ASP.NET Core, EF Core, and SQLite.

## Features

- Create short URLs from long URLs
- Reuse existing short URL when the same URL is submitted again
- Optional custom alias support
- Redirect from short code to original URL
- Click count tracking
- Adaptive smart-link rules (device, country, language, schedule, A/B bucket)
- Rule hit count tracking
- Resolution preview endpoint for debugging routing behavior
- SQLite persistence with EF Core migrations

## Tech Stack

- ASP.NET Core (.NET 10)
- Entity Framework Core
- SQLite

## Prerequisites

- .NET SDK 10.x
- Optional (for migrations): dotnet-ef tool

## Getting Started

1. Clone the repository.
2. Open a terminal in the project root.
3. Restore dependencies:

```bash
dotnet restore
```

4. Build the project:

```bash
dotnet build
```

5. Install EF CLI tool (one-time on your machine):

```bash
dotnet tool install --global dotnet-ef
```

## Database Migrations

Run these commands from the project root.

1. Create a migration (only when model/schema changes):

```bash
dotnet ef migrations add InitialCreate
```

2. Apply migrations to the database:

```bash
dotnet ef database update
```

If migrations already exist, you usually only need:

```bash
dotnet ef database update
```

## Run the Project

```bash
dotnet run
```

The app will print its listening URL in the terminal.

## API Endpoints

### POST /api/url/shorten

Request body:

```json
{
  "url": "https://google.com",
  "alias": "my-google"
}
```

Notes:

- alias is optional and nullable.
- Alias must match: 3-32 chars, letters/numbers/underscore/hyphen.

Typical responses:

- New short URL created
- URL already shortened (returns existing code)
- Alias already in use

### GET /{code}

Redirects to the original URL and increments click count.

If smart-link rules exist for that short code, the first matching rule (ordered by priority, then id) is used. If no rule matches, the fallback original URL is used.

### POST /api/url/{code}/rules

Adds a smart-link routing rule to an existing short code.

Request body example:

```json
{
  "targetUrl": "https://m.example.com/sale",
  "priority": 10,
  "deviceType": "mobile",
  "countryCode": "IN",
  "languagePrefix": "en",
  "activeFromUtc": "2026-04-23T00:00:00Z",
  "activeUntilUtc": "2026-04-30T23:59:59Z",
  "bucketStart": 0,
  "bucketEnd": 69
}
```

Notes:

- deviceType allowed values: any, mobile, desktop
- countryCode must be a 2-letter code
- bucketStart/bucketEnd are optional and must be provided together (0-99)

### GET /api/url/{code}/rules

Lists all rules for a short code, including hit counts.

### GET /api/url/{code}/resolve-preview

Preview how routing resolves for a mocked context without redirecting.

Supported query params:

- device
- country
- language
- bucket
- atUtc

## Curl Examples

Use your actual port if different from 5070.

Create short URL without alias:

```bash
curl.exe -X POST "http://localhost:5070/api/url/shorten" -H "Content-Type: application/json" -d "{\"url\":\"https://google.com\"}"
```

Create short URL with alias:

```bash
curl.exe -X POST "http://localhost:5070/api/url/shorten" -H "Content-Type: application/json" -d "{\"url\":\"https://google.com\",\"alias\":\"my-google\"}"
```

Check redirect headers:

```bash
curl.exe -i "http://localhost:5070/my-google"
```

Follow redirect:

```bash
curl.exe -L "http://localhost:5070/my-google"
```

Add a smart rule (mobile users in IN, bucket 0-69):

```bash
curl.exe -X POST "http://localhost:5070/api/url/my-google/rules" -H "Content-Type: application/json" -d "{\"targetUrl\":\"https://m.example.com/sale\",\"priority\":10,\"deviceType\":\"mobile\",\"countryCode\":\"IN\",\"bucketStart\":0,\"bucketEnd\":69}"
```

List rules:

```bash
curl.exe "http://localhost:5070/api/url/my-google/rules"
```

Preview route resolution:

```bash
curl.exe "http://localhost:5070/api/url/my-google/resolve-preview?device=mobile&country=IN&language=en-US&bucket=15"
```

## Project Structure

- Program.cs: API route definitions and app setup
- Data/AppDbContext.cs: EF Core DbContext
- Models/ShortUrl.cs: URL entity
- Models/ShortUrlRule.cs: Smart-link rule entity
- Contracts/ShortenRequest.cs: Request contract
- Contracts/CreateRuleRequest.cs: Rule creation request contract
- Services/ShortCodeGenerator.cs: Random short code generator
- Migrations/: EF Core migrations

## Common Commands

```bash
dotnet build
dotnet run
dotnet ef database update
```

## Notes

- SQLite database files are local and ignored via .gitignore.
- Use HTTPS URLs for production deployment and reverse proxy configuration.
