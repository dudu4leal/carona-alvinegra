<!-- PIPELINE-CONTRACT: Task completion is determined by "**Status:** [x] done" at the end of each "### Task N" section.
     Checkboxes "- [ ]" under "**Validation:**" are human verification guides — the pipeline ignores them completely.
     To mark a task as done, add "**Status:** [x] done". To mark it as pending, use "**Status:** [ ] pending". -->
# Spec 60: Marcação de Pagamento — Implementation Tasks

## Task Order

Tasks MUST be executed sequentially in numerical order. Each task depends on the completion of all preceding tasks.

## Tasks

### Task 1: Criar interface IPagamentoRepository

**Objective:** Definir contrato de persistência.
**Requirements Covered:** FR-001
**Design References:** "Componente: IPagamentoRepository"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Application/Interfaces/IPagamentoRepository.cs`.
**Validation:** Arquivo existe e compila.
**Status:** [ ] pending

### Task 2: Criar DTOs de Pagamento

**Objective:** Definir contratos de saída.
**Requirements Covered:** FR-003
**Design References:** "Componente: DTOs"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Application/Dtos/PagamentoDtos.cs` com `PagamentoResponse` e `DesmarcarPagamentoResponse`.
**Validation:** Arquivo existe e compila.
**Status:** [ ] pending

### Task 3: Implementar PagamentoRepository

**Objective:** Persistência real.
**Requirements Covered:** FR-002
**Design References:** "Componente: PagamentoRepository"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Infrastructure/Repositories/PagamentoRepository.cs`.
2. Implementar `GetByParticipanteJogoAsync` e `GetByJogoAsync`.
**Validation:** `dotnet build backend/CaronaAlvinegra.sln` compila.
**Status:** [ ] pending

### Task 4: Implementar PagamentoService

**Objective:** Casos de uso de pagamento.
**Requirements Covered:** FR-004, FR-005
**Design References:** "Componente: PagamentoService"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Application/Services/PagamentoService.cs`.
2. Implementar `MarcarPagoAsync`, `DesmarcarPagoAsync` (com cálculo de reembolso de 48h) e `ListarPorJogoAsync`.
**Validation:** Arquivo existe e compila.
**Status:** [ ] pending

### Task 5: Criar PagamentosController

**Objective:** Endpoints REST de pagamento.
**Requirements Covered:** FR-006
**Design References:** "Componente: PagamentosController"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Api/Controllers/PagamentosController.cs` com `[Authorize]` e as 3 rotas.
**Validation:** Rotas existem; `dotnet build` compila.
**Status:** [ ] pending

### Task 6: Registrar DI

**Objective:** Registrar repositório e service.
**Requirements Covered:** FR-007
**Design References:** "Componente: PagamentosController"
**Actions:**
1. Editar `Program.cs`: `AddScoped<IPagamentoRepository, PagamentoRepository>()` e `AddScoped<PagamentoService>()`.
**Validation:** `Program.cs` compila.
**Status:** [ ] pending

### Task 7: Criar testes do PagamentoService

**Objective:** Testar marcação e reembolso.
**Requirements Covered:** FR-008
**Design References:** "Testing Strategy"
**Actions:**
1. Criar `backend/tests/CaronaAlvinegra.Domain.Tests/PagamentoServiceTests.cs`.
2. Testar marcar pago, desmarcar ≥48h → `ReembolsoDevido=true`, desmarcar <48h → `ReembolsoDevido=false`.
**Validation:** `dotnet test backend/tests/CaronaAlvinegra.Domain.Tests` termina com exit code 0.
**Status:** [ ] pending
