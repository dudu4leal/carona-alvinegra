# Spec 10: Fundação do Backend — Design

## Design Approach

Scaffolding determinístico de uma solução .NET 9 em DDD tático. A prioridade é uma **estrutura mínima, compilável e executável**, sem lógica de negócio. Usamos os templates padrão do SDK (`dotnet new`) e apenas os pacotes estritamente necessários, respeitando KISS. O `ApplicationDbContext` é criado vazio (sem `DbSet`) — as entidades entram no Spec 20.

## Architecture Decisions

- **Estrutura de pastas** `backend/src/` (produção) e `backend/tests/` (testes) para separar código de teste do de produção.
- **Nome do namespace raiz:** `CaronaAlvinegra.<Camada>`.
- **Provider PostgreSQL via Npgsql** (ADR-003), registrado por DI no `Program.cs`.
- **Sem camada extra de abstração de repositório** nesta fundação — repositórios são introduzidos apenas quando houver entidades (Spec 20+), evitando abstração prematura (KISS).

## Component / Module Breakdown

### Componente: Solução e projetos

**Purpose:** Estrutura física da solução.

**Interface:** 5 arquivos `.csproj` + `CaronaAlvinegra.sln`.

**Internal Logic:** Projetos criados com `dotnet new classlib` / `webapi --use-controllers` / `xunit`, depois movidos para `src/` e `tests/` e adicionados à solução com `dotnet sln add`.

**Dependencies:** SDK .NET 9.

**Error Handling:** Se um diretório já existir, reutilizar em vez de recriar (idempotência).

### Componente: ApplicationDbContext

**Purpose:** Contexto EF Core para acesso ao PostgreSQL.

**Interface:**
```csharp
namespace CaronaAlvinegra.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
}
```

**Internal Logic:** Sem `DbSet` neste spec. O provider e a connection string são fornecidos via `AddDbContext` no `Program.cs`.

**Dependencies:** `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`.

**Error Handling:** Nenhuma lógica própria; erros de conexão só ocorrem na primeira query (comportamento padrão do EF Core).

### Componente: Program.cs (bootstrap)

**Purpose:** Configurar DI, middleware e health check da API.

**Interface (esqueleto):**
```csharp
using System.Text.Json.Serialization;
using CaronaAlvinegra.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' não configurada. Defina ConnectionStrings:DefaultConnection (appsettings.Development.json) ou a variável de ambiente ConnectionStrings__DefaultConnection.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok("Healthy"));
app.MapControllers();

app.Run();
```

**Internal Logic:** Registra `ApplicationDbContext`, CORS permissivo, Swagger e o endpoint `/health`. `JsonStringEnumConverter` garante o contrato do enum `Rota` como string (Spec 90). O CORS permissivo permite que o frontend (Spec 90) chame a API em desenvolvimento; em produção o nginx (Spec 100) serve o SPA na mesma origem.

**Development URL:** `backend/src/CaronaAlvinegra.Api/Properties/launchSettings.json` com perfil `http`:
```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```
Este arquivo é a fonte da porta 5000 usada pelo `baseURL` do frontend (Spec 90); o Spec 100 usa 8080 dentro do container via `ASPNETCORE_URLS` e não depende dele.

**Dependencies:** `CaronaAlvinegra.Application`, `CaronaAlvinegra.Infrastructure`.

**Error Handling:** O app sobe mesmo sem banco (lazy connection). Se `ConnectionStrings:DefaultConnection` estiver ausente em todas as fontes, a inicialização falha com `InvalidOperationException` de mensagem explícita (antes de qualquer query) — em produção a variável `ConnectionStrings__DefaultConnection` é obrigatória.

### Componente: Health check

**Purpose:** Liveness probe.

**Interface:** `GET /health` → `200 OK` com corpo `"Healthy"`.

**Internal Logic:** `MapGet` minimal API, sem dependências externas.

**Dependencies:** Nenhuma.

**Error Handling:** Sempre retorna 200 se o processo estiver vivo.

### Componente: Smoke test

**Purpose:** Validar o runner xUnit.

**Interface:** `SmokeTest.cs` com um `[Fact]` trivial (ex.: `Assert.True(true)`).

**Internal Logic:** Não testa lógica de domínio (ainda não existe).

**Dependencies:** `xunit`, `Microsoft.NET.Test.Sdk`.

**Error Handling:** N/A.

## Data Flow

1. `dotnet build` restaura pacotes e compila os 5 projetos na ordem das referências (Domain → Application → Infrastructure → Api; Tests → Domain).
2. `dotnet run --project backend/src/CaronaAlvinegra.Api` inicia a API, registra o DbContext (sem abrir conexão) e expõe `/health` e `/swagger`.
3. `GET /health` retorna 200 sem tocar o banco.
4. `dotnet test` executa o smoke test e retorna 0.

## Data Structures

- **Connection string (dev):** `Host=localhost;Port=5432;Database=carona_alvinegra;Username=postgres;Password=postgres` em `appsettings.Development.json` (ajustável localmente). Produção usa variável de ambiente `ConnectionStrings__DefaultConnection`.
- **Não há tabelas/entidades neste spec.**

## File / Module Layout

```
global.json                           # pin do SDK 9.0.x (reprodutibilidade, evita .slnx do SDK 10)
backend/
├── CaronaAlvinegra.sln
├── .gitignore                        # bin/, obj/
├── src/
│   ├── CaronaAlvinegra.Domain/
│   │   └── CaronaAlvinegra.Domain.csproj
│   ├── CaronaAlvinegra.Application/
│   │   └── CaronaAlvinegra.Application.csproj
│   ├── CaronaAlvinegra.Infrastructure/
│   │   ├── CaronaAlvinegra.Infrastructure.csproj
│   │   └── Data/
│   │       └── ApplicationDbContext.cs
│   └── CaronaAlvinegra.Api/
│       ├── CaronaAlvinegra.Api.csproj  # OutputType=Exe explícito (FR-001)
│       ├── Properties/
│       │   └── launchSettings.json     # applicationUrl http://localhost:5000 (Spec 90)
│       ├── Program.cs
│       ├── appsettings.json
│       └── appsettings.Development.json
└── tests/
    └── CaronaAlvinegra.Domain.Tests/
        ├── CaronaAlvinegra.Domain.Tests.csproj
        └── SmokeTest.cs
```

## Testing Strategy

- **Smoke test** (`SmokeTest.cs`): um `[Fact]` trivial que sempre passa, validando que `dotnet test` funciona.
- **Build check:** `dotnet build backend/CaronaAlvinegra.sln` deve sair com exit code 0.
- **Camada check:** verificar manualmente que `CaronaAlvinegra.Domain.csproj` e `CaronaAlvinegra.Application.csproj` não possuem `ProjectReference`/`PackageReference` indevidos.
- Testes de domínio reais entram no Spec 20.
- **Testes de serviço (Application):** usar **fakes in-memory escritos à mão** (classes de teste que implementam `IGrupoRepository`, `IParticipanteRepository`, `IJogoRepository`, `IPagamentoRepository`, `IAlocacaoRepository` etc. com listas em memória). **Não** adicionar pacotes de mock (Moq/NSubstitute) nem EF Core InMemory — mantém o projeto de testes com apenas xunit e evita decisões de versão.

## Migration / Rollback

- **Migrations:** este spec NÃO gera migration de dados (não há entidades). A infraestrutura de migrations fica pronta para o Spec 20, que criará a primeira migration real.
- **Rollback:** como não há estado persistido, rollback = remover os diretórios `backend/src/*` e `backend/tests/*` (tudo é rederivável do `dotnet new`).
