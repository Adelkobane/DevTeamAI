# syntax=docker/dockerfile:1

# Build context: the DevTeam.Api folder
#   docker build -t devteam-api ./DevTeam.Api

# ---------- Stage 1: build & publish (SDK) ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project file first so the restore layer is cached until dependencies change
COPY DevTeam.Api.csproj ./
RUN dotnet restore DevTeam.Api.csproj

COPY . .
RUN dotnet publish DevTeam.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---------- Stage 2: runtime (ASP.NET) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Listen on 8080 (default for .NET 8+ containers). On Azure App Service for Containers set WEBSITES_PORT=8080.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# Run as the non-root "app" user that ships with the official .NET images
USER $APP_UID

ENTRYPOINT ["dotnet", "DevTeam.Api.dll"]
