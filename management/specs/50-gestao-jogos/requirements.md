---
name: gestao-jogos
status: verified
references: V1
---

# Spec 50: Gestão de Jogos — Requirements

## Value Delivery

> **V1: Back-office do Jones** (parcial — este spec entrega a gestão de jogos; participantes/grupos são Spec 40 e pagamentos são Spec 60).
>
> "Substitui o caderninho manual e a lógica de alocação feita à mão por um sistema web centralizado. O Jones cadastra participantes, grupos, jogos e marca pagamentos em um único lugar."

Este spec implementa o **CRUD completo de Jogos** (camadas Application, Infrastructure e API), protegido por `[Authorize]`. O jogo é a competição/partida que motiva a alocação (Spec 70) e o cabeçalho da lista oficial (Spec 80).

## Functional Requirements

### FR-001: Interface `IJogoRepository`

**What:** Criar `IJogoRepository` no projeto Application.

**Why:** Contrato de persistência sem acoplar a Application ao EF Core.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Application/Interfaces/IJogoRepository.cs` existe com `GetByIdAsync`, `GetAllAsync` (ordenado por data), `AddAsync`, `Update`, `Remove` e `SaveChangesAsync`.

### FR-002: Implementação `JogoRepository`

**What:** Criar `JogoRepository` na Infrastructure usando `ApplicationDbContext`.

**Why:** Implementa a persistência real no PostgreSQL.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Infrastructure/Repositories/JogoRepository.cs` existe e implementa `IJogoRepository`.
- [ ] `GetAllAsync` retorna jogos ordenados por `Data` (ascendente — próximos jogos primeiro).

### FR-003: DTOs de Jogo

**What:** Criar `JogoCreateRequest`, `JogoUpdateRequest` e `JogoResponse` na Application.

**Why:** Desacopla o contrato HTTP da entidade de domínio.

**Acceptance Criteria:**
- [ ] `JogoCreateRequest { CompeticaoCodigo, Adversario, Data, HorarioSaida, PontoEmbarque }` existe.
- [ ] `JogoUpdateRequest { CompeticaoCodigo, Adversario, Data, HorarioSaida, PontoEmbarque }` existe.
- [ ] `JogoResponse { Id, CompeticaoCodigo, Adversario, Data, HorarioSaida, PontoEmbarque }` existe.

### FR-004: Caso de uso `JogoService`

**What:** Criar `JogoService` na Application com criar, listar, obter, atualizar e excluir.

**Why:** Centraliza a lógica de aplicação do recurso Jogo.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Application/Services/JogoService.cs` existe.
- [ ] Métodos `CriarAsync`, `ListarAsync`, `ObterAsync`, `AtualizarAsync` e `ExcluirAsync` implementados.

### FR-005: `JogosController`

**What:** Criar controller REST para jogos, protegido por `[Authorize]`.

**Why:** Expõe o CRUD de jogos ao frontend.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Api/Controllers/JogosController.cs` existe com `[Authorize]`.
- [ ] Rotas: `POST /api/jogos` (201), `GET /api/jogos` (200), `GET /api/jogos/{id}` (200/404), `PUT /api/jogos/{id}` (200/404), `DELETE /api/jogos/{id}` (204/404).

### FR-006: Registro de injeção de dependência

**What:** Registrar `IJogoRepository` → `JogoRepository` e `JogoService` no DI.

**Why:** Torna as dependências resolvíveis pelo controller.

**Acceptance Criteria:**
- [ ] `Program.cs` registra `AddScoped<IJogoRepository, JogoRepository>()` e `AddScoped<JogoService>()`.

### FR-007: Testes de caso de uso

**What:** Criar testes para o `JogoService`.

**Why:** Garante que o CRUD e as validações de domínio funcionam.

**Acceptance Criteria:**
- [ ] Teste valida que criar um jogo com `Adversario` vazio lança `DomainException`.
- [ ] Teste valida criar/obter/atualizar/excluir um jogo.
- [ ] `dotnet test backend/tests/CaronaAlvinegra.Domain.Tests` termina com exit code 0.

## Non-Functional Requirements

### NFR-001: Camadas isoladas

**What:** A Application não pode referenciar EF Core nem Infrastructure.

**Acceptance Criteria:**
- [ ] `CaronaAlvinegra.Application.csproj` não referencia Infrastructure nem EF Core.

### NFR-002: Respostas HTTP consistentes

**What:** Códigos semânticos (201/200/204/404) e `DomainException` → 400.

**Acceptance Criteria:**
- [ ] Violação de domínio vira 400 com mensagem (middleware do Spec 40 reutilizado).

## Constraints

- **.NET 9**, **DDD tático**, **Guid** como identificador.
- **`[Authorize]`** em todos os controllers de domínio.
- **KISS/DRY**: repositório simples; reutilizar o middleware de `DomainException` do Spec 40.

## Edge Cases & Error States

- **Jogo não encontrado:** 404.
- **Campos obrigatórios vazios** (`CompeticaoCodigo`, `Adversario`, `PontoEmbarque`): `DomainException` → 400 (validação do domínio, Spec 20).
- **Data/horário inválidos:** validação de modelo (400) via `[ApiController]`.
- **Excluir jogo com pagamentos/alocações vinculados:** por ora exclui diretamente; a checagem de dependências será adicionada quando Specs 60/70 existirem.

## Dependencies

- **Spec 20 (modelo-dominio)** — entidade `Jogo` e `DomainException`.
- **Spec 30 (autenticacao)** — `[Authorize]`.
- **Spec 40 (crud-participantes-grupos)** — middleware de `DomainException` e padrão de repositório/service/controller.

## Out of Scope

- Marcação de pagamento (Spec 60).
- Alocação de vans/spins (Spec 70).
- Geração da lista oficial (Spec 80).
- Frontend (Spec 90).
