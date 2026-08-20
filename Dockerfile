# Use the official .NET 9 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 8080

# Install ICU libraries to enable globalization (required for SQL Server culture support)
# and healthcheck dependencies
RUN apk add --no-cache curl icu-libs icu-data-full

# Create non-root user
RUN addgroup -g 1001 -S dotnet && \
    adduser -S -D -H -u 1001 -h /app -s /sbin/nologin -G dotnet dotnet

# Set environment variables for production
# DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false enables full globalization support
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_PRINT_TELEMETRY_MESSAGE=false \
    DOTNET_DbgEnableMiniDump=false \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    COMPlus_EnableDiagnostics=0

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["src/SkillLedger.Api/SkillLedger.Api.csproj", "src/SkillLedger.Api/"]
COPY ["src/SkillLedger.Core/SkillLedger.Core.csproj", "src/SkillLedger.Core/"]
COPY ["src/SkillLedger.Infrastructure/SkillLedger.Infrastructure.csproj", "src/SkillLedger.Infrastructure/"]

RUN dotnet restore "src/SkillLedger.Api/SkillLedger.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/SkillLedger.Api"
RUN dotnet build "SkillLedger.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SkillLedger.Api.csproj" -c Release -o /app/publish \
    --self-contained false \
    --runtime linux-musl-x64 \
    /p:UseAppHost=false

# Build the runtime image
FROM base AS final
WORKDIR /app

# Create directories for HTTPS certificates and logs
RUN mkdir -p /https /app/logs && \
    chown -R dotnet:dotnet /app /https

# Copy the published app
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R dotnet:dotnet /app

# Switch to non-root user
USER dotnet

# Health check
HEALTHCHECK --interval=5m --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "SkillLedger.Api.dll"]
