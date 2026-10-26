# syntax=docker/dockerfile:1

# One image serving the Files service: the .NET host with the Form 1 module, and the hosted pages and console. The
# build context MUST be engineering/codebase/, which holds both files.backend-services and files.frontend-services.
#
#   docker build --build-arg APP_VERSION=0.1.99 --build-arg VCS_REF=$(git rev-parse HEAD) \
#     -f engineering/deployment/Dockerfile -t files:local engineering/codebase
#
# The image holds no environment: the SQLite path (a file on the /data volume) arrives at run time, through the
# environment's settings file (/app/appsettings.Target.json) or the DB_CONNECTION variable.

# ---- Stage 1: build the frontend workspace (the fe-files package first, then apps/web) ----
FROM node:24.11.0-alpine AS spa
RUN npm install --global pnpm@10.33.2
WORKDIR /src/files.frontend-services
COPY files.frontend-services/package.json files.frontend-services/pnpm-lock.yaml files.frontend-services/pnpm-workspace.yaml ./
COPY files.frontend-services/apps/web/package.json ./apps/web/package.json
COPY files.frontend-services/packages/fe-files/package.json ./packages/fe-files/package.json
RUN pnpm install --frozen-lockfile
COPY files.frontend-services/ ./
# The build's version from the shared publish workflow (X.Y.Z or a pre-release); unset in a local build. Declared
# after the dependency layers, since a changed build argument misses the cache of every RUN after it.
ARG APP_VERSION
RUN pnpm build

# ---- Stage 2: publish the .NET service with its packages (the pages arrive from stage 1) ----
# Stays in global.json's 10.0.3xx band; the floating 10.0 tag moved to 10.0.4xx, which the pin refuses.
FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
ARG TARGETARCH
WORKDIR /src
# packages/ and service/ build together: the service takes the domain's packages by ProjectReference.
COPY files.backend-services/ ./
COPY --from=spa /src/files.frontend-services/apps/web/dist ./service/Files.Api/wwwroot
# MSBuild reads APP_VERSION from the environment; Directory.Build.props turns it into the version. VCS_REF is the full
# commit SHA the shared publish workflow passes; the build appends it to the informational version.
ARG APP_VERSION
ARG VCS_REF
RUN case "$TARGETARCH" in amd64) rid=linux-x64 ;; arm64) rid=linux-arm64 ;; *) exit 1 ;; esac \
    && dotnet publish service/Files.Api/Files.Api.csproj -c Release -o /app -r "$rid" --self-contained false \
       -p:BuildSpa=false -p:SourceRevisionId="$VCS_REF"

# ---- Stage 3: runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app ./

# The SQLite file lives on a volume so data survives container replacement; the volume inherits this ownership.
RUN mkdir -p /data && chown app:app /data

ENV ASPNETCORE_HTTP_PORTS=8080
USER app
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=5s --start-period=60s --retries=4 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Files.Api.dll"]
