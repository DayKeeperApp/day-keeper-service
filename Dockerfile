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

# Create non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .

# Create logs directory owned by appuser
RUN mkdir -p /app/logs && chown -R appuser:appgroup /app/logs

USER appuser

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "DayKeeper.Api.dll"]
