# Spec 10: Fundação do Backend — Requirements

## Value Delivery

> **V5: Fundação técnica e modelo de domínio** (parcial — este spec entrega a **fundação técnica**; o modelo de domínio é o Spec 20).
>
> "Estabelece a fundação técnica e o modelo de domínio: backend .NET 9 em DDD tático (camadas Domain / Application / Infrastructure / API) com EF Core e PostgreSQL, além das entidades do domínio — Participante, Grupo, Van, Spin, Jogo, Pagamento e Lista de espera. É a base sobre a qual todos os outros specs constroem."

Este spec cria a **estrutura física e a infraestrutura mínima** do backend: solução .NET 9, os quatro projetos de camada (Domain, Application, Infrastructure, Api), o projeto de testes, as referências DDD, o `ApplicationDbContext` conectado ao PostgreSQL, o bootstrap da API (DI, Swagger, health check) e a configuração de migrations. Nenhuma entidade de domínio é criada aqui (fica no Spec 20).

## Functional Requirements

### FR-001: Solução e projetos .NET 9

**What:** Criar a solução `backend/CaronaAlvinegra.sln` contendo cinco projetos .NET 9 (net9.0): `CaronaAlvinegra.Domain` (classlib), `CaronaAlvinegra.Application` (classlib), `CaronaAlvinegra.Infrastructure` (classlib), `CaronaAlvinegra.Api` (webapi) e `CaronaAlvinegra.Domain.Tests` (xunit).

**Why:** Estabelece a estrutura física do DDD tático e é pré-requisito para todos os specs de backend.

**Acceptance Criteria:**
- [ ] Arquivo `backend/CaronaAlvinegra.sln` existe e lista os 5 projetos.
- [ ] `backend/src/CaronaAlvinegra.Domain/CaronaAlvinegra.Domain.csproj` existe com `<TargetFramework>net9.0</TargetFramework>`.
- [ ] `backend/src/CaronaAlvinegra.Application/CaronaAlvinegra.Application.csproj` existe com `<TargetFramework>net9.0</TargetFramework>`.
- [ ] `backend/src/CaronaAlvinegra.Infrastructure/CaronaAlvinegra.Infrastructure.csproj` existe com `<TargetFramework>net9.0</TargetFramework>`.
- [ ] `backend/src/CaronaAlvinegra.Api/CaronaAlvinegra.Api.csproj` existe com `<OutputType>Exe</OutputType>` e `<TargetFramework>net9.0</TargetFramework>`.
- [ ] `backend/tests/CaronaAlvinegra.Domain.Tests/CaronaAlvinegra.Domain.Tests.csproj` existe com `<TargetFramework>net9.0</TargetFramework>`.

### FR-002: Referências entre camadas (DDD)

**What:** Configurar as referências de projeto respeitando a regra de dependência do DDD tático.

**Why:** Garante que o domínio não dependa de infraestrutura, evitando acoplamento e permitindo evolução independente.

**Acceptance Criteria:**
- [ ] `CaronaAlvinegra.Domain` NÃO possui nenhuma `ProjectReference` (zero referências).
- [ ] `CaronaAlvinegra.Application` referencia apenas `CaronaAlvinegra.Domain`.
- [ ] `CaronaAlvinegra.Infrastructure` referencia `CaronaAlvinegra.Application` e `CaronaAlvinegra.Domain` (e nenhum projeto da camada API).
- [ ] `CaronaAlvinegra.Api` referencia `CaronaAlvinegra.Application` e `CaronaAlvinegra.Infrastructure`.
- [ ] `CaronaAlvinegra.Domain.Tests` referencia `CaronaAlvinegra.Domain`.

### FR-003: Pacotes NuGet essenciais

**What:** Adicionar apenas os pacotes necessários para a fundação.

**Why:** Prepara EF Core + PostgreSQL e as ferramentas de desenvolvimento sem adicionar dependências desnecessárias.

**Acceptance Criteria:**
- [ ] `CaronaAlvinegra.Infrastructure` referencia `Microsoft.EntityFrameworkCore` (versão 9.x).
- [ ] `CaronaAlvinegra.Infrastructure` referencia `Npgsql.EntityFrameworkCore.PostgreSQL` (versão 9.x).
- [ ] `CaronaAlvinegra.Api` referencia `Swashbuckle.AspNetCore` (Swagger/OpenAPI).
- [ ] `CaronaAlvinegra.Domain.Tests` referencia `Microsoft.NET.Test.Sdk`, `xunit` e `xunit.runner.visualstudio`.
- [ ] `CaronaAlvinegra.Domain` e `CaronaAlvinegra.Application` NÃO possuem nenhum `PackageReference` (zero).

### FR-004: ApplicationDbContext conectado ao PostgreSQL

**What:** Criar `ApplicationDbContext : DbContext` no projeto Infrastructure, configurado para usar o provider Npgsql.

**Why:** É o ponto único de acesso ao banco via EF Core; futuros specs adicionam `DbSet<T>` nele.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Infrastructure/Data/ApplicationDbContext.cs` existe.
- [ ] `ApplicationDbContext` herda de `DbContext` e possui construtor `ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)`.
- [ ] A classe está no namespace `CaronaAlvinegra.Infrastructure.Data`.

### FR-005: Connection string configurável

**What:** Definir a connection string PostgreSQL em `appsettings.Development.json` (para desenvolvimento local) com suporte a override por variável de ambiente em produção.

**Why:** Separa a configuração de ambiente da compilação; permite apontar para o PostgreSQL local em dev e para o do VPS em produção.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Api/appsettings.Development.json` existe e contém `ConnectionStrings:DefaultConnection` com host `localhost`, porta `5432` e database `carona_alvinegra`.
- [ ] `Program.cs` lê a connection string de `builder.Configuration.GetConnectionString("DefaultConnection")`.
- [ ] Nenhuma credencial real (senha de produção) é hardcoded.

### FR-006: Bootstrap da API (Program.cs)

**What:** Configurar o `Program.cs` da API com injeção de dependência, controllers, Swagger, health check e CORS permissivo (desenvolvimento).

**Why:** Torna a API executável e inspecionável desde o primeiro dia.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Api/Program.cs` existe.
- [ ] `builder.Services.AddDbContext<ApplicationDbContext>(...)` usa `UseNpgsql(connectionString)`.
- [ ] `builder.Services.AddControllers()`, `AddEndpointsApiExplorer()` e `AddSwaggerGen()` são chamados.
- [ ] `app.UseSwagger()` e `app.UseSwaggerUI()` são chamados em desenvolvimento.
- [ ] `app.MapControllers()` é chamado.

### FR-007: Health check endpoint

**What:** Expor um endpoint `GET /health` que retorna `200 OK`.

**Why:** Permite verificar que a API está no ar (liveness) e será usado pelo Docker/Compose no Spec 100.

**Acceptance Criteria:**
- [ ] `GET /health` retorna status HTTP 200 com corpo indicando estado saudável (ex.: `"Healthy"`).
- [ ] O endpoint NÃO exige autenticação neste spec.

### FR-008: Projeto de testes com smoke test

**What:** Criar o projeto `CaronaAlvinegra.Domain.Tests` com um teste de smoke que sempre passa e exercita o runner.

**Why:** Garante que a infraestrutura de testes (xUnit) está funcional antes de qualquer lógica de domínio existir.

**Acceptance Criteria:**
- [ ] Arquivo `backend/tests/CaronaAlvinegra.Domain.Tests/SmokeTest.cs` existe com pelo menos um `[Fact]` que passa.
- [ ] `dotnet test` no projeto de testes termina com exit code 0.

### FR-009: Build limpa e .gitignore de .NET

**What:** Garantir que a solução compila sem erros e que artefatos de build (`bin/`, `obj/`) não entrem no controle de versão.

**Why:** Uma build limpa é pré-condição para todos os specs seguintes; ignorar `bin/`/`obj/` evita commit de binários.

**Acceptance Criteria:**
- [ ] `dotnet build backend/CaronaAlvinegra.sln` termina com exit code 0 e zero erros.
- [ ] Arquivo `backend/.gitignore` existe e contém entradas para `bin/` e `obj/`.
- [ ] `git status` não lista nenhum diretório `bin/` ou `obj/` sob `backend/`.

## Non-Functional Requirements

### NFR-001: Isolamento de camadas

**What:** O domínio deve permanecer livre de dependências de infraestrutura e de API.

**Acceptance Criteria:**
- [ ] Nenhum arquivo sob `CaronaAlvinegra.Domain` referencia namespaces de `Infrastructure` ou `Api`.
- [ ] Nenhum arquivo sob `CaronaAlvinegra.Application` referencia namespaces de `Infrastructure` ou `Api`.

### NFR-002: Determinismo de build

**What:** A build deve ser reproduzível em máquina limpa.

**Acceptance Criteria:**
- [ ] `dotnet restore` + `dotnet build` (sem cache manual) terminam com exit code 0.

## Constraints

- **.NET 9** (net9.0) — não usar versões anteriores nem previews.
- **DDD tático** com 4 camadas; **sem CQRS/event sourcing** (ADR-001).
- **PostgreSQL** via `Npgsql.EntityFrameworkCore.PostgreSQL` (ADR-003).
- Seguir **KISS** e **DRY** (arch.md): nenhuma abstração além do necessário.
- Documentos escritos para IA (DeepSeek V4 Pro): precisão e determinismo acima de legibilidade humana.

## Edge Cases & Error States

- **PostgreSQL indisponível no startup:** a API NÃO deve falhar na inicialização só porque o banco está fora; `AddDbContext` só abre conexão na primeira query. O health check de liveness (`/health`) continua respondendo 200.
- **Connection string ausente:** se `ConnectionStrings:DefaultConnection` não estiver definida, `UseNpgsql` deve receber `null` de forma segura em dev (usando o valor de `appsettings.Development.json`). Documentar que produção exige a variável de ambiente.
- **`dotnet ef` não instalado:** a execução de migrations é um passo manual do Spec 20; este spec apenas garante que o pacote/ferramenta está disponível e que o DbContext é resolvível via DI.

## Dependencies

- **Nenhum spec anterior** (este é o primeiro spec de backend).
- **SDK .NET 9** instalado na máquina de build (`dotnet --version` ≥ 9.0).
- **PostgreSQL** rodando localmente para validar a conexão (opcional para o build; obrigatório para o Spec 20).

## Out of Scope

- Entidades de domínio e value objects (Spec 20).
- Autenticação/Identity/JWT (Spec 30).
- Endpoints de CRUD de participantes, grupos, jogos e pagamentos (Specs 40–60).
- Algoritmo de alocação (Spec 70) e geração de lista (Spec 80).
- Frontend (Spec 90) e deploy (Spec 100).
