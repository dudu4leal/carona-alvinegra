---
name: lista-oficial
status: verified
references: V3
---

# Spec 80: Geração da Lista Oficial — Requirements

## Value Delivery

> **V3: Lista oficial para WhatsApp**
>
> "Gera a lista oficial do jogo no formato exato para colar no grupo do WhatsApp: cabeçalho (competição, data, horário de saída, ponto de embarque, confronto), blocos de van (com responsável marcado como 'Resp') e bloco spin, além do rodapé fixo (regras de atraso, drogas, horário de volta e o lema da carona)."

Este spec gera a **lista oficial** (texto copiável para o WhatsApp) a partir do jogo (Spec 50) e da alocação (Spec 70), seguindo o formato do `brainstorm.md` §7.

## Functional Requirements

### FR-001: Serviço `ListaOficialService`

**What:** Criar `ListaOficialService` na Application com o método `GerarAsync(Guid jogoId)` que retorna o texto completo da lista.

**Why:** Centraliza a montagem da lista (única responsabilidade).

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Application/Services/ListaOficialService.cs` existe.
- [ ] `GerarAsync(Guid jogoId)` retorna um `ListaOficialResponse` com o campo `Texto`.

### FR-002: Cabeçalho da lista

**What:** Montar o cabeçalho a partir dos dados do `Jogo`, no formato exato.

**Why:** O cabeçalho identifica a competição, data, saída, embarque e confronto.

**Acceptance Criteria:**
- [ ] O texto inicia com `LISTA OFICIAL`.
- [ ] Contém o `CompeticaoCodigo` (ex.: `BR-26`).
- [ ] Contém `Data: {dia}-{mes}, {diaDaSemana}` (ex.: `Data: 7-10, Quarta`).
- [ ] Contém `Saída, {HorarioSaida}` (ex.: `Saída, 17:00`).
- [ ] Contém `Embarque: {PontoEmbarque} ✈️`.
- [ ] Contém `Botafogo x {Adversario}`.

### FR-003: Blocos de van

**What:** Montar um bloco por van, com o responsável em primeiro lugar, numerando os participantes.

**Why:** Formato oficial exige responsável marcado como `( Resp)` e numeração sequencial.

**Acceptance Criteria:**
- [ ] Cada van gera o cabeçalho `Van {n} 🚐 🚐 ( {rota})` com `n` sequencial a partir de 1.
- [ ] O responsável da van aparece na posição `1-` com o sufixo `( Resp)`.
- [ ] Os demais participantes aparecem numerados (`2-`, `3-`, ...) com prefixo `✅`.
- [ ] Nenhuma van excede 15 linhas numeradas.

### FR-004: Bloco de spin

**What:** Montar o bloco `Spin` quando houver spin na alocação.

**Why:** Representa os passageiros do carro menor.

**Acceptance Criteria:**
- [ ] Quando há spin, gera o cabeçalho `Spin` seguido dos participantes numerados (`1- ✅ ...` até `6-`).
- [ ] Quando não há spin, o bloco é omitido.

### FR-005: Rodapé fixo

**What:** Anexar o rodapé fixo, idêntico em todos os jogos.

**Why:** Texto padrão de regras da carona.

**Acceptance Criteria:**
- [ ] O rodapé é exatamente o texto fixo do `brainstorm.md` §7 (4 linhas: atraso, drogas, volta, lema), sem alteração.
- [ ] O rodapé é uma constante única (não duplicado).

### FR-006: Endpoint da lista

**What:** Criar `GET /api/jogos/{jogoId}/lista` protegido por `[Authorize]`, retornando `{ texto }`.

**Why:** Permite ao frontend (Spec 90) obter e copiar a lista.

**Acceptance Criteria:**
- [ ] Arquivo `backend/src/CaronaAlvinegra.Api/Controllers/ListaController.cs` existe com `[Authorize]`.
- [ ] `GET /api/jogos/{jogoId}/lista` retorna 200 com `{ texto }`, ou 404 se o jogo não existir.

### FR-007: Testes de formatação

**What:** Testar a geração da lista.

**Why:** Garante o formato exato (é o que o Jones cola no grupo).

**Acceptance Criteria:**
- [ ] Teste valida que o cabeçalho contém `LISTA OFICIAL`, o código e o adversário.
- [ ] Teste valida que o responsável aparece primeiro com `( Resp)`.
- [ ] Teste valida que o rodapé fixo está presente.
- [ ] `dotnet test backend/tests/CaronaAlvinegra.Domain.Tests` termina com exit code 0.

## Non-Functional Requirements

### NFR-001: Texto copiável

**What:** A saída deve ser texto puro com quebras de linha, pronto para colar.

**Acceptance Criteria:**
- [ ] O texto usa quebras de linha (`\n`) e não contém marcação HTML/JSON além do campo `Texto`.

## Constraints

- **.NET 9**, **DDD tático**, **Guid**.
- **`[Authorize]`** no endpoint.
- **Formato exato** do `brainstorm.md` §7 (fonte única do formato).
- **KISS/DRY**: rodapé e emojis como constantes únicas.

## Edge Cases & Error States

- **Jogo sem alocação:** retornar erro claro (400/404) — a lista só é gerada após a alocação (Spec 70).
- **Jogo sem nenhum participante:** retorna erro claro (400).
- **Grupo escalado (não alocado):** não aparece na lista (a lista cobre apenas vans e spin).
- **Responsável não encontrado entre os participantes:** o responsável é omitido do topo e os participantes são listados normalmente (sem `( Resp)`).
- **Grupo referenciado na alocação que não existe mais (excluído após alocar):** é ignorado na montagem (sem exceção); se todos os grupos de uma van foram excluídos, o bloco daquela van é omitido da lista.

## Dependencies

- **Spec 50 (gestao-jogos)** — dados do `Jogo` (cabeçalho).
- **Spec 70 (algoritmo-alocacao)** — `Alocacao`/`VanAlocada`/`SpinAlocado`.
- **Spec 40 (crud-participantes-grupos)** — nomes dos participantes e grupos.
- **Spec 30 (autenticacao)** — `[Authorize]`.

## Out of Scope

- Formatação visual/PDF (a v1 é texto para WhatsApp).
- Envio automático ao grupo do WhatsApp.
- Edição manual da lista antes de copiar (futuro).
