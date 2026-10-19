<!-- PIPELINE-CONTRACT: Task completion is determined by "**Status:** [x] done" at the end of each "### Task N" section.
     Checkboxes "- [ ]" under "**Validation:**" are human verification guides — the pipeline ignores them completely.
     To mark a task as done, add "**Status:** [x] done". To mark it as pending, use "**Status:** [ ] pending". -->
# Spec 10: Fundação do Backend — Implementation Tasks

## Task Order

Tasks MUST be executed sequentially in numerical order. Each task depends on the completion of all preceding tasks.

## Tasks

### Task 1: Criar estrutura de diretórios e solução

**Objective:** Criar a pasta `backend/` com a solução vazia.
**Requirements Covered:** FR-001
**Design References:** "Solução e projetos", "File / Module Layout"
**Actions:**
1. Criar `backend/src/` e `backend/tests/`.
2. Executar `dotnet new sln -n CaronaAlvinegra -o backend` criando `backend/CaronaAlvinegra.sln`.
**Validation:** `backend/CaronaAlvinegra.sln` existe.
**Status:** [x] done

### Task 2: Criar os cinco projetos .NET

**Objective:** Criar os projetos de cada camada.
**Requirements Covered:** FR-001
**Design References:** "Solução e projetos"
**Actions:**
1. `dotnet new classlib -n CaronaAlvinegra.Domain -o backend/src/CaronaAlvinegra.Domain -f net9.0`
2. `dotnet new classlib -n CaronaAlvinegra.Application -o backend/src/CaronaAlvinegra.Application -f net9.0`
3. `dotnet new classlib -n CaronaAlvinegra.Infrastructure -o backend/src/CaronaAlvinegra.Infrastructure -f net9.0`
4. `dotnet new webapi -n CaronaAlvinegra.Api -o backend/src/CaronaAlvinegra.Api -f net9.0 --use-controllers`
5. `dotnet new xunit -n CaronaAlvinegra.Domain.Tests -o backend/tests/CaronaAlvinegra.Domain.Tests -f net9.0`
6. Adicionar os cinco projetos à solução com `dotnet sln backend/CaronaAlvinegra.sln add <csproj>`.
7. Remover os arquivos de exemplo gerados pelo template: `backend/src/CaronaAlvinegra.Api/WeatherForecast.cs` e `backend/src/CaronaAlvinegra.Api/Controllers/WeatherForecastController.cs`.
**Validation:** Os 5 `.csproj` existem e `dotnet sln backend/CaronaAlvinegra.sln list` mostra os 5 projetos.
**Status:** [x] done

### Task 3: Configurar referências entre camadas

**Objective:** Aplicar a regra de dependência DDD.
**Requirements Covered:** FR-002
**Design References:** "Architecture Decisions"
**Actions:**
1. `dotnet add backend/src/CaronaAlvinegra.Application reference backend/src/CaronaAlvinegra.Domain`
2. `dotnet add backend/src/CaronaAlvinegra.Infrastructure reference backend/src/CaronaAlvinegra.Application backend/src/CaronaAlvinegra.Domain`
3. `dotnet add backend/src/CaronaAlvinegra.Api reference backend/src/CaronaAlvinegra.Application backend/src/CaronaAlvinegra.Infrastructure`
4. `dotnet add backend/tests/CaronaAlvinegra.Domain.Tests reference backend/src/CaronaAlvinegra.Domain backend/src/CaronaAlvinegra.Application`
5. Remover qualquer referência padrão indevida (ex.: o template `webapi` não traz referências extras; o template `xunit` não referencia projetos).
**Validation:** `CaronaAlvinegra.Domain.csproj` e `CaronaAlvinegra.Application.csproj` não possuem `ProjectReference` além do esperado; `Api.csproj` referencia Application e Infrastructure; `Domain.Tests.csproj` referencia Domain e Application.
**Status:** [x] done

### Task 4: Adicionar pacotes NuGet

**Objective:** Instalar os pacotes necessários.
**Requirements Covered:** FR-003
**Design References:** "Componente: ApplicationDbContext", "Componente: Program.cs"
**Actions:**
1. `dotnet add backend/src/CaronaAlvinegra.Infrastructure package Microsoft.EntityFrameworkCore`
2. `dotnet add backend/src/CaronaAlvinegra.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL`
3. `dotnet add backend/src/CaronaAlvinegra.Api package Swashbuckle.AspNetCore`
4. Verificar que `CaronaAlvinegra.Domain` e `CaronaAlvinegra.Application` não recebem pacotes.
**Validation:** Os pacotes aparecem nos `.csproj` corretos; Domain e Application não possuem `PackageReference`.
**Status:** [x] done

### Task 5: Criar ApplicationDbContext

**Objective:** Criar o contexto EF Core no Infrastructure.
**Requirements Covered:** FR-004
**Design References:** "Componente: ApplicationDbContext"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Infrastructure/Data/ApplicationDbContext.cs`.
2. Implementar `public class ApplicationDbContext : DbContext` com o construtor `ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)`, no namespace `CaronaAlvinegra.Infrastructure.Data`.
**Validation:** O arquivo existe e compila; a classe herda de `DbContext`.
**Status:** [x] done

### Task 6: Configurar connection string

**Objective:** Definir a connection string de desenvolvimento.
**Requirements Covered:** FR-005
**Design References:** "Data Structures"
**Actions:**
1. Criar/editar `backend/src/CaronaAlvinegra.Api/appsettings.Development.json` com `ConnectionStrings:DefaultConnection` = `Host=localhost;Port=5432;Database=carona_alvinegra;Username=postgres;Password=postgres`.
2. Garantir que `appsettings.json` (produção) NÃO contém credenciais hardcoded.
**Validation:** A chave `ConnectionStrings:DefaultConnection` existe em `appsettings.Development.json`.
**Status:** [x] done

### Task 7: Configurar Program.cs

**Objective:** Bootstrap da API (DI, Swagger, health check).
**Requirements Covered:** FR-006, FR-007
**Design References:** "Componente: Program.cs", "Componente: Health check"
**Actions:**
1. Editar `backend/src/CaronaAlvinegra.Api/Program.cs` conforme o esqueleto do design: `AddControllers().AddJsonOptions(...JsonStringEnumConverter...)`, `AddEndpointsApiExplorer`, `AddSwaggerGen`, `AddCors` (AllowAnyOrigin/AllowAnyMethod/AllowAnyHeader), `AddDbContext<ApplicationDbContext>(UseNpgsql(...))`, `UseCors`, `UseSwagger`/`UseSwaggerUI` em dev, `MapGet("/health", ...)`, `MapControllers`.
2. Adicionar os usings `CaronaAlvinegra.Infrastructure.Data` e `System.Text.Json.Serialization`.
3. Criar/ajustar `backend/src/CaronaAlvinegra.Api/Properties/launchSettings.json` com `applicationUrl` = `http://localhost:5000` e `ASPNETCORE_ENVIRONMENT` = `Development` (substituir a porta gerada pelo template).
**Validation:** `Program.cs` compila e contém `MapGet("/health"`, `UseNpgsql`, `UseCors` e `JsonStringEnumConverter`; `launchSettings.json` contém `http://localhost:5000`.
**Status:** [x] done

### Task 8: Criar smoke test

**Objective:** Validar o runner xUnit.
**Requirements Covered:** FR-008
**Design References:** "Componente: Smoke test"
**Actions:**
1. Criar `backend/tests/CaronaAlvinegra.Domain.Tests/SmokeTest.cs` com um `[Fact]` que faz `Assert.True(true)`.
2. Remover o arquivo `UnitTest1.cs` gerado pelo template (se existir).
**Validation:** `dotnet test backend/tests/CaronaAlvinegra.Domain.Tests` termina com exit code 0.
**Status:** [x] done

### Task 9: .gitignore de .NET e build final

**Objective:** Ignorar bin/obj e validar a build completa.
**Requirements Covered:** FR-009
**Design References:** "File / Module Layout"
**Actions:**
1. Criar `backend/.gitignore` com entradas para `bin/`, `obj/` (e `*.user`).
2. Executar `dotnet build backend/CaronaAlvinegra.sln` e `dotnet test backend/tests/CaronaAlvinegra.Domain.Tests`.
3. Confirmar que `git status` não lista `bin/`/`obj/` sob `backend/`.
**Validation:** Build e test terminam com exit code 0; `git status` não mostra `bin/`/`obj/`.
**Status:** [x] done
