<!-- PIPELINE-CONTRACT: Task completion is determined by "**Status:** [x] done" at the end of each "### Task N" section.
     Checkboxes "- [ ]" under "**Validation:**" are human verification guides — the pipeline ignores them completely.
     To mark a task as done, add "**Status:** [x] done". To mark it as pending, use "**Status:** [ ] pending". -->
# Spec 90: Frontend React + TypeScript — Implementation Tasks

## Task Order

Tasks MUST be executed sequentially in numerical order. Each task depends on the completion of all preceding tasks.

## Tasks

### Task 1: Criar projeto Vite + React + TS

**Objective:** Inicializar o frontend.
**Requirements Covered:** FR-001
**Design References:** "Componente: api/client.ts" (setup)
**Actions:**
1. Criar `frontend/` com Vite + React + TypeScript (ex.: `npm create vite@latest frontend -- --template react-ts`).
2. Instalar `axios` e `react-router-dom`.
3. Adicionar `.gitignore` (node_modules, dist).
**Validation:** `npm run build` termina com exit code 0.
**Status:** [x] done

### Task 2: Criar tipos e cliente HTTP

**Objective:** Tipos e cliente com token.
**Requirements Covered:** FR-002
**Design References:** "Componente: api/client.ts", "Componente: Tipos"
**Actions:**
1. Criar `frontend/src/types/index.ts` com os tipos dos DTOs.
2. Criar `frontend/src/api/client.ts` com base URL e interceptor de JWT.
**Validation:** `tsc --noEmit` passa.
**Status:** [x] done

### Task 3: Criar AuthContext

**Objective:** Contexto de autenticação.
**Requirements Covered:** FR-003
**Design References:** "Componente: auth/AuthContext.tsx"
**Actions:**
1. Criar `frontend/src/auth/AuthContext.tsx` com login/logout e persistência do token.
**Validation:** `tsc --noEmit` passa.
**Status:** [x] done

### Task 4: Criar LoginPage

**Objective:** Tela de login.
**Requirements Covered:** FR-004
**Design References:** "Componente: Páginas"
**Actions:**
1. Criar `frontend/src/pages/LoginPage.tsx`.
**Validation:** `tsc --noEmit` passa; login redireciona para o shell.
**Status:** [x] done

### Task 5: Criar Layout e rotas protegidas

**Objective:** Shell e navegação.
**Requirements Covered:** FR-005
**Design References:** "Componente: components/Layout.tsx"
**Actions:**
1. Criar `frontend/src/components/Layout.tsx` e configurar rotas protegidas no `App.tsx`.
**Validation:** Rota sem token redireciona para `/login`.
**Status:** [x] done

### Task 6: Criar tela de Grupos

**Objective:** CRUD de grupos.
**Requirements Covered:** FR-006
**Design References:** "Componente: Páginas"
**Actions:**
1. Criar `frontend/src/pages/GruposPage.tsx` (listar/criar/editar/excluir).
**Validation:** `tsc --noEmit` passa.
**Status:** [x] done

### Task 7: Criar tela de Participantes

**Objective:** CRUD de participantes.
**Requirements Covered:** FR-007
**Design References:** "Componente: Páginas"
**Actions:**
1. Criar `frontend/src/pages/ParticipantesPage.tsx` (listar/criar/editar/excluir, com seleção de grupo).
**Validation:** `tsc --noEmit` passa.
**Status:** [x] done

### Task 8: Criar tela de Jogos

**Objective:** CRUD de jogos.
**Requirements Covered:** FR-008
**Design References:** "Componente: Páginas"
**Actions:**
1. Criar `frontend/src/pages/JogosPage.tsx`.
**Validation:** `tsc --noEmit` passa.
**Status:** [x] done

### Task 9: Criar tela de Pagamentos

**Objective:** Marcar/desmarcar pagamentos.
**Requirements Covered:** FR-009
**Design References:** "Componente: Páginas"
**Actions:**
1. Criar `frontend/src/pages/PagamentosPage.tsx`.
2. Carregar `GET /api/participantes` + `GET /api/jogos/{id}/pagamentos`, cruzar por `participanteId` e renderizar uma linha por participante (nome + grupo) com estado pago/não pago (sem registro = não pago).
3. Chamar `POST .../marcar-pago` e `POST .../desmarcar-pago`; ao desmarcar, ler `reembolsoDevido` e exibir o aviso correspondente ("reembolso devido" / "sem reembolso").
**Validation:** `tsc --noEmit` passa; um participante sem registro pode ser marcado como pago e a linha atualiza; o aviso de reembolso reflete `reembolsoDevido`.
**Status:** [x] done

### Task 10: Criar tela de Alocação

**Objective:** Rodar e visualizar alocação.
**Requirements Covered:** FR-010
**Design References:** "Componente: Páginas"
**Actions:**
1. Criar `frontend/src/pages/AlocacaoPage.tsx`.
2. Chamar `POST /api/jogos/{id}/alocar` e `GET /api/jogos/{id}/alocacao`, armazenando a resposta tipada como `Alocacao[]` (array com uma alocação por rota) e renderizando um bloco por item (rota, vans, spin opcional, grupos escalados). Não indexar o array como se fosse um objeto único.
**Validation:** `tsc --noEmit` passa; a tela renderiza os dois blocos de rota quando a API retorna 2 alocações.
**Status:** [x] done

### Task 11: Criar tela de Lista Oficial

**Objective:** Gerar e copiar a lista.
**Requirements Covered:** FR-011
**Design References:** "Componente: Páginas"
**Actions:**
1. Criar `frontend/src/pages/ListaPage.tsx` com botão copiar.
**Validation:** `tsc --noEmit` passa.
**Status:** [x] done

### Task 12: Ajustar responsividade e build final

**Objective:** Layout responsivo e build limpo.
**Requirements Covered:** FR-012
**Design References:** "Componente: components/Layout.tsx"
**Actions:**
1. Ajustar CSS responsivo.
2. Executar `npm run build`.
**Validation:** Build termina com exit code 0.
**Status:** [x] done
