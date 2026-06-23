# DeveloperStore Sales API

[![CI](https://github.com/JailtonJunior94/DeveloperStore/actions/workflows/ci.yml/badge.svg)](https://github.com/JailtonJunior94/DeveloperStore/actions/workflows/ci.yml)
![.NET Version](https://img.shields.io/badge/.NET-10.0-blue?style=flat-square&logo=dotnet)
![C# Version](https://img.shields.io/badge/C%23-14-green?style=flat-square&logo=csharp)
![Architecture](https://img.shields.io/badge/Architecture-Clean--DDD-orange?style=flat-square)
![Database](https://img.shields.io/badge/Database-PostgreSQL-blue?style=flat-square&logo=postgresql)
![Docker](https://img.shields.io/badge/Docker-Supported-blue?style=flat-square&logo=docker)
![License](https://img.shields.io/badge/License-MIT-lightgrey?style=flat-square)

API de vendas construída para o teste técnico da DeveloperStore, com foco em robustez, DDD, validação semântica, contratos HTTP consistentes e modelagem inspirada em `Domain Modeling Made Functional`.

Repositório público: `https://github.com/JailtonJunior94/DeveloperStore`

## O que foi construído

Foi implementada uma API completa para gerenciamento de vendas, cobrindo:

- criação de venda
- consulta de venda por id
- listagem paginada com filtros e ordenação
- atualização de venda
- cancelamento lógico de venda
- cancelamento de item de venda
- publicação de eventos de domínio via log

Cada venda armazena:

- número da venda
- data da venda
- cliente
- filial
- itens vendidos
- quantidade por item
- preço unitário
- desconto aplicado
- total por item
- total consolidado da venda
- status da venda

## Escopo implementado

O escopo implementado neste repositório é `Sales API`.

Isso significa que a solução entregue cobre o domínio de vendas e suas evidências técnicas correspondentes:

- domínio de vendas
- endpoints de vendas
- persistência de vendas
- testes de vendas

Arquivos em `.doc` que descrevem `auth`, `users`, `products` e `carts` devem ser lidos como documentação de referência mais ampla da plataforma, não como prova automática de que esses módulos existem neste repositório.

## Como interpretar prontidão e evidência

Neste projeto, uma afirmação de prontidão só é considerada válida quando existe evidência objetiva executada sobre o estado atual do código.

Na prática, isso significa:

- build limpo
- testes relevantes verdes
- prova em PostgreSQL real para comportamento sensível a persistência
- documentação coerente com o comportamento efetivamente implementado

Teste em memória ajuda, mas não é prova suficiente para consultas, ordenação, filtros, tradução de LINQ, constraints ou migrations.

## Objetivo deste projeto no teste técnico

Este projeto foi desenhado para demonstrar:

- domínio explícito e semântico, evitando obsessão por tipos primitivos no núcleo do negócio
- separação clara entre `Domain`, `Application`, `ORM`, `IoC`, `Common` e `WebApi`
- validação fail-fast com retorno de lista semântica de erros para o cliente
- regras de desconto implementadas no domínio, não espalhadas em controllers ou infraestrutura
- aderência forte a testes automatizados em múltiplos níveis
- prova real com PostgreSQL, e não apenas testes em memória

## Stack utilizada

- `.NET 10`
- `C# 14`
- `ASP.NET Core Web API`
- `MediatR`
- `FluentValidation`
- `Entity Framework Core`
- `PostgreSQL`
- `xUnit`
- `FluentAssertions`
- `NSubstitute`
- `Serilog`

## Arquitetura

### Visual Architecture & Dependency Flow

```mermaid
graph TD
    %% Layers
    WebApi["DeveloperStore.WebApi (Presentation)"]
    Application["DeveloperStore.Application (Use Cases)"]
    Domain["DeveloperStore.Domain (Core Business Rules)"]
    ORM["DeveloperStore.ORM (Infrastructure / Persistence)"]
    IoC["DeveloperStore.IoC (Dependency Injection Composition)"]
    Common["DeveloperStore.Common (Cross-Cutting Concerns)"]

    %% Dependency Flow
    WebApi --> Application
    Application --> Domain
    ORM --> Domain
    IoC --> WebApi
    IoC --> Application
    IoC --> ORM
    IoC --> Domain
    Common -.-> WebApi
    Common -.-> Application
    Common -.-> ORM
    Common -.-> Domain

    %% Styling
    classDef core fill:#a5d6a7,stroke:#2e7d32,stroke-width:2px;
    classDef app fill:#90caf9,stroke:#1565c0,stroke-width:2px;
    classDef infra fill:#ffe082,stroke:#ff8f00,stroke-width:2px;
    classDef presentation fill:#ef9a9a,stroke:#c62828,stroke-width:2px;
    classDef cross fill:#b0bec5,stroke:#37474f,stroke-width:1px,stroke-dasharray: 5 5;

    class Domain core;
    class Application app;
    class ORM,IoC infra;
    class WebApi presentation;
    class Common cross;
```

### Directory Tree Structure

```text
.
├── src
│   ├── DeveloperStore.Domain
│   ├── DeveloperStore.Application
│   ├── DeveloperStore.ORM
│   ├── DeveloperStore.IoC
│   ├── DeveloperStore.Common
│   └── DeveloperStore.WebApi
├── tests
│   ├── DeveloperStore.Unit
│   ├── DeveloperStore.Integration
│   ├── DeveloperStore.Functional
│   └── DeveloperStore.Postgres
├── scripts
└── README.md
```

### Resumo das camadas

- `DeveloperStore.Domain`: regras de negócio, aggregate `Sale`, entidades, value objects, eventos e exceções de domínio.
- `DeveloperStore.Application`: comandos, queries, handlers, DTOs e validações de aplicação.
- `DeveloperStore.ORM`: `DbContext`, mappings e implementação do repositório.
- `DeveloperStore.IoC`: composição das dependências.
- `DeveloperStore.Common`: validação, logging e health checks.
- `DeveloperStore.WebApi`: controllers, middleware, contrato HTTP e bootstrap da API.

## Modelagem de domínio

O agregado principal é `Sale`.

Conceitos semânticos importantes foram modelados com tipos dedicados, por exemplo:

- `SaleId`
- `SaleItemId`
- `SaleNumber`
- `SoldAt`
- `Money`
- `ItemQuantity`
- `DiscountRate`
- `CustomerReference`
- `BranchReference`
- `ProductReference`

Essa modelagem reduz ambiguidade e concentra invariantes no lugar correto.

## Regras de negócio implementadas

- compras com `1` a `3` unidades idênticas recebem `0%` de desconto
- compras com `4` a `9` unidades idênticas recebem `10%` de desconto
- compras com `10` a `20` unidades idênticas recebem `20%` de desconto
- não é permitido vender mais de `20` unidades do mesmo produto
- vendas canceladas não podem ser alteradas
- cancelamento de venda cancela logicamente todos os itens
- cancelamento de item recalcula o total da venda

## Eventos de domínio implementados

Os eventos são publicados in-process e registrados em log:

- `SaleCreated`
- `SaleModified`
- `SaleCancelled`
- `ItemCancelled`

## Contrato da API

### Endpoints

- `POST /api/sales`
- `GET /api/sales/{id}`
- `GET /api/sales`
- `PUT /api/sales/{id}`
- `DELETE /api/sales/{id}`
- `POST /api/sales/{saleId}/items/{itemId}/cancel`

### Paginação

- `_page`: padrão `1`
- `_size`: padrão `10`

### Ordenação

Campos suportados em `_order`:

- `saleNumber`
- `soldAt`
- `customerName`
- `branchName`
- `totalAmount`
- `status`
- `itemCount`

Exemplo:

```bash
GET /api/sales?_page=1&_size=10&_order=soldAt desc, saleNumber asc
```

### Filtros suportados

- `saleNumber`
- `customerName`
- `branchName`
- `status`
- `_minSoldAt`
- `_maxSoldAt`

Compatibilidade temporária:

- `customer` continua aceito como alias legado de `customerName`
- `branch` continua aceito como alias legado de `branchName`

### Semântica de filtros de texto

- `saleNumber=SALE-123`: match exato
- `saleNumber=SALE*`: prefixo
- `customerName=*Silva`: sufixo
- `branchName=*Center*`: contains

## Erros da API

A API retorna erros semânticos com lista de detalhes para facilitar tratamento no cliente.

Exemplo:

```json
{
  "type": "validation_failed",
  "error": "Request validation failed",
  "detail": "One or more request values are invalid.",
  "status": 422,
  "traceId": "00-...",
  "errors": [
    {
      "code": "sale_number_required",
      "field": "SaleNumber",
      "message": "saleNumber is required"
    }
  ]
}
```

## Como executar

### Pré-requisitos

- `.NET SDK 10`
- `Docker`
- `Docker Compose v2`
- `GNU Make`

### Como usar o Makefile em macOS, Linux e Windows

O `Makefile` é a forma principal de operar o projeto. Ele centraliza os comandos úteis de build, execução, testes, Docker e migrations.

Instalação e uso por sistema operacional:

- `macOS`: o mais comum é já ter `make` com `Xcode Command Line Tools`. Se faltar, rode `xcode-select --install`.
- `Linux`: instale `make` pelo gerenciador da sua distro, por exemplo `sudo apt-get install make`, `sudo dnf install make` ou `sudo pacman -S make`.
- `Windows`: o fluxo recomendado é executar o projeto em `WSL` ou `Git Bash`, porque este `Makefile` usa `bash`. Em shell nativo `PowerShell` ou `cmd.exe`, prefira os comandos diretos `.NET` e `docker compose` se não tiver `make` compatível instalado.

Primeiro passo em qualquer ambiente:

```bash
make help
```

### Fluxos principais com Makefile

Fluxo mais comum para trabalhar no projeto:

```bash
make restore
make build
make test
make run
```

Fluxo completo com Docker:

```bash
make docker-up-build
```

Validação mais forte do repositório:

```bash
make verify
make verify-full
```

### Comandos úteis disponíveis

- `make help`: lista todos os alvos disponíveis.
- `make doctor`: mostra versões locais de `dotnet`, `docker` e `docker compose`.
- `make restore`: restaura pacotes NuGet e a tool local `dotnet-ef`.
- `make build`: compila a solução.
- `make run`: sobe a API localmente.
- `make watch`: sobe a API com hot reload.
- `make format`: formata a solução.
- `make format-check`: valida formatação sem alterar arquivos.
- `make test`: roda `Unit`, `Integration` e `Functional`.
- `make test-bdd`: roda a suíte BDD com `Testcontainers`.
- `make test-postgres`: sobe PostgreSQL via Docker Compose e roda a suíte dedicada ao provider real.
- `make coverage`: coleta cobertura das suítes locais rápidas.
- `make verify`: executa `format-check`, `build` e `test`.
- `make verify-full`: executa `verify` e adiciona `BDD` + `PostgreSQL real`.
- `make publish`: publica a API em `artifacts/publish`.
- `make docker-up`: sobe a stack atual em background.
- `make docker-up-build`: reconstrói a imagem e sobe a stack em background.
- `make docker-down`: derruba a stack sem apagar volume.
- `make docker-down-volumes`: derruba a stack e remove o volume nomeado do PostgreSQL.
- `make docker-logs`: acompanha logs da stack.
- `make docker-ps`: mostra status dos serviços.
- `make db-up`: sobe apenas o PostgreSQL.
- `make db-logs`: acompanha logs do PostgreSQL.
- `make db-shell`: abre `psql` dentro do container do PostgreSQL.
- `make migrate`: aplica migrations pendentes no banco configurado.
- `make migrate-list`: lista migrations conhecidas pelo EF Core; com banco configurado, também tenta informar status aplicado ou pendente.
- `make migrate-add NAME=NomeDaMigration`: cria uma nova migration.
- `make migrate-remove`: remove a última migration ainda não aplicada.
- `make migrate-script`: gera script SQL idempotente em `artifacts/migrations/idempotent.sql`.

### Passo a passo com Docker

### 1. Suba a infraestrutura e a API

```bash
make docker-up-build
```

### 2. Acesse a aplicação

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- Health: `http://localhost:8080/health`

### 3. Derrube a stack quando terminar

```bash
make docker-down
```

Se quiser apagar também o volume do PostgreSQL:

```bash
make docker-down-volumes
```

### 4. Observação importante

No ambiente Docker, as migrations são aplicadas no startup porque `Database__ApplyMigrationsOnStartup=true` está configurado no compose.

## Passo a passo sem Docker

### 1. Suba um PostgreSQL local

Você precisa de uma instância acessível localmente.

### 2. Configure a connection string

Exemplo:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=developerstore;Username=developerstore_app;Password=developerstore_local_only"
```

### 3. Aplique migrations manualmente

```bash
make migrate
```

### 4. Escolha se quer aplicar migration automaticamente

Por padrão, a API não aplica migrations automaticamente.

Para habilitar:

```bash
export Database__ApplyMigrationsOnStartup=true
```

### 5. Rode a API

```bash
make run
```

## Referência direta sem Makefile

Se preferir operar sem `make`, estes são os equivalentes diretos mais úteis.

### Docker

```bash
docker compose up -d --build
docker compose down --remove-orphans
docker compose down --volumes --remove-orphans
docker compose logs -f
docker compose up -d developerstore.db
```

### .NET

```bash
dotnet tool restore
dotnet restore DeveloperStore.slnx
dotnet build DeveloperStore.slnx
dotnet run --project src/DeveloperStore.WebApi/DeveloperStore.WebApi.csproj
dotnet watch --project src/DeveloperStore.WebApi/DeveloperStore.WebApi.csproj run
dotnet format DeveloperStore.slnx --verify-no-changes
```

### EF Core migrations

Para `migrate-list`, `migrate` e comandos que precisem verificar o estado aplicado no banco, configure uma `ConnectionStrings__DefaultConnection` válida antes de executar.

```bash
dotnet tool run dotnet-ef migrations list \
  --project src/DeveloperStore.ORM/DeveloperStore.ORM.csproj \
  --startup-project src/DeveloperStore.WebApi/DeveloperStore.WebApi.csproj

dotnet tool run dotnet-ef database update \
  --project src/DeveloperStore.ORM/DeveloperStore.ORM.csproj \
  --startup-project src/DeveloperStore.WebApi/DeveloperStore.WebApi.csproj

dotnet tool run dotnet-ef migrations add NomeDaMigration \
  --project src/DeveloperStore.ORM/DeveloperStore.ORM.csproj \
  --startup-project src/DeveloperStore.WebApi/DeveloperStore.WebApi.csproj
```

## Como testar

### Testes unitários

Validam regras de domínio e handlers isolados.

```bash
make test-unit
```

### Testes de integração

Validam o repositório e consultas em nível de infraestrutura.

```bash
make test-integration
```

### Testes funcionais

Validam o contrato HTTP da API.

```bash
make test-functional
```

### Suíte local rápida

```bash
make test
```

### Prova real com PostgreSQL

Sobe um PostgreSQL via Docker, aplica migrations e roda a suíte dedicada ao banco real.

```bash
make test-postgres
```

### BDD com Testcontainers

```bash
make test-bdd
```

### Formatter

```bash
make format-check
```

## O que os testes comprovam

- regras de desconto
- limite máximo de `20` unidades por produto
- cálculo de total por item e total da venda
- validação semântica com payload de erro estruturado
- persistência real em PostgreSQL
- listagem com paginação, ordenação e filtros
- cancelamento de venda e cancelamento de item

## Evidências executadas na entrega

As seguintes validações são as evidências principais de que a solução está operacional no escopo de `sales`:

```bash
make build
make test
make test-bdd
make test-postgres
make format-check
```

## Exemplo de criação de venda

```json
{
  "saleNumber": "SALE-301",
  "soldAt": "2026-06-23T12:00:00Z",
  "customerExternalId": "customer-1",
  "customerName": "Jane Doe",
  "branchExternalId": "branch-1",
  "branchName": "Central",
  "items": [
    {
      "productExternalId": "product-1",
      "productName": "Product 1",
      "quantity": 4,
      "unitPrice": 10.0
    }
  ]
}
```

## Health checks

- `GET /health/live`
- `GET /health/ready`
- `GET /health`

## Observações relevantes para a avaliação

- o projeto foi conduzido com foco em `Sales API`
- o domínio foi priorizado sobre conveniência de ORM
- há validação semântica em borda e invariantes no núcleo do domínio
- a solução possui evidência automatizada em memória e em banco real
- a documentação foi atualizada para refletir o comportamento efetivamente implementado
- aderência literal a toda a pasta `.doc` não deve ser inferida sem confronto específico com cada contrato e com o escopo material deste repositório
