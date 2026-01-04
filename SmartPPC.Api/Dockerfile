# SmartPPC.Api Dockerfile
# Multi-stage build for optimized production image

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for better layer caching
COPY ["SmartPPC.sln", "./"]
COPY ["SmartPPC.Api/SmartPPC.Api.csproj", "SmartPPC.Api/"]
COPY ["SmartPPC.Core/SmartPPC.Core.csproj", "SmartPPC.Core/"]
COPY ["SmartPPC.ServiceDefaults/SmartPPC.ServiceDefaults.csproj", "SmartPPC.ServiceDefaults/"]

# Restore dependencies
RUN dotnet restore "SmartPPC.Api/SmartPPC.Api.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/SmartPPC.Api"
RUN dotnet build "SmartPPC.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "SmartPPC.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r smartppc && useradd -r -g smartppc smartppc

# Copy published app
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R smartppc:smartppc /app

# Switch to non-root user
USER smartppc

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "SmartPPC.Api.dll"]
