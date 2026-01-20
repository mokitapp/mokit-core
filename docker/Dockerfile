# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY MockHub.sln .
COPY src/MockHub.Domain/MockHub.Domain.csproj src/MockHub.Domain/
COPY src/MockHub.Application/MockHub.Application.csproj src/MockHub.Application/
COPY src/MockHub.Infrastructure/MockHub.Infrastructure.csproj src/MockHub.Infrastructure/
COPY src/MockHub.MockEngine/MockHub.MockEngine.csproj src/MockHub.MockEngine/
COPY src/MockHub.HostManager/MockHub.HostManager.csproj src/MockHub.HostManager/
COPY src/MockHub.Web/MockHub.Web.csproj src/MockHub.Web/

# Restore packages
RUN dotnet restore

# Copy source code
COPY src/ src/

# Build and publish
WORKDIR /src/src/MockHub.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create data directory for SQLite
RUN mkdir -p /app/data

# Copy published app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/mockhub.db"

# Expose ports
# Main web UI port
EXPOSE 5000
# Mock server ports range
EXPOSE 5100-6000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "MockHub.Web.dll"]


