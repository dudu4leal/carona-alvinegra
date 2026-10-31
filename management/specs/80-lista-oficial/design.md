# Spec 80: Geração da Lista Oficial — Design

## Design Approach

A geração da lista é uma **formatação de saída pura** (sem regra de negócio nova), implementada como um serviço na Application. O `ListaOficialService` carrega o jogo, a alocação e os participantes, e monta o texto usando o formato canônico do `brainstorm.md` §7.

## Architecture Decisions

- **Serviço na Application** (não no Domain) — é apresentação/formatação, não regra de negócio.
- **Rodapé e emojis como constantes** únicas (DRY): `Rodape`, `EmojiAviao`, `EmojiVan`, `EmojiCheck`.
- **Numeração global das vans** (1, 2, 3, ...) na ordem de processamento das rotas; alocações ordenadas por `Rota` para determinismo.
- **Responsável em primeiro** na van, com sufixo `( Resp)`.
- **Limites de exibição:** vans limitadas a `Van.Capacidade` (15) e spin a `Spin.Capacidade` (6).

## Component / Module Breakdown

### Componente: `ListaOficialService` (Application)

**Purpose:** Montar o texto da lista oficial.

**Interface:**
```csharp
public class ListaOficialService
{
    Task<ListaOficialResponse?> GerarAsync(Guid jogoId);
}
```

**Internal Logic:**
1. Carrega `Jogo` (via `IJogoRepository`); se não existir, retorna `null` (404).
2. Carrega `Alocacao`(ões) do jogo (via `IAlocacaoRepository`); se não houver, lança `DomainException` (400).
3. Carrega os grupos com participantes (via `IGrupoRepository`) e resolve os participantes de cada van/spin; o responsável (`ResponsavelId`) vem primeiro.
4. Ordena as alocações por `Rota` e monta cabeçalho → blocos de van (limite 15) → blocos spin (limite 6) → rodapé, com blocos separados por `\n\n`.
5. Se nenhuma van/spin produziu participantes, lança `DomainException` (400).

**Dependencies:** `IJogoRepository`, `IAlocacaoRepository`, `IGrupoRepository` (os participantes são resolvidos pelos grupos, sem consulta separada).

**Error Handling:** Jogo inexistente → `null` (404). Sem alocação ou sem participantes alocados → `DomainException` (400).

### Componente: Formatação do cabeçalho

**Purpose:** Linhas iniciais.

**Internal Logic:**
```
LISTA OFICIAL
{CompeticaoCodigo}
Data: {Data:d-M}, {DiaDaSemana}
Saída, {HorarioSaida}
Embarque: {PontoEmbarque} ✈️
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

- **ListaOficialServiceTests** (com fakes in-memory escritos à mão, sem pacote de mock — ver Spec 10): cabeçalho contém `LISTA OFICIAL` + código + adversário; responsável aparece primeiro com `( Resp)`; responsável inexistente não marca; rodapé fixo presente; spin omitido quando não há e incluído quando há; jogo inexistente → `null`; sem alocação/sem participantes → `DomainException`; van com grupo excluído é omitida; limites de 15 (van) e 6 (spin) linhas.
- **Build/test:** `dotnet build` e `dotnet test` com exit code 0.

## Migration / Rollback

- **Sem migration nova** — não há mudança de schema.
- **Rollback:** N/A.
