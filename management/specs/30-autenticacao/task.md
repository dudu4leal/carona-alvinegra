<!-- PIPELINE-CONTRACT: Task completion is determined by "**Status:** [x] done" at the end of each "### Task N" section.
     Checkboxes "- [ ]" under "**Validation:**" are human verification guides — the pipeline ignores them completely.
     To mark a task as done, add "**Status:** [x] done". To mark it as pending, use "**Status:** [ ] pending". -->
# Spec 30: Autenticação — Implementation Tasks

## Task Order

Tasks MUST be executed sequentially in numerical order. Each task depends on the completion of all preceding tasks.

## Tasks

### Task 1: Adicionar pacotes de Identity e JWT

**Objective:** Instalar os pacotes necessários.
**Requirements Covered:** FR-001
**Design References:** "Componente: ApplicationDbContext", "Componente: JwtTokenService"
**Actions:**
1. `dotnet add backend/src/CaronaAlvinegra.Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore`
2. `dotnet add backend/src/CaronaAlvinegra.Api package Microsoft.AspNetCore.Authentication.JwtBearer`
**Validation:** Os pacotes aparecem nos `.csproj` corretos.
**Status:** [x] done

### Task 2: Estender ApplicationDbContext para Identity

**Objective:** Herdar de IdentityDbContext.
**Requirements Covered:** FR-002
**Design References:** "Componente: ApplicationDbContext"
**Actions:**
1. Editar `backend/src/CaronaAlvinegra.Infrastructure/Data/ApplicationDbContext.cs` mudando `: DbContext` para `: IdentityDbContext<IdentityUser>`.
2. Garantir que `OnModelCreating` chama `base.OnModelCreating(modelBuilder)`.
**Validation:** `dotnet build backend/CaronaAlvinegra.sln` termina com exit code 0.
**Status:** [x] done

### Task 3: Configurar Identity no Program.cs

**Objective:** Registrar Identity no DI.
**Requirements Covered:** FR-003
**Design References:** "Componente: Configuração do Program.cs"
**Actions:**
1. Editar `Program.cs`: adicionar `AddIdentityCore<IdentityUser>(...)` com política de senha (min 6, sem requisitos complexos) e `.AddEntityFrameworkStores<ApplicationDbContext>()`.
**Validation:** `Program.cs` compila.
**Status:** [x] done

### Task 4: Configurar JWT Bearer no Program.cs

**Objective:** Registrar autenticação JWT.
**Requirements Covered:** FR-004
**Design References:** "Componente: Configuração do Program.cs"
**Actions:**
1. Editar `Program.cs`: adicionar `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` com `TokenValidationParameters` lidos de `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key`.
2. Adicionar `app.UseAuthentication()` e `app.UseAuthorization()` antes de `MapControllers()`.
**Validation:** `Program.cs` compila e chama `UseAuthentication`/`UseAuthorization`.
**Status:** [x] done

### Task 5: Criar JwtTokenService

**Objective:** Serviço de geração de token.
**Requirements Covered:** FR-005
**Design References:** "Componente: JwtTokenService"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Infrastructure/Services/JwtTokenService.cs`.
2. Implementar `GenerateToken(IdentityUser user)` com claims `sub`/`email`, expiração e assinatura HMAC-SHA256, lendo configuração.
**Validation:** Arquivo existe e compila.
**Status:** [x] done

### Task 6: Criar AuthController e DTOs

**Objective:** Endpoint de login.
**Requirements Covered:** FR-006
**Design References:** "Componente: AuthController", "Componente: DTOs de login"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Api/Contracts/LoginRequest.cs` e `LoginResponse.cs` (records).
2. Criar `backend/src/CaronaAlvinegra.Api/Controllers/AuthController.cs` com `POST /api/auth/login` usando `UserManager` + `JwtTokenService`.
3. Registrar `JwtTokenService` no DI (`builder.Services.AddScoped<JwtTokenService>()`).
**Validation:** `dotnet build` compila; a rota existe.
**Status:** [x] done

### Task 7: Implementar seed do usuário Jones

**Objective:** Criar usuário admin na inicialização.
**Requirements Covered:** FR-007
**Design References:** "Componente: Seed do usuário Jones"
**Actions:**
1. Criar o método `SeedAdminUserAsync(IServiceProvider)` (estático) em `backend/src/CaronaAlvinegra.Api/` — nome único e definitivo, referenciado pelo Spec 100.
2. Chamar o seed no `Program.cs` **após** `builder.Build()`. (Quando o Spec 100 for implementado, o bloco de migrations será inserido **antes** desta chamada.)
3. Adicionar `SeedAdmin:Email`/`SeedAdmin:Password` em `appsettings.Development.json`.
4. Adicionar `Jwt:Issuer`, `Jwt:Audience` e `Jwt:ExpirationMinutes` (480) em `appsettings.json`, sem `Jwt:Key`.
**Validation:** Ao iniciar a API, o usuário `SeedAdmin:Email` é criado se não existir; seed é idempotente; `grep -n "SeedAdminUserAsync" backend/src/CaronaAlvinegra.Api/Program.cs` retorna uma ocorrência.
**Status:** [x] done

### Task 8: Proteger rotas com [Authorize]

**Objective:** Aplicar autorização padrão.
**Requirements Covered:** FR-008
**Design References:** "Architecture Decisions"
**Actions:**
1. Configurar `app.MapControllers().RequireAuthorization()`; manter o `AuthController` com `[AllowAnonymous]` (o `/health` é minimal API e já é público).
2. Garantir que `GET /health` continua público.
**Validation:** `GET /health` responde sem token; controllers protegidos exigem token.
**Status:** [x] done

### Task 9: Gerar migration AddIdentity

**Objective:** Persistir tabelas de Identity.
**Requirements Covered:** FR-009
**Design References:** "Migration / Rollback"
**Actions:**
1. Executar `dotnet ef migrations add AddIdentity --project src/CaronaAlvinegra.Infrastructure --startup-project src/CaronaAlvinegra.Api`.
**Validation:** Migration `AddIdentity` existe sob `Migrations/`.
**Status:** [x] done

### Task 10: Criar testes do JwtTokenService

**Objective:** Testar geração de token.
**Requirements Covered:** FR-010
**Design References:** "Testing Strategy"
**Actions:**
1. Criar `backend/tests/CaronaAlvinegra.Domain.Tests/JwtTokenServiceTests.cs`.
2. Testar que `GenerateToken` retorna token não-vazio e que o token decodificado contém claims `sub` e `email`.
**Validation:** `dotnet test backend/tests/CaronaAlvinegra.Domain.Tests` termina com exit code 0.
**Status:** [x] done
