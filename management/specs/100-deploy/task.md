<!-- PIPELINE-CONTRACT: Task completion is determined by "**Status:** [x] done" at the end of each "### Task N" section.
     Checkboxes "- [ ]" under "**Validation:**" are human verification guides — the pipeline ignores them completely.
     To mark a task as done, add "**Status:** [x] done". To mark it as pending, use "**Status:** [ ] pending". -->
# Spec 100: Deploy (Docker + VPS) — Implementation Tasks

## Task Order

Tasks MUST be executed sequentially in numerical order. Each task depends on the completion of all preceding tasks.

## Tasks

### Task 1: Criar Dockerfile do backend

**Objective:** Imagem da API.
**Requirements Covered:** FR-001
**Design References:** "Componente: backend/Dockerfile"
**Actions:**
1. Criar `backend/Dockerfile` (multi-stage SDK→runtime).
**Validation:** `docker build -f backend/Dockerfile .` compila (ou validação manual no VPS).
**Status:** [ ] pending

### Task 2: Criar Dockerfile do frontend e nginx.conf

**Objective:** Imagem do SPA + proxy.
**Requirements Covered:** FR-002, FR-003
**Design References:** "Componente: frontend/Dockerfile", "frontend/nginx.conf"
**Actions:**
1. Criar `frontend/Dockerfile`.
2. Criar `frontend/nginx.conf` (proxy `/api` → backend).
**Validation:** `docker build -f frontend/Dockerfile .` compila.
**Status:** [ ] pending

### Task 3: Criar docker-compose.yml

**Objective:** Orquestração dos 3 serviços.
**Requirements Covered:** FR-004
**Design References:** "Componente: docker-compose.yml"
**Actions:**
1. Criar `docker-compose.yml` com `db`, `backend` e `frontend` (healthcheck e depends_on).
**Validation:** `docker compose config` valida o arquivo.
**Status:** [ ] pending

### Task 4: Criar .env.example e ajustar .gitignore

**Objective:** Template de segredos.
**Requirements Covered:** FR-005
**Design References:** "Componente: .env / .env.example"
**Actions:**
1. Criar `.env.example` com as chaves de segredo.
2. Adicionar `.env` ao `.gitignore`.
**Validation:** `.env.example` existe e `.env` está gitignored.
**Status:** [ ] pending

### Task 5: Aplicar migrations no startup

**Objective:** Garantir schema no banco.
**Requirements Covered:** FR-006
**Design References:** "Componente: Aplicação de migrations"
**Actions:**
1. Adicionar `db.Database.Migrate()` no startup do backend (ou documentar passo manual).
**Validation:** `dotnet build` compila; migrations aplicam ao subir.
**Status:** [ ] pending

### Task 6: Documentar deploy

**Objective:** Passos de deploy no VPS.
**Requirements Covered:** FR-007
**Design References:** "Documentação"
**Actions:**
1. Criar `deploy.md` (ou seção no README) com: clonar, criar `.env`, `docker compose up -d --build`, verificar.
**Validation:** Documento existe e descreve o fluxo.
**Status:** [ ] pending
