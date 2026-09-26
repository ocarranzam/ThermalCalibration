# Imagen de la Web API para desarrollo y demostración con docker compose.
# Producción sigue siendo un servicio de Windows en la PC del laboratorio (acceso al puerto COM).
# Contexto de construcción: la raíz del repositorio (usa Directory.Build.props y Directory.Packages.props).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/Thermal.Domain/Thermal.Domain.csproj src/Thermal.Domain/
COPY src/Thermal.Application/Thermal.Application.csproj src/Thermal.Application/
COPY src/Thermal.Infrastructure/Thermal.Infrastructure.csproj src/Thermal.Infrastructure/
COPY src/Thermal.Api/Thermal.Api.csproj src/Thermal.Api/
RUN dotnet restore src/Thermal.Api/Thermal.Api.csproj

COPY src/ src/
RUN dotnet publish src/Thermal.Api/Thermal.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "Thermal.Api.dll"]
