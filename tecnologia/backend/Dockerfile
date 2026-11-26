# API del SIGH (Sigh.Api): build con el SDK de .NET 10 y ejecución con el runtime de ASP.NET Core 10.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props Sigh.slnx ./
COPY src/ src/
RUN dotnet publish src/Sigh.Api/Sigh.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Imagen Alpine: trae wget para el healthcheck de docker-compose.yml.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Sigh.Api.dll"]
