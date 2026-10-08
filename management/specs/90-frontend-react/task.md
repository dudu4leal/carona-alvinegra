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
**Status:** [ ] pending

### Task 2: Criar tipos e cliente HTTP

**Objective:** Tipos e cliente com token.
**Requirements Covered:** FR-002
**Design References:** "Componente: api/client.ts", "Componente: Tipos"
**Actions:**
1. Criar `frontend/src/types/index.ts` com os tipos dos DTOs.
2. Criar `frontend/src/api/client.ts` com base URL e interceptor de JWT.
**Validation:** `tsc --noEmit` passa.
**Status:** [ ] pending

### Task 3: Criar AuthContext

**Objective:** Contexto de autenticação.
**Requirements Covered:** FR-003
**Design References:** "Componente: auth/AuthContext.tsx"
**Actions:**
1. Criar `frontend/src/auth/AuthContext.tsx` com login/logout e persistência do token.
**Validation:** `tsc --noEmit` passa.
**Status:** [ ] pending

### Task 4: Criar LoginPage

**Objective:** Tela de login.
**Requirements Covered:** FR-004
**Design References:** "Componente: Páginas"
**Actions:**
1. Criar `frontend/src/pages/LoginPage.tsx`.
**Validation:** `tsc --noEmit` passa; login redireciona para o shell.
**Status:** [ ] pending

### Task 5: Criar Layout e rotas protegidas

**Objective:** Shell e navegação.
**Requirements Covered:** FR-005
**Design References:** "Componente: components/Layout.tsx"
**Actions:**
1. Criar `frontend/src/components/Layout.tsx` e configurar rotas protegidas no `App.tsx`.
**Validation:** Rota sem token redireciona para `/login`.
**Status:** [ ] pending

### Task 6: Criar tela de Grupos

**Objective:** CRUD de grupos.
**Requirements Covered:** FR-006
**Design References:** "Componente: Páginas"
**Actions:**
1. Criar `frontend/src/pages/GruposPage.tsx` (listar/criar/editar/excluir).
**Validation:** `tsc --noEmit` passa.
**Status:** [ ] pending

### Task 7: Criar tela de Participantes

**Objective:** CRUD de participantes.
**Requirements Covered:** FR-007
**Design References:** "Componente: Páginas"
**Actions:**
1. Criar `frontend/src/pages/ParticipantesPage.tsx` (listar/criar/editar/excluir, com seleção de grupo).
**Validation:** `tsc --noEmit` passa.
**Status:** [ ] pending

### Task 8: Criar tela de Jogos

**Objective:** CRUD de jogos.
**Requirements Covered:** FR-008
**Design References:** "Componente: Páginas"
**Actions:**
1. Criar `frontend/src/pages/JogosPage.tsx`.
**Validation:** `tsc --noEmit` passa.
**Status:** [ ] pending

### Task 9: Criar tela de Pagamentos

**Objective:** Marcar/desmarcar pagamentos.
**Requirements Covered:** FR-009
**Design References:** "Componente: Páginas"
**Actions:**
1. Criar `frontend/src/pages/PagamentosPage.tsx`.
**Validation:** `tsc --noEmit` passa.
**Status:** [ ] pending

### Task 10: Criar tela de Alocação

**Objective:** Rodar e visualizar alocação.
**Requirements Covered:** FR-010
**Design References:** "Componente: Páginas"
**Actions:**
1. Criar `frontend/src/pages/AlocacaoPage.tsx`.
**Validation:** `tsc --noEmit` passa.
**Status:** [ ] pending

### Task 11: Criar tela de Lista Oficial

**Objective:** Gerar e copiar a lista.
**Requirements Covered:** FR-011
**Design References:** "Componente: Páginas"
**Actions:**
1. Criar `frontend/src/pages/ListaPage.tsx` com botão copiar.
**Validation:** `tsc --noEmit` passa.
**Status:** [ ] pending

### Task 12: Ajustar responsividade e build final

**Objective:** Layout responsivo e build limpo.
**Requirements Covered:** FR-012
**Design References:** "Componente: components/Layout.tsx"
**Actions:**
1. Ajustar CSS responsivo.
2. Executar `tsc --noEmit` e `npm run build`.
**Validation:** Build termina com exit code 0.
**Status:** [ ] pending
