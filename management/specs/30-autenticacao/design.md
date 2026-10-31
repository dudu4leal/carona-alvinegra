# Spec 30: Autenticação — Design

## Design Approach

Adicionar **Identity + JWT** à API existente de forma mínima: o `ApplicationDbContext` passa a herdar de `IdentityDbContext<IdentityUser>`, o login é um único endpoint no `AuthController`, a geração de token fica num serviço dedicado (`JwtTokenService`) e o usuário Jones é semeado na inicialização. Sem refresh token, roles ou endpoint de registro (KISS).

## Architecture Decisions

- **`IdentityDbContext<IdentityUser>`** em vez de um contexto separado — um único banco/contexto para domínio + Identity.
- **`JwtTokenService` na Infrastructure** (gera token a partir de `IConfiguration`), injetado no controller.
- **Login no controller (Api)**, não numa camada Application, porque depende de `UserManager`/`IdentityUser` (Infrastructure) e a regra de dependência não permite a Application referenciar Infrastructure. Evita abstração prematura para um único endpoint (KISS).
- **Autorização global** via `app.MapControllers().RequireAuthorization()`, com `[AllowAnonymous]` no `AuthController`; o `/health` (minimal API) permanece público.

## Component / Module Breakdown

### Componente: `ApplicationDbContext` (alterado)

**Purpose:** Contexto EF Core unificado (domínio + Identity).

**Interface:** `public class ApplicationDbContext : IdentityDbContext<IdentityUser> { ... }`.

**Internal Logic:** Mantém os `DbSet` do Spec 20 e o `OnModelCreating`. O `base.OnModelCreating(builder)` aplica os mappings de Identity.

**Dependencies:** `Microsoft.AspNetCore.Identity.EntityFrameworkCore`.

**Error Handling:** N/A.

### Componente: `JwtTokenService`

**Purpose:** Gerar JWT a partir de um usuário.

**Interface:**
```csharp
namespace CaronaAlvinegra.Infrastructure.Services;

public class JwtTokenService
{
    private readonly IConfiguration _configuration;
    public JwtTokenService(IConfiguration configuration) { }
    public string GenerateToken(IdentityUser user) { }
}
```

**Internal Logic:** Lê `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationMinutes` (ausente ou inválido usa 480); cria `SymmetricSecurityKey` a partir da chave; monta `JwtSecurityToken` com claims `sub` (user.Id), `email` (user.Email) e expiração; assina com `HmacSha256` e retorna `JwtSecurityTokenHandler().WriteToken(...)`.

**Dependencies:** `System.IdentityModel.Tokens.Jwt` (referenciado pela Infrastructure; traz `Microsoft.IdentityModel.Tokens` transitivamente). `Microsoft.AspNetCore.Authentication.JwtBearer` fica na Api, onde é usado para validação.

**Error Handling:** Se `Jwt:Key` estiver ausente/vazia, lança `InvalidOperationException` com mensagem clara.

### Componente: `AuthController`

**Purpose:** Endpoint de login.

**Interface:**
```csharp
namespace CaronaAlvinegra.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    // POST /api/auth/login
}
```

**Internal Logic:** `POST login` recebe `LoginRequest { Email, Password }`; busca o usuário via `UserManager.FindByEmailAsync`; valida senha via `CheckPasswordAsync`; se inválido retorna `Unauthorized()`; se válido retorna `Ok(new LoginResponse(token))` usando `JwtTokenService`.

**Dependencies:** `UserManager<IdentityUser>`, `JwtTokenService`.

**Error Handling:** E-mail inexistente ou senha errada → `401 Unauthorized` (sem diferenciar o motivo).

### Componente: DTOs de login

**Purpose:** Contratos do endpoint.

**Interface:**
```csharp
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token);
```

**Internal Logic:** N/A.

**Dependencies:** Nenhuma.

**Error Handling:** N/A.

### Componente: Seed do usuário Jones

**Purpose:** Criar o usuário admin na inicialização.

**Interface:** Método estático/extension `SeedAdminUserAsync(IServiceProvider)` chamado no `Program.cs`.

**Internal Logic:** Resolve `UserManager<IdentityUser>`; lê `SeedAdmin:Email` e `SeedAdmin:Password` de `IConfiguration`; se o e-mail não existir, `CreateAsync` com a senha; caso contrário, no-op.

**Dependencies:** `UserManager<IdentityUser>`, `IConfiguration`.

**Error Handling:** Se a criação falhar (ex.: senha fora da política), loga o erro e não interrompe a inicialização (mas documenta).

### Componente: Configuração do `Program.cs` (alterado)

**Purpose:** Registrar Identity, JWT e autorização.

**Interface:** trechos adicionados ao `Program.cs` existente.

**Internal Logic:**
```csharp
builder.Services.AddIdentityCore<IdentityUser>(options => {
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireDigit = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    });
builder.Services.AddAuthorization();
```

**Dependencies:** Identity, JwtBearer, `ApplicationDbContext`.

**Error Handling:** `Jwt:Key` ausente lança `InvalidOperationException` antes de `builder.Build()` (fail-fast; a mensagem indica `appsettings.Development.json` ou `Jwt__Key`). Assim o processo não sobe num ambiente sem chave.

## Data Flow

1. `POST /api/auth/login` com `{ email, password }`.
2. `AuthController` busca o usuário e valida a senha via `UserManager`.
3. Se válido, `JwtTokenService.GenerateToken` monta e assina o JWT.
4. O cliente (frontend) envia o token no header `Authorization: Bearer <token>`.
5. O middleware `UseAuthentication` valida o token; `[Authorize]` libera ou bloqueia a rota.

## Data Structures

- **LoginRequest:** `{ string Email, string Password }`.
- **LoginResponse:** `{ string Token }`.
- **Config (`appsettings.Development.json`):**
```json
"Jwt": {
  "Key": "<chave-dev-32+caracteres>",
  "Issuer": "CaronaAlvinegra",
  "Audience": "CaronaAlvinegra",
  "ExpirationMinutes": 480
},
"SeedAdmin": {
  "Email": "jones@caronaalvinegra.local",
  "Password": "Trocar123!"
}
```

## File / Module Layout

```
backend/src/CaronaAlvinegra.Infrastructure/
├── Data/ApplicationDbContext.cs        (editar: herdar IdentityDbContext<IdentityUser>)
├── Services/JwtTokenService.cs         (novo)
└── Migrations/                         (nova migration AddIdentity)

backend/src/CaronaAlvinegra.Api/
├── Program.cs                          (editar: Identity + JWT + seed)
├── AdminSeeder.cs                      (novo: extension SeedAdminUserAsync)
├── Controllers/AuthController.cs       (novo)
├── Contracts/LoginRequest.cs           (novo)
├── Contracts/LoginResponse.cs          (novo)
├── appsettings.json                    (editar: seção Jwt sem segredo)
└── appsettings.Development.json        (editar: Jwt + SeedAdmin)

backend/tests/CaronaAlvinegra.Domain.Tests/
└── JwtTokenServiceTests.cs             (novo; o projeto passa a referenciar a Infrastructure
                                        e o pacote Microsoft.Extensions.Configuration)
```

## Testing Strategy

- **JwtTokenServiceTests:** `GenerateToken` retorna string não-vazia; o token decodificado contém claims `sub` e `email`; sem `Jwt:Key` lança `InvalidOperationException` com mensagem contendo `Jwt:Key`.
- **Login (manual/integração opcional):** credenciais do seed retornam 200; credenciais erradas retornam 401.
- **Build/test:** `dotnet build` e `dotnet test` com exit code 0.

## Migration / Rollback

- **Migration:** `dotnet ef migrations add AddIdentity --project src/CaronaAlvinegra.Infrastructure --startup-project src/CaronaAlvinegra.Api`.
- **Rollback:** `dotnet ef database update <migration-anterior>` (ou `migrations remove` antes de aplicar).
