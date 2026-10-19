---
name: modelo-dominio
status: audited
references: V5
---

# Spec 20: Modelo de Domínio — Requirements

## Value Delivery

> **V5: Fundação técnica e modelo de domínio** (parcial — este spec entrega o **modelo de domínio**; a fundação técnica é o Spec 10).
>
> "Estabelece a fundação técnica e o modelo de domínio: backend .NET 9 em DDD tático (camadas Domain / Application / Infrastructure / API) com EF Core e PostgreSQL, além das entidades do domínio — Participante, Grupo, Van, Spin, Jogo, Pagamento e Lista de espera. É a base sobre a qual todos os outros specs constroem."

Este spec define as **entidades e value objects do domínio** no projeto `CaronaAlvinegra.Domain`, com seus invariantes de negócio (capacidades, rota única, grupo indivisível), a **persistência** (DbSets + mappings Fluent API + primeira migration) e os **testes de invariantes**. As regras de alocação (empacotar grupos em vans) ficam no Spec 70.

## Functional Requirements

### FR-001: Value Object `Rota`

**What:** Criar o enum `Rota` com os valores `Quitandinha` e `Bingen`, no namespace `CaronaAlvinegra.Domain.Enums`.

**Why:** A rota é um atributo central (determina a van) e só admite dois valores fixos.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Domain/Enums/Rota.cs` existe.
- [ ] `public enum Rota { Quitandinha, Bingen }` com os dois valores exatos.

### FR-002: Entidade `Participante`

**What:** Criar a entidade `Participante` com `Id` (Guid), `Nome` (string não-vazia), `WhatsApp` (string não-vazia) e `GrupoId` (Guid, FK para Grupo).

**Why:** Representa um torcedor individual que pertence a um grupo (a rota é herdada do grupo).

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Domain/Entities/Participante.cs` existe.
- [ ] Propriedades: `Guid Id`, `string Nome`, `string WhatsApp`, `Guid GrupoId`.
- [ ] Construtor rejeita `Nome` e `WhatsApp` vazios ou nulos (lança `DomainException`).
- [ ] Namespace `CaronaAlvinegra.Domain.Entities`.

### FR-003: Aggregate `Grupo`

**What:** Criar o aggregate root `Grupo` com `Id`, `Nome`, `Rota` e uma coleção de `Participante`. Invariante: máximo **15** participantes e grupo indivisível (participantes só entram/saem via métodos do grupo).

**Why:** O grupo é a unidade indivisível do algoritmo de alocação (Spec 70); a capacidade máxima 15 é regra de negócio.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Domain/Entities/Grupo.cs` existe.
- [ ] `public const int CapacidadeMaxima = 15`.
- [ ] Método `AdicionarParticipante(Participante)` lança `DomainException` quando o grupo já tem 15 participantes.
- [ ] A coleção de participantes é exposta como `IReadOnlyCollection<Participante>` (não mutável externamente).
- [ ] Método `RemoverParticipante(Guid participanteId)` existe.

### FR-004: Entidade `Van`

**What:** Criar a entidade `Van` com `Id`, `Rota`, `Motorista` (string) e `ResponsavelId` (Guid, referência a Participante). Capacidade fixa de **15** lugares e rota única.

**Why:** Representa o veículo de 15 lugares que atende uma única rota por jogo.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Domain/Entities/Van.cs` existe.
- [ ] `public const int Capacidade = 15`.
- [ ] Propriedades: `Guid Id`, `Rota Rota`, `string Motorista`, `Guid ResponsavelId`.

### FR-005: Entidade `Spin`

**What:** Criar a entidade `Spin` com `Id` e `Rota`. Capacidade fixa de **6** lugares.

**Why:** Representa o carro menor usado quando uma rota não fecha 12 pessoas.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Domain/Entities/Spin.cs` existe.
- [ ] `public const int Capacidade = 6`.
- [ ] Propriedades: `Guid Id`, `Rota Rota`.

### FR-006: Aggregate `Jogo`

**What:** Criar o aggregate root `Jogo` com `Id`, `CompeticaoCodigo` (string, ex. `"BR-26"`), `Adversario` (string), `Data` (DateTime), `HorarioSaida` (TimeOnly) e `PontoEmbarque` (string).

**Why:** É a competição/partida que motiva a alocação; cabeçalho da lista oficial (Spec 80) usa esses dados.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Domain/Entities/Jogo.cs` existe.
- [ ] Propriedades: `Guid Id`, `string CompeticaoCodigo`, `string Adversario`, `DateTime Data`, `TimeOnly HorarioSaida`, `string PontoEmbarque`.
- [ ] Construtor rejeita `CompeticaoCodigo`, `Adversario` e `PontoEmbarque` vazios (lança `DomainException`).

### FR-007: Entidade `Pagamento`

**What:** Criar a entidade `Pagamento` com `Id`, `ParticipanteId` (Guid), `JogoId` (Guid) e `Pago` (bool, default `false`).

**Why:** Marca apenas o estado pago/não-pago (a v1 não processa Pix).

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Domain/Entities/Pagamento.cs` existe.
- [ ] Propriedades: `Guid Id`, `Guid ParticipanteId`, `Guid JogoId`, `bool Pago`.
- [ ] Método `MarcarPago()` define `Pago = true`; método `DesmarcarPago()` define `Pago = false`.

### FR-008: Entidade `ListaEspera`

**What:** Criar a entidade `ListaEspera` com `Id`, `JogoId` (Guid), `Rota` e uma coleção de identificadores de participantes.

**Why:** Representa quem entrou mas ainda não tem vaga definida; usada na promoção `spin + lista de espera ≥ 12` (Spec 70).

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Domain/Entities/ListaEspera.cs` existe.
- [ ] Propriedades: `Guid Id`, `Guid JogoId`, `Rota Rota`.
- [ ] Método `AdicionarParticipante(Guid participanteId)` e `RemoverParticipante(Guid participanteId)` existem.

### FR-009: Exceção de domínio `DomainException`

**What:** Criar `DomainException : Exception` no namespace `CaronaAlvinegra.Domain.Exceptions`.

**Why:** Tipo único para violações de regras de negócio, facilitando tratamento e testes.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Domain/Exceptions/DomainException.cs` existe.
- [ ] `DomainException` herda de `Exception` com construtor `DomainException(string message)`.

### FR-010: Persistência (DbSets, mappings e migration)

**What:** Adicionar os `DbSet<T>` no `ApplicationDbContext`, configurar os mappings via Fluent API (tipos, tamanhos, chaves) e gerar a primeira migration.

**Why:** Torna as entidades persistíveis no PostgreSQL (completa a fundação do Spec 10).

**Acceptance Criteria:**
- [ ] `ApplicationDbContext` possui `DbSet` para `Participante`, `Grupo`, `Van`, `Spin`, `Jogo`, `Pagamento` e `ListaEspera`.
- [ ] Há uma classe de configuração (ex.: `GrupoConfiguration`) aplicando o mapeamento de `Rota` (convertido para `string` no banco) e limites de tamanho das strings.
- [ ] A migration `InitialCreate` existe sob `backend/src/CaronaAlvinegra.Infrastructure/Migrations/`.
- [ ] `dotnet build backend/CaronaAlvinegra.sln` termina com exit code 0.

### FR-011: Testes de invariantes do domínio

**What:** Criar testes xUnit para os invariantes principais.

**Why:** Garante que as regras de negócio (capacidade, rota única, validações) estão corretas e não regridem.

**Acceptance Criteria:**
- [ ] Arquivo `backend/tests/CaronaAlvinegra.Domain.Tests/GrupoTests.cs` testa que adicionar o 16º participante lança `DomainException`.
- [ ] Arquivo `backend/tests/CaronaAlvinegra.Domain.Tests/GrupoTests.cs` testa que 15 participantes são aceitos.
- [ ] Teste valida que `Participante` rejeita `Nome` vazio.
- [ ] `dotnet test backend/tests/CaronaAlvinegra.Domain.Tests` termina com exit code 0.

## Non-Functional Requirements

### NFR-001: Domínio puro (sem dependência de EF/API)

**What:** O projeto `CaronaAlvinegra.Domain` não pode referenciar EF Core, Infrastructure ou Api.

**Acceptance Criteria:**
- [ ] `CaronaAlvinegra.Domain.csproj` não possui `PackageReference` para EF Core e não possui `ProjectReference`.

### NFR-002: Encapsulamento de coleções

**What:** Coleções de agregados não podem ser mutadas externamente.

**Acceptance Criteria:**
- [ ] `Grupo.Participantes` e `ListaEspera` expõem apenas coleções read-only; mutação só via métodos do aggregate.

## Constraints

- **.NET 9** (net9.0).
- **DDD tático**: Domain puro, invariantes no domínio, mappings na Infrastructure (ADR-001).
- **PostgreSQL** via Npgsql (ADR-003).
- **KISS/DRY**: sem abstrações desnecessárias (ex.: sem repositório genérico nesta fase).
- Identificadores como **Guid**.

## Edge Cases & Error States

- **Grupo cheio:** `AdicionarParticipante` no 16º lança `DomainException` (nunca cria estado inválido).
- **Participante com nome/WhatsApp vazio:** construtor lança `DomainException`.
- **Remover participante inexistente:** `RemoverParticipante` é idempotente (não lança).
- **Grupo com participantes de rotas diferentes:** a rota é do grupo, então participantes não têm rota própria — não há divergência possível.
- **Pagamento sem participante/jogo:** os Guids são obrigatórios (FK); o banco garante integridade referencial.

## Dependencies

- **Spec 10 (fundacao-backend)** — projetos, `ApplicationDbContext` vazio e infraestrutura de migrations prontos.
- **SDK .NET 9** e **PostgreSQL** local (para gerar/validar a migration).
- Ferramenta `dotnet-ef` (via `dotnet tool`) para gerar a migration.

## Out of Scope

- Lógica de alocação (empacotar grupos em vans/spins) — Spec 70.
- Regras de cancelamento/reembolso — Spec 60.
- Repositórios e casos de uso (Application) — Specs 40–60.
- Autenticação — Spec 30.
- Frontend e deploy — Specs 90/100.
