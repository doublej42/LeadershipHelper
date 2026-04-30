# Leadership Helper

A passwordless leadership coaching tool built with ASP.NET Core MVC, EF Core, and SQL Server.
Users log in via a one-time email code, browse/create leadership situations, work through reflection
prompts, and track their experiences over time.

---

## Project layout

| Path | Purpose |
|------|---------|
| `src/LeadershipHelper.Web` | MVC app (controllers, views, entry point) |
| `src/LeadershipHelper.Application` | Application services and contracts |
| `src/LeadershipHelper.Domain` | Domain entities |
| `src/LeadershipHelper.Infrastructure` | EF Core, SQL Server, Azure Email, seeding |
| `tests/LeadershipHelper.IntegrationTests` | Test project scaffold |

---

## Docker deployment (recommended)

### Prerequisites

- Docker Engine 24+ and Docker Compose v2
- An **Azure Communication Services** resource with a verified email sender domain
	(used to send one-time login codes)

### 1 — Copy and edit the environment file

```bash
cp .env.example .env
nano .env          # or any editor
```

Fill in every value in `.env`:

| Variable | Description |
|----------|-------------|
| `DB_SA_PASSWORD` | SQL Server SA password (min 8 chars, upper + lower + digit + symbol) |
| `ACS_CONNECTION_STRING` | Azure Communication Services connection string from the Azure portal |
| `ACS_EMAIL_FROM` | Verified sender address in your ACS email domain |
| `APP_PORT` | Host port to expose the app on (default `8080`) |

> **Security:** Never commit `.env` to source control. It is listed in `.gitignore`.

### 2 — Build and start

```bash
docker compose up -d --build
```

This will:
1. Build the .NET 10 application image.
2. Start a SQL Server 2022 container (`db`) and wait for it to be healthy.
3. Start the web app (`web`), which automatically applies EF Core migrations and
	 seeds situations from `LeadershipJourney.md` on first boot.

The app will be available at `http://your-server-ip:8080` (or whichever `APP_PORT` you set).

### 3 — Put it behind a reverse proxy (optional but recommended)

To serve over HTTPS with a domain name, add a reverse proxy such as **Nginx Proxy Manager**,
**Caddy**, or **Traefik** in front of the `web` container. Point it at port `8080` on the
`web` service and handle TLS termination there.

Example Nginx location block:

```nginx
location / {
		proxy_pass         http://localhost:8080;
		proxy_http_version 1.1;
		proxy_set_header   Host $host;
		proxy_set_header   X-Real-IP $remote_addr;
		proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
		proxy_set_header   X-Forwarded-Proto $scheme;
}
```

### 4 — Updating to a new version

```bash
git pull
docker compose up -d --build
```

Migrations are applied automatically on startup, so no manual migration step is needed.

### 5 — Viewing logs

```bash
docker compose logs -f web   # app logs
docker compose logs -f db    # SQL Server logs
```

### 6 — Stopping / removing

```bash
docker compose down          # stop containers, keep data volume
docker compose down -v       # stop containers AND delete the database volume
```

---

## Local development

### Prerequisites

- .NET SDK 10
- SQL Server (local instance, Docker, or remote)

### 1 — Set user secrets

Run from the repo root:

```bash
cd src/LeadershipHelper.Web
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Database=LeadershipHelper;User Id=sa;Password=YourPassword;Encrypt=True;TrustServerCertificate=True"
dotnet user-secrets set "Acs:ConnectionString" "endpoint=https://YOUR_ACS_RESOURCE.communication.azure.com/;accesskey=YOUR_KEY"
dotnet user-secrets set "Acs:EmailFrom" "DoNotReply@yourdomain.com"
```

### 2 — Run

```bash
dotnet run --project src/LeadershipHelper.Web
```

On startup the app applies migrations and seeds `LeadershipJourney.md` automatically.

---

## Architecture notes

- Passwordless auth: users receive a 6-digit OTP by email via Azure Communication Services.
- No passwords are stored. Sessions are 30-day persistent cookies.
- EF Core migrations run automatically on startup — no manual `dotnet ef database update` needed in production.
- Seed data is loaded from `LeadershipJourney.md` at startup (idempotent — safe to run multiple times).
