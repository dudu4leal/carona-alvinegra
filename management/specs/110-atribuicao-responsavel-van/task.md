<!-- PIPELINE-CONTRACT: Task completion is determined by "**Status:** [x] done" at the end of each "### Task N" section.
     Checkboxes "- [ ]" under "**Validation:**" are human verification guides — the pipeline ignores them completely.
     To mark a task as done, add "**Status:** [x] done". To mark it as pending, use "**Status:** [ ] pending". -->
# Spec 110: Atribuição de motorista e responsável da van — Implementation Tasks

## Task Order

Tasks MUST be executed sequentially in numerical order. Domain → DTO → service → controller → frontend → validação E2E. Nenhuma migration é necessária.

## Tasks

### Task 1: Adicionar mutator em `VanAlocada`

**Objective:** Permitir atualizar motorista/responsável com encapsulamento.
**Requirements Covered:** FR-001
**Design References:** "Componente: `VanAlocada` (Domain, editar)"
**Actions:**
1. Editar `backend/src/CaronaAlvinegra.Domain/Entities/VanAlocada.cs` adicionando `public void DefinirResponsavel(string motorista, Guid responsavelId)` com o corpo exato do `design.md` (normaliza `motorista` com `Trim`, valida > 100 caracteres com `DomainException`, grava `ResponsavelId`).
2. Garantir `using CaronaAlvinegra.Domain.Exceptions;` (namespace de `DomainException`, definido no Spec 20).
**Validation:**
- [ ] `grep -n "DefinirResponsavel" backend/src/CaronaAlvinegra.Domain/Entities/VanAlocada.cs` retorna 1 ocorrência.
- [ ] O método não altera `GrupoIds` nem `Ocupacao`.
- [ ] `dotnet build backend/CaronaAlvinegra.sln` conclui sem erros.
**Status:** [ ] pending

### Task 2: Adicionar `DefinirResponsavelVanRequest`

**Objective:** Contrato de entrada da API.
**Requirements Covered:** FR-003
**Design References:** "Componente: `DefinirResponsavelVanRequest` (Application, novo)"
**Actions:**
1. Editar `backend/src/CaronaAlvinegra.Application/Dtos/AlocacaoDtos.cs` adicionando `public record DefinirResponsavelVanRequest(string Motorista, Guid ResponsavelId);`.
**Validation:**
- [ ] `grep -n "DefinirResponsavelVanRequest" backend/src/CaronaAlvinegra.Application/Dtos/AlocacaoDtos.cs` retorna 1 ocorrência.
**Status:** [ ] pending

### Task 3: Implementar `DefinirResponsavelVanAsync`

**Objective:** Caso de uso com validação de pertencimento.
**Requirements Covered:** FR-002, NFR-001
**Design References:** "Componente: `AlocacaoService` (Application, editar)"
**Actions:**
1. Editar `backend/src/CaronaAlvinegra.Application/Services/AlocacaoService.cs` adicionando `Task<VanAlocadaResponse?> DefinirResponsavelVanAsync(Guid jogoId, Guid vanId, string motorista, Guid responsavelId)` seguindo os 7 passos do `design.md`.
2. Usar `IAlocacaoRepository.GetByJogoAsync`, `IGrupoRepository.GetAllAsync` (método existente do Spec 40) e `IAlocacaoRepository.SaveChangesAsync`.
3. Criar `backend/tests/CaronaAlvinegra.Domain.Tests/AlocacaoResponsavelTests.cs` cobrindo: sucesso, `null` para jogo sem alocação, `null` para van inexistente, `DomainException` para responsável fora da van e `Guid.Empty` limpando o responsável.
**Validation:**
- [ ] `grep -n "DefinirResponsavelVanAsync" backend/src/CaronaAlvinegra.Application/Services/AlocacaoService.cs` retorna 1 ocorrência.
- [ ] `dotnet test backend/tests/CaronaAlvinegra.Domain.Tests` termina com exit code 0.
**Status:** [ ] pending

### Task 4: Adicionar ação PUT no `AlocacoesController`

**Objective:** Expor a atribuição na API.
**Requirements Covered:** FR-003
**Design References:** "Componente: `AlocacoesController` (Api, editar)"
**Actions:**
1. Editar `backend/src/CaronaAlvinegra.Api/Controllers/AlocacoesController.cs` adicionando a ação `[HttpPut("alocacao/vans/{vanId}")]` com o corpo exato do `design.md`, retornando `NotFound()` quando o service devolver `null` e `Ok(response)` caso contrário.
2. Não alterar as ações `POST .../alocar` e `GET .../alocacao`.
**Validation:**
- [ ] `grep -n "alocacao/vans/{vanId}" backend/src/CaronaAlvinegra.Api/Controllers/AlocacoesController.cs` retorna 1 ocorrência.
- [ ] `dotnet build backend/CaronaAlvinegra.sln` conclui sem erros.
- [ ] Com a API rodando, `PUT` sem token retorna 401.
**Status:** [ ] pending

### Task 5: Adicionar tipo e chamada no cliente HTTP

**Objective:** Contrato tipado no frontend.
**Requirements Covered:** FR-004
**Design References:** "Componente: Frontend — tipos e cliente (editar)"
**Actions:**
1. Adicionar `interface DefinirResponsavelVanRequest { motorista: string; responsavelId: string; }` em `frontend/src/types/index.ts`.
2. Adicionar `definirResponsavelVan(jogoId, vanId, body): Promise<VanAlocada>` em `frontend/src/api/client.ts` fazendo `api.put('/api/jogos/' + jogoId + '/alocacao/vans/' + vanId, body)`.
**Validation:**
- [ ] `cd frontend && npx tsc --noEmit` passa.
**Status:** [ ] pending

### Task 6: Editar `AlocacaoPage`

**Objective:** Jones define motorista/responsável por van.
**Requirements Covered:** FR-005, FR-006
**Design References:** "Componente: Frontend — `AlocacaoPage` (editar)", "Data Flow"
**Actions:**
1. Em `frontend/src/pages/AlocacaoPage.tsx`, carregar `GET /api/grupos` e montar o mapa `grupoId → { grupo, participantes }`.
2. Para cada van, renderizar `input` "Motorista", `select` "Responsável" (opção vazia + participantes dos grupos da van com texto `"{nome} ({grupo.nome})"`) e botão "Salvar".
3. No salvar, chamar `definirResponsavelVan`; `responsavelId` vazio → `'00000000-0000-0000-0000-000000000000'`; atualizar a van no estado com a resposta; exibir `message` em erro.
4. Não alterar a exibição de spin, escalados e pendentes.
**Validation:**
- [ ] `cd frontend && npm run build` termina com exit code 0.
- [ ] A tela mostra os campos por van e o botão "Salvar".
- [ ] Salvar uma van atualiza o nome do responsável sem recarregar a página.
**Status:** [ ] pending

### Task 7: Validar o fluxo completo

**Objective:** Provar que o `( Resp)` chega à lista oficial.
**Requirements Covered:** FR-006, NFR-001, NFR-002
**Design References:** "Testing Strategy"
**Actions:**
1. Com API + banco locais, criar grupos/participantes, marcar todos os participantes de um grupo como pagos no jogo e rodar `POST /api/jogos/{id}/alocar`.
2. Executar os comandos 3–7 da tabela "Testing Strategy" do `design.md` (sucesso, 400, 404, GET alocação, GET lista) e registrar as saídas.
3. Rodar `dotnet ef migrations list --project backend/src/CaronaAlvinegra.Infrastructure --startup-project backend/src/CaronaAlvinegra.Api` e confirmar que nenhuma migration nova aparece.
**Validation:**
- [ ] `PUT` com responsável válido retorna 200 e `GET .../lista` contém `( Resp)` na van editada.
- [ ] `PUT` com responsável de outro grupo retorna 400.
- [ ] `PUT` com `vanId` inexistente retorna 404.
- [ ] Nenhuma migration nova.
**Status:** [ ] pending
