# Spec 60: Marcação de Pagamento — Design

## Design Approach

Seguir o padrão dos Specs 40/50 (interface de repositório → implementação EF Core → service → controller). O `PagamentoService` concentra a marcação/desmarcação e a **regra de reembolso de 48h**. O "momento do jogo" é `Jogo.Data.Date + Jogo.HorarioSaida`.

## Architecture Decisions

- **Pagamento é identificado por `(ParticipanteId, JogoId)`** — um pagamento por participante por jogo.
- **Upsert na marcação:** `MarcarPagoAsync` cria ou atualiza o registro (idempotente).
- **Regra de reembolso no service**, usando `Jogo` para calcular a janela de 48h.
- **Reposição de vaga é decisão manual do Jones** (não automatizada).
- **Fuso horário explícito:** "horário de saída" é horário de Brasília (UTC-3, sem horário de verão desde 2019); o service converte o momento do jogo para UTC e compara com `DateTime.UtcNow`, independente do fuso do servidor.

## Component / Module Breakdown

### Componente: `IPagamentoRepository` (Application)

**Purpose:** Contrato de persistência.

**Interface:**
```csharp
public interface IPagamentoRepository
{
    Task<Pagamento?> GetByParticipanteJogoAsync(Guid participanteId, Guid jogoId);
    Task<List<Pagamento>> GetByJogoAsync(Guid jogoId);
    Task AddAsync(Pagamento pagamento);
    void Update(Pagamento pagamento);
    void Remove(Pagamento pagamento);
    Task SaveChangesAsync();
}
```

**Internal Logic:** Apenas assinaturas.

**Dependencies:** `Pagamento` (domínio).

**Error Handling:** N/A.

### Componente: `PagamentoRepository` (Infrastructure)

**Purpose:** Implementação EF Core.

**Interface:** Implementa `IPagamentoRepository`.

**Internal Logic:** `GetByParticipanteJogoAsync` usa `FirstOrDefaultAsync(p => p.ParticipanteId == participanteId && p.JogoId == jogoId)`. `GetByJogoAsync` usa `Where(p => p.JogoId == jogoId)`.

**Dependencies:** `ApplicationDbContext`.

**Error Handling:** N/A.

### Componente: DTOs (Application)

**Purpose:** Contratos de saída.

**Interface:**
```csharp
public record PagamentoResponse(Guid Id, Guid ParticipanteId, Guid JogoId, bool Pago);
public record DesmarcarPagamentoResponse(bool ReembolsoDevido);
```

**Internal Logic:** N/A.

**Dependencies:** Nenhuma.

**Error Handling:** N/A.

### Componente: `PagamentoService` (Application)

**Purpose:** Casos de uso de pagamento.

**Interface:**
```csharp
public class PagamentoService
{
    Task<PagamentoResponse?> MarcarPagoAsync(Guid jogoId, Guid participanteId);
    Task<DesmarcarPagamentoResponse?> DesmarcarPagoAsync(Guid jogoId, Guid participanteId);
    Task<List<PagamentoResponse>> ListarPorJogoAsync(Guid jogoId);
}
```

**Internal Logic:**
- `MarcarPagoAsync`: valida que jogo e participante existem; se algum não existir, retorna `null` (404 no controller). Busca pagamento por `(participanteId, jogoId)`; se não existir, cria `Pagamento(participanteId, jogoId)` e `MarcarPago()`; senão `MarcarPago()`; salva.
- `DesmarcarPagoAsync`: se o jogo não existir, retorna `null` (404). Busca pagamento por `(participanteId, jogoId)`; se **não existir**, retorna `DesmarcarPagamentoResponse(ReembolsoDevido: false)` sem criar registro; se existir, `DesmarcarPago()` + salva e calcula `ReembolsoDevido` comparando `DateTime.UtcNow` com o momento do jogo (horário de Brasília convertido para UTC) menos 48h.
- `ListarPorJogoAsync`: retorna pagamentos do jogo.

**Dependencies:** `IPagamentoRepository`, `IJogoRepository`, `IParticipanteRepository`, `Pagamento`, `Jogo`.

**Error Handling:** `DomainException` propagada; participante/jogo inexistente → erro de aplicação (404 no controller).

### Componente: `PagamentosController` (Api)

**Purpose:** Endpoints REST.

**Interface:** `[Authorize] [ApiController] [Route("api/jogos/{jogoId}/pagamentos")]`.

**Internal Logic:** Mapeia `GET` (lista), `POST .../marcar-pago`, `POST .../desmarcar-pago` para o service; retorna 200/404.

**Dependencies:** `PagamentoService`.

**Error Handling:** Não encontrado → 404; `DomainException` → 400.

## Data Flow

1. `POST /api/jogos/{jogoId}/pagamentos/{participanteId}/marcar-pago` → `PagamentoService.MarcarPagoAsync` (upsert `Pago=true`) → 200.
2. `POST .../desmarcar-pago` → `PagamentoService.DesmarcarPagoAsync` (`Pago=false` + `ReembolsoDevido`) → 200 com `{ reembolsoDevido }`.
3. `GET /api/jogos/{jogoId}/pagamentos` → `PagamentoService.ListarPorJogoAsync` → 200.

## Data Structures

- **Tabela:** `Pagamentos` (Spec 20): `Id`, `ParticipanteId`, `JogoId`, `Pago`.
- **Cálculo do momento do jogo:** `jogo.Data.Date + jogo.HorarioSaida`.

## File / Module Layout

```
backend/src/CaronaAlvinegra.Application/
├── Interfaces/IPagamentoRepository.cs   (novo)
├── Dtos/PagamentoDtos.cs                (novo)
└── Services/PagamentoService.cs         (novo)

backend/src/CaronaAlvinegra.Infrastructure/
└── Repositories/PagamentoRepository.cs  (novo)

backend/src/CaronaAlvinegra.Api/
├── Controllers/PagamentosController.cs  (novo)
└── Program.cs                           (editar: registrar DI)

backend/tests/CaronaAlvinegra.Domain.Tests/
└── PagamentoServiceTests.cs             (novo)
```

## Testing Strategy

- **PagamentoServiceTests** (com fakes in-memory escritos à mão, sem pacote de mock — ver Spec 10): marcar cria `Pago=true`; desmarcar ≥48h → `ReembolsoDevido=true`; desmarcar <48h → `ReembolsoDevido=false`; idempotência.
- **Build/test:** `dotnet build` e `dotnet test` com exit code 0.

## Migration / Rollback

- **Sem migration nova** — a tabela `Pagamentos` já existe do Spec 20.
- **Rollback:** N/A.
