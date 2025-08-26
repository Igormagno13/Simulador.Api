# =========================
# STAGE 1: build
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Simulador.Api.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# =========================
# STAGE 2: runtime
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# (1) Instalar ICU (globalização) e wget (para healthcheck)
#   - Para Debian Bookworm: libicu72
#   - Se em algum momento a base migrar, troque para libicu73 (ou a versão vigente)
RUN apt-get update \
 && apt-get install -y --no-install-recommends libicu72 wget \
 && rm -rf /var/lib/apt/lists/*

# (2) Habilitar globalization completa
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# (3) Variáveis padrão (podem ser sobrescritas no compose)
ENV ASPNETCORE_URLS=http://+:8080
ENV LOCAL_DB_PATH=/data/local.db

# Pasta para o SQLite
RUN mkdir -p /data

# Artefatos
COPY --from=build /app/publish ./

EXPOSE 8080
ENTRYPOINT ["dotnet", "Simulador.Api.dll"]
