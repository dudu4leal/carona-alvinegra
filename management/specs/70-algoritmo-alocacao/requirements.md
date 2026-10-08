# Spec 70: Algoritmo de Alocação — Requirements

## Value Delivery

> **V2: Alocação automática de vans**
>
> "O núcleo do produto: distribuir automaticamente os grupos confirmados em vans (15 lugares) e spins (6 lugares), por jogo e por rota. Mantém a fila de confirmados, empacota grupos inteiros (greedy/first-fit), abre van quando a rota tem ≥12 pessoas, usa spin quando sobra <12, promove spin+lista de espera ≥12 para uma van nova e, quando um grupo não encaixa (ex.: família de 8), escala para o Jones decidir — o sistema nunca divide grupo sozinho."

Este spec implementa o **algoritmo de alocação contínua e automática** — o coração do produto. Dado um jogo e uma rota, empacota grupos inteiros (indivisíveis) em vans/spins, aplica as regras de abertura (≥12), spin (≤6) e promoção (spin + lista de espera ≥ 12), e escala os grupos que não encaixam para o Jones. **O sistema nunca divide grupo sozinho.**

## Functional Requirements

### FR-001: Entidades de alocação

**What:** Criar no Domain as entidades `Alocacao` (aggregate), `VanAlocada` e `SpinAlocado`.

**Why:** Representam o resultado da alocação (persistível) que alimenta a lista oficial (Spec 80).

**Acceptance Criteria:**
- [ ] `backend/src/CaronaAlvinegra.Domain/Entities/Alocacao.cs` existe com `Id`, `JogoId`, `Rota`, lista de `VanAlocada`, `SpinAlocado?` e lista de `GrupoId` escalados.
- [ ] `backend/src/CaronaAlvinegra.Domain/Entities/VanAlocada.cs` existe com `Id`, `Motorista`, `ResponsavelId` e lista de `GrupoId`.
- [ ] `backend/src/CaronaAlvinegra.Domain/Entities/SpinAlocado.cs` existe com `Id` e lista de `GrupoId`.

### FR-002: Serviço de domínio `Alocador`

**What:** Criar `Alocador` no Domain com o método `Alocar(...)` que executa o empacotamento.

**Why:** Centraliza a regra de negócio pura (sem IO), tornando o algoritmo testável de forma determinística.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Domain/Services/Alocador.cs` existe.
- [ ] Método `Alocacao Alocar(Guid jogoId, Rota rota, IReadOnlyList<Grupo> grupos, int tamanhoListaEspera = 0)`.
- [ ] O algoritmo é puro (não acessa repositório nem banco).

### FR-003: Empacotamento first-fit decrescente

**What:** Ordenar grupos por tamanho decrescente e empacotá-los inteiros em vans de capacidade 15 (first-fit).

**Why:** Aproxima um bin-packing eficiente; grupos inteiros nunca são divididos.

**Acceptance Criteria:**
- [ ] Grupos são ordenados por tamanho decrescente antes de empacotar.
- [ ] Um grupo nunca é dividido (integridade do grupo é preservada).
- [ ] Nenhuma van excede 15 lugares.

### FR-004: Regra de abertura de van (≥12)

**What:** Uma van só é considerada justificada se sua ocupação for **≥ 12**.

**Why:** Regra de negócio — uma rota precisa de ≥12 pessoas para justificar uma van.

**Acceptance Criteria:**
- [ ] Vans com ocupação ≥12 são mantidas como vans.
- [ ] Vans com ocupação <12 têm seus grupos movidos para a "sobra".

### FR-005: Regra de spin (sobra ≤ 6) e escalação (sobra 7–11)

**What:** A sobra (grupos que não formam van ≥12) vira **spin** se couber em 6 lugares; senão, é **escalada para o Jones**.

**Why:** Spin (6 lugares) é usado quando a rota não fecha 12; grupos que não encaixam são escalados (o sistema não decide sozinho).

**Acceptance Criteria:**
- [ ] Sobra com total ≤ 6 → vira `SpinAlocado` com esses grupos.
- [ ] Sobra com total ≥ 7 → os grupos são adicionados à lista de escalados (não entram em van/spin).

### FR-006: Regra de promoção (spin + lista de espera ≥ 12)

**What:** Quando existe spin e `spin + lista de espera ≥ 12`, promover para uma van (o spin deixa de existir).

**Why:** Regra de negócio do brainstorm §5/§6.

**Acceptance Criteria:**
- [ ] Se `ocupacaoSpin + tamanhoListaEspera >= 12`, o spin é removido e uma `VanAlocada` promovida é criada com os grupos do spin.
- [ ] Se a condição não é atendida, o spin permanece.

### FR-007: Persistência da alocação

**What:** Criar `IAlocacaoRepository` (Application) e `AlocacaoRepository` (Infrastructure), com migration das novas tabelas.

**Why:** Persistir o resultado da alocação para gerar a lista oficial (Spec 80).

**Acceptance Criteria:**
- [ ] `backend/src/CaronaAlvinegra.Application/Interfaces/IAlocacaoRepository.cs` existe com `AddAsync`, `GetByJogoRotaAsync`, `RemoveByJogoAsync` e `SaveChangesAsync`.
- [ ] `backend/src/CaronaAlvinegra.Infrastructure/Repositories/AlocacaoRepository.cs` existe e implementa a interface.
- [ ] Migration nova cria as tabelas de `Alocacao`, `VanAlocada` e `SpinAlocado`.

### FR-008: Caso de uso `AlocacaoService`

**What:** Criar `AlocacaoService` (Application) que carrega os grupos confirmados do jogo, chama o `Alocador` por rota e persiste.

**Why:** Orquestra o algoritmo com os dados reais do banco.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Application/Services/AlocacaoService.cs` existe.
- [ ] Método `AlocarJogoAsync(Guid jogoId)` agrupa os grupos confirmados por rota, chama `Alocador.Alocar` para cada rota e persiste as alocações.
- [ ] Método `ObterAlocacaoAsync(Guid jogoId)` retorna as alocações persistidas.

### FR-009: `AlocacoesController`

**What:** Criar controller REST para rodar e consultar a alocação, protegido por `[Authorize]`.

**Why:** Expõe o algoritmo ao frontend.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Api/Controllers/AlocacoesController.cs` existe com `[Authorize]`.
- [ ] `POST /api/jogos/{jogoId}/alocar` roda o algoritmo e retorna o resultado (200).
- [ ] `GET /api/jogos/{jogoId}/alocacao` retorna a alocação persistida (200/404).

### FR-010: Testes do algoritmo

**What:** Criar testes unitários cobrindo empacotamento, spin, escalação e promoção.

**Why:** O algoritmo é o núcleo; precisa de cobertura determinística.

**Acceptance Criteria:**
- [ ] `backend/tests/CaronaAlvinegra.Domain.Tests/AlocadorTests.cs` existe.
- [ ] Testes cobrem os casos: van cheia (15), `[8,7]`→1 van, `[8,6,6]`→van+spin, `[8,8,6]`→van+escalado, `[6]`→spin, `[6]+espera 6`→promoção, `[10]`→escalado.
- [ ] `dotnet test backend/tests/CaronaAlvinegra.Domain.Tests` termina com exit code 0.

## Non-Functional Requirements

### NFR-001: Determinismo

**What:** O algoritmo deve produzir sempre o mesmo resultado para a mesma entrada.

**Acceptance Criteria:**
- [ ] Sem aleatoriedade; a ordenação e o empacotamento são determinísticos.

### NFR-002: Domínio puro

**What:** O `Alocador` não pode acessar infraestrutura.

**Acceptance Criteria:**
- [ ] `Alocador.cs` não referencia EF Core, repositórios ou API.

## Constraints

- **.NET 9**, **DDD tático**, **Guid**.
- **Grupo é indivisível** — o algoritmo nunca divide grupo.
- **Capacidades:** van = 15, spin = 6 (constantes do Spec 20).
- **`[Authorize]`** nos controllers.
- **KISS/DRY**: usar `Van.Capacidade`, `Spin.Capacidade` e `Grupo.CapacidadeMaxima` como fontes únicas.

## Edge Cases & Error States

- **Nenhum grupo na rota:** nenhuma alocação é criada.
- **Total da rota < 12:** não abre van; se ≤6 → spin; se 7–11 → escalação.
- **Grupo de 15:** enche uma van e abre outra para os demais.
- **Sobra exatamente 6:** vira spin.
- **Sobra exatamente 7:** escalação (não cabe na spin de 6 e não justifica van de 12).
- **Lista de espera sem spin:** a promoção não se aplica (sem spin não há o que promover).
- **Reexecutar a alocação:** `RemoveByJogoAsync` remove alocações anteriores do jogo antes de regravar (idempotente).

## Dependencies

- **Spec 20 (modelo-dominio)** — `Grupo`, `Van`, `Spin`, `ListaEspera`, constantes de capacidade.
- **Spec 40/50** — participantes/grupos e jogos cadastrados.
- **Spec 30 (autenticacao)** — `[Authorize]`.

## Out of Scope

- Edição manual fina da alocação (arrastar grupos entre vans) — futuro.
- Geração da lista oficial (Spec 80).
- Notificação automática ao Jones/escalados — futuro.
