---
name: frontend-react
status: verified
references: V6
---

# Spec 90: Frontend React + TypeScript — Requirements

## Value Delivery

> **V6: Frontend responsivo**
>
> "Interface web responsiva (React + TypeScript) que o Jones usa no celular e no PC: shell com navegação e telas para participantes, grupos, jogos, pagamentos, alocação e a lista oficial. Consome a API REST do backend."

Este spec cria o app **React + TypeScript** (Vite) que consome a API REST do backend, com login, shell de navegação e as telas do back-office. É o spec mais amplo (múltiplas telas), todas seguindo o mesmo padrão (listar + formulário + consumir API).

## Functional Requirements

### FR-001: Setup do projeto

**What:** Criar o app `frontend/` com Vite + React + TypeScript.

**Why:** Base do frontend, com build/dev server rápidos e tipagem.

**Acceptance Criteria:**
- [ ] `frontend/package.json` existe com React, TypeScript e Vite.
- [ ] `frontend/vite.config.ts` e `frontend/tsconfig.json` existem.
- [ ] `npm run build` termina com exit code 0.

### FR-002: Cliente HTTP com token

**What:** Criar um cliente HTTP em `api/client.ts` com base URL configurável e interceptor que injeta `Authorization: Bearer <token>`.

**Why:** Centraliza as chamadas à API e o envio do JWT.

**Acceptance Criteria:**
- [ ] Arquivo `frontend/src/api/client.ts` existe.
- [ ] O cliente injeta o token JWT (do armazenamento) em toda requisição.
- [ ] A base URL é configurável (variável de ambiente).

### FR-003: Autenticação (contexto)

**What:** Criar `AuthContext` com login, logout e persistência do token.

**Why:** Gerencia a sessão do Jones no frontend.

**Acceptance Criteria:**
- [ ] Arquivo `frontend/src/auth/AuthContext.tsx` existe.
- [ ] `login(email, senha)` chama `POST /api/auth/login` e armazena o token.
- [ ] `logout()` remove o token.

### FR-004: Tela de login

**What:** Criar tela de login com formulário de e-mail/senha.

**Why:** Porta de entrada do Jones.

**Acceptance Criteria:**
- [ ] `frontend/src/pages/LoginPage.tsx` existe.
- [ ] Credenciais válidas levam ao shell; inválidas exibem erro.

### FR-005: Shell e navegação

**What:** Criar layout com menu de navegação e rotas protegidas (redireciona para login se não autenticado).

**Why:** Estrutura de navegação entre as telas.

**Acceptance Criteria:**
- [ ] `frontend/src/components/Layout.tsx` existe com links para as telas.
- [ ] Rotas protegidas redirecionam para `/login` sem token.
- [ ] Usa React Router com as rotas das telas; a rota `/` redireciona para `/grupos`.

### FR-006: Tela de Grupos

**What:** Tela de CRUD de grupos (listar, criar, editar, excluir).

**Why:** O Jones gerencia os grupos.

**Acceptance Criteria:**
- [ ] `frontend/src/pages/GruposPage.tsx` existe.
- [ ] Consome `GET/POST/PUT/DELETE /api/grupos`.

### FR-007: Tela de Participantes

**What:** Tela de CRUD de participantes (listar, criar, editar, excluir), com seleção de grupo.

**Why:** O Jones cadastra torcedores.

**Acceptance Criteria:**
- [ ] `frontend/src/pages/ParticipantesPage.tsx` existe.
- [ ] Consome `GET/POST/PUT/DELETE /api/participantes` e lista os grupos para seleção.

### FR-008: Tela de Jogos

**What:** Tela de CRUD de jogos.

**Why:** O Jones cadastra as partidas.

**Acceptance Criteria:**
- [ ] `frontend/src/pages/JogosPage.tsx` existe.
- [ ] Consome `GET/POST/PUT/DELETE /api/jogos`.

### FR-009: Tela de Pagamentos

**What:** Tela para marcar/desmarcar pagamentos por jogo.

**Why:** O Jones registra quem pagou.

**Acceptance Criteria:**
- [ ] `frontend/src/pages/PagamentosPage.tsx` existe.
- [ ] Carrega `GET /api/participantes` **e** `GET /api/jogos/{id}/pagamentos` e exibe uma linha por participante (nome + grupo) — não apenas os pagamentos já existentes. Participante sem registro de pagamento aparece como "não pago" (primeiro pagamento é marcável; `POST .../marcar-pago` cria o registro).
- [ ] Consome os endpoints `POST /api/jogos/{id}/pagamentos/{participanteId}/marcar-pago` e `.../desmarcar-pago`.
- [ ] Ao desmarcar, lê `reembolsoDevido` da resposta e informa o Jones se o reembolso é devido (Spec 60 FR-005); sem isso, a regra de 48h não tem consumidor na v1.

### FR-010: Tela de Alocação

**What:** Tela para rodar a alocação de um jogo e visualizar o resultado (vans, spin, escalados).

**Why:** O Jones aciona o algoritmo.

**Acceptance Criteria:**
- [ ] `frontend/src/pages/AlocacaoPage.tsx` existe.
- [ ] Consome `POST /api/jogos/{id}/alocar` e `GET /api/jogos/{id}/alocacao`, tratando a resposta como **array** `Alocacao[]` (o backend retorna uma alocação por rota — `Quitandinha` e `Bingen`; ver Spec 70).
- [ ] Renderiza cada alocação do array em um bloco próprio, com rota, vans (motorista/ocupação), spin (se houver), grupos escalados e grupos pendentes (não pagos).

### FR-011: Tela de Lista Oficial

**What:** Tela para gerar e copiar a lista oficial.

**Why:** O Jones copia a lista para o WhatsApp.

**Acceptance Criteria:**
- [ ] `frontend/src/pages/ListaPage.tsx` existe.
- [ ] Consome `GET /api/jogos/{id}/lista` e exibe o texto com botão "copiar".

### FR-012: Responsividade

**What:** As telas devem ser usáveis no celular e no PC.

**Why:** O Jones usa no celular (regra de escopo da v1).

**Acceptance Criteria:**
- [ ] O layout usa CSS responsivo (sem quebrar em larguras de celular).

## Non-Functional Requirements

### NFR-001: Tipagem

**What:** TypeScript estrito, sem `any` não justificado.

**Acceptance Criteria:**
- [ ] `tsc --noEmit` passa sem erros.

### NFR-002: Estado simples

**What:** Sem gerenciador de estado global complexo (Redux); usar estado local + contexto de auth.

**Acceptance Criteria:**
- [ ] Não há Redux/MobX/Zustand no projeto (KISS).

## Constraints

- **React + TypeScript + Vite** (ADR-002).
- **KISS**: sem framework de UI pesado; CSS simples (sem Tailwind).
- **API REST** do backend (contratos dos Specs 40–80).
- **JWT** no `Authorization` header.

## Edge Cases & Error States

- **Token expirado/401:** redirecionar para `/login`.
- **Erro de rede/API:** exibir mensagem de erro amigável.
- **Lista vazia:** exibir estado vazio ("nenhum registro").
- **Capacidade de grupo excedida (400):** exibir a mensagem de erro da API.

## Dependencies

- **Spec 30 (autenticacao)** — login/JWT.
- **Specs 40/50/60/70/80** — endpoints REST consumidos.
- **CORS no backend** (Spec 10/30) habilitado para o frontend em dev.

## Out of Scope

- App nativo / PWA offline.
- Portal do torcedor (auto-inscrição).
- Notificações em tempo real.
- Deploy do frontend (Spec 100).
