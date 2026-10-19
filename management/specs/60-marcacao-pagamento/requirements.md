---
name: marcacao-pagamento
status: verified
references: V1
---

# Spec 60: Marcação de Pagamento — Requirements

## Value Delivery

> **V1: Back-office do Jones** (parcial — este spec entrega a marcação de pagamento; participantes/grupos são Spec 40 e jogos são Spec 50).
>
> "Substitui o caderninho manual e a lógica de alocação feita à mão por um sistema web centralizado. O Jones cadastra participantes, grupos, jogos e marca pagamentos em um único lugar."

Este spec implementa a **marcação de pagamento (pago/não pago)** por participante e por jogo, incluindo a regra de reembolso no cancelamento (janela de 48h). A v1 **não** processa Pix — apenas registra o estado.

## Functional Requirements

### FR-001: Interface `IPagamentoRepository`

**What:** Criar `IPagamentoRepository` no projeto Application.

**Why:** Contrato de persistência sem acoplar a Application ao EF Core.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Application/Interfaces/IPagamentoRepository.cs` existe com `GetByParticipanteJogoAsync`, `GetByJogoAsync`, `AddAsync`, `Update`, `Remove` e `SaveChangesAsync`.

### FR-002: Implementação `PagamentoRepository`

**What:** Criar `PagamentoRepository` na Infrastructure usando `ApplicationDbContext`.

**Why:** Persistência real no PostgreSQL.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Infrastructure/Repositories/PagamentoRepository.cs` existe e implementa `IPagamentoRepository`.
- [ ] `GetByParticipanteJogoAsync` busca pagamento por `(ParticipanteId, JogoId)`.

### FR-003: DTOs de Pagamento

**What:** Criar `PagamentoResponse` e `DesmarcarPagamentoResponse` na Application.

**Why:** Contratos de entrada/saída do recurso.

**Acceptance Criteria:**
- [ ] `PagamentoResponse { Id, ParticipanteId, JogoId, Pago }` existe.
- [ ] `DesmarcarPagamentoResponse { ReembolsoDevido }` existe.

### FR-004: Caso de uso `PagamentoService`

**What:** Criar `PagamentoService` na Application com marcar pago, desmarcar pago e listar por jogo.

**Why:** Centraliza a lógica de aplicação do recurso Pagamento.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Application/Services/PagamentoService.cs` existe.
- [ ] `MarcarPagoAsync(jogoId, participanteId)`, `DesmarcarPagoAsync(jogoId, participanteId)` e `ListarPorJogoAsync(jogoId)` implementados.
- [ ] `MarcarPagoAsync` cria (se não existe) ou atualiza o pagamento para `Pago = true`.
- [ ] `DesmarcarPagoAsync` define `Pago = false` quando o pagamento existe; se não existir, retorna `ReembolsoDevido = false` sem criar registro.

### FR-005: Regra de reembolso (janela de 48h)

**What:** Ao desmarcar um pagamento (cancelamento), calcular `ReembolsoDevido` conforme a janela de 48h em relação ao horário de saída do jogo.

**Why:** Aplica a regra de negócio de cancelamento (brainstorm §5).

**Acceptance Criteria:**
- [ ] `DesmarcarPagoAsync` retorna `ReembolsoDevido = true` quando o cancelamento ocorre **≥ 48h** antes do horário de saída do jogo.
- [ ] `DesmarcarPagoAsync` retorna `ReembolsoDevido = false` quando o cancelamento ocorre **< 48h** antes.
- [ ] O "momento do jogo" é calculado como `Jogo.Data.Date` + `Jogo.HorarioSaida`.
- [ ] A exceção "reposição de vaga" é tratada como **decisão manual do Jones** (fora da automação v1).

### FR-006: `PagamentosController`

**What:** Criar controller REST para pagamentos, protegido por `[Authorize]`.

**Why:** Expõe as operações de pagamento ao frontend.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Api/Controllers/PagamentosController.cs` existe com `[Authorize]`.
- [ ] Rotas: `GET /api/jogos/{jogoId}/pagamentos` (200), `POST /api/jogos/{jogoId}/pagamentos/{participanteId}/marcar-pago` (200), `POST /api/jogos/{jogoId}/pagamentos/{participanteId}/desmarcar-pago` (200).

### FR-007: Registro de injeção de dependência

**What:** Registrar `IPagamentoRepository` → `PagamentoRepository` e `PagamentoService` no DI.

**Why:** Torna as dependências resolvíveis pelo controller.

**Acceptance Criteria:**
- [ ] `Program.cs` registra `AddScoped<IPagamentoRepository, PagamentoRepository>()` e `AddScoped<PagamentoService>()`.

### FR-008: Testes de caso de uso

**What:** Criar testes para a marcação e a regra de reembolso.

**Why:** Garante que marcar/desmarcar e a janela de 48h funcionam.

**Acceptance Criteria:**
- [ ] Teste valida marcar pagamento (cria `Pago = true`).
- [ ] Teste valida desmarcar ≥48h antes → `ReembolsoDevido = true`.
- [ ] Teste valida desmarcar <48h antes → `ReembolsoDevido = false`.
- [ ] `dotnet test backend/tests/CaronaAlvinegra.Domain.Tests` termina com exit code 0.

## Non-Functional Requirements

### NFR-001: Camadas isoladas

**What:** A Application não pode referenciar EF Core nem Infrastructure.

**Acceptance Criteria:**
- [ ] `CaronaAlvinegra.Application.csproj` não referencia Infrastructure nem EF Core.

### NFR-002: Respostas HTTP consistentes

**What:** `DomainException` → 400; não encontrado → 404.

**Acceptance Criteria:**
- [ ] Violação de domínio vira 400 (middleware reutilizado).

## Constraints

- **.NET 9**, **DDD tático**, **Guid**.
- **`[Authorize]`** nos controllers de domínio.
- **Sem processamento de Pix** (a v1 só marca estado).
- **KISS/DRY**: reutilizar padrão de repositório/service/controller e o middleware de `DomainException`.

## Edge Cases & Error States

- **Participante ou jogo inexistente** ao marcar pagamento: 404.
- **Marcar pago quando já pago:** idempotente (mantém `Pago = true`).
- **Desmarcar pago quando já não-pago:** idempotente (mantém `Pago = false`).
- **Cancelamento exatamente em 48h:** considerar como **≥ 48h** (reembolso devido) — definir limite inclusive.
- **Reposição de vaga (lista de espera):** não automatizada na v1; o Jones decide manualmente o reembolso.

## Dependencies

- **Spec 20 (modelo-dominio)** — entidade `Pagamento`, `Jogo`, `DomainException`.
- **Spec 40 (crud-participantes-grupos)** — participantes existentes.
- **Spec 50 (gestao-jogos)** — jogos existentes.
- **Spec 30 (autenticacao)** — `[Authorize]`.

## Out of Scope

- Processamento/verificação de Pix (integração bancária).
- Reposição automática de vaga a partir da lista de espera (Spec 70).
- Alocação de vans/spins (Spec 70).
- Geração da lista oficial (Spec 80).
