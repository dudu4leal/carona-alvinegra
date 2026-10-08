<!-- PIPELINE-CONTRACT: Task completion is determined by "**Status:** [x] done" at the end of each "### Task N" section.
     Checkboxes "- [ ]" under "**Validation:**" are human verification guides — the pipeline ignores them completely.
     To mark a task as done, add "**Status:** [x] done". To mark it as pending, use "**Status:** [ ] pending". -->
# Spec 50: Gestão de Jogos — Implementation Tasks

## Task Order

Tasks MUST be executed sequentially in numerical order. Each task depends on the completion of all preceding tasks.

## Tasks

### Task 1: Criar interface IJogoRepository

**Objective:** Definir contrato de persistência.
**Requirements Covered:** FR-001
**Design References:** "Componente: IJogoRepository"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Application/Interfaces/IJogoRepository.cs`.
**Validation:** Arquivo existe e compila.
**Status:** [ ] pending

### Task 2: Criar DTOs de Jogo

**Objective:** Definir contratos de entrada/saída.
**Requirements Covered:** FR-003
**Design References:** "Componente: DTOs"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Application/Dtos/JogoDtos.cs` com Create/Update/Response.
**Validation:** Arquivo existe e compila.
**Status:** [ ] pending

### Task 3: Implementar JogoRepository

**Objective:** Persistência real.
**Requirements Covered:** FR-002
**Design References:** "Componente: JogoRepository"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Infrastructure/Repositories/JogoRepository.cs`.
2. Implementar `GetAllAsync` ordenado por `Data` ascendente.
**Validation:** `dotnet build backend/CaronaAlvinegra.sln` compila.
**Status:** [ ] pending

### Task 4: Implementar JogoService

**Objective:** Casos de uso de Jogo.
**Requirements Covered:** FR-004
**Design References:** "Componente: JogoService"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Application/Services/JogoService.cs` com Criar/Listar/Obter/Atualizar/Excluir.
**Validation:** Arquivo existe e compila.
**Status:** [ ] pending

### Task 5: Criar JogosController

**Objective:** Endpoints REST de jogos.
**Requirements Covered:** FR-005
**Design References:** "Componente: JogosController"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Api/Controllers/JogosController.cs` com `[Authorize]` e as 5 rotas.
**Validation:** Rotas existem; `dotnet build` compila.
**Status:** [ ] pending

### Task 6: Registrar DI

**Objective:** Registrar repositório e service.
**Requirements Covered:** FR-006
**Design References:** "Componente: JogosController"
**Actions:**
1. Editar `Program.cs`: `AddScoped<IJogoRepository, JogoRepository>()` e `AddScoped<JogoService>()`.
**Validation:** `Program.cs` compila.
**Status:** [ ] pending

### Task 7: Criar testes do JogoService

**Objective:** Testar CRUD e validações.
**Requirements Covered:** FR-007
**Design References:** "Testing Strategy"
**Actions:**
1. Criar `backend/tests/CaronaAlvinegra.Domain.Tests/JogoServiceTests.cs`.
2. Testar: `Adversario` vazio lança `DomainException`; criar/obter/atualizar/excluir funciona.
**Validation:** `dotnet test backend/tests/CaronaAlvinegra.Domain.Tests` termina com exit code 0.
**Status:** [ ] pending
