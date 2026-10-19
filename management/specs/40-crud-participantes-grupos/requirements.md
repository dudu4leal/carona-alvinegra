---
name: crud-participantes-grupos
status: verified
references: V1
---

# Spec 40: CRUD de Participantes e Grupos — Requirements

## Value Delivery

> **V1: Back-office do Jones** (parcial — este spec entrega a gestão de participantes e grupos; jogos e pagamentos são Specs 50/60).
>
> "Substitui o caderninho manual e a lógica de alocação feita à mão por um sistema web centralizado. O Jones cadastra participantes, grupos, jogos e marca pagamentos em um único lugar, eliminando o vai-e-vem de 'tem vaga?' e o risco de erro do caderninho físico."

Este spec implementa o **CRUD completo de Participantes e Grupos** (camadas Application, Infrastructure e API), com validação da capacidade máxima do grupo (15), protegido por `[Authorize]`.

## Functional Requirements

### FR-001: Interfaces de repositório

**What:** Criar `IGrupoRepository` e `IParticipanteRepository` no projeto Application.

**Why:** Define o contrato de persistência sem acoplar a Application ao EF Core (regra de dependência DDD).

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Application/Interfaces/IGrupoRepository.cs` existe com métodos `GetByIdAsync`, `GetAllAsync`, `AddAsync`, `Update`, `Remove` e `SaveChangesAsync`.
- [ ] Arquivo `backend/src/CaronaAlvinegra.Application/Interfaces/IParticipanteRepository.cs` existe com `GetByIdAsync`, `GetAllAsync`, `AddAsync`, `Update`, `Remove`, `SaveChangesAsync` e `CountByGrupoAsync(Guid grupoId)`.

### FR-002: Implementações EF Core dos repositórios

**What:** Criar `GrupoRepository` e `ParticipanteRepository` na Infrastructure usando `ApplicationDbContext`.

**Why:** Implementa a persistência real no PostgreSQL.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Infrastructure/Repositories/GrupoRepository.cs` existe e implementa `IGrupoRepository`.
- [ ] Arquivo `backend/src/CaronaAlvinegra.Infrastructure/Repositories/ParticipanteRepository.cs` existe e implementa `IParticipanteRepository`.
- [ ] `CountByGrupoAsync` conta participantes com `GrupoId == grupoId`.
- [ ] `GrupoRepository.GetByIdAsync` e `GrupoRepository.GetAllAsync` carregam os participantes com `Include(g => g.Participantes)` (sem lazy loading configurado, sem isso `GrupoResponse.Participantes` vem vazio e os Specs 70/80/110 não enxergam ninguém).

### FR-003: DTOs de Grupo e Participante

**What:** Criar DTOs de request/response na Application.

**Why:** Desacopla o contrato HTTP das entidades de domínio.

**Acceptance Criteria:**
- [ ] `GrupoCreateRequest { Nome, Rota }`, `GrupoUpdateRequest { Nome, Rota }`, `GrupoResponse { Id, Nome, Rota, Participantes }` existem.
- [ ] `ParticipanteCreateRequest { Nome, WhatsApp, GrupoId }`, `ParticipanteUpdateRequest { Nome, WhatsApp, GrupoId }`, `ParticipanteResponse { Id, Nome, WhatsApp, GrupoId }` existem.

### FR-004: Caso de uso de Grupo (CRUD)

**What:** Criar `GrupoService` na Application com criar, listar, obter, atualizar e excluir.

**Why:** Centraliza a lógica de aplicação do recurso Grupo.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Application/Services/GrupoService.cs` existe.
- [ ] `CriarAsync`, `ListarAsync`, `ObterAsync`, `AtualizarAsync` e `ExcluirAsync` implementados.
- [ ] `ExcluirAsync` retorna conflito se o grupo possui participantes.

### FR-005: Caso de uso de Participante (CRUD)

**What:** Criar `ParticipanteService` na Application com criar, listar, obter, atualizar e excluir.

**Why:** Centraliza a lógica de aplicação do recurso Participante.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Application/Services/ParticipanteService.cs` existe.
- [ ] `CriarAsync`, `ListarAsync`, `ObterAsync`, `AtualizarAsync` e `ExcluirAsync` implementados.

### FR-006: Validação de capacidade do grupo

**What:** Ao criar/atualizar um participante em um grupo, rejeitar quando o grupo já tem 15 participantes.

**Why:** Respeita a regra de negócio "grupo indivisível, máx. 15 pessoas" (a constante `Grupo.CapacidadeMaxima` é a fonte única).

**Acceptance Criteria:**
- [ ] `CriarAsync` lança `DomainException` quando o grupo-alvo já tem 15 participantes.
- [ ] `AtualizarAsync` (mudança de grupo) lança `DomainException` quando o grupo de destino está cheio.
- [ ] A validação usa `Grupo.CapacidadeMaxima` (não um literal `15` duplicado).

### FR-007: `GruposController`

**What:** Criar controller REST para grupos, protegido por `[Authorize]`.

**Why:** Expõe o CRUD de grupos ao frontend.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Api/Controllers/GruposController.cs` existe com `[Authorize]`.
- [ ] Rotas: `POST /api/grupos` (201), `GET /api/grupos` (200), `GET /api/grupos/{id}` (200/404), `PUT /api/grupos/{id}` (200/404), `DELETE /api/grupos/{id}` (204/404/409).

### FR-008: `ParticipantesController`

**What:** Criar controller REST para participantes, protegido por `[Authorize]`.

**Why:** Expõe o CRUD de participantes ao frontend.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Api/Controllers/ParticipantesController.cs` existe com `[Authorize]`.
- [ ] Rotas: `POST /api/participantes` (201), `GET /api/participantes` (200, com filtro opcional `?grupoId=`), `GET /api/participantes/{id}` (200/404), `PUT /api/participantes/{id}` (200/404), `DELETE /api/participantes/{id}` (204/404).

### FR-009: Registro de injeção de dependência

**What:** Registrar repositórios e services no contêiner de DI.

**Why:** Torna as dependências resolvíveis pelos controllers.

**Acceptance Criteria:**
- [ ] `Program.cs` registra `IGrupoRepository` → `GrupoRepository`, `IParticipanteRepository` → `ParticipanteRepository`, e os services (`GrupoService`, `ParticipanteService`).

### FR-010: Testes de caso de uso

**What:** Criar testes para a validação de capacidade e o CRUD básico.

**Why:** Garante que as regras (máx. 15) e o fluxo CRUD estão corretos.

**Acceptance Criteria:**
- [ ] Teste valida que criar o 16º participante num grupo lança `DomainException`.
- [ ] Teste valida que mover participante para grupo cheio lança `DomainException`.
- [ ] Teste valida que excluir grupo com participantes retorna conflito.
- [ ] `dotnet test backend/tests/CaronaAlvinegra.Domain.Tests` termina com exit code 0.

## Non-Functional Requirements

### NFR-001: Camadas isoladas

**What:** A Application não pode referenciar EF Core nem Infrastructure.

**Acceptance Criteria:**
- [ ] `CaronaAlvinegra.Application.csproj` não possui `ProjectReference` para Infrastructure e não referencia EF Core.

### NFR-002: Respostas HTTP consistentes

**What:** Retornar códigos HTTP semânticos (201/200/204/404/409) e `DomainException` mapeada para 400.

**Acceptance Criteria:**
- [ ] Violação de regra de domínio (`DomainException`) vira `400 Bad Request` com mensagem.

## Constraints

- **.NET 9**, **DDD tático** (Domain puro; Application define contratos; Infrastructure implementa; Api expõe).
- **Guid** como identificador.
- **KISS/DRY**: repositórios simples (sem Unit of Work genérico; usar `SaveChangesAsync` do DbContext).
- **`[Authorize]`** em todos os controllers de domínio (Spec 30).

## Edge Cases & Error States

- **Criar participante em grupo cheio (15):** `DomainException` → 400.
- **Mover participante para grupo cheio:** `DomainException` → 400.
- **Excluir grupo com participantes:** 409 Conflict (remover participantes primeiro).
- **Grupo/participante não encontrado:** 404.
- **`Nome`/`WhatsApp` vazios:** `DomainException` (validação do domínio) → 400.
- **Participante com `GrupoId` inexistente:** rejeitar com erro claro (validar existência do grupo no caso de uso).
- **Excluir participante que possui `Pagamento`:** permitido; a FK `Pagamentos.ParticipanteId → Participantes.Id` é `ON DELETE CASCADE` (Spec 20), então os pagamentos do participante são removidos junto. Não há tratamento especial no service.

## Dependencies

- **Spec 20 (modelo-dominio)** — entidades `Grupo`, `Participante`, `DomainException`.
- **Spec 30 (autenticacao)** — `[Authorize]` e JWT.
- **Spec 10 (fundacao-backend)** — `ApplicationDbContext`, DI.

## Out of Scope

- Gestão de jogos (Spec 50) e pagamentos (Spec 60).
- Algoritmo de alocação (Spec 70) — este spec só mantém os dados.
- Geração de lista oficial (Spec 80).
- Frontend (Spec 90).
