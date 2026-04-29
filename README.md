# Leadership Helper

ASP.NET Core MVC (`net10.0`) starter for Leadership Helper using SQL Server on `frankie` for development.

## Architecture choices

- MVC only (`Controllers` + `Views`)
- No Razor Pages
- EF Core SQL Server persistence
- Startup seeding from `LeadershipJourney.md`
- Passwordless OTP flow scaffolded (ACS integration placeholder)

## Prerequisites

- .NET SDK 10
- Access to SQL Server `frankie`

## Project layout

- `src/LeadershipHelper.Web` MVC app
- `src/LeadershipHelper.Application` application services and contracts
- `src/LeadershipHelper.Domain` domain entities
- `src/LeadershipHelper.Infrastructure` EF Core, SQL wiring, seeding
- `tests/LeadershipHelper.IntegrationTests` test project scaffold

## Private secret setup (local only)

Run these from `src/LeadershipHelper.Web`:

1. `dotnet user-secrets init`
2. `dotnet user-secrets set "ConnectionStrings:Default" "Server=FRANKIE;Database=LeadershipHelper;User Id=APP_USER;Password=APP_PASSWORD;Encrypt=True;TrustServerCertificate=True"`
3. `dotnet user-secrets set "Acs:ConnectionString" "endpoint=https://YOUR_ACS_RESOURCE.communication.azure.com/;accesskey=YOUR_ACS_ACCESS_KEY"`
4. `dotnet user-secrets set "Acs:SmsFrom" "+1YOUR_SMS_NUMBER"`
5. `dotnet user-secrets set "Acs:EmailFrom" "DoNotReply@YOUR_DOMAIN.com"`
6. `dotnet user-secrets set "Auth:CookieSigningKey" "YOUR_LONG_RANDOM_SIGNING_KEY"`

## Run

1. `dotnet build LeadershipHelper.slnx`
2. `dotnet run --project src/LeadershipHelper.Web/LeadershipHelper.Web.csproj`

On startup, the app applies migrations and seeds leadership situations from `LeadershipJourney.md`.

## Notes

- Current OTP request endpoint returns a development-only sample code in the response. Replace this with Azure Communication Services delivery before production.
- Ensure a `LeadershipHelper` database exists and your app login has migration rights during early setup.
