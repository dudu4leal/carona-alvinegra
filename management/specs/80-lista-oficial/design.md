# Spec 80: Geração da Lista Oficial — Design

## Design Approach

A geração da lista é uma **formatação de saída pura** (sem regra de negócio nova), implementada como um serviço na Application. O `ListaOficialService` carrega o jogo, a alocação e os participantes, e monta o texto usando o formato canônico do `brainstorm.md` §7.

## Architecture Decisions

- **Serviço na Application** (não no Domain) — é apresentação/formatação, não regra de negócio.
- **Rodapé e emojis como constantes** únicas (DRY).
- **Numeração global das vans** (1, 2, 3, ...) na ordem de processamento das rotas.
- **Responsável em primeiro** na van, com sufixo `( Resp)`.

## Component / Module Breakdown

### Componente: `ListaOficialService` (Application)

**Purpose:** Montar o texto da lista oficial.

**Interface:**
```csharp
public class ListaOficialService
{
    Task<ListaOficialResponse> GerarAsync(Guid jogoId);
}
```

**Internal Logic:**
1. Carrega `Jogo` (via `IJogoRepository`); se não existir, propaga erro (404).
2. Carrega `Alocacao`(ões) do jogo (via `IAlocacaoRepository`).
3. Para cada van, resolve os participantes dos grupos (via `IGrupoRepository`/`IParticipanteRepository`) e ordena com o responsável primeiro.
4. Monta cabeçalho → blocos de van → bloco spin → rodapé, concatenando com `\n`.

**Dependencies:** `IJogoRepository`, `IAlocacaoRepository`, `IGrupoRepository`, `IParticipanteRepository`.

**Error Handling:** Jogo sem alocação/participantes → lança erro de aplicação (mapeado para 400/404).

### Componente: Formatação do cabeçalho

**Purpose:** Linhas iniciais.

**Internal Logic:**
```
LISTA OFICIAL
{CompeticaoCodigo}
Data: {Data:dd-MM}, {DiaDaSemana}
Saída, {HorarioSaida}
Embarque: {PontoEmbarque}
Botafogo x {Adversario}
```
`DiaDaSemana` em português (Segunda…Domingo) a partir de `Jogo.Data`.

**Dependencies:** `Jogo`.

**Error Handling:** N/A.

### Componente: Formatação do bloco de van

**Purpose:** Lista numerada de participantes por van.

**Internal Logic:** Para cada `VanAlocada`, emite:
```
Van {n} 🚐 🚐 ( {rota})
1- ✅ {Responsavel}( Resp)
2- ✅ {Participante}
...
```
O responsável (`ResponsavelId`) vem primeiro; os demais seguem na ordem dos grupos/participantes.

**Dependencies:** `VanAlocada`, participantes.

**Error Handling:** Responsável não encontrado → omite a marcação `( Resp)` e lista normalmente.

### Componente: Formatação do bloco de spin

**Purpose:** Lista do spin.

**Internal Logic:** Se houver `SpinAlocado`, emite:
```
Spin
1- ✅ {Participante}
...
6- ✅ {Participante}
```

**Dependencies:** `SpinAlocado`, participantes.

**Error Handling:** N/A.

### Componente: Rodapé fixo

**Purpose:** Texto padrão.

**Internal Logic:** Constante com o texto exato do `brainstorm.md` §7:
```
❌ Não é permiti atraso
❌ Não é permitido o uso de drogas.
💯 Volta será uma hora após o jogo, com tolerância de 10 minutos. Caso não apareça, o responsável da van está autorizado a partir sem você.
✅ O carona não é meu, é nosso então, cuide dele!
```

**Dependencies:** Nenhuma.

**Error Handling:** N/A.

### Componente: `ListaOficialResponse` (Application)

**Purpose:** Contrato de saída.

**Interface:** `public record ListaOficialResponse(string Texto);`.

**Internal Logic:** N/A.

**Dependencies:** Nenhuma.

**Error Handling:** N/A.

### Componente: `ListaController` (Api)

**Purpose:** Endpoint REST.

**Interface:** `[Authorize] [ApiController] [Route("api/jogos/{jogoId}/lista")]`.

**Internal Logic:** `GET` → `ListaOficialService.GerarAsync` → 200 com `{ texto }`; 404 se jogo não existe.

**Dependencies:** `ListaOficialService`.

**Error Handling:** 404 jogo inexistente; 400 sem alocação.

## Data Flow

1. `GET /api/jogos/{jogoId}/lista` → `ListaController`.
2. `ListaOficialService.GerarAsync` carrega jogo + alocação + participantes.
3. Monta o texto (cabeçalho + vans + spin + rodapé).
4. Retorna `{ texto }`.

## Data Structures

- **`ListaOficialResponse { string Texto }`**.
- Formato final (exemplo):
```
LISTA OFICIAL
BR-26
Data: 7-10, Quarta
Saída, 17:00
Embarque: Praça 14 bis ✈️
Botafogo x Vasco

Van 1 🚐 🚐 ( Bingen)
1- ✅ Daodock( Resp)
2- ✅ Nicolle
...

Spin
1- ✅ Jones
...

❌ Não é permiti atraso
❌ Não é permitido o uso de drogas.
💯 Volta será uma hora após o jogo, com tolerância de 10 minutos. Caso não apareça, o responsável da van está autorizado a partir sem você.
✅ O carona não é meu, é nosso então, cuide dele!
```

## File / Module Layout

```
backend/src/CaronaAlvinegra.Application/
├── Dtos/ListaOficialDtos.cs            (novo)
└── Services/ListaOficialService.cs     (novo)

backend/src/CaronaAlvinegra.Api/
├── Controllers/ListaController.cs      (novo)
└── Program.cs                          (editar: registrar DI)

backend/tests/CaronaAlvinegra.Domain.Tests/
└── ListaOficialServiceTests.cs         (novo)
```

## Testing Strategy

- **ListaOficialServiceTests** (repositórios in-memory/mock): cabeçalho contém `LISTA OFICIAL` + código + adversário; responsável aparece primeiro com `( Resp)`; rodapé fixo presente; spin omitido quando não há.
- **Build/test:** `dotnet build` e `dotnet test` com exit code 0.

## Migration / Rollback

- **Sem migration nova** — não há mudança de schema.
- **Rollback:** N/A.
