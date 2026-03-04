# ── Stage 1: Restore (cacheable layer) ────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

# Copy solution and project files first for layer caching
COPY .editorconfig ./
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

# Workaround: MSBuild glob expansion fails on overlay2 when bin/Debug
# is missing (restore creates obj/ refs to it). Create the directory so
# the **/*.resx glob resolves instead of being treated as a literal path.
RUN mkdir -p src/DayKeeper.Api/bin/Debug \
    && dotnet publish src/DayKeeper.Api/DayKeeper.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 3: Migration Bundle ────────────────────────────────
FROM restore AS bundle

# Copy tool manifest before source so this layer is only invalidated
# when the tool version changes, not on every source change.
COPY .config/ .config/
RUN dotnet tool restore

COPY src/ src/
RUN mkdir -p src/DayKeeper.Api/bin/Debug \
    && dotnet ef migrations bundle \
    --project src/DayKeeper.Infrastructure/DayKeeper.Infrastructure.csproj \
    --startup-project src/DayKeeper.Api/DayKeeper.Api.csproj \
    --force \
    --self-contained \
    --output /app/efbundle

# ── Stage 4: Runtime ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Copy self-contained migration bundle (run via: /app/efbundle --connection "...")
COPY --from=bundle /app/efbundle ./efbundle
RUN chmod +x ./efbundle

# Create attachment storage directory with correct ownership before
# switching to non-root. VOLUME must follow chown so ownership is
# baked into the volume seed layer.
RUN mkdir -p /data/attachments \
    && chown -R "${APP_UID}:${APP_UID}" /data/attachments

USER $APP_UID

VOLUME /data/attachments

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "DayKeeper.Api.dll"]
