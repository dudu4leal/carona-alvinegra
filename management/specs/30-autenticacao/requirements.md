# Spec 30: Autenticação — Requirements

## Value Delivery

> **V4: Acesso autenticado**
>
> "Protege o back-office com login e senha. Apenas o Jones acessa o sistema (único usuário na v1), autenticado via ASP.NET Core Identity e autorizado nas rotas da API por JWT. Evita que terceiros alterem participantes, pagamentos ou alocações."

Este spec adiciona **ASP.NET Core Identity** (login/senha) e **JWT** à API, expõe o endpoint de login, semeia o usuário único do Jones e protege as rotas com `[Authorize]`. É o único spec de segurança da v1.

## Functional Requirements

### FR-001: Pacotes de Identity e JWT

**What:** Adicionar os pacotes `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (Infrastructure) e `Microsoft.AspNetCore.Authentication.JwtBearer` (Api).

**Why:** Identity fornece login/hash de senha pronto; JwtBearer valida tokens nas rotas.

**Acceptance Criteria:**
- [ ] `CaronaAlvinegra.Infrastructure.csproj` referencia `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (9.x).
- [ ] `CaronaAlvinegra.Api.csproj` referencia `Microsoft.AspNetCore.Authentication.JwtBearer` (9.x).

### FR-002: ApplicationDbContext herda de IdentityDbContext

**What:** Alterar `ApplicationDbContext` para herdar de `IdentityDbContext<IdentityUser>` (em vez de `DbContext`).

**Why:** Adiciona as tabelas de Identity (`AspNetUsers`, `AspNetRoles`, etc.) ao mesmo contexto/banco, sem um contexto separado.

**Acceptance Criteria:**
- [ ] `ApplicationDbContext` declara `: IdentityDbContext<IdentityUser>`.
- [ ] Os `DbSet<T>` de domínio do Spec 20 permanecem inalterados.
- [ ] `dotnet build backend/CaronaAlvinegra.sln` termina com exit code 0.

### FR-003: Configuração do Identity

**What:** Registrar Identity no `Program.cs` com `AddIdentityCore<IdentityUser>` (ou equivalente) e uma política de senha mínima razoável.

**Why:** Habilita `UserManager<IdentityUser>` e o hash de senha para login.

**Acceptance Criteria:**
- [ ] `Program.cs` registra `UserManager<IdentityUser>` e o sign-in manager (se usado) no contêiner de DI.
- [ ] Política de senha configurada (mín. 6 caracteres; sem regras complexas para não travar o uso).

### FR-004: Configuração do JWT Bearer

**What:** Configurar autenticação JWT (`AddAuthentication` + `AddJwtBearer`) com chave simétrica, issuer, audience e expiração lidos de configuração.

**Why:** Permite emitir e validar tokens stateless nas rotas protegidas.

**Acceptance Criteria:**
- [ ] `Program.cs` chama `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`.
- [ ] Os valores `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` e `Jwt:ExpirationMinutes` vêm de `IConfiguration`.
- [ ] `app.UseAuthentication()` e `app.UseAuthorization()` são chamados antes de `MapControllers()`.

### FR-005: Serviço de geração de token (`JwtTokenService`)

**What:** Criar `JwtTokenService` na Infrastructure que gera um JWT a partir de um `IdentityUser` e das configurações.

**Why:** Isola a geração de token (responsabilidade única) e facilita testes.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Infrastructure/Services/JwtTokenService.cs` existe.
- [ ] Método `string GenerateToken(IdentityUser user)` retorna um token não-vazio.
- [ ] O token contém claims `sub` (id do usuário) e `email`.

### FR-006: Endpoint de login

**What:** Criar `AuthController` com `POST /api/auth/login` recebendo `{ email, password }` e retornando `{ token }` em caso de sucesso, ou `401 Unauthorized` em credenciais inválidas.

**Why:** É a porta de entrada do frontend (Spec 90) para obter o token.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Api/Controllers/AuthController.cs` existe.
- [ ] `POST /api/auth/login` com credenciais válidas retorna `200 OK` com corpo contendo o token.
- [ ] `POST /api/auth/login` com credenciais inválidas retorna `401 Unauthorized`.

### FR-007: Seed do usuário Jones

**What:** Semear um usuário administrador (Jones) na inicialização, com e-mail e senha lidos de configuração (dev) / variável de ambiente (prod).

**Why:** A v1 tem um único usuário; garante que o login funcione sem endpoint de registro.

**Acceptance Criteria:**
- [ ] A inicialização da API cria o usuário `SeedAdmin:Email` se ele ainda não existir.
- [ ] A senha vem de `SeedAdmin:Password` (nunca hardcoded em código).
- [ ] O processo de seed é idempotente (não duplica usuário).

### FR-008: Proteção das rotas

**What:** Aplicar `[Authorize]` nas rotas protegidas; manter `/health` e o login públicos.

**Why:** Restringe acesso aos dados do back-office a quem tem token válido.

**Acceptance Criteria:**
- [ ] `GET /health` e `POST /api/auth/login` permanecem acessíveis sem token.
- [ ] Existe um mecanismo padrão (ex.: `[Authorize]` global ou por controller) que protege os controllers de domínio criados nos specs futuros.

### FR-009: Migration das tabelas de Identity

**What:** Gerar migration adicional com as tabelas de Identity.

**Why:** Persistir usuários (`AspNetUsers`) no PostgreSQL.

**Acceptance Criteria:**
- [ ] Uma migration (ex.: `AddIdentity`) existe sob `backend/src/CaronaAlvinegra.Infrastructure/Migrations/` incluindo as tabelas de Identity.
- [ ] `dotnet build backend/CaronaAlvinegra.sln` termina com exit code 0.

### FR-010: Testes de autenticação

**What:** Criar testes de unidade para o `JwtTokenService`.

**Why:** Garante que o token é gerado corretamente e com claims esperados.

**Acceptance Criteria:**
- [ ] Teste valida que `GenerateToken` retorna token não-vazio e que o token decodificado contém `sub` e `email`.
- [ ] `dotnet test backend/tests/CaronaAlvinegra.Domain.Tests` termina com exit code 0.

## Non-Functional Requirements

### NFR-001: Segredos fora do código

**What:** Nenhum segredo (chave JWT, senha) hardcoded.

**Acceptance Criteria:**
- [ ] `Jwt:Key` e `SeedAdmin:Password` não aparecem hardcoded em `Program.cs` ou controllers; vêm de `appsettings.Development.json`/variável de ambiente.

### NFR-002: `/health` público

**What:** O health check não pode exigir autenticação.

**Acceptance Criteria:**
- [ ] `GET /health` retorna 200 sem token.

## Constraints

- **.NET 9** (net9.0).
- **ASP.NET Core Identity** com `IdentityUser` (ADR-004).
- **JWT Bearer** stateless (ADR-004).
- **KISS/DRY**: sem refresh token, sem roles múltiplas, sem endpoint de registro público (v1 = usuário único).

## Edge Cases & Error States

- **Login com e-mail inexistente:** retorna `401` (não revela se o e-mail existe).
- **Login com senha errada:** retorna `401`.
- **Token expirado/ausente em rota protegida:** retorna `401`/`403` pelo middleware JwtBearer.
- **Usuário seed já existente:** o seed é no-op (idempotente).
- **`Jwt:Key` ausente em produção:** falhar de forma clara na inicialização (mensagem de erro), pois não é possível emitir tokens.

## Dependencies

- **Spec 10 (fundacao-backend)** — API, DI e `ApplicationDbContext`.
- **Spec 20 (modelo-dominio)** — `ApplicationDbContext` com `DbSet` (será estendido para `IdentityDbContext`).
- **PostgreSQL** local para gerar a migration de Identity.

## Out of Scope

- Refresh tokens e logout com revogação (v1 usa token com expiração).
- Recuperação de senha / múltiplos usuários / roles (v1 = Jones único).
- Autorização por permissão granular (futuro).
- Processamento de Pix (fora da v1).
