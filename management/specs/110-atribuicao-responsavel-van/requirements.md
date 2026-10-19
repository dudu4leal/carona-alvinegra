---
name: atribuicao-responsavel-van
status: verified
references: V3
---

# Spec 110: Atribuição de motorista e responsável da van — Requirements

## Value Delivery

> **V3: Lista oficial para WhatsApp** (complemento — a lista precisa do marcador `( Resp)`)
>
> "Gera a lista oficial do jogo no formato exato para colar no grupo do WhatsApp: cabeçalho (competição, data, horário de saída, ponto de embarque, confronto), blocos de van (com responsável marcado como \"Resp\") e bloco spin, além do rodapé fixo (regras de atraso, drogas, horário de volta e o lema da carona)."

O Spec 70 cria cada `VanAlocada` com `Motorista = ""` e `ResponsavelId = Guid.Empty`. Sem uma forma de o Jones definir quem é o responsável, o marcador `( Resp)` do Spec 80 nunca aparece. Este spec adiciona exatamente essa capacidade: **endpoint REST + campo na tela de alocação**, sem alterar o algoritmo de empacotamento.

**Resultado verificável:** após rodar a alocação, o Jones informa motorista e responsável de cada van na tela de alocação; `GET /api/jogos/{id}/lista` passa a marcar o responsável com `( Resp)` na posição `1-`.

## Functional Requirements

### FR-001: Mutator na entidade `VanAlocada`

**What:** Adicionar em `backend/src/CaronaAlvinegra.Domain/Entities/VanAlocada.cs` o método `DefinirResponsavel(string motorista, Guid responsavelId)`.

**Why:** Permitir atualizar a tripulação da van preservando o encapsulamento do domínio.

**Acceptance Criteria:**
- [ ] O método `DefinirResponsavel(string motorista, Guid responsavelId)` existe, é público e retorna `void`.
- [ ] `Motorista` recebe `motorista.Trim()`; se `motorista` for `null`, recebe `string.Empty`.
- [ ] `Motorista` com mais de 100 caracteres lança `DomainException("Motorista deve ter no máximo 100 caracteres.")`.
- [ ] `ResponsavelId` recebe `responsavelId` (inclusive `Guid.Empty`, que significa "sem responsável").
- [ ] O método **não** valida se o responsável pertence à van (essa regra depende dos grupos e fica na Application — FR-002).
- [ ] O método **não** altera `GrupoIds` nem `Ocupacao`.

### FR-002: Caso de uso no `AlocacaoService`

**What:** Adicionar `DefinirResponsavelVanAsync(Guid jogoId, Guid vanId, string motorista, Guid responsavelId)` em `backend/src/CaronaAlvinegra.Application/Services/AlocacaoService.cs`.

**Why:** Concentra a validação de integridade (responsável tem de estar na van) antes de persistir.

**Acceptance Criteria:**
- [ ] Carrega as alocações via `IAlocacaoRepository.GetByJogoAsync(jogoId)`; se o jogo não tiver alocação, retorna `null` (o controller converte em 404).
- [ ] Localiza a van por `vanId` entre todas as alocações do jogo; se não existir, retorna `null` (404).
- [ ] Se `responsavelId != Guid.Empty`: carrega os grupos da van (`van.GrupoIds`) com seus participantes via `IGrupoRepository`; o `responsavelId` deve ser o `Id` de um participante de algum desses grupos — caso contrário lança `DomainException("Responsável deve ser participante de um grupo da van.")` (400).
- [ ] Chama `van.DefinirResponsavel(motorista, responsavelId)`, persiste com `IAlocacaoRepository.SaveChangesAsync()` e retorna o `VanAlocadaResponse` atualizado.
- [ ] Não altera outras vans nem outras alocações.
- [ ] Assinatura: `Task<VanAlocadaResponse?> DefinirResponsavelVanAsync(Guid jogoId, Guid vanId, string motorista, Guid responsavelId)` (não existe tipo de exceção de 404 no projeto; o padrão é retorno nulo, como no Spec 40).
- [ ] Testes cobrem: sucesso com responsável válido; `null` para jogo sem alocação; `null` para van inexistente; `DomainException` para responsável de fora da van; limpar responsável com `Guid.Empty`.

### FR-003: Endpoint REST

**What:** Adicionar a ação `PUT /api/jogos/{jogoId}/alocacao/vans/{vanId}` em `backend/src/CaronaAlvinegra.Api/Controllers/AlocacoesController.cs` (controller existente do Spec 70, protegido por `[Authorize]`).

**Why:** Expõe a atribuição ao frontend.

**Acceptance Criteria:**
- [ ] A ação usa `[HttpPut("alocacao/vans/{vanId}")]` e recebe `DefinirResponsavelVanRequest(string Motorista, Guid ResponsavelId)` no corpo.
- [ ] Retorna `200 OK` com `VanAlocadaResponse` em caso de sucesso.
- [ ] Retorna `404 Not Found` quando o service retorna `null` (jogo sem alocação ou van inexistente).
- [ ] Retorna `400 Bad Request` com `{ "message": ... }` quando o responsável não pertence à van (via middleware de `DomainException` existente).
- [ ] A rota exige token (`[Authorize]` herdado do controller).
- [ ] Nenhum outro endpoint é alterado.

### FR-004: Tipo e chamada no frontend

**What:** Adicionar `DefinirResponsavelVanRequest` em `frontend/src/types/index.ts` e a chamada `api.put` correspondente em `frontend/src/api/client.ts`.

**Why:** Contrato tipado para a tela.

**Acceptance Criteria:**
- [ ] `interface DefinirResponsavelVanRequest { motorista: string; responsavelId: string; }` existe em `frontend/src/types/index.ts`.
- [ ] `client.ts` expõe `definirResponsavelVan(jogoId: string, vanId: string, body: DefinirResponsavelVanRequest): Promise<VanAlocada>` que faz `PUT /api/jogos/{jogoId}/alocacao/vans/{vanId}`.
- [ ] `tsc --noEmit` passa.

### FR-005: Edição na `AlocacaoPage`

**What:** Em `frontend/src/pages/AlocacaoPage.tsx`, permitir que o Jones informe motorista e responsável de cada van.

**Why:** É a única forma de preencher o `( Resp)` da lista oficial na v1.

**Acceptance Criteria:**
- [ ] A página carrega `GET /api/grupos` (com participantes) para resolver `grupoIds` → participantes da van.
- [ ] Cada van renderiza um `input` "Motorista" (texto, opcional) e um `select` "Responsável" com os participantes dos grupos daquela van, mais a opção vazia "— sem responsável —".
- [ ] Um botão "Salvar" por van chama `definirResponsavelVan(jogoId, van.id, { motorista, responsavelId })`; `responsavelId` vazio é enviado como `Guid.Empty` (`"00000000-0000-0000-0000-000000000000"`).
- [ ] Em sucesso, o estado local da van é atualizado com a resposta (sem recarregar a página).
- [ ] Em erro 400/404, a mensagem da API é exibida na tela.
- [ ] A seleção de responsável mostra o nome do participante e o nome do grupo de origem.
- [ ] `tsc --noEmit` e `npm run build` passam.

### FR-006: Persistência e leitura

**What:** Os valores definidos devem sobreviver a recarregamentos e ser usados pela lista oficial.

**Why:** Fecha o fluxo V3.

**Acceptance Criteria:**
- [ ] Após `PUT`, um `GET /api/jogos/{jogoId}/alocacao` retorna a van com `motorista` e `responsavelId` persistidos.
- [ ] `GET /api/jogos/{jogoId}/lista` marca o participante `responsavelId` com `( Resp)` na posição `1-` (comportamento do Spec 80, sem alteração neste spec).
- [ ] Reexecutar `POST /api/jogos/{jogoId}/alocar` zera `motorista`/`responsavelId` de todas as vans (comportamento do Spec 70, documentado).

## Non-Functional Requirements

### NFR-001: Integridade do responsável

**What:** Nunca persistir um responsável que não esteja na van.

**Acceptance Criteria:**
- [ ] A validação de pertencimento acontece no servidor (FR-002), não apenas no `select` da UI.
- [ ] Requisição forjada com `responsavelId` de outro grupo retorna 400 e não altera o banco.

### NFR-002: Nenhuma migration nova

**What:** `VanAlocada` já possui `Motorista` e `ResponsavelId` (Spec 70).

**Acceptance Criteria:**
- [ ] Nenhuma migration é criada; `dotnet ef migrations list` não muda em relação ao Spec 70.

## Constraints

- **.NET 9 / React + TypeScript** (mesma stack).
- **Sem alteração no algoritmo** `Alocador` nem na regra de grupos confirmados (Spec 70).
- **Reutilizar** `VanAlocadaResponse` e o middleware de `DomainException` (Spec 40).
- **KISS**: uma única ação PUT por van; sem edição de grupos dentro da van.

## Edge Cases & Error States

- **Jogo sem alocação:** 404.
- **`vanId` inexistente ou de outro jogo:** 404.
- **`responsavelId` de participante fora da van:** 400 com mensagem clara.
- **`responsavelId = Guid.Empty`:** limpa o responsável; a lista deixa de marcar `( Resp)` para aquela van.
- **`motorista` vazio:** permitido (van sem motorista definido).
- **`motorista` com mais de 100 caracteres:** 400 (`DomainException`).
- **Reexecutar a alocação:** valores de motorista/responsável são perdidos (nova alocação); o `deploy.md` não interfere.

## Dependencies

- **Spec 70 (algoritmo-alocacao)** — `Alocacao`, `VanAlocada`, `AlocacoesController`, `AlocacaoService`, `VanAlocadaResponse`, `IAlocacaoRepository`.
- **Spec 90 (frontend-react)** — `AlocacaoPage`, `types/index.ts`, `api/client.ts`.
- **Spec 40 (crud-participantes-grupos)** — `IGrupoRepository`, participantes e middleware de `DomainException`.
- **Spec 30 (autenticacao)** — `[Authorize]`.
- **Spec 80 (lista-oficial)** — consumidor do `ResponsavelId`.

## Out of Scope

- Reordenar participantes dentro da van.
- Mover grupos entre vans (Spec 70 out of scope).
- Escolher motorista a partir de cadastro (motorista é texto livre, terceirizado).
- Definir responsável para o spin (o spin não tem responsável no formato oficial do Spec 80).
