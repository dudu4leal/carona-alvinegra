# Spec 70: Algoritmo de Alocação — Design

## Design Approach

O algoritmo vive como **serviço de domínio puro** (`Alocador`), sem IO, operando sobre as entidades do Spec 20 e produzindo o aggregate `Alocacao`. A Application (`AlocacaoService`) carrega os grupos confirmados por rota, chama o `Alocador` e persiste o resultado. O empacotamento usa **first-fit decrescente** (ordena grupos do maior para o menor e coloca cada um na primeira van com espaço).

## Architecture Decisions

- **`Alocador` no Domain** (regra de negócio pura), sem dependência de repositório/EF.
- **Resultado = aggregate `Alocacao`** (por jogo + rota), persistido para o Spec 80.
- **First-fit decrescente** para empacotamento (simples, determinístico e próximo do ótimo para o caso de uso).
- **Promoção automática** no próprio algoritmo: quando `spin + lista de espera ≥ 12`, o spin vira van.

## Component / Module Breakdown

### Componente: `Alocacao` (aggregate root, Domain)

**Purpose:** Resultado persistível de uma rota de um jogo.

**Interface:**
```csharp
public class Alocacao
{
    public Guid Id { get; private set; }
    public Guid JogoId { get; private set; }
    public Rota Rota { get; private set; }
    public IReadOnlyList<VanAlocada> Vans => _vans;
    public SpinAlocado? Spin { get; private set; }
    public IReadOnlyList<Guid> GruposEscalados => _gruposEscalados;

    public Alocacao(Guid jogoId, Rota rota);
    public void AdicionarVan(VanAlocada van);
    public void DefinirSpin(SpinAlocado spin);
    public void Escalar(Guid grupoId);
}
```

**Internal Logic:** `Id = Guid.NewGuid()` no construtor; listas privadas expostas como read-only.

**Dependencies:** `Rota`, `VanAlocada`, `SpinAlocado`.

**Error Handling:** N/A.

### Componente: `VanAlocada` (Domain)

**Purpose:** Uma van com os grupos alocados.

**Interface:**
```csharp
public class VanAlocada
{
    public Guid Id { get; private set; }
    public string Motorista { get; private set; }
    public Guid ResponsavelId { get; private set; }
    public IReadOnlyList<Guid> GrupoIds => _grupoIds;
    public int Ocupacao { get; private set; }

    public VanAlocada(string motorista, Guid responsavelId, IReadOnlyList<Guid> grupoIds, int ocupacao);
}
```

**Internal Logic:** Guarda os `GrupoIds` e a ocupação calculada.

**Dependencies:** Nenhuma.

**Error Handling:** N/A.

### Componente: `SpinAlocado` (Domain)

**Purpose:** Um spin com os grupos alocados.

**Interface:**
```csharp
public class SpinAlocado
{
    public Guid Id { get; private set; }
    public IReadOnlyList<Guid> GrupoIds => _grupoIds;
    public int Ocupacao { get; private set; }

    public SpinAlocado(IReadOnlyList<Guid> grupoIds, int ocupacao);
}
```

**Internal Logic:** Guarda `GrupoIds` e ocupação.

**Dependencies:** Nenhuma.

**Error Handling:** N/A.

### Componente: `Alocador` (serviço de domínio, Domain)

**Purpose:** Empacotar grupos em vans/spins e escalar.

**Interface:**
```csharp
public class Alocador
{
    public Alocacao Alocar(Guid jogoId, Rota rota, IReadOnlyList<Grupo> grupos, int tamanhoListaEspera = 0);
}
```

**Internal Logic (determinístico):**
1. `gruposOrdenados = grupos.OrderByDescending(g => g.Participantes.Count)`.
2. Empacotamento first-fit: para cada grupo (do maior ao menor), tenta encaixar na primeira van com `ocupacao + tamanho <= Van.Capacidade`; se não couber, abre nova van.
3. Separar vans justificadas (`ocupacao >= 12`) das sobras (`ocupacao < 12`).
4. Achatar os grupos das vans-sobra em `sobra`. Se `soma(sobra) <= Spin.Capacidade (6)` → criar `SpinAlocado`; senão → `Escalar(grupoId)` para cada grupo da sobra.
5. Promoção: se existe spin e `spin.Ocupacao + tamanhoListaEspera >= 12`, remover o spin e criar uma `VanAlocada` promovida com os grupos do spin.

**Dependencies:** `Grupo`, `Van`, `Spin`, `Alocacao`, `VanAlocada`, `SpinAlocado`.

**Error Handling:** N/A (nenhuma exceção prevista; casos extremos cobertos por regras).

### Componente: `IAlocacaoRepository` / `AlocacaoRepository`

**Purpose:** Persistir alocações.

**Interface:**
```csharp
public interface IAlocacaoRepository
{
    Task AddAsync(Alocacao alocacao);
    Task<List<Alocacao>> GetByJogoAsync(Guid jogoId);
    Task RemoveByJogoAsync(Guid jogoId);
    Task SaveChangesAsync();
}
```

**Internal Logic:** `AlocacaoRepository` usa `ApplicationDbContext`; `RemoveByJogoAsync` remove as alocações do jogo (para reexecutar idempotente).

**Dependencies:** `ApplicationDbContext`.

**Error Handling:** N/A.

### Componente: `AlocacaoService` (Application)

**Purpose:** Orquestrar o algoritmo com dados reais.

**Interface:**
```csharp
public class AlocacaoService
{
    Task<List<AlocacaoResponse>> AlocarJogoAsync(Guid jogoId);
    Task<List<AlocacaoResponse>> ObterAsync(Guid jogoId);
}
```

**Internal Logic:** `AlocarJogoAsync` carrega os grupos do jogo (via `IGrupoRepository`/participantes), agrupa por `Rota`, obtém o tamanho da lista de espera por rota (via `ListaEspera`), chama `Alocador.Alocar` para cada rota, remove alocações antigas e persiste. Converte para DTO de resposta.

**Dependencies:** `Alocador`, `IGrupoRepository`, `IAlocacaoRepository`, `IJogoRepository`.

**Error Handling:** Jogo inexistente → erro de aplicação (404 no controller).

### Componente: `AlocacoesController` (Api)

**Purpose:** Endpoints REST.

**Interface:** `[Authorize] [ApiController] [Route("api/jogos/{jogoId}")]`.

**Internal Logic:** `POST .../alocar` → `AlocacaoService.AlocarJogoAsync`; `GET .../alocacao` → `AlocacaoService.ObterAsync`.

**Dependencies:** `AlocacaoService`.

**Error Handling:** Não encontrado → 404.

### Componente: DTOs (Application)

**Purpose:** Contrato de resposta.

**Interface:**
```csharp
public record VanAlocadaResponse(Guid Id, string Motorista, Guid ResponsavelId, List<Guid> GrupoIds, int Ocupacao);
public record SpinAlocadoResponse(Guid Id, List<Guid> GrupoIds, int Ocupacao);
public record AlocacaoResponse(Guid Id, Guid JogoId, Rota Rota, List<VanAlocadaResponse> Vans, SpinAlocadoResponse? Spin, List<Guid> GruposEscalados);
```

**Internal Logic:** N/A.

**Dependencies:** `Rota`.

**Error Handling:** N/A.

## Data Flow

1. `POST /api/jogos/{jogoId}/alocar` → `AlocacaoService.AlocarJogoAsync`.
2. Service carrega grupos do jogo, agrupa por rota.
3. Para cada rota, `Alocador.Alocar(jogoId, rota, grupos, espera)` produz `Alocacao` (vans/spin/escalados).
4. Service remove alocações antigas e persiste as novas.
5. Retorna DTO com o resultado por rota.

## Data Structures

- **Tabelas novas:** `Alocacoes` (Id, JogoId, Rota), `VansAlocadas` (Id, AlocacaoId, Motorista, ResponsavelId), `SpinsAlocados` (Id, AlocacaoId, Ocupacao), e tabelas de junção para `GrupoIds` (ou colunas JSON).
- **Constantes:** `Van.Capacidade = 15`, `Spin.Capacidade = 6`, `Grupo.CapacidadeMaxima = 15`.

## File / Module Layout

```
backend/src/CaronaAlvinegra.Domain/
├── Entities/Alocacao.cs                (novo)
├── Entities/VanAlocada.cs              (novo)
├── Entities/SpinAlocado.cs             (novo)
└── Services/Alocador.cs                (novo)

backend/src/CaronaAlvinegra.Application/
├── Interfaces/IAlocacaoRepository.cs   (novo)
├── Dtos/AlocacaoDtos.cs                (novo)
└── Services/AlocacaoService.cs         (novo)

backend/src/CaronaAlvinegra.Infrastructure/
├── Repositories/AlocacaoRepository.cs  (novo)
└── Migrations/                         (nova migration)

backend/src/CaronaAlvinegra.Api/
├── Controllers/AlocacoesController.cs  (novo)
└── Program.cs                          (editar: registrar DI)

backend/tests/CaronaAlvinegra.Domain.Tests/
└── AlocadorTests.cs                    (novo)
```

## Testing Strategy

Casos do `AlocadorTests` (determinísticos):
- Grupos `[15]` → 1 van cheia.
- Grupos `[8,7]` → 1 van (15).
- Grupos `[8,6,6]` → 1 van (14) + 1 spin (6).
- Grupos `[8,8,6]` → 1 van (14) + 1 grupo escalado (8).
- Grupos `[6]` → 1 spin (6) [rota < 12].
- Grupos `[6]` + lista de espera 6 → promoção (1 van 12, sem spin).
- Grupos `[10]` → 1 grupo escalado (10).

## Migration / Rollback

- **Migration:** `dotnet ef migrations add AddAlocacao --project src/CaronaAlvinegra.Infrastructure --startup-project src/CaronaAlvinegra.Api`.
- **Rollback:** `dotnet ef database update <migration-anterior>`.
