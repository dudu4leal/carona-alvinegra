# Spec 70: Algoritmo de Alocação — Design

## Design Approach

O algoritmo vive como **serviço de domínio puro** (`Alocador`), sem IO, operando sobre as entidades do Spec 20 e produzindo o aggregate `Alocacao`. A Application (`AlocacaoService`) carrega os grupos confirmados por rota, chama o `Alocador` e persiste o resultado. O empacotamento usa **first-fit decrescente** (ordena grupos do maior para o menor e coloca cada um na primeira van com espaço).

## Architecture Decisions

- **`Alocador` no Domain** (regra de negócio pura), sem dependência de repositório/EF.
- **Resultado = aggregate `Alocacao`** (por jogo + rota), persistido para o Spec 80.
- **First-fit decrescente** para empacotamento (simples, determinístico e próximo do ótimo para o caso de uso).
- **Promoção automática** no próprio algoritmo: quando `spin + lista de espera ≥ 12`, o spin vira van.
- **"Grupo confirmado" = todos os participantes pagos**: a Application consulta `IPagamentoRepository.GetByJogoAsync` e só passa ao `Alocador` os grupos cujos participantes têm `Pago == true` para o jogo; os demais são devolvidos em `GruposPendentes` (não persistido). Grupos sem participantes são ignorados. O `Alocador` permanece puro.
- **Lista de espera sempre vazia na v1** (nenhum spec a escreve): `tamanhoListaEspera = 0` em produção; a promoção é coberta apenas por testes.

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
5. Promoção: se existe spin e `spin.Ocupacao + tamanhoListaEspera >= 12`, remover o spin e criar uma `VanAlocada` promovida com os grupos do spin e `Ocupacao = spin.Ocupacao + tamanhoListaEspera` (a lista de espera é movida para a van promovida pelo `AlocacaoService`).

**Preenchimento de Motorista/Responsável:** o `Alocador` cria toda `VanAlocada` com `Motorista = ""` e `ResponsavelId = Guid.Empty` (o algoritmo não tem esses dados). O preenchimento posterior é responsabilidade do **Spec 110** (`PUT /api/jogos/{jogoId}/alocacao/vans/{vanId}` + campo na `AlocacaoPage`); reexecutar a alocação zera esses valores.

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

### Componente: `IListaEsperaRepository` / `ListaEsperaRepository`

**Purpose:** Ler a lista de espera por rota.

**Interface:**
```csharp
public interface IListaEsperaRepository
{
    Task<ListaEspera?> GetByJogoRotaAsync(Guid jogoId, Rota rota);
}
```

**Internal Logic:** `ListaEsperaRepository` usa `ApplicationDbContext`; `GetByJogoRotaAsync` busca a `ListaEspera` com `JogoId == jogoId && Rota == rota`.

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

**Internal Logic (fechada):**
1. Carrega os jogos/grupos: `IGrupoRepository` (grupos com participantes) e valida o jogo via `IJogoRepository` (404 se inexistente).
2. `pagamentos = await _pagamentoRepository.GetByJogoAsync(jogoId)`; `pagantes = pagamentos.Where(p => p.Pago).Select(p => p.ParticipanteId).ToHashSet()`.
3. Descarta grupos com `Participantes.Count == 0`.
4. Confirma um grupo quando **todos** os seus participantes estão em `pagantes`. Os demais (com ≥1 participante) vão para `pendentesPorRota[grupo.Rota]`.
5. Agrupa os confirmados por `Rota`. Itera sobre as rotas presentes em confirmados ∪ pendentes.
6. Para cada rota: `tamanhoEspera = (await _listaEsperaRepository.GetByJogoRotaAsync(jogoId, rota))?.ParticipanteIds.Count ?? 0` (sempre 0 na v1, ver Constraints); `alocacao = _alocador.Alocar(jogoId, rota, confirmadosDaRota, tamanhoEspera)`.
7. `await _alocacaoRepository.RemoveByJogoAsync(jogoId)` e persiste as novas alocações (inclusive as de rota sem confirmados, com `Vans`/`Spin` vazios).
8. Converte para `AlocacaoResponse`, preenchendo `GruposPendentes = pendentesPorRota[rota]`.

`ObterAsync` lê as alocações persistidas e recalcula `GruposPendentes` com os pagamentos atuais.

**Dependencies:** `Alocador`, `IGrupoRepository`, `IAlocacaoRepository`, `IListaEsperaRepository`, `IJogoRepository`, `IPagamentoRepository` (Spec 60, somente leitura).

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
public record AlocacaoResponse(Guid Id, Guid JogoId, Rota Rota, List<VanAlocadaResponse> Vans, SpinAlocadoResponse? Spin, List<Guid> GruposEscalados, List<Guid> GruposPendentes);
```

**Internal Logic:** N/A.

**Dependencies:** `Rota`.

**Error Handling:** N/A.

## Data Flow

1. `POST /api/jogos/{jogoId}/alocar` → `AlocacaoService.AlocarJogoAsync`.
2. Service carrega pagamentos do jogo ("pagantes"), grupos com participantes e classifica cada grupo em confirmado (todos pagantes) ou pendente.
3. Para cada rota com grupos (confirmados ou pendentes), `Alocador.Alocar(jogoId, rota, confirmados, espera)` produz `Alocacao` (vans/spin/escalados).
4. Service remove alocações antigas e persiste as novas.
5. Retorna DTO por rota com `gruposEscalados` e `gruposPendentes`.

## Data Structures

- **Tabelas novas:** `Alocacoes` (Id, JogoId, Rota), `VansAlocadas` (Id, AlocacaoId, Motorista, ResponsavelId), `SpinsAlocados` (Id, AlocacaoId, Ocupacao). As listas de `GrupoIds` (e `GruposEscalados`) são persistidas como **JSON** (coluna de texto) via `HasConversion`, consistente com o Spec 20.
- **`GruposPendentes` NÃO é persistido**: é calculado em cada resposta a partir de `Pagamentos` (`JogoId` + `Pago`).
- **`GruposEscalados`** é persistido como JSON na `Alocacao`.
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
├── Interfaces/IListaEsperaRepository.cs (novo)
├── Dtos/AlocacaoDtos.cs                (novo)
└── Services/AlocacaoService.cs         (novo)

backend/src/CaronaAlvinegra.Infrastructure/
├── Repositories/AlocacaoRepository.cs  (novo)
├── Repositories/ListaEsperaRepository.cs (novo)
└── Migrations/                         (nova migration)

backend/src/CaronaAlvinegra.Api/
├── Controllers/AlocacoesController.cs  (novo)
└── Program.cs                          (editar: registrar DI)

backend/tests/CaronaAlvinegra.Domain.Tests/
├── AlocadorTests.cs                    (novo)
└── AlocacaoServiceTests.cs             (novo)
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

Casos do `AlocacaoServiceTests` (Application, com fakes in-memory escritos à mão — ver Spec 10):
- Grupo com todos os participantes pagos → entra na alocação.
- Grupo com 1 participante sem pagamento → não entra e aparece em `GruposPendentes` da rota.
- Grupo sem participantes → ignorado (não entra nem em `GruposPendentes`).
- `ObterAsync` recalcula `GruposPendentes` após alteração de pagamentos.

## Migration / Rollback

- **Migration:** `dotnet ef migrations add AddAlocacao --project src/CaronaAlvinegra.Infrastructure --startup-project src/CaronaAlvinegra.Api`.
- **Rollback:** `dotnet ef database update <migration-anterior>`.
