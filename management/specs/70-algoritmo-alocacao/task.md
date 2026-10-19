<!-- PIPELINE-CONTRACT: Task completion is determined by "**Status:** [x] done" at the end of each "### Task N" section.
     Checkboxes "- [ ]" under "**Validation:**" are human verification guides — the pipeline ignores them completely.
     To mark a task as done, add "**Status:** [x] done". To mark it as pending, use "**Status:** [ ] pending". -->
# Spec 70: Algoritmo de Alocação — Implementation Tasks

## Task Order

Tasks MUST be executed sequentially in numerical order. Each task depends on the completion of all preceding tasks.

## Tasks

### Task 1: Criar entidades de alocação

**Objective:** Criar Alocacao, VanAlocada e SpinAlocado.
**Requirements Covered:** FR-001
**Design References:** "Componente: Alocacao", "VanAlocada", "SpinAlocado"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Domain/Entities/VanAlocada.cs`.
2. Criar `backend/src/CaronaAlvinegra.Domain/Entities/SpinAlocado.cs`.
3. Criar `backend/src/CaronaAlvinegra.Domain/Entities/Alocacao.cs` (aggregate com Vans/Spin/GruposEscalados).
**Validation:** Arquivos existem e `dotnet build` compila.
**Status:** [ ] pending

### Task 2: Implementar Alocador

**Objective:** Algoritmo de empacotamento.
**Requirements Covered:** FR-002, FR-003, FR-004, FR-005, FR-006
**Design References:** "Componente: Alocador"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Domain/Services/Alocador.cs`.
2. Implementar first-fit decrescente, separação de sobras, spin/escalação e promoção.
**Validation:** `dotnet build` compila; lógica segue o design.
**Status:** [ ] pending

### Task 3: Criar testes do Alocador

**Objective:** Testar o algoritmo.
**Requirements Covered:** FR-010
**Design References:** "Testing Strategy"
**Actions:**
1. Criar `backend/tests/CaronaAlvinegra.Domain.Tests/AlocadorTests.cs`.
2. Implementar os 7 casos de teste do design.
**Validation:** `dotnet test backend/tests/CaronaAlvinegra.Domain.Tests` termina com exit code 0.
**Status:** [ ] pending

### Task 4: Criar DTOs de alocação

**Objective:** Contratos de resposta.
**Requirements Covered:** FR-008 (parcial)
**Design References:** "Componente: DTOs"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Application/Dtos/AlocacaoDtos.cs`.
**Validation:** Arquivo existe e compila.
**Status:** [ ] pending

### Task 5: Criar IAlocacaoRepository e AlocacaoRepository

**Objective:** Persistência da alocação.
**Requirements Covered:** FR-007
**Design References:** "Componente: IAlocacaoRepository / AlocacaoRepository"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Application/Interfaces/IAlocacaoRepository.cs`.
2. Criar `backend/src/CaronaAlvinegra.Infrastructure/Repositories/AlocacaoRepository.cs`.
3. Criar `backend/src/CaronaAlvinegra.Application/Interfaces/IListaEsperaRepository.cs` (com `GetByJogoRotaAsync`).
4. Criar `backend/src/CaronaAlvinegra.Infrastructure/Repositories/ListaEsperaRepository.cs`.
**Validation:** `dotnet build backend/CaronaAlvinegra.sln` compila.
**Status:** [ ] pending

### Task 6: Criar mappings e migration

**Objective:** Persistir novas entidades no banco.
**Requirements Covered:** FR-007
**Design References:** "Data Structures", "Migration / Rollback"
**Actions:**
1. Adicionar configurações/mappings para Alocacao/VanAlocada/SpinAlocado no `ApplicationDbContext`.
2. Executar `dotnet ef migrations add AddAlocacao --project src/CaronaAlvinegra.Infrastructure --startup-project src/CaronaAlvinegra.Api`.
**Validation:** Migration `AddAlocacao` existe sob `Migrations/`.
**Status:** [ ] pending

### Task 7: Implementar AlocacaoService

**Objective:** Caso de uso de alocação.
**Requirements Covered:** FR-008
**Design References:** "Componente: AlocacaoService"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Application/Services/AlocacaoService.cs`.
2. Injetar `IPagamentoRepository` e implementar a regra de grupo confirmado: todos os participantes com `Pagamento.Pago == true` para o jogo; grupos não confirmados → `pendentesPorRota`; grupos vazios ignorados.
3. Implementar `AlocarJogoAsync` (rotas de confirmados ∪ pendentes, chama Alocador por rota, remove antigas, persiste) e `ObterAsync` (recalcula `GruposPendentes`).
4. Atualizar o DTO `AlocacaoResponse` com `List<Guid> GruposPendentes`.
**Validation:** Arquivo compila; teste cobre grupo 100% pago (entra), parcialmente pago (fica em `GruposPendentes`) e sem participantes (ignorado).
**Status:** [ ] pending

### Task 8: Criar AlocacoesController

**Objective:** Endpoints REST de alocação.
**Requirements Covered:** FR-009
**Design References:** "Componente: AlocacoesController"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Api/Controllers/AlocacoesController.cs` com `[Authorize]`, `POST .../alocar` e `GET .../alocacao`.
**Validation:** Rotas existem; `dotnet build` compila.
**Status:** [ ] pending

### Task 9: Registrar DI

**Objective:** Registrar repositório e service.
**Requirements Covered:** FR-009 (parcial)
**Design References:** "Componente: AlocacoesController"
**Actions:**
1. Editar `Program.cs`: `AddScoped<IAlocacaoRepository, AlocacaoRepository>()` e `AddScoped<AlocacaoService>()`.
**Validation:** `Program.cs` compila.
**Status:** [ ] pending
