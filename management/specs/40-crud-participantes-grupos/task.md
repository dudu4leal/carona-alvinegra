<!-- PIPELINE-CONTRACT: Task completion is determined by "**Status:** [x] done" at the end of each "### Task N" section.
     Checkboxes "- [ ]" under "**Validation:**" are human verification guides — the pipeline ignores them completely.
     To mark a task as done, add "**Status:** [x] done". To mark it as pending, use "**Status:** [ ] pending". -->
# Spec 40: CRUD de Participantes e Grupos — Implementation Tasks

## Task Order

Tasks MUST be executed sequentially in numerical order. Each task depends on the completion of all preceding tasks.

## Tasks

### Task 1: Criar interfaces de repositório

**Objective:** Definir contratos de persistência.
**Requirements Covered:** FR-001
**Design References:** "Componente: IGrupoRepository / IParticipanteRepository"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Application/Interfaces/IGrupoRepository.cs`.
2. Criar `backend/src/CaronaAlvinegra.Application/Interfaces/IParticipanteRepository.cs` (com `CountByGrupoAsync`).
**Validation:** Os arquivos existem e `dotnet build` compila.
**Status:** [ ] pending

### Task 2: Criar DTOs

**Objective:** Definir contratos de entrada/saída.
**Requirements Covered:** FR-003
**Design References:** "Componente: DTOs"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Application/Dtos/GrupoDtos.cs` (records Create/Update/Response).
2. Criar `backend/src/CaronaAlvinegra.Application/Dtos/ParticipanteDtos.cs` (records Create/Update/Response).
**Validation:** Arquivos existem e compilam.
**Status:** [ ] pending

### Task 3: Implementar repositórios EF Core

**Objective:** Persistência real.
**Requirements Covered:** FR-002
**Design References:** "Componente: GrupoRepository / ParticipanteRepository"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Infrastructure/Repositories/GrupoRepository.cs`.
2. Criar `backend/src/CaronaAlvinegra.Infrastructure/Repositories/ParticipanteRepository.cs` (incluindo `CountByGrupoAsync` e filtro por `grupoId`).
**Validation:** `dotnet build backend/CaronaAlvinegra.sln` compila.
**Status:** [ ] pending

### Task 4: Implementar GrupoService

**Objective:** Casos de uso de Grupo.
**Requirements Covered:** FR-004
**Design References:** "Componente: GrupoService"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Application/Services/GrupoService.cs` com Criar/Listar/Obter/Atualizar/Excluir.
2. `ExcluirAsync` retorna conflito se o grupo tem participantes.
**Validation:** Arquivo existe e compila.
**Status:** [ ] pending

### Task 5: Implementar ParticipanteService

**Objective:** Casos de uso de Participante.
**Requirements Covered:** FR-005, FR-006
**Design References:** "Componente: ParticipanteService"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Application/Services/ParticipanteService.cs`.
2. Validar existência do grupo e capacidade (`CountByGrupoAsync >= Grupo.CapacidadeMaxima` → `DomainException`).
**Validation:** Arquivo existe e compila.
**Status:** [ ] pending

### Task 6: Criar middleware de DomainException

**Objective:** Mapear exceções de domínio para 400.
**Requirements Covered:** NFR-002
**Design References:** "Componente: Tratamento de DomainException"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Api/Middleware/DomainExceptionMiddleware.cs`.
2. Registrar no `Program.cs` (`app.UseMiddleware<DomainExceptionMiddleware>()`).
**Validation:** `DomainException` retorna 400 com mensagem.
**Status:** [ ] pending

### Task 7: Criar GruposController

**Objective:** Endpoints REST de grupos.
**Requirements Covered:** FR-007
**Design References:** "Componente: GruposController / ParticipantesController"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Api/Controllers/GruposController.cs` com `[Authorize]` e as 5 rotas.
**Validation:** Rotas existem; `dotnet build` compila.
**Status:** [ ] pending

### Task 8: Criar ParticipantesController

**Objective:** Endpoints REST de participantes.
**Requirements Covered:** FR-008
**Design References:** "Componente: GruposController / ParticipantesController"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Api/Controllers/ParticipantesController.cs` com `[Authorize]` e as 5 rotas (filtro `?grupoId=`).
**Validation:** Rotas existem; `dotnet build` compila.
**Status:** [ ] pending

### Task 9: Registrar DI

**Objective:** Registrar repositórios e services.
**Requirements Covered:** FR-009
**Design References:** "Componente: GruposController / ParticipantesController"
**Actions:**
1. Editar `Program.cs`: `AddScoped<IGrupoRepository, GrupoRepository>()`, `AddScoped<IParticipanteRepository, ParticipanteRepository>()`, `AddScoped<GrupoService>()`, `AddScoped<ParticipanteService>()`.
**Validation:** `Program.cs` compila e os services são resolvíveis.
**Status:** [ ] pending

### Task 10: Criar testes de caso de uso

**Objective:** Testar capacidade e CRUD.
**Requirements Covered:** FR-010
**Design References:** "Testing Strategy"
**Actions:**
1. Criar `backend/tests/CaronaAlvinegra.Domain.Tests/ParticipanteServiceTests.cs` (repositórios in-memory/mock).
2. Testar 15 OK, 16º lança `DomainException`, excluir grupo com participantes retorna conflito.
**Validation:** `dotnet test backend/tests/CaronaAlvinegra.Domain.Tests` termina com exit code 0.
**Status:** [ ] pending
