---
name: deploy
status: verified
references: V7
---

# Spec 100: Deploy (Docker + VPS) — Requirements

## Value Delivery

> **V7: Deploy em produção**
>
> "Publica o sistema para uso real do Jones em um VPS Linux, empacotado com Docker (Docker Compose orquestrando backend, frontend e PostgreSQL). Torna a v1 acessível em produção."

Este spec empacota backend, frontend e PostgreSQL em **Docker Compose** e documenta o deploy em **VPS Linux**. É o passo final da v1.

**Resultado verificável:** em um VPS Linux limpo, `git clone` + `cp .env.example .env` (com valores reais) + `docker compose up -d --build` publica a aplicação acessível em `http://<IP-do-VPS>/`, com login funcionando, dados persistindo em volume e a API acessível pelo SPA na mesma origem (sem CORS).

## Functional Requirements

### FR-001: Dockerfile do backend

**What:** Criar `backend/Dockerfile` **multi-stage** (`build` → `runtime`) para a API .NET 9. O **contexto de build é a raiz do repositório** (o arquivo `.dockerignore` da raiz se aplica).

**Why:** Gera imagem enxuta (runtime sem SDK) da API.

**Acceptance Criteria:**

- [ ] `backend/Dockerfile` existe com o stage `build` baseado em `mcr.microsoft.com/dotnet/sdk:9.0` e o stage `runtime` baseado em `mcr.microsoft.com/dotnet/aspnet:9.0`.
- [ ] O stage `build` define `WORKDIR /src`, copia `backend/` para `/src/backend/` (`COPY backend/ ./backend/`) e executa `dotnet restore backend/CaronaAlvinegra.sln`.
- [ ] O stage `build` publica **o caminho explícito do projeto** `backend/src/CaronaAlvinegra.Api/CaronaAlvinegra.Api.csproj` (nunca um nome nu) com `dotnet publish ... -c Release -o /app/publish --no-restore`.
- [ ] O stage `runtime` define `WORKDIR /app`, copia `/app/publish` do stage `build`, define `ENV ASPNETCORE_URLS=http://+:8080`, expõe `8080` e tem `ENTRYPOINT ["dotnet", "CaronaAlvinegra.Api.dll"]`.
- [ ] `docker build -f backend/Dockerfile .` (na raiz) conclui com exit code 0.

### FR-002: Dockerfile do frontend

**What:** Criar `frontend/Dockerfile` multi-stage: build estático (Vite) em `node:20-alpine` e serviço por `nginx:alpine`.

**Why:** Serve o SPA em produção de forma estática.

**Acceptance Criteria:**

- [ ] `frontend/Dockerfile` existe com stage `build` (`node:20-alpine`) e stage `nginx` (`nginx:alpine`).
- [ ] O stage `build` roda `npm ci` usando `frontend/package.json` **e** `frontend/package-lock.json` (ambos copiados antes do `COPY frontend/ ./`).
- [ ] `frontend/package-lock.json` existe e está rastreado pelo git. Se ainda não existir no momento da implementação, é gerado com `npm install` executado dentro de `frontend/` (Node 20+) e commitado (a negação `!frontend/package-lock.json` no `.gitignore` já foi aplicada na Task 2).
- [ ] O stage `build` define `ARG VITE_API_URL=/` e `ENV VITE_API_URL=$VITE_API_URL` **antes** de `RUN npm run build`, para que o bundle gerado chame a API na mesma origem (`/api/...`) e não `http://localhost:5000`.
- [ ] O stage `nginx` copia `/app/dist` para `/usr/share/nginx/html` e copia `frontend/nginx.conf` para `/etc/nginx/conf.d/default.conf`; expõe `80`.
- [ ] Antes de escrever o `COPY --from=build`, executar `grep -n "outDir" frontend/vite.config.ts`: se **não** houver `outDir` (caso esperado, Spec 90), usar `/app/dist`; se houver, usar exatamente o diretório declarado.
- [ ] `docker build -f frontend/Dockerfile .` (na raiz) conclui com exit code 0.

### FR-003: nginx como proxy reverso

**What:** Criar `frontend/nginx.conf` que serve o SPA e faz proxy de `/api/` para o backend.

**Why:** O frontend e a API ficam sob a mesma origem (elimina CORS em produção).

**Acceptance Criteria:**

- [ ] `frontend/nginx.conf` existe e contém um único bloco `server` com `listen 80;` e `server_name _;`.
- [ ] Existe `location /api/` com `proxy_pass http://backend:8080;` (**sem barra final** no `proxy_pass`, para preservar o caminho `/api/...`).
- [ ] O bloco de proxy define `proxy_http_version 1.1`, `proxy_set_header Host $host`, `proxy_set_header X-Real-IP $remote_addr`, `proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for` e `proxy_set_header X-Forwarded-Proto $scheme`.
- [ ] Existe `location /` com `root /usr/share/nginx/html;` e `try_files $uri $uri/ /index.html;` (fallback do SPA).
- [ ] O arquivo **não** faz proxy de `/health` (a verificação de health é feita direto na porta 8080 do backend em loopback; ver NFR-002).

### FR-004: docker-compose.yml

**What:** Criar `docker-compose.yml` na raiz com os serviços `db`, `backend` e `frontend`.

**Why:** Orquestra toda a aplicação com um comando.

**Acceptance Criteria:**

- [ ] `docker-compose.yml` existe na raiz, **sem** a chave `version:` (Compose v2), com exatamente os serviços `db`, `backend` e `frontend` e o volume nomeado `pgdata`.
- [ ] `db` usa `image: postgres:16`, define `POSTGRES_DB: carona_alvinegra`, `POSTGRES_USER: postgres` e `POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?POSTGRES_PASSWORD ausente no .env}`, monta `pgdata:/var/lib/postgresql/data`, **não** publica porta no host e tem `healthcheck` com `test: ["CMD-SHELL", "pg_isready -U postgres -d carona_alvinegra"]`, `interval: 10s`, `timeout: 5s`, `retries: 5`, `start_period: 10s`.
- [ ] `backend` usa `build: { context: ., dockerfile: backend/Dockerfile }`, declara `depends_on: db: { condition: service_healthy }`, publica **apenas em loopback** com `ports: ["127.0.0.1:8080:8080"]` e define `restart: unless-stopped`.
- [ ] `backend` recebe por `environment` as 8 variáveis listadas na tabela de variáveis do `design.md` (`ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpirationMinutes`, `SeedAdmin__Email`, `SeedAdmin__Password`), sendo a connection string `Host=db;Port=5432;Database=carona_alvinegra;Username=postgres;Password=${POSTGRES_PASSWORD:?POSTGRES_PASSWORD ausente no .env}`, `ASPNETCORE_ENVIRONMENT: Production` e `Jwt__ExpirationMinutes: "480"`.
- [ ] `frontend` usa `build: { context: ., dockerfile: frontend/Dockerfile }`, publica `ports: ["80:80"]`, declara `depends_on: [backend]` e define `restart: unless-stopped`.
- [ ] `db` também define `restart: unless-stopped`.
- [ ] `docker compose config` (com um `.env` presente) conclui com exit code 0 e mostra os 3 serviços.

### FR-005: Variáveis de ambiente e segredos

**What:** Usar um arquivo `.env` (gitignored) lido automaticamente pelo Compose no diretório do projeto, com `.env.example` como template versionado.

**Why:** Separa segredos (senha do banco, chave JWT, credenciais do admin) do código.

**Acceptance Criteria:**

- [ ] `.env.example` existe na raiz com **exatamente** as chaves `POSTGRES_PASSWORD`, `JWT_KEY`, `SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD`, cada uma com um comentário acima explicando o formato e uma restrição.
- [ ] `.env.example` contém apenas valores de exemplo (nenhum segredo real) e documenta que a senha do banco deve usar **apenas letras e dígitos** (sem espaços, `;`, `#` ou `$`), porque `;` quebra a connection string do Npgsql, `#` inicia comentário e `$` sofre interpolação do Compose (se `$` for inevitável, escapar como `$$`).
- [ ] `.env.example` documenta que `JWT_KEY` deve ter **no mínimo 32 caracteres** (HS256) e que `SEED_ADMIN_PASSWORD` deve ter **no mínimo 6 caracteres** (política do Identity definida no Spec 30: `RequiredLength = 6`, sem exigência de maiúscula/dígito/símbolo).
- [ ] O `docker-compose.yml` referencia as 4 variáveis do `.env` por interpolação com guarda de erro (`${POSTGRES_PASSWORD:?mensagem}`, `${JWT_KEY:?mensagem}`, `${SEED_ADMIN_EMAIL:?mensagem}`, `${SEED_ADMIN_PASSWORD:?mensagem}`), sem `env_file:` e sem valores default para segredos.
- [ ] `.env.example` é copiável e o valor de exemplo de `JWT_KEY` tem ≥ 32 caracteres.
- [ ] `.env` está listado no `.gitignore` da raiz e **não** é commitado.

### FR-006: Aplicação de migrations no startup

**What:** Aplicar automaticamente as migrations EF Core no startup do backend, em `backend/src/CaronaAlvinegra.Api/Program.cs`, **após** `var app = builder.Build();` e **antes** de `app.UseCors();` / `app.MapControllers()` (e antes do seed do usuário admin do Spec 30).

**Why:** O banco precisa do schema (Specs 20/30/70) antes de servir.

**Acceptance Criteria:**

- [ ] Existe um bloco que cria um escopo (`app.Services.CreateScope()`), resolve `ApplicationDbContext` e executa `db.Database.Migrate()`.
- [ ] A execução é tentada no máximo **10 vezes**, com espera de **3 segundos** entre tentativas, logando um `LogWarning` com o número da tentativa em cada falha recuperável.
- [ ] Se a 10ª tentativa falhar, a exceção **propaga** (o processo encerra com exit code ≠ 0 e `restart: unless-stopped` reinicia o container). Não existe caminho alternativo (ex.: seguir servindo sem schema).
- [ ] O bloco não está dentro de `if (app.Environment.IsDevelopment())` — vale em produção.
- [ ] O bloco aparece **antes** da chamada de seed do usuário admin (`SeedAdminUserAsync`).
- [ ] `dotnet build backend/CaronaAlvinegra.sln` conclui sem erros após a alteração.
- [ ] **Resolução de conflito documentada:** este requisito supersede o edge case do Spec 10 ("a API NÃO deve falhar na inicialização só porque o banco está fora") em todos os ambientes; o Spec 10 não é alterado e a decisão fica registrada em "Spec Conflict Resolution" no `design.md`.

### FR-007: Documentação de deploy

**What:** Criar o arquivo `deploy.md` na raiz do repositório com os passos operacionais de deploy, atualização, rollback e diagnóstico.

**Why:** Torna o deploy reproduzível e claro.

**Acceptance Criteria:**

- [ ] `deploy.md` existe na raiz do repositório (não é uma seção do `README.md`).
- [ ] Contém a seção **Pré-requisitos** listando: VPS Linux com Docker Engine ≥ 24 e o plugin `docker compose` v2, Git, e portas 80/tcp abertas no firewall.
- [ ] Contém a seção **Primeiro deploy** com os comandos literais, nesta ordem: `git clone <repo> && cd carona-alvinegra`, `cp .env.example .env`, edição do `.env` (4 chaves), `docker compose up -d --build`, `docker compose ps`.
- [ ] Contém a seção **Verificação** com os comandos literais `curl -fsS http://127.0.0.1:8080/health` (espera `Healthy`), `curl -fsS -o /dev/null -w "%{http_code}" http://127.0.0.1/` (espera `200`) e o smoke E2E do FR-009.
- [ ] Contém a seção **Atualização (redeploy)** com `git pull`, `docker compose up -d --build` e `docker compose ps`.
- [ ] Contém a seção **Rollback** com `docker compose down` (preserva o volume `pgdata`) e o aviso de que `docker compose down -v` **apaga** os dados.
- [ ] Contém a seção **Diagnóstico** com `docker compose logs backend`, `docker compose logs db` e `docker compose logs frontend`, e as causas prováveis para: porta ocupada no host (80 → `"8081:80"` no `frontend`; `127.0.0.1:8080` → `"127.0.0.1:8081:8080"` no `backend`), `JWT_KEY` com menos de 32 caracteres, senha do seed com menos de 6 caracteres, e migration falhando em loop.
- [ ] Nenhum valor real de segredo aparece no documento.

### FR-008: `.dockerignore` na raiz

**What:** Criar `.dockerignore` na raiz do repositório, usado pelos dois builds (contexto = raiz).

**Why:** Evita enviar ao daemon arquivos irrelevantes/pesados e impede que `node_modules`, `bin/` e `obj/` do host (gerados em Windows) sejam copiados para dentro da imagem Linux, o que quebraria o build.

**Acceptance Criteria:**

- [ ] `.dockerignore` existe na raiz e contém, no mínimo, as entradas: `.git`, `.gitignore`, `.dscode`, `dscode.cmd`, `dscode.mjs`, `node.exe`, `LICENSE`, `NOTICE`, `management`, `templates`, `node_modules`, `**/node_modules`, `**/bin`, `**/obj`, `**/dist`, `.env`, `**/.env`, `*.md`.
- [ ] O build do backend (FR-001) e do frontend (FR-002) conclui com exit code 0 **mesmo com `frontend/node_modules` e `backend/**/obj` presentes no host**.

### FR-009: Smoke test E2E de produção

**What:** Verificar, após o deploy, o fluxo completo SPA → nginx → API → PostgreSQL.

**Why:** Prova que a v1 está realmente acessível em produção (V7), não apenas que os containers subiram.

**Acceptance Criteria:**

- [ ] `curl -fsS http://127.0.0.1:8080/health` retorna HTTP 200 com corpo `Healthy`.
- [ ] `curl -fsS -o /dev/null -w "%{http_code}" http://127.0.0.1/` retorna `200` (SPA servido pelo nginx).
- [ ] `curl -fsS -o /dev/null -w "%{http_code}" http://127.0.0.1/login` retorna `200` (rota real do React Router, servida pelo fallback `index.html` do nginx).
- [ ] `POST http://127.0.0.1/api/auth/login` com `{"email": "<SEED_ADMIN_EMAIL>", "password": "<SEED_ADMIN_PASSWORD>"}` retorna HTTP 200 e um campo `token` não vazio — provando o proxy `/api/` e a conexão com o banco.
- [ ] `GET http://127.0.0.1/api/jogos` com `Authorization: Bearer <token>` retorna HTTP 200 (array, possivelmente vazio).
- [ ] O resultado da verificação (data, saída dos comandos) é registrado em uma seção **Registro do último smoke test** no `deploy.md`.

## Non-Functional Requirements

### NFR-001: Segredos fora do versionamento

**What:** Nenhum segredo real no repositório.

**Acceptance Criteria:**

- [ ] `.env` está no `.gitignore` da raiz; apenas `.env.example` (sem valores reais) é commitado.
- [ ] `git ls-files | grep -x ".env"` não retorna nada.
- [ ] `git grep -n "JWT_KEY=" -- .env` falha (arquivo não rastreado/não existe no índice).

### NFR-002: Disponibilidade após um comando

**What:** O stack fica saudável em até 300 segundos após `docker compose up -d --build` (VPS com 2 vCPU / 2 GB RAM, build incluso).

**Acceptance Criteria:**

- [ ] Em até 300 s, `curl -fsS http://127.0.0.1:8080/health` retorna 200.
- [ ] Em até 300 s, `docker compose ps` mostra os 3 serviços com estado `running` e o `db` como `healthy`.

### NFR-003: Persistência de dados

**What:** Dados sobrevivem ao ciclo de vida dos containers.

**Acceptance Criteria:**

- [ ] Após `docker compose down` seguido de `docker compose up -d`, os dados criados antes (ex.: um participante) continuam presentes via `GET /api/participantes`.
- [ ] O volume é o nomeado `pgdata`; nenhum bind mount de dados do PostgreSQL é usado.

### NFR-004: Tamanho das imagens

**What:** Imagens enxutas (multi-stage efetivo).

**Acceptance Criteria:**

- [ ] `docker image ls` mostra a imagem do backend com **≤ 350 MB**.
- [ ] `docker image ls` mostra a imagem do frontend com **≤ 100 MB**.

### NFR-005: Superfície de rede mínima

**What:** Apenas a porta 80 fica acessível externamente.

**Acceptance Criteria:**

- [ ] `docker compose config` mostra o backend publicado em `127.0.0.1:8080` (não `0.0.0.0:8080`).
- [ ] O serviço `db` não publica nenhuma porta no host.
- [ ] O `frontend` publica apenas `80:80`.

### NFR-006: Build reprodutível a partir de clone limpo

**What:** O build não depende de artefatos gerados na máquina do desenvolvedor.

**Acceptance Criteria:**

- [ ] Em um clone limpo (`git clone` em diretório vazio), `docker compose build` conclui com exit code 0 sem passos manuais além de criar o `.env`.
- [ ] `frontend/package-lock.json` está **rastreado pelo git** (necessário para `npm ci`). O `.gitignore` atual ignora `package-lock.json`; a alteração exigida está especificada no `design.md` ("Componente: `.gitignore`").

## Constraints

- **Docker + Docker Compose** (ADR-005), Compose v2 (sem `version:`).
- **VPS Linux** como alvo (x86_64, Docker Engine ≥ 24).
- **PostgreSQL 16** (imagem `postgres:16`).
- **.NET 9** (imagens `sdk:9.0` / `aspnet:9.0`), **Node 20** (`node:20-alpine`), **nginx** (`nginx:alpine`).
- **KISS**: sem Kubernetes, sem orquestração complexa, sem HTTPS na v1.
- **Sem alteração de código de aplicação**, exceto o bloco de migrations no `Program.cs` (FR-006).

## Edge Cases & Error States

- **Banco não saudável no boot:** o `healthcheck` (`pg_isready -U postgres -d carona_alvinegra`) impede o backend de subir antes do banco (`condition: service_healthy`).
- **Migration falha (schema inválido/permissão):** 10 tentativas de 3 s; depois o container do backend sai com exit ≠ 0 e entra em loop de restart. Diagnóstico: `docker compose logs backend`.
- **`.env` ausente:** `docker compose up` falha imediatamente com a mensagem `POSTGRES_PASSWORD ausente no .env` (graças ao `${VAR:?...}`), sem subir containers parcialmente.
- **`JWT_KEY` com menos de 32 caracteres:** o login retorna HTTP 500. Documentado no `deploy.md` (Diagnóstico) com a correção (regenerar a chave com ≥32 caracteres e `docker compose up -d`).
- **Senha do seed com menos de 6 caracteres:** a política do Identity do Spec 30 exige `RequiredLength = 6`; o seed loga o erro e não cria o usuário, e o login retorna 401. Documentado no `deploy.md` (Diagnóstico).
- **Senha do banco com caracteres problemáticos:** `$` sofre interpolação do Compose (escapar como `$$`); `;` quebra o parsing da connection string do Npgsql; `#` inicia comentário no `.env`; espaços podem ser aparados. O `.env.example` exige apenas letras e dígitos para `POSTGRES_PASSWORD`.
- **Porta ocupada no host:** `docker compose up` falha com `address already in use`. Correção documentada no `deploy.md` (Diagnóstico): para a 80, trocar `ports` do `frontend` para `"8081:80"` (o SPA passa a responder em `http://<ip>:8081/`); para `127.0.0.1:8080`, trocar o `ports` do `backend` para `"127.0.0.1:8081:8080"` (o proxy interno `http://backend:8080` do nginx não muda) e ajustar as URLs de verificação.
- **Primeiros segundos após o boot:** o nginx pode responder 502 em `/api/...` enquanto o backend aplica migrations; repetir a requisição após o `curl /health` responder 200.
- **Reboot do VPS:** com `restart: unless-stopped` nos 3 serviços, o stack volta automaticamente (desde que o daemon Docker esteja habilitado no boot).
- **`frontend/node_modules` ou `backend/**/obj` presentes no host:** ignorados pelo `.dockerignore` (FR-008); sem ele o build falharia por binários de Windows.

## Dependencies

- **Spec 10 (fundacao-backend)** — solução `backend/CaronaAlvinegra.sln`, projetos em `backend/src/`, `ApplicationDbContext` e endpoint `/health`.
- **Spec 20 (modelo-dominio)** — migrations em `backend/src/CaronaAlvinegra.Infrastructure/Migrations/`.
- **Spec 30 (autenticacao)** — chaves `Jwt:Key`/`Jwt:Issuer`/`Jwt:Audience` (env: `Jwt__*`), `SeedAdmin:Email`/`SeedAdmin:Password` (env: `SeedAdmin__*`) e o seed executado no startup (que deve rodar **depois** das migrations).
- **Spec 70 (algoritmo-alocacao)** — migration `AddAlocacao`.
- **Spec 110 (atribuicao-responsavel-van)** — endpoint `PUT /api/jogos/{jogoId}/alocacao/vans/{vanId}` e campos de motorista/responsável na `AlocacaoPage`; não altera schema nem o deploy.
- **Spec 90 (frontend-react)** — `frontend/package.json`, `frontend/package-lock.json`, `npm run build` com saída em `frontend/dist/` e `src/api/client.ts` cujo `baseURL` é `import.meta.env.VITE_API_URL` (default `http://localhost:5000`). **Este spec define `VITE_API_URL=/` no build de produção** (FR-002) para que as chamadas `/api/...` fiquem na mesma origem.
- **`.gitignore` da raiz** — precisa de duas alterações (`.env` e des-ignorar `frontend/package-lock.json`), especificadas no `design.md`.

## Out of Scope

- HTTPS/TLS e domínio (futuro — ex.: Caddy/Traefik).
- CI/CD automatizado (GitHub Actions) — futuro.
- Backup automático do banco — futuro.
- Escalar horizontalmente, múltiplas réplicas ou orquestração (Kubernetes).
- Observabilidade (Prometheus/Grafana) e logs centralizados.
- Alterar qualquer endpoint, contrato de API ou tela do frontend.
