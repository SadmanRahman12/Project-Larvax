# Multi-stage build for ASP.NET Core 10 Web Application
# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first to leverage Docker layer caching during restore
COPY ["LarvaX.slnx", "./"]
COPY ["LarvaX.Core/LarvaX.Core.csproj", "LarvaX.Core/"]
COPY ["LarvaX.Application/LarvaX.Application.csproj", "LarvaX.Application/"]
COPY ["LarvaX.Infrastructure/LarvaX.Infrastructure.csproj", "LarvaX.Infrastructure/"]
COPY ["LarvaX.Web/LarvaX.Web.csproj", "LarvaX.Web/"]
COPY ["LarvaX.Tests/LarvaX.Tests.csproj", "LarvaX.Tests/"]

# Restore dependencies
RUN dotnet restore "LarvaX.slnx"

# Copy remaining source code
COPY . .

# Publish Release build
RUN dotnet publish "LarvaX.Web/LarvaX.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install fontconfig for QuestPDF / SkiaSharp font rendering support on Linux
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Expose default HTTP port for ASP.NET Core 8+ and Render
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LarvaX.Web.dll"]
