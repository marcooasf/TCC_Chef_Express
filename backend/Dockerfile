# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (only csproj files) so Docker caches the NuGet layer
COPY global.json Directory.Build.props ChefExpress.sln ./
COPY src/ChefExpress.Domain/ChefExpress.Domain.csproj                 src/ChefExpress.Domain/
COPY src/ChefExpress.Application/ChefExpress.Application.csproj       src/ChefExpress.Application/
COPY src/ChefExpress.Infrastructure/ChefExpress.Infrastructure.csproj src/ChefExpress.Infrastructure/
COPY src/ChefExpress.Api/ChefExpress.Api.csproj                       src/ChefExpress.Api/
RUN dotnet restore src/ChefExpress.Api/ChefExpress.Api.csproj

COPY src/ src/
RUN dotnet publish src/ChefExpress.Api/ChefExpress.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "ChefExpress.Api.dll"]
