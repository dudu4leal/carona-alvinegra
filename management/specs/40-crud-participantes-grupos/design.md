# Spec 40: CRUD de Participantes e Grupos — Design

## Design Approach

Implementar o padrão DDD tático completo para os dois recursos: **interfaces de repositório** na Application, **implementações EF Core** na Infrastructure, **casos de uso (services)** na Application e **controllers** na API. A regra de capacidade (máx. 15) é validada no caso de uso usando a constante `Grupo.CapacidadeMaxima`.

## Architecture Decisions

- **Participante tem repositório próprio** (`IParticipanteRepository`) com FK `GrupoId`, em vez de ser manipulado exclusivamente via aggregate `Grupo` — mais ergonômico para o back-office e para os endpoints REST. A invariante de capacidade é garantida no caso de uso.
- **Fonte única do número 15:** a constante `Grupo.CapacidadeMaxima` (Spec 20) é usada em toda validação (DRY).
- **`DomainException` → 400** via `[ApiController]` + um middleware/filtro simples, para não vazar stack trace.
- **Excluir grupo com participantes retorna 409** (não cascade), evitando perda acidental de dados.
- **Mutação encapsulada no domínio:** para viabilizar `AtualizarAsync` sem expor setters, este spec adiciona `Grupo.Atualizar(nome, rota)` e `Participante.Atualizar(nome, whatsApp, grupoId)`. Ambos reutilizam a validação do construtor e não alteram o schema.

## Component / Module Breakdown

### Componente: `IGrupoRepository` / `IParticipanteRepository` (Application)

**Purpose:** Contrato de persistência.

**Interface:**
```csharp
public interface IGrupoRepository
{
    Task<Grupo?> GetByIdAsync(Guid id);
    Task<List<Grupo>> GetAllAsync();
    Task AddAsync(Grupo grupo);
    void Update(Grupo grupo);
    void Remove(Grupo grupo);
    Task SaveChangesAsync();
}

public interface IParticipanteRepository
{
    Task<Participante?> GetByIdAsync(Guid id);
    Task<List<Participante>> GetAllAsync(Guid? grupoId = null);
    Task AddAsync(Participante p);
    void Update(Participante p);
    void Remove(Participante p);
    Task<int> CountByGrupoAsync(Guid grupoId);
    Task SaveChangesAsync();
}
```

**Internal Logic:** Apenas assinaturas.

**Dependencies:** Entidades do domínio.

**Error Handling:** N/A.

### Componente: `GrupoRepository` / `ParticipanteRepository` (Infrastructure)

**Purpose:** Implementação EF Core.

**Interface:** Implementam as interfaces acima.

**Internal Logic:** Usam `ApplicationDbContext`. `GetAllAsync` de participante aplica `Where(p => p.GrupoId == grupoId)` quando o filtro é informado. `CountByGrupoAsync` faz `CountAsync(p => p.GrupoId == grupoId)`.

**Dependencies:** `ApplicationDbContext`, entidades.

**Error Handling:** N/A.

### Componente: DTOs (Application)

**Purpose:** Contratos de entrada/saída.

**Interface:**
```csharp
public record GrupoCreateRequest(string Nome, Rota Rota);
public record GrupoUpdateRequest(string Nome, Rota Rota);
public record GrupoResponse(Guid Id, string Nome, Rota Rota, List<ParticipanteResponse> Participantes);

public record ParticipanteCreateRequest(string Nome, string WhatsApp, Guid GrupoId);
public record ParticipanteUpdateRequest(string Nome, string WhatsApp, Guid GrupoId);
public record ParticipanteResponse(Guid Id, string Nome, string WhatsApp, Guid GrupoId);
```

**Internal Logic:** N/A.

**Dependencies:** `Rota`.

**Error Handling:** N/A.

### Componente: `GrupoService` (Application)

**Purpose:** Casos de uso de Grupo.

**Interface:**
```csharp
public enum ExclusaoResult { Ok, NotFound, Conflito }

public class GrupoService
{
    Task<GrupoResponse> CriarAsync(GrupoCreateRequest req);
    Task<List<GrupoResponse>> ListarAsync();
    Task<GrupoResponse?> ObterAsync(Guid id);
    Task<GrupoResponse?> AtualizarAsync(Guid id, GrupoUpdateRequest req);
    Task<ExclusaoResult> ExcluirAsync(Guid id);
}
```

**Internal Logic:** `CriarAsync` instancia `Grupo(nome, rota)` e persiste. `ExcluirAsync` carrega o grupo; se não existir retorna `NotFound`; se `CountByGrupoAsync(grupoId) > 0` retorna `Conflito`; senão remove, salva e retorna `Ok`.

**Dependencies:** `IGrupoRepository`, `IParticipanteRepository`.

**Error Handling:** `DomainException` para nome vazio (domínio).

### Componente: `ParticipanteService` (Application)

**Purpose:** Casos de uso de Participante.

**Interface:**
```csharp
public class ParticipanteService
{
    Task<ParticipanteResponse> CriarAsync(ParticipanteCreateRequest req);
    Task<List<ParticipanteResponse>> ListarAsync(Guid? grupoId = null);
    Task<ParticipanteResponse?> ObterAsync(Guid id);
    Task<ParticipanteResponse?> AtualizarAsync(Guid id, ParticipanteUpdateRequest req);
    Task<bool> ExcluirAsync(Guid id); // true = excluído; false = não encontrado
}
```

**Internal Logic:** `CriarAsync` valida existência do grupo e a capacidade (`CountByGrupoAsync(grupoId) >= Grupo.CapacidadeMaxima` → `DomainException`); instancia `Participante(nome, whatsapp, grupoId)` e persiste. `AtualizarAsync` valida capacidade ao mudar de grupo.

**Dependencies:** `IGrupoRepository`, `IParticipanteRepository`, `Grupo.CapacidadeMaxima`.

**Error Handling:** `DomainException` para capacidade excedida ou campos vazios.

### Componente: `GruposController` / `ParticipantesController` (Api)

**Purpose:** Endpoints REST.

**Interface:** `[Authorize] [ApiController] [Route("api/grupos")]` e `[Route("api/participantes")]`.

**Internal Logic:** Delegam aos services e traduzem resultados para códigos HTTP (200/201/204/404/409/400).

**Dependencies:** Services da Application.

**Error Handling:** `DomainException` → 400; não encontrado → 404; conflito → 409.

### Componente: Tratamento de `DomainException` (Api)

**Purpose:** Mapear exceção de domínio para 400.

**Interface:** `DomainExceptionMiddleware` (middleware dedicado) registrado no `Program.cs`.

**Internal Logic:** Captura `DomainException` e retorna `400` com `{ "message": ... }`.

**Dependencies:** `DomainException`.

**Error Handling:** N/A.

## Data Flow

1. `POST /api/grupos` → `GruposController` → `GrupoService.CriarAsync` → `GrupoRepository.AddAsync` + `SaveChangesAsync` → 201.
2. `POST /api/participantes` → `ParticipantesController` → `ParticipanteService.CriarAsync` (valida grupo + capacidade) → `ParticipanteRepository.AddAsync` + save → 201.
3. `DELETE /api/grupos/{id}` → `GrupoService.ExcluirAsync` (checa participantes) → remove ou 409.

## Data Structures

- **Tabelas:** `Grupos` e `Participantes` (definidas no Spec 20), com `Participante.GrupoId` como FK para `Grupo.Id`.
- **Respostas:** `GrupoResponse` inclui a lista de `ParticipanteResponse` (participantes do grupo).

## File / Module Layout

```
backend/src/CaronaAlvinegra.Domain/
└── Entities/Grupo.cs, Participante.cs     (editar: mutators Atualizar)

backend/src/CaronaAlvinegra.Application/
├── Interfaces/IGrupoRepository.cs          (novo)
├── Interfaces/IParticipanteRepository.cs   (novo)
├── Dtos/GrupoDtos.cs                       (novo)
├── Dtos/ParticipanteDtos.cs                (novo)
└── Services/GrupoService.cs                (novo)
└── Services/ParticipanteService.cs         (novo)

backend/src/CaronaAlvinegra.Infrastructure/
└── Repositories/GrupoRepository.cs         (novo)
└── Repositories/ParticipanteRepository.cs  (novo)

backend/src/CaronaAlvinegra.Api/
├── Controllers/GruposController.cs         (novo)
├── Controllers/ParticipantesController.cs  (novo)
├── Middleware/DomainExceptionMiddleware.cs (novo)
└── Program.cs                              (editar: registrar DI)

backend/tests/CaronaAlvinegra.Domain.Tests/
└── ParticipanteServiceTests.cs             (novo)
```

## Testing Strategy

- **ParticipanteServiceTests** (com fakes in-memory escritos à mão, sem pacote de mock — ver Spec 10): criar até 15 OK; 16º lança `DomainException`; mover para grupo cheio lança `DomainException`; atualizar participante no mesmo grupo cheio não lança (não consome nova vaga); criar em grupo inexistente lança `DomainException`; excluir grupo com participantes retorna conflito.
- **Build/test:** `dotnet build` e `dotnet test` com exit code 0.

## Migration / Rollback

- **Sem migration nova** — as tabelas `Grupos`/`Participantes` já existem do Spec 20. Nenhuma alteração de schema.
- **Rollback:** N/A (apenas código de aplicação/infraestrutura).
