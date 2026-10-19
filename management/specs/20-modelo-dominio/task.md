<!-- PIPELINE-CONTRACT: Task completion is determined by "**Status:** [x] done" at the end of each "### Task N" section.
     Checkboxes "- [ ]" under "**Validation:**" are human verification guides — the pipeline ignores them completely.
     To mark a task as done, add "**Status:** [x] done". To mark it as pending, use "**Status:** [ ] pending". -->
# Spec 20: Modelo de Domínio — Implementation Tasks

## Task Order

Tasks MUST be executed sequentially in numerical order. Each task depends on the completion of all preceding tasks.

## Tasks

### Task 1: Criar `DomainException` e `Rota`

**Objective:** Criar a exceção de domínio e o enum de rota.
**Requirements Covered:** FR-001, FR-009
**Design References:** "Componente: Rota", "Componente: DomainException"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Domain/Exceptions/DomainException.cs` com `public class DomainException : Exception { public DomainException(string message) : base(message) { } }`.
2. Criar `backend/src/CaronaAlvinegra.Domain/Enums/Rota.cs` com `public enum Rota { Quitandinha, Bingen }`.
**Validation:** Os dois arquivos existem e compilam.
**Status:** [x] done

### Task 2: Criar entidade `Participante`

**Objective:** Criar a entidade Participante.
**Requirements Covered:** FR-002
**Design References:** "Componente: Participante"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Domain/Entities/Participante.cs`.
2. Implementar propriedades `Id`, `Nome`, `WhatsApp`, `GrupoId` e construtor que gera Id e valida `Nome`/`WhatsApp` não-vazios (lança `DomainException`).
**Validation:** Arquivo existe e `dotnet build` compila.
**Status:** [x] done

### Task 3: Criar aggregate `Grupo`

**Objective:** Criar o aggregate Grupo com regra de capacidade.
**Requirements Covered:** FR-003
**Design References:** "Componente: Grupo"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Domain/Entities/Grupo.cs`.
2. Implementar `CapacidadeMaxima = 15`, lista privada de participantes, `IReadOnlyCollection` exposto, `AdicionarParticipante` (lança `DomainException` quando cheio) e `RemoverParticipante` (idempotente).
**Validation:** Arquivo existe e compila; o 16º participante lança exceção (validado em Task 8).
**Status:** [x] done

### Task 4: Criar entidades `Van` e `Spin`

**Objective:** Criar as entidades de veículo.
**Requirements Covered:** FR-004, FR-005
**Design References:** "Componente: Van", "Componente: Spin"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Domain/Entities/Van.cs` com `Capacidade = 15`, `Rota`, `Motorista`, `ResponsavelId` e validação de motorista não-vazio.
2. Criar `backend/src/CaronaAlvinegra.Domain/Entities/Spin.cs` com `Capacidade = 6` e `Rota`.
**Validation:** Arquivos existem e compilam.
**Status:** [x] done

### Task 5: Criar aggregate `Jogo`

**Objective:** Criar a entidade Jogo.
**Requirements Covered:** FR-006
**Design References:** "Componente: Jogo"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Domain/Entities/Jogo.cs`.
2. Implementar `CompeticaoCodigo`, `Adversario`, `Data`, `HorarioSaida`, `PontoEmbarque` com validações de não-vazio.
**Validation:** Arquivo existe e compila.
**Status:** [x] done

### Task 6: Criar entidades `Pagamento` e `ListaEspera`

**Objective:** Criar Pagamento e ListaEspera.
**Requirements Covered:** FR-007, FR-008
**Design References:** "Componente: Pagamento", "Componente: ListaEspera"
**Actions:**
1. Criar `backend/src/CaronaAlvinegra.Domain/Entities/Pagamento.cs` com `Pago` default false e métodos `MarcarPago`/`DesmarcarPago`.
2. Criar `backend/src/CaronaAlvinegra.Domain/Entities/ListaEspera.cs` com lista privada de `Guid` e métodos `AdicionarParticipante`/`RemoverParticipante`.
**Validation:** Arquivos existem e compilam.
**Status:** [x] done

### Task 7: Adicionar DbSets e mappings (Infrastructure)

**Objective:** Persistir as entidades via EF Core.
**Requirements Covered:** FR-010
**Design References:** "Componente: Configurações de mapeamento"
**Actions:**
1. Editar `backend/src/CaronaAlvinegra.Infrastructure/Data/ApplicationDbContext.cs` adicionando `DbSet` para as 7 entidades e um `OnModelCreating` que aplica as configurações.
2. Criar `backend/src/CaronaAlvinegra.Infrastructure/Data/Configurations/` com `IEntityTypeConfiguration<T>` para cada entidade (conversão de `Rota`, `HasMaxLength`, relacionamento Grupo→Participante).
**Validation:** `dotnet build backend/CaronaAlvinegra.sln` termina com exit code 0.
**Status:** [x] done

### Task 8: Gerar migration inicial

**Objective:** Criar a migration `InitialCreate`.
**Requirements Covered:** FR-010
**Design References:** "Migration / Rollback"
**Actions:**
1. Instalar/garantir `dotnet-ef`: `dotnet tool install --global dotnet-ef` (ou `dotnet tool update --global dotnet-ef`).
2. Executar `dotnet ef migrations add InitialCreate --project src/CaronaAlvinegra.Infrastructure --startup-project src/CaronaAlvinegra.Api`.
**Validation:** Pasta `backend/src/CaronaAlvinegra.Infrastructure/Migrations/` existe com arquivos da migration.
**Status:** [x] done

### Task 9: Criar testes de invariantes

**Objective:** Testar regras de domínio.
**Requirements Covered:** FR-011
**Design References:** "Testing Strategy"
**Actions:**
1. Criar `backend/tests/CaronaAlvinegra.Domain.Tests/GrupoTests.cs`.
2. Testar: 15 participantes aceitos; 16º lança `DomainException`; `Participante` rejeita nome vazio; `Jogo` rejeita adversário vazio.
**Validation:** `dotnet test backend/tests/CaronaAlvinegra.Domain.Tests` termina com exit code 0.
**Status:** [x] done
