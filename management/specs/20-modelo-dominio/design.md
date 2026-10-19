# Spec 20: Modelo de Domínio — Design

## Design Approach

Modelar o domínio como **entidades puras C#** no projeto `CaronaAlvinegra.Domain`, com invariantes protegidos por construtores e métodos. Os agregados são `Grupo` (contém `Participante`) e `Jogo` (raiz independente). A persistência é feita via **Fluent API** na Infrastructure, sem poluir o domínio com atributos de EF Core.

## Architecture Decisions

- **Guid como identificador** de todas as entidades (evita problemas de `int` identity e é gerado no domínio).
- **`Grupo` é o aggregate root** que contém `Participante`; a rota pertence ao grupo (participante herda a rota do grupo, evitando redundância e divergência).
- **`Rota` é um enum**, mapeado para o banco como `string` (mais legível que `int`).
- **`Van` e `Spin` são entidades de capacidade fixa** (constantes 15 e 6), sem relação de alocação ainda — a alocação é responsabilidade do Spec 70.
- **`DomainException`** é a exceção única para violações de regra de negócio.
- **Integridade referencial no banco:** `Pagamento → Participante/Jogo` e `ListaEspera → Jogo` usam `OnDelete(Cascade)` (registros-filho não sobrevivem ao pai); `Grupo → Participante` usa `OnDelete(Restrict)` para impedir exclusão silenciosa de participantes (o Spec 40 responde 409 antes de chegar ao banco).
- **Chave lógica de `Pagamento`:** índice único `(ParticipanteId, JogoId)` sustenta o upsert "marcar/desmarcar" do Spec 60 e impede duplicatas.
- **`OnModelCreating` chama `base.OnModelCreating`**: necessário para que o Spec 30 (que troca a base por `IdentityDbContext`) continue mapeando as tabelas de Identity.

## Component / Module Breakdown

### Componente: `Rota` (enum)

**Purpose:** Valores fixos de rota.

**Interface:** `enum Rota { Quitandinha, Bingen }`.

**Internal Logic:** N/A.

**Dependencies:** Nenhuma.

**Error Handling:** N/A.

### Componente: `Participante` (entidade)

**Purpose:** Torcedor individual.

**Interface:**
```csharp
namespace CaronaAlvinegra.Domain.Entities;

public class Participante
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string WhatsApp { get; private set; }
    public Guid GrupoId { get; private set; }

    public Participante(string nome, string whatsApp, Guid grupoId) { /* valida não-vazio, gera Id */ }
}
```

**Internal Logic:** Construtor gera `Id = Guid.NewGuid()` e lança `DomainException` se `Nome`/`WhatsApp` forem nulos ou vazios (após `Trim()`).

**Dependencies:** `DomainException`.

**Error Handling:** `DomainException` para nome/WhatsApp inválidos.

### Componente: `Grupo` (aggregate root)

**Purpose:** Unidade indivisível de até 15 participantes, com rota única.

**Interface:**
```csharp
public class Grupo
{
    public const int CapacidadeMaxima = 15;
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public Rota Rota { get; private set; }
    public IReadOnlyCollection<Participante> Participantes => _participantes.AsReadOnly();

    public Grupo(string nome, Rota rota) { /* valida nome, gera Id */ }
    public void AdicionarParticipante(Participante p) { /* valida capacidade */ }
    public void RemoverParticipante(Guid participanteId) { }
}
```

**Internal Logic:** `_participantes` é `List<Participante>` privada. `AdicionarParticipante` lança `DomainException` quando `_participantes.Count == CapacidadeMaxima`. `RemoverParticipante` remove por Id sem lançar (idempotente).

**Dependencies:** `Participante`, `Rota`, `DomainException`.

**Error Handling:** `DomainException` para grupo cheio e nome vazio.

### Componente: `Van` (entidade)

**Purpose:** Veículo de 15 lugares, rota única, com motorista e responsável.

**Interface:**
```csharp
public class Van
{
    public const int Capacidade = 15;
    public Guid Id { get; private set; }
    public Rota Rota { get; private set; }
    public string Motorista { get; private set; }
    public Guid ResponsavelId { get; private set; }

    public Van(Rota rota, string motorista, Guid responsavelId) { }
}
```

**Internal Logic:** Construtor valida `Motorista` não-vazio e gera Id.

**Dependencies:** `Rota`, `DomainException`.

**Error Handling:** `DomainException` para motorista vazio.

### Componente: `Spin` (entidade)

**Purpose:** Carro menor de 6 lugares.

**Interface:**
```csharp
public class Spin
{
    public const int Capacidade = 6;
    public Guid Id { get; private set; }
    public Rota Rota { get; private set; }

    public Spin(Rota rota) { }
}
```

**Internal Logic:** Gera Id, define rota.

**Dependencies:** `Rota`.

**Error Handling:** N/A.

### Componente: `Jogo` (aggregate root)

**Purpose:** A partida que motiva a alocação.

**Interface:**
```csharp
public class Jogo
{
    public Guid Id { get; private set; }
    public string CompeticaoCodigo { get; private set; }
    public string Adversario { get; private set; }
    public DateTime Data { get; private set; }
    public TimeOnly HorarioSaida { get; private set; }
    public string PontoEmbarque { get; private set; }

    public Jogo(string competicaoCodigo, string adversario, DateTime data, TimeOnly horarioSaida, string pontoEmbarque) { }
}
```

**Internal Logic:** Construtor valida `CompeticaoCodigo`, `Adversario` e `PontoEmbarque` não-vazios; gera Id.

**Dependencies:** `DomainException`.

**Error Handling:** `DomainException` para campos obrigatórios vazios.

### Componente: `Pagamento` (entidade)

**Purpose:** Marca pago/não-pago.

**Interface:**
```csharp
public class Pagamento
{
    public Guid Id { get; private set; }
    public Guid ParticipanteId { get; private set; }
    public Guid JogoId { get; private set; }
    public bool Pago { get; private set; }

    public Pagamento(Guid participanteId, Guid jogoId) { }
    public void MarcarPago() { }
    public void DesmarcarPago() { }
}
```

**Internal Logic:** Construtor gera Id e `Pago = false`. `MarcarPago`/`DesmarcarPago` alteram o flag.

**Dependencies:** Nenhuma.

**Error Handling:** N/A.

### Componente: `ListaEspera` (entidade)

**Purpose:** Fila de quem ainda não tem vaga.

**Interface:**
```csharp
public class ListaEspera
{
    public Guid Id { get; private set; }
    public Guid JogoId { get; private set; }
    public Rota Rota { get; private set; }
    public IReadOnlyCollection<Guid> ParticipanteIds => _participanteIds.AsReadOnly();

    public ListaEspera(Guid jogoId, Rota rota) { }
    public void AdicionarParticipante(Guid participanteId) { }
    public void RemoverParticipante(Guid participanteId) { }
}
```

**Internal Logic:** `_participanteIds` é `List<Guid>` privada; `AdicionarParticipante` evita duplicados (idempotente); `RemoverParticipante` idempotente.

**Dependencies:** `Rota`.

**Error Handling:** N/A.

### Componente: `DomainException`

**Purpose:** Exceção única de domínio.

**Interface:** `public class DomainException : Exception { public DomainException(string message) : base(message) { } }`.

**Internal Logic:** N/A.

**Dependencies:** Nenhuma.

**Error Handling:** N/A.

### Componente: Configurações de mapeamento (Infrastructure)

**Purpose:** Mapear entidades para tabelas PostgreSQL via Fluent API.

**Interface:** Classes `IEntityTypeConfiguration<T>` (ex.: `GrupoConfiguration`, `ParticipanteConfiguration`, etc.) registradas em `ApplicationDbContext.OnModelCreating`.

**Internal Logic:** Cada entidade tem sua própria `IEntityTypeConfiguration<T>` (aplicadas via `ApplyConfigurationsFromAssembly`). Mapeamentos exatos:
- **Chaves:** `Id` (Guid) é PK em todas as 7 tabelas (`Participantes`, `Grupos`, `Vans`, `Spins`, `Jogos`, `Pagamentos`, `ListasEspera`).
- **`Rota`:** `HasConversion<string>()` + `HasMaxLength(20)` + `IsRequired()` em `Grupo`, `Van`, `Spin` e `ListaEspera`.
- **Tamanhos:** `Participante.Nome` 100 / `WhatsApp` 20; `Grupo.Nome` 100; `Van.Motorista` 100; `Jogo.CompeticaoCodigo` 20 / `Adversario` 100 / `PontoEmbarque` 200. Todas `IsRequired()`.
- **`Grupo` 1:N `Participante`:** `HasMany(g => g.Participantes).WithOne().HasForeignKey(p => p.GrupoId).OnDelete(Restrict)` (impede exclusão silenciosa; o Spec 40 responde 409) e `Navigation(...).UsePropertyAccessMode(Field)` para popular a lista privada `_participantes`.
- **`Pagamento`:** FKs `ParticipanteId → Participantes.Id` e `JogoId → Jogos.Id`, ambas `OnDelete(Cascade)`; índice **único** `(ParticipanteId, JogoId)` (garante a chave lógica do upsert do Spec 60).
- **`ListaEspera`:** FK `JogoId → Jogos.Id` `OnDelete(Cascade)`; `ParticipanteIds` mapeado pelo **campo** `_participanteIds` (coluna `text`, `IsRequired`), com `HasConversion` JSON **e** `ValueComparer<List<Guid>>` (sem o comparer o EF não detecta mudanças na lista). A propriedade pública `ParticipanteIds` é ignorada pelo modelo (`Ignore`).
- **`Van.ResponsavelId`:** Guid simples, **sem FK** para `Participante` nesta fase (a v1 não cria `Van`; a alocação usa `VanAlocada` no Spec 70).

**Dependencies:** EF Core, entidades do domínio.

**Error Handling:** N/A.

## Data Flow

1. `Grupo.AdicionarParticipante` valida capacidade e adiciona o `Participante` à lista privada.
2. O `ApplicationDbContext` persiste `Grupo` e seus `Participante` (via mapeamento 1:N) quando `SaveChanges` é chamado (casos de uso nos Specs 40+).
3. A migration `InitialCreate` cria as tabelas no PostgreSQL.

## Data Structures

| Entidade | Tabela (proposta) | Colunas principais |
|---|---|---|
| Participante | `Participantes` | Id (PK), Nome, WhatsApp, GrupoId (FK) |
| Grupo | `Grupos` | Id (PK), Nome, Rota |
| Van | `Vans` | Id (PK), Rota, Motorista, ResponsavelId |
| Spin | `Spins` | Id (PK), Rota |
| Jogo | `Jogos` | Id (PK), CompeticaoCodigo, Adversario, Data, HorarioSaida, PontoEmbarque |
| Pagamento | `Pagamentos` | Id (PK), ParticipanteId (FK cascade), JogoId (FK cascade), Pago — índice único (ParticipanteId, JogoId) |
| ListaEspera | `ListasEspera` | Id (PK), JogoId (FK cascade), Rota, ParticipanteIds (JSON/text) |

> **Nota:** `ListaEspera.ParticipanteIds` é uma coleção de Guids persistida como **JSON** na coluna `text` `ParticipanteIds`, via `HasConversion` sobre o campo `_participanteIds`, com `ValueComparer<List<Guid>>` (opção mais simples, KISS).

## File / Module Layout

```
backend/src/CaronaAlvinegra.Domain/
├── Enums/Rota.cs
├── Entities/Participante.cs
├── Entities/Grupo.cs
├── Entities/Van.cs
├── Entities/Spin.cs
├── Entities/Jogo.cs
├── Entities/Pagamento.cs
├── Entities/ListaEspera.cs
└── Exceptions/DomainException.cs

backend/src/CaronaAlvinegra.Infrastructure/
├── Data/ApplicationDbContext.cs        (editar: adicionar DbSets + OnModelCreating)
├── Data/Configurations/*Configuration.cs (novo)
└── Migrations/                         (gerado pelo dotnet ef)

backend/tests/CaronaAlvinegra.Domain.Tests/
└── GrupoTests.cs                       (novo)
```

## Testing Strategy

- **GrupoTests:** adicionar 15 participantes (passa) e o 16º (lança `DomainException`); remover participante inexistente (não lança).
- **Participante:** construtor rejeita `Nome` vazio/nulo.
- **Jogo:** construtor rejeita `Adversario` vazio.
- **Build/test:** `dotnet build` e `dotnet test` com exit code 0.

## Migration / Rollback

- **Migration:** `dotnet ef migrations add InitialCreate --project src/CaronaAlvinegra.Infrastructure --startup-project src/CaronaAlvinegra.Api`. Requer `dotnet-ef` instalado como ferramenta global.
- **Rollback:** `dotnet ef database update 0` (desfaz a migration) ou `dotnet ef migrations remove` antes de aplicar.
