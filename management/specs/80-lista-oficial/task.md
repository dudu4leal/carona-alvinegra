<!-- PIPELINE-CONTRACT: Task completion is determined by "**Status:** [x] done" at the end of each "### Task N" section.
     Checkboxes "- [ ]" under "**Validation:**" are human verification guides — the pipeline ignores them completely.
     To mark a task as done, add "**Status:** [x] done". To mark it as pending, use "**Status:** [ ] pending". -->
# Spec 80: Geração da Lista Oficial — Implementation Tasks

## Task Order

Tasks MUST be executed sequentially in numerical order. Each task depends on the completion of all preceding tasks.

## Tasks

### Task 1: Criar DTO da lista

**Objective:** Definir contrato de saída.
**Requirements Covered:** FR-001
**Design References:** "Componente: ListaOficialResponse"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Application/Dtos/ListaOficialDtos.cs` com `ListaOficialResponse(string Texto)`.
**Validation:** Arquivo existe e compila.
**Status:** [x] done

### Task 2: Implementar ListaOficialService

**Objective:** Montar o texto da lista.
**Requirements Covered:** FR-001, FR-002, FR-003, FR-004, FR-005
**Design References:** "Componente: ListaOficialService" e formatações
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Application/Services/ListaOficialService.cs`.
2. Implementar `GerarAsync` (cabeçalho, blocos de van com responsável primeiro, bloco spin, rodapé fixo como constante).
**Validation:** `dotnet build` compila.
**Status:** [x] done

### Task 3: Criar ListaController

**Objective:** Endpoint REST da lista.
**Requirements Covered:** FR-006
**Design References:** "Componente: ListaController"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Api/Controllers/ListaController.cs` com `[Authorize]` e `GET /api/jogos/{jogoId}/lista`.
**Validation:** Rota existe; `dotnet build` compila.
**Status:** [x] done

### Task 4: Registrar DI

**Objective:** Registrar o service.
**Requirements Covered:** FR-006 (parcial)
**Design References:** "Componente: ListaController"
**Actions:**
1. Editar `Program.cs`: `AddScoped<ListaOficialService>()`.
**Validation:** `Program.cs` compila.
**Status:** [x] done

### Task 5: Criar testes da lista

**Objective:** Testar o formato.
**Requirements Covered:** FR-007
**Design References:** "Testing Strategy"
**Actions:**
1. Criar `backend/tests/CaronaAlvinegra.Domain.Tests/ListaOficialServiceTests.cs`.
2. Testar cabeçalho, responsável primeiro com `( Resp)`, rodapé fixo e omissão do spin quando ausente.
**Validation:** `dotnet test backend/tests/CaronaAlvinegra.Domain.Tests` termina com exit code 0.
**Status:** [x] done
