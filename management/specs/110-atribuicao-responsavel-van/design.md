# Spec 110: Atribuição de motorista e responsável da van — Design

## Design Approach

Adicionar uma única operação de escrita sobre a alocação existente: `PUT /api/jogos/{jogoId}/alocacao/vans/{vanId}` atualiza `Motorista` e `ResponsavelId` de uma `VanAlocada`. A validação de integridade (o responsável precisa ser participante de um dos grupos da van) fica na Application; o Domain só aplica a mutação e uma regra de tamanho. O frontend ganha campos de edição em cada van da `AlocacaoPage`. Nenhuma mudança no algoritmo, no schema ou na lista oficial.

## Architecture Decisions

- **Sem migration**: `VansAlocadas` já tem `Motorista` e `ResponsavelId` (Spec 70). Update in-place via EF Core change tracking.
- **404 por retorno nulo** (`Task<VanAlocadaResponse?>`), seguindo o padrão do Spec 40; o projeto não possui tipo de exceção de "não encontrado".
- **400 via `DomainException`** já mapeada pelo `DomainExceptionMiddleware` (Spec 40) para `{ "message": ... }`.
- **Reutilizar** `AlocacoesController`, `AlocacaoService`, `VanAlocadaResponse`, `IGrupoRepository` — sem novos controllers/services.
- **`Guid.Empty` = "sem responsável"** (sem necessidade de tornar `ResponsavelId` nullable, o que exigiria migration).
- **Frontend resolve nomes** com `GET /api/grupos`; o backend não recebe/retorna nomes de participantes nesta operação (evita endpoint novo).

## Component / Module Breakdown

### Componente: `VanAlocada` (Domain, editar)

**File path:** `backend/src/CaronaAlvinegra.Domain/Entities/VanAlocada.cs`

**Purpose:** Mutação encapsulada da tripulação da van.

**Interface (trecho adicionado):**
```csharp
public void DefinirResponsavel(string motorista, Guid responsavelId)
{
    var motoristaNormalizado = (motorista ?? string.Empty).Trim();
    if (motoristaNormalizado.Length > 100)
        throw new DomainException("Motorista deve ter no máximo 100 caracteres.");

    Motorista = motoristaNormalizado;
    ResponsavelId = responsavelId;
}
```

**Internal Logic:** normaliza `Motorista` e grava `ResponsavelId`. Não valida pertencimento (não conhece participantes).

**Dependencies:** `DomainException`.

**Error Handling:** `DomainException` para motorista > 100 caracteres.

### Componente: `AlocacaoService` (Application, editar)

**File path:** `backend/src/CaronaAlvinegra.Application/Services/AlocacaoService.cs`

**Purpose:** Caso de uso com validação de integridade.

**Interface (método adicionado):**
```csharp
Task<VanAlocadaResponse?> DefinirResponsavelVanAsync(
    Guid jogoId, Guid vanId, string motorista, Guid responsavelId);
```

**Internal Logic:**
1. `alocacoes = await _alocacaoRepository.GetByJogoAsync(jogoId)`; se vazio → `null`.
2. `alocacao = alocacoes.FirstOrDefault(a => a.Vans.Any(v => v.Id == vanId))`; se `null` → `null`.
3. `van = alocacao.Vans.First(v => v.Id == vanId)`.
4. Se `responsavelId != Guid.Empty`:
   - `todos = await _grupoRepository.GetAllAsync()`; `grupos = todos.Where(g => van.GrupoIds.Contains(g.Id))` (usa o método existente do Spec 40; não alterar a interface);
   - `participantesDaVan = grupos.SelectMany(g => g.Participantes).Select(p => p.Id).ToHashSet()`;
   - se `!participantesDaVan.Contains(responsavelId)` → `throw new DomainException("Responsável deve ser participante de um grupo da van.")`.
5. `van.DefinirResponsavel(motorista, responsavelId)`.
6. `await _alocacaoRepository.SaveChangesAsync()`.
7. Retorna `new VanAlocadaResponse(van.Id, van.Motorista, van.ResponsavelId, van.GrupoIds.ToList(), van.Ocupacao)`.

**Dependencies:** `IAlocacaoRepository`, `IGrupoRepository`, `VanAlocadaResponse`, `DomainException`.

**Error Handling:** `null` → 404; `DomainException` → 400 (middleware).

### Componente: `DefinirResponsavelVanRequest` (Application, novo)

**File path:** `backend/src/CaronaAlvinegra.Application/Dtos/AlocacaoDtos.cs` (editar)

**Interface:**
```csharp
public record DefinirResponsavelVanRequest(string Motorista, Guid ResponsavelId);
```

**Internal Logic:** N/A.

**Dependencies:** Nenhuma.

**Error Handling:** Corpo inválido/JSON malformado → 400 do `[ApiController]`.

### Componente: `AlocacoesController` (Api, editar)

**File path:** `backend/src/CaronaAlvinegra.Api/Controllers/AlocacoesController.cs`

**Purpose:** Expor a operação.

**Interface (ação adicionada, herda `[Authorize] [ApiController] [Route("api/jogos/{jogoId}")]`):**
```csharp
[HttpPut("alocacao/vans/{vanId}")]
public async Task<ActionResult<VanAlocadaResponse>> DefinirResponsavelVan(
    Guid jogoId, Guid vanId, [FromBody] DefinirResponsavelVanRequest request)
{
    var response = await _alocacaoService.DefinirResponsavelVanAsync(
        jogoId, vanId, request.Motorista, request.ResponsavelId);

    return response is null ? NotFound() : Ok(response);
}
```

**Route resultante:** `PUT /api/jogos/{jogoId}/alocacao/vans/{vanId}`.

**Internal Logic:** delega ao service; `null` → 404; `DomainException` → 400 pelo middleware.

**Dependencies:** `AlocacaoService`.

**Error Handling:** ver Internal Logic.

### Componente: Frontend — tipos e cliente (editar)

**File paths:** `frontend/src/types/index.ts`, `frontend/src/api/client.ts`

**Interface (types):**
```ts
interface DefinirResponsavelVanRequest { motorista: string; responsavelId: string; }
```

**Interface (client):**
```ts
definirResponsavelVan(jogoId: string, vanId: string, body: DefinirResponsavelVanRequest): Promise<VanAlocada>;
// PUT `/api/jogos/${jogoId}/alocacao/vans/${vanId}` com body
```

**Internal Logic:** usa a instância `api` existente (baseURL + interceptor de token).

**Dependencies:** `api/client.ts`, tipos do Spec 90.

**Error Handling:** erros propagam (`api` já trata 401); a página exibe a mensagem.

### Componente: Frontend — `AlocacaoPage` (editar)

**File path:** `frontend/src/pages/AlocacaoPage.tsx`

**Purpose:** Editar motorista/responsável por van.

**Internal Logic:**
1. Ao montar/selecionar jogo: `GET /api/jogos/{id}/alocacao` (array) e `GET /api/grupos` (para mapear `grupoId` → grupo e participantes).
2. Para cada van de cada alocação, renderizar:
   - `input` controlado "Motorista" (valor inicial `van.motorista`);
   - `select` "Responsável" com opção `"— sem responsável —"` (`value=""`) e uma opção por participante dos grupos da van (`value=participante.id`, texto `"{nome} ({grupo.nome})"`), mais uma opção extra "Responsável atual (fora dos grupos da van)" com o `responsavelId` atual quando ele não estiver entre os participantes listados (sem inventar participante);
   - botão `"Salvar"`.
3. `Salvar` → `definirResponsavelVan(jogoId, van.id, { motorista, responsavelId: valor === '' ? '00000000-0000-0000-0000-000000000000' : valor })`.
4. Em sucesso, substituir a van no estado local pela resposta.
5. Em erro, exibir `error.response.data.message` (ou mensagem genérica).

**Dependencies:** `client.ts`, `types/index.ts`, React Router (jogo selecionado).

**Error Handling:** mensagens 400/404 visíveis; estado local só muda após 200. Erro ao carregar a alocação limpa `alocacoes`/`rascunhos` para não exibir dados de outro jogo.

## Data Flow

1. `GET /api/jogos` → lista de jogos; Jones seleciona um.
2. `GET /api/jogos/{id}/alocacao` → `Alocacao[]` (Spec 70) + `GET /api/grupos` → grupos com participantes.
3. Jones edita motorista e escolhe responsável; `PUT /api/jogos/{id}/alocacao/vans/{vanId}`.
4. `AlocacoesController` → `AlocacaoService.DefinirResponsavelVanAsync` → valida pertencimento → `VanAlocada.DefinirResponsavel` → `SaveChangesAsync`.
5. Resposta 200 `VanAlocadaResponse`; UI atualiza a van.
6. `GET /api/jogos/{id}/lista` (Spec 80) usa `ResponsavelId` para escrever `1- {Nome}( Resp)`.

## Data Structures

**`DefinirResponsavelVanRequest`** (JSON):
| Campo | Tipo | Obrigatório | Restrição |
|---|---|---|---|
| `motorista` | string | sim (pode ser `""`) | ≤ 100 caracteres após `Trim` |
| `responsavelId` | Guid (string) | sim | `Guid.Empty` limpa; caso contrário deve ser participante de um grupo da van |

**Sem alterações de schema.** Tabela `VansAlocadas` (Spec 70) permanece com `Motorista` (texto) e `ResponsavelId` (Guid).

## File / Module Layout

```
backend/src/CaronaAlvinegra.Domain/
└── Entities/VanAlocada.cs                      (editar: + DefinirResponsavel)

backend/src/CaronaAlvinegra.Application/
├── Dtos/AlocacaoDtos.cs                        (editar: + DefinirResponsavelVanRequest)
└── Services/AlocacaoService.cs                 (editar: + DefinirResponsavelVanAsync)

backend/src/CaronaAlvinegra.Api/
└── Controllers/AlocacoesController.cs          (editar: + ação PUT)

backend/tests/CaronaAlvinegra.Domain.Tests/
└── AlocacaoResponsavelTests.cs                 (novo)

frontend/src/
├── types/index.ts                              (editar: + DefinirResponsavelVanRequest)
├── api/client.ts                               (editar: + definirResponsavelVan)
└── pages/AlocacaoPage.tsx                      (editar: campos motorista/responsável)
```

Nenhum outro arquivo é criado ou modificado. Nenhuma migration.

## Testing Strategy

Testes de serviço usam fakes in-memory escritos à mão (sem pacote de mock — ver Spec 10); `AlocacaoResponsavelTests` fica em `backend/tests/CaronaAlvinegra.Domain.Tests/`.

| # | Comando | Resultado esperado |
|---|---|---|
| 1 | `dotnet build backend/CaronaAlvinegra.sln` | exit 0 |
| 2 | `dotnet test backend/tests/CaronaAlvinegra.Domain.Tests` | exit 0 |
| 3 | `curl -X PUT http://localhost:5000/api/jogos/{jogoId}/alocacao/vans/{vanId} -H "Authorization: Bearer <token>" -H "Content-Type: application/json" -d '{"motorista":"Seu Zé","responsavelId":"<participanteId>"}'` | 200 com `responsavelId` igual ao enviado |
| 4 | mesmo `PUT` com `responsavelId` de participante de outro grupo | 400 com `message` |
| 5 | mesmo `PUT` com `vanId` aleatório | 404 |
| 6 | `GET /api/jogos/{jogoId}/alocacao` após #3 | van com `motorista`/`responsavelId` persistidos |
| 7 | `GET /api/jogos/{jogoId}/lista` após #3 | `1- {Nome}( Resp)` na van editada |
| 8 | `cd frontend && npm run build` | exit 0 |

## Migration / Rollback

- **Sem migration** (schema inalterado).
- **Rollback de dados:** reexecutar `POST /api/jogos/{id}/alocar` zera motorista/responsável de todas as vans (comportamento do Spec 70).
- **Rollback de código:** reverter o commit; as colunas continuam existindo e compatíveis.
