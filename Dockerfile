# ── Stage 1: Restore (cacheable layer) ────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

# Copy solution and project files first for layer caching
COPY DayKeeper.slnx ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./

COPY src/DayKeeper.Domain/DayKeeper.Domain.csproj src/DayKeeper.Domain/
COPY src/DayKeeper.Application/DayKeeper.Application.csproj src/DayKeeper.Application/
COPY src/DayKeeper.Infrastructure/DayKeeper.Infrastructure.csproj src/DayKeeper.Infrastructure/
COPY src/DayKeeper.Api/DayKeeper.Api.csproj src/DayKeeper.Api/
COPY tests/DayKeeper.Api.Tests/DayKeeper.Api.Tests.csproj tests/DayKeeper.Api.Tests/

RUN dotnet restore DayKeeper.slnx

# ── Stage 2: Build & Publish ─────────────────────────────────
FROM restore AS build
COPY src/ src/
RUN dotnet publish src/DayKeeper.Api/DayKeeper.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 3: Runtime ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Use the built-in non-root user from Microsoft's base image
USER $APP_UID

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "DayKeeper.Api.dll"]
