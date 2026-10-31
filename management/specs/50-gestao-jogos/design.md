# Spec 50: Gestão de Jogos — Design

## Design Approach

Seguir o mesmo padrão do Spec 40 (interface de repositório → implementação EF Core → service → controller), mas para o recurso único `Jogo`. Reutiliza o middleware de `DomainException` e o padrão de DI já estabelecidos.

## Architecture Decisions

- **`Jogo` é aggregate root** (Spec 20), com repositório e CRUD próprios.
- **Ordenação da listagem por `Data` ascendente** (próximos jogos primeiro), útil para o Jones e para o cabeçalho da lista.
- **Reutilização do middleware `DomainExceptionMiddleware`** (Spec 40) para mapear regras de domínio para 400.
- **`Data` persistida em UTC:** Npgsql mapeia `DateTime` para `timestamp with time zone` e só aceita `DateTimeKind.Utc`; o `JogoService` normaliza `Data` antes de criar/atualizar (`Unspecified` → UTC, `Local` → UTC). A parte de data do calendário é preservada para os Specs 80/90.

## Component / Module Breakdown

### Componente: `IJogoRepository` (Application)

**Purpose:** Contrato de persistência.

**Interface:**
```csharp
public interface IJogoRepository
{
    Task<Jogo?> GetByIdAsync(Guid id);
    Task<List<Jogo>> GetAllAsync();
    Task AddAsync(Jogo jogo);
    void Update(Jogo jogo);
    void Remove(Jogo jogo);
    Task SaveChangesAsync();
}
```

**Internal Logic:** Apenas assinaturas.

**Dependencies:** `Jogo` (domínio).

**Error Handling:** N/A.

### Componente: `JogoRepository` (Infrastructure)

**Purpose:** Implementação EF Core.

**Interface:** Implementa `IJogoRepository`.

**Internal Logic:** `GetAllAsync` retorna `await _context.Jogos.OrderBy(j => j.Data).ToListAsync()`. `Update`/`Remove` chamam `_context.Jogos.Update/Remove`. `SaveChangesAsync` chama `_context.SaveChangesAsync()`.

**Dependencies:** `ApplicationDbContext`.

**Error Handling:** N/A.

### Componente: DTOs (Application)

**Purpose:** Contratos de entrada/saída.

**Interface:**
```csharp
public record JogoCreateRequest(string CompeticaoCodigo, string Adversario, DateTime Data, TimeOnly HorarioSaida, string PontoEmbarque);
public record JogoUpdateRequest(string CompeticaoCodigo, string Adversario, DateTime Data, TimeOnly HorarioSaida, string PontoEmbarque);
public record JogoResponse(Guid Id, string CompeticaoCodigo, string Adversario, DateTime Data, TimeOnly HorarioSaida, string PontoEmbarque);
```

**Internal Logic:** N/A.

**Dependencies:** Nenhuma.

**Error Handling:** N/A.

### Componente: `JogoService` (Application)

**Purpose:** Casos de uso de Jogo.

**Interface:**
```csharp
public class JogoService
{
    Task<JogoResponse> CriarAsync(JogoCreateRequest req);
    Task<List<JogoResponse>> ListarAsync();
    Task<JogoResponse?> ObterAsync(Guid id);
    Task<JogoResponse?> AtualizarAsync(Guid id, JogoUpdateRequest req);
    Task<bool> ExcluirAsync(Guid id); // true = excluído; false = não encontrado
}
```

**Internal Logic:** `CriarAsync` instancia `Jogo(...)` (o domínio valida campos vazios) e persiste. `AtualizarAsync` carrega o jogo, chama `Jogo.Atualizar(competicaoCodigo, adversario, data, horarioSaida, pontoEmbarque)` e salva; retorna `null` se o jogo não existir. `ExcluirAsync` remove e salva; retorna `false` se o jogo não existir.

> **Nota:** o método `Jogo.Atualizar(string competicaoCodigo, string adversario, DateTime data, TimeOnly horarioSaida, string pontoEmbarque)` deve ser adicionado ao domínio como parte deste spec (com as mesmas validações de não-vazio do construtor), pois o Spec 20 definiu apenas o construtor.

**Dependencies:** `IJogoRepository`, `Jogo` (domínio).

**Error Handling:** `DomainException` propagada (mapeada para 400 pelo middleware).

### Componente: `JogosController` (Api)

**Purpose:** Endpoints REST.

**Interface:** `[Authorize] [ApiController] [Route("api/jogos")]`.

**Internal Logic:** Mapeia as 5 rotas para `JogoService`, retornando 201/200/204/404.

**Dependencies:** `JogoService`.

**Error Handling:** Não encontrado → 404; `DomainException` → 400 (middleware).

## Data Flow

1. `POST /api/jogos` → `JogosController` → `JogoService.CriarAsync` → `JogoRepository.AddAsync` + `SaveChangesAsync` → 201.
2. `GET /api/jogos` → `JogoService.ListarAsync` → `JogoRepository.GetAllAsync` (ordenado por data) → 200.
3. `PUT /api/jogos/{id}` → carrega, atualiza, salva → 200/404.
4. `DELETE /api/jogos/{id}` → remove, salva → 204/404.

## Data Structures

- **Tabela:** `Jogos` (definida no Spec 20): `Id`, `CompeticaoCodigo`, `Adversario`, `Data`, `HorarioSaida`, `PontoEmbarque`.

## File / Module Layout

```
backend/src/CaronaAlvinegra.Application/
├── Interfaces/IJogoRepository.cs        (novo)
├── Dtos/JogoDtos.cs                     (novo)
└── Services/JogoService.cs              (novo)

backend/src/CaronaAlvinegra.Infrastructure/
└── Repositories/JogoRepository.cs       (novo)

backend/src/CaronaAlvinegra.Api/
├── Controllers/JogosController.cs       (novo)
└── Program.cs                           (editar: registrar DI)

backend/tests/CaronaAlvinegra.Domain.Tests/
└── JogoServiceTests.cs                  (novo)
```

## Testing Strategy

- **JogoServiceTests** (com fakes in-memory escritos à mão, sem pacote de mock — ver Spec 10): criar jogo com `Adversario` vazio lança `DomainException`; criar/obter/atualizar/excluir funciona; criar/atualizar com `Data` sem fuso normaliza para UTC.
- **Build/test:** `dotnet build` e `dotnet test` com exit code 0.

## Migration / Rollback

- **Sem migration nova** — a tabela `Jogos` já existe do Spec 20.
- **Rollback:** N/A.
