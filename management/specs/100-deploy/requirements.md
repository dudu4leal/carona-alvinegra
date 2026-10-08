# Spec 100: Deploy (Docker + VPS) — Requirements

## Value Delivery

> **V7: Deploy em produção**
>
> "Publica o sistema para uso real do Jones em um VPS Linux, empacotado com Docker (Docker Compose orquestrando backend, frontend e PostgreSQL). Torna a v1 acessível em produção."

Este spec empacota backend, frontend e PostgreSQL em **Docker Compose** e documenta o deploy em **VPS Linux**. É o passo final da v1.

## Functional Requirements

### FR-001: Dockerfile do backend

**What:** Criar `backend/Dockerfile` multi-stage (SDK → runtime) para a API .NET 9.

**Why:** Gera imagem otimizada (runtime enxuto) da API.

**Acceptance Criteria:**
- [ ] `backend/Dockerfile` existe com stage `build` (SDK 9.0) e stage `runtime` (aspnet 9.0).
- [ ] A imagem expõe a porta da API (ex.: 8080) e executa `CaronaAlvinegra.Api.dll`.

### FR-002: Dockerfile do frontend

**What:** Criar `frontend/Dockerfile` que faz o build estático (Vite) e serve via nginx.

**Why:** Serve o SPA em produção de forma estática.

**Acceptance Criteria:**
- [ ] `frontend/Dockerfile` existe com stage `build` (node) e stage `nginx`.
- [ ] O build copia `dist/` para `/usr/share/nginx/html`.

### FR-003: nginx como proxy reverso

**What:** Criar `frontend/nginx.conf` que serve o SPA e faz proxy de `/api` para o backend.

**Why:** O frontend e a API ficam sob a mesma origem (evita CORS em produção).

**Acceptance Criteria:**
- [ ] `frontend/nginx.conf` existe.
- [ ] Requisições `/api/*` são encaminhadas para o serviço `backend`.
- [ ] Demais rotas servem o SPA (fallback para `index.html`).

### FR-004: docker-compose.yml

**What:** Criar `docker-compose.yml` na raiz com os serviços `db`, `backend` e `frontend`.

**Why:** Orquestra toda a aplicação com um comando.

**Acceptance Criteria:**
- [ ] `docker-compose.yml` existe com os 3 serviços.
- [ ] `db` usa `postgres:16` com volume persistente e healthcheck `pg_isready`.
- [ ] `backend` usa `depends_on: db (condition: service_healthy)` e recebe a connection string por variável de ambiente.
- [ ] `frontend` depende do `backend` e expõe a porta 80.

### FR-005: Variáveis de ambiente e segredos

**What:** Usar um arquivo `.env` (gitignored) para segredos, com `.env.example` como template.

**Why:** Separa segredos (senha do banco, JWT key, senha do admin) do código.

**Acceptance Criteria:**
- [ ] `.env.example` existe com as chaves: `POSTGRES_PASSWORD`, `JWT_KEY`, `SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD`.
- [ ] `.env` é referenciado pelo `docker-compose.yml` e **não** é commitado (entrada no `.gitignore`).

### FR-006: Aplicação de migrations no startup

**What:** Garantir que as migrations EF Core são aplicadas ao banco no deploy.

**Why:** O banco precisa do schema (Specs 20/30/70) antes de servir.

**Acceptance Criteria:**
- [ ] O backend aplica as migrations automaticamente no startup (ex.: `db.Database.Migrate()`) OU há um passo de deploy documentado para `dotnet ef database update`.

### FR-007: Documentação de deploy

**What:** Documentar os passos de deploy no VPS.

**Why:** Torna o deploy reproduzível e claro.

**Acceptance Criteria:**
- [ ] Documento (ex.: `deploy.md` ou seção no `README.md`) descreve: clonar, criar `.env`, `docker compose up -d --build`, e verificar o app.

## Non-Functional Requirements

### NFR-001: Segredos fora do versionamento

**What:** Nenhum segredo real no repositório.

**Acceptance Criteria:**
- [ ] `.env` está no `.gitignore`; apenas `.env.example` (sem valores reais) é commitado.

## Constraints

- **Docker + Docker Compose** (ADR-005).
- **VPS Linux** como alvo.
- **PostgreSQL 16** (ou compatível).
- **KISS**: sem Kubernetes/orquestração complexa.

## Edge Cases & Error States

- **Banco não saudável:** `depends_on` com healthcheck impede o backend de subir antes do banco.
- **Segredo ausente:** `docker compose` falha com erro claro se `.env` não existir.
- **Porta ocupada:** documentar como alterar as portas expostas.

## Dependencies

- **Spec 10/20/30/70** — migrations e conexão PostgreSQL.
- **Spec 90 (frontend-react)** — build do SPA.
- **Spec 30 (autenticacao)** — variáveis de JWT e seed.

## Out of Scope

- HTTPS/TLS e domínio (futuro — ex.: Caddy/Traefik).
- CI/CD automatizado (GitHub Actions) — futuro.
- Backup automático do banco — futuro.
