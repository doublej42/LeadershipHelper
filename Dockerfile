# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY LeadershipHelper.slnx ./
COPY src/LeadershipHelper.Domain/LeadershipHelper.Domain.csproj               src/LeadershipHelper.Domain/
COPY src/LeadershipHelper.Application/LeadershipHelper.Application.csproj     src/LeadershipHelper.Application/
COPY src/LeadershipHelper.Infrastructure/LeadershipHelper.Infrastructure.csproj src/LeadershipHelper.Infrastructure/
COPY src/LeadershipHelper.Web/LeadershipHelper.Web.csproj                     src/LeadershipHelper.Web/

RUN dotnet restore src/LeadershipHelper.Web/LeadershipHelper.Web.csproj

# Copy everything else and publish
COPY . .
RUN dotnet publish src/LeadershipHelper.Web/LeadershipHelper.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy seed file so startup seeding works
COPY LeadershipJourney.md ./

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "LeadershipHelper.Web.dll"]
