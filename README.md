# 📊 Simulador de Crédito -- Hackathon Caixa 2025

Este projeto foi desenvolvido para o **Hackathon Caixa 2025**,
implementando um **simulador de crédito** com cálculo das parcelas (SAC
e PRICE), integração com banco SQL Server e persistência em SQLite.

A aplicação está containerizada em **Docker**, podendo ser executada
tanto localmente via `docker-compose` quanto diretamente pela imagem
publicada no **Docker Hub**.

O Github do projeto é https://github.com/Igormagno13/Simulador.Api

------------------------------------------------------------------------

## 🚀 Como rodar o projeto

### 1️⃣ Rodar via Docker Compose (código fonte)

Clone o repositório e rode:

``` bash
git clone https://github.com/Igormagno13/Simulador.Api.git
cd Simulador.Api
docker compose up -d --build
```

A API ficará disponível em:

    http://localhost:5267/swagger

------------------------------------------------------------------------

### 2️⃣ Rodar direto da imagem no Docker Hub

Sem precisar clonar o código, basta executar:

``` bash
docker pull reddyroxx/simulador-api:1.0.0
docker run -d -p 5267:8080 reddyroxx/simulador-api:1.0.0
```

API disponível em:

    http://localhost:5267/swagger

------------------------------------------------------------------------

## 🔍 Endpoints principais

-   **Health check**

        GET /health/db

-   **Produtos**

        GET /api/produtos/localizar?valorDesejado=300&prazo=3

-   **Simulação**

    -   GET:

            GET /api/simulacao?valorDesejado=300&prazo=3

    -   POST:

        ``` json
        POST /api/simulacao
        {
          "ValorDesejado": 300,
          "Prazo": 3
        }
        ```

-   **Storage**

    -   Listagem de simulações:

            GET /api/storage/simulacoes?pagina=1&qtdRegistrosPagina=200

    -   Volume por produto:

            GET /api/storage/volume-por-produto

    -   Telemetria:

            GET /api/storage/telemetria

------------------------------------------------------------------------

## 🛠️ Tecnologias utilizadas

-   .NET 8 (ASP.NET Core Web API)
-   SQL Server (Azure)
-   SQLite (armazenamento local no container)
-   Docker & Docker Compose
-   Swagger (documentação da API)

------------------------------------------------------------------------

## 📦 Estrutura do projeto

-   `Simulador.Api/` → Código fonte da API\
-   `docker-compose.yml` → Orquestração dos containers\
-   `Dockerfile` → Build da aplicação\
-   `local.db` → Banco SQLite persistido no volume `/data`

------------------------------------------------------------------------

## 👨‍💻 Time & Hackathon

Projeto desenvolvido durante o **Hackathon Caixa 2025**.

Imagem oficial no Docker Hub:\
👉
[reddyroxx/simulador-api:1.0.0](https://hub.docker.com/repository/docker/reddyroxx/simulador-api)
