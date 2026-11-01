# ---------- Etapa 1: compilación ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos primero solo los archivos de proyecto para aprovechar la caché de capas:
# mientras no cambien las dependencias, "dotnet restore" no se vuelve a ejecutar.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/SistemaAcademico.Domain/SistemaAcademico.Domain.csproj src/SistemaAcademico.Domain/
COPY src/SistemaAcademico.Application/SistemaAcademico.Application.csproj src/SistemaAcademico.Application/
COPY src/SistemaAcademico.Infrastructure/SistemaAcademico.Infrastructure.csproj src/SistemaAcademico.Infrastructure/
COPY src/SistemaAcademico.Api/SistemaAcademico.Api.csproj src/SistemaAcademico.Api/
RUN dotnet restore src/SistemaAcademico.Api/SistemaAcademico.Api.csproj

COPY src/ src/
RUN dotnet publish src/SistemaAcademico.Api/SistemaAcademico.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Etapa 2: runtime (imagen mínima, sin SDK) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .

# Usuario no root incluido en las imágenes oficiales de .NET (UID 1654).
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "SistemaAcademico.Api.dll"]
