# UrlShortener

A minimal URL shortener API built with ASP.NET Core, EF Core, and SQLite.

## Features

- Create short URLs from long URLs
- Reuse existing short URL when the same URL is submitted again
- Optional custom alias support
- Redirect from short code to original URL
- Click count tracking
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

## Project Structure

- Program.cs: API route definitions and app setup
- Data/AppDbContext.cs: EF Core DbContext
- Models/ShortUrl.cs: URL entity
- Contracts/ShortenRequest.cs: Request contract
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
