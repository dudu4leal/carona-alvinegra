# Spec 100: Deploy (Docker + VPS) — Design

## Design Approach

Empacotar a aplicação em três contêineres orquestrados por **Docker Compose**: `db` (PostgreSQL), `backend` (API .NET) e `frontend` (nginx servindo o SPA). O nginx faz proxy de `/api/` para o backend, eliminando CORS em produção. Segredos ficam em `.env` (gitignored). O build de ambos os containers usa a **raiz do repositório como contexto**, filtrado por um `.dockerignore` único.

Todas as decisões abaixo são fechadas: nenhuma escolha é deixada para o momento da implementação.

## Architecture Decisions

- **Multi-stage build** no backend (SDK → aspnet runtime) para imagem enxuta.
- **Multi-stage build** no frontend (Node 20 → nginx alpine), com `VITE_API_URL=/` injetado no build.
- **nginx** no frontend servindo estático + proxy reverso (mesma origem).
- **`depends_on` com `condition: service_healthy`** para garantir o banco antes do backend.
- **Migrations aplicadas no startup** do backend, com 10 tentativas de 3 s (mais simples que passo manual; ver "Spec Conflict Resolution").
- **`127.0.0.1:8080:8080`** para o backend: a API não é exposta à internet, apenas ao loopback do VPS (verificação e diagnóstico via SSH).
- **`restart: unless-stopped`** nos 3 serviços (sobrevivem a reboot do VPS).

## Component / Module Breakdown

### Componente: `backend/Dockerfile`

**Purpose:** Imagem da API.

**File path:** `backend/Dockerfile` (novo). Contexto de build: raiz do repositório.

**Interface (conteúdo exato):**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY backend/ ./backend/
RUN dotnet restore backend/CaronaAlvinegra.sln
RUN dotnet publish backend/src/CaronaAlvinegra.Api/CaronaAlvinegra.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CaronaAlvinegra.Api.dll"]
```

**Internal Logic:** `restore` restaura a solução inteira (incluindo referências entre Domain/Application/Infrastructure/Api); `publish` usa o **caminho explícito** do `.csproj` da API (nunca `dotnet publish CaronaAlvinegra.Api`, que não resolve projeto por nome). O runtime não contém SDK.

**Dependencies:** SDK/runtime .NET 9 (imagens oficiais).

**Error Handling:** falha de restore/publish → build falha com exit ≠ 0 (comportamento padrão do Docker). Nenhum tratamento adicional.

### Componente: `frontend/Dockerfile`

**Purpose:** Imagem do SPA.

**File path:** `frontend/Dockerfile` (novo). Contexto de build: raiz do repositório.

**Prerequisite:** `frontend/package-lock.json` existe e está **rastreado pelo git** (o `.gitignore` da raiz atualmente ignora `package-lock.json`; ver componente `.gitignore`). Sem ele, `npm ci` falha com `The npm ci command can only install with an existing package-lock.json`. Se o arquivo ainda não existir (Spec 90 não implementado), gerá-lo com `npm install` dentro de `frontend/` (Node 20+) **antes** de escrever o Dockerfile.

**Output dir:** o Dockerfile assume o padrão do Vite, `frontend/dist`. Antes de escrever o `COPY --from=build`, verificar `grep -n "outDir" frontend/vite.config.ts`; se houver `build.outDir` customizado, usar exatamente esse caminho.

**Interface (conteúdo exato):**
```dockerfile
FROM node:20-alpine AS build
WORKDIR /app
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
ARG VITE_API_URL=/
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

**Internal Logic:** `npm ci` instala o lockfile exato (inclui devDependencies necessárias ao Vite); `VITE_API_URL=/` faz o `axios` (`baseURL` de `import.meta.env.VITE_API_URL`, Spec 90) chamar `/api/...` na mesma origem do SPA. Se `VITE_API_URL` não fosse definido, o default do código (`http://localhost:5000`) quebraria a produção.

**Dependencies:** Node 20, nginx alpine, `frontend/package.json`, `frontend/package-lock.json`, `frontend/nginx.conf`.

**Error Handling:** falha de `npm ci`/`npm run build` → build falha com exit ≠ 0. Nenhum tratamento adicional.

### Componente: `frontend/nginx.conf`

**Purpose:** Servir SPA + proxy `/api/`.

**File path:** `frontend/nginx.conf` (novo).

**Interface (conteúdo exato):**
```nginx
server {
  listen 80;
  server_name _;

  location /api/ {
    proxy_pass http://backend:8080;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
  }

  location / {
    root /usr/share/nginx/html;
    try_files $uri $uri/ /index.html;
  }
}
```

**Internal Logic:** `proxy_pass` **sem barra final** preserva o caminho (`/api/jogos` → `http://backend:8080/api/jogos`). `location /` com `try_files` serve arquivos estáticos e cai em `index.html` para rotas do React Router (`/login`, `/grupos`, `/jogos`). `/health` **não** é proxiado: a verificação é feita direto em `127.0.0.1:8080`.

**Dependencies:** serviço `backend` na rede do Compose.

**Error Handling:** `try_files` nunca retorna 404 para rotas do SPA (fallback `index.html`). `location /api/` retorna 502 enquanto o backend não estiver pronto (ver Edge Cases).

### Componente: `docker-compose.yml`

**Purpose:** Orquestração.

**File path:** `docker-compose.yml` (novo, na raiz).

**Interface (conteúdo exato):**
```yaml
services:
  db:
    image: postgres:16
    restart: unless-stopped
    environment:
      POSTGRES_DB: carona_alvinegra
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?POSTGRES_PASSWORD ausente no .env}
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d carona_alvinegra"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 10s

  backend:
    build:
      context: .
      dockerfile: backend/Dockerfile
    restart: unless-stopped
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: "Host=db;Port=5432;Database=carona_alvinegra;Username=postgres;Password=${POSTGRES_PASSWORD:?POSTGRES_PASSWORD ausente no .env}"
      Jwt__Key: ${JWT_KEY:?JWT_KEY ausente no .env}
      Jwt__Issuer: CaronaAlvinegra
      Jwt__Audience: CaronaAlvinegra
      Jwt__ExpirationMinutes: "480"
      SeedAdmin__Email: ${SEED_ADMIN_EMAIL:?SEED_ADMIN_EMAIL ausente no .env}
      SeedAdmin__Password: ${SEED_ADMIN_PASSWORD:?SEED_ADMIN_PASSWORD ausente no .env}
    depends_on:
      db:
        condition: service_healthy
    ports:
      - "127.0.0.1:8080:8080"

  frontend:
    build:
      context: .
      dockerfile: frontend/Dockerfile
    restart: unless-stopped
    ports:
      - "80:80"
    depends_on:
      - backend

volumes:
  pgdata:
```

**Internal Logic:** o Compose carrega automaticamente o `.env` do diretório do projeto e interpola as 4 variáveis; `:?mensagem` aborta com mensagem clara se o `.env` estiver incompleto. `ASPNETCORE_ENVIRONMENT: Production` garante que `appsettings.Development.json` não seja carregado. `db` não publica portas no host (só a rede interna, `db:5432`).

**Dependencies:** Docker Compose v2; imagens `postgres:16`; os dois Dockerfiles.

**Error Handling:** `.env` ausente ou chave faltando → `docker compose up` falha antes de criar containers, com a mensagem definida em `:?`. Banco nunca saudável → `backend` não inicia (`condition: service_healthy`).

### Componente: `.env` / `.env.example`

**Purpose:** Segredos.

**File paths:** `.env.example` (novo, versionado) e `.env` (criado no deploy, gitignored).

**Interface (`.env.example`, conteúdo exato):**
```
# Senha do PostgreSQL. Obrigatória.
# Use apenas letras e dígitos (sem espaços, ";" ou "#"):
# ";" quebra a connection string e "#" inicia comentário.
# Se precisar de "$", escape como "$$" (interpolação do Compose).
POSTGRES_PASSWORD=changeme123

# Chave de assinatura do JWT (HS256). Obrigatória.
# Mínimo de 32 caracteres. Gere com: openssl rand -base64 48
JWT_KEY=troque-esta-chave-por-uma-aleatoria-de-32+-caracteres

# Credenciais do usuário Jones (criado no primeiro boot, idempotente).
# Política do Identity (Spec 30): mínimo de 6 caracteres.
SEED_ADMIN_EMAIL=jones@caronaalvinegra.local
SEED_ADMIN_PASSWORD=Trocar123!
```

**Internal Logic:** `.env` real é copiado de `.env.example` e preenchido no VPS. O Compose lê `.env` automaticamente (sem `env_file:`), então o arquivo não é referenciado em lugar nenhum do YAML.

**Data structures:** ver "Data Structures — Variáveis de ambiente".

**Dependencies:** nenhuma.

**Error Handling:** ver "Edge Cases" em `requirements.md` (senha com caracteres problemáticos, `JWT_KEY` curta, senha do seed curta).

### Componente: `.dockerignore`

**Purpose:** Higiene do contexto de build (raiz).

**File path:** `.dockerignore` (novo, na raiz).

**Interface (conteúdo exato):**
```
.git
.gitignore
.dscode
dscode.cmd
dscode.mjs
node.exe
LICENSE
NOTICE
management
templates
node_modules
**/node_modules
**/bin
**/obj
**/dist
.env
**/.env
*.md
```

**Internal Logic:** remove do contexto `.git` (grande), os binários e a licença da ferramenta DsCode (`node.exe` tem dezenas de MB e não pertence à imagem), documentos de gestão e artefatos de build gerados no host (essenciais: `node_modules` do Windows quebraria o `npm ci`/build Linux, e `bin/obj` do host conflitam no `dotnet publish`).

**Dependencies:** nenhuma.

**Error Handling:** N/A.

### Componente: `.gitignore` (edição na raiz)

**Purpose:** Manter segredos fora do git e permitir o lockfile do frontend.

**File path:** `.gitignore` (existente — editar).

**Interface (acrescentar ao final do arquivo, conteúdo exato):**
```
# --- Deploy (Spec 100) ---
.env
!frontend/package-lock.json
```

**Internal Logic:** `.env` passa a ser ignorado. O padrão pré-existente `package-lock.json` (bloco da ferramenta DsCode) ignora lockfiles em qualquer diretório; a negação `!frontend/package-lock.json` reabilita apenas o lockfile do produto, exigido por `npm ci`.

**Dependencies:** nenhuma.

**Error Handling:** N/A.

### Componente: Aplicação de migrations

**Purpose:** Garantir schema no banco antes de aceitar requisições.

**File path:** `backend/src/CaronaAlvinegra.Api/Program.cs` (editar).

**Position:** imediatamente **após** `var app = builder.Build();` e **antes** de `app.UseCors();`, de `app.MapControllers()` / `app.Run()` e **antes** da chamada de seed do usuário admin (Spec 30).

**Usings necessários:** `Microsoft.EntityFrameworkCore` (extension `Migrate`) e o namespace do contexto (`CaronaAlvinegra.Infrastructure.Data`).

**Interface (trecho exato):**
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            app.Logger.LogWarning(ex,
                "Migration attempt {Attempt}/{Max} failed; retrying in 3s.",
                attempt, maxAttempts);
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }
    }
}
```

**Internal Logic:** cobre as migrations `InitialCreate`, `AddIdentity` e `AddAlocacao` (Specs 20/30/70). Tenta até 10 vezes com 3 s de intervalo (tolerância a banco que ficou saudável mas ainda inicializando). Na 10ª falha o filtro `when (attempt < maxAttempts)` não intercepta: a exceção propaga, o processo sai com exit ≠ 0 e o `restart: unless-stopped` reinicia o container — falha explícita, nunca um container "verde" com banco sem schema.

**Dependencies:** `ApplicationDbContext` (Spec 10), migrations (Specs 20/30/70).

**Error Handling:** ver Internal Logic. Cada tentativa recuperável gera `LogWarning` com o número da tentativa, visível em `docker compose logs backend`.

### Componente: `deploy.md`

**Purpose:** Runbook de deploy/atualização/rollback/diagnóstico.

**File path:** `deploy.md` (novo, na raiz).

**Interface (estrutura obrigatória, nesta ordem):**
1. `## Pré-requisitos` — VPS Linux (x86_64), Docker Engine ≥ 24, plugin `docker compose` v2, Git, porta 80/tcp liberada no firewall.
2. `## Primeiro deploy` — `git clone <url-do-repo> carona-alvinegra`, `cd carona-alvinegra`, `cp .env.example .env`, editar as 4 chaves, `docker compose up -d --build`, `docker compose ps`.
3. `## Verificação` — os comandos do Testing Strategy abaixo (health, SPA, fallback, login, lista).
4. `## Atualização (redeploy)` — `git pull`, `docker compose up -d --build`, `docker compose ps`.
5. `## Rollback` — `docker compose down` (dados preservados); aviso em negrito de que `docker compose down -v` apaga o volume `pgdata`.
6. `## Diagnóstico` — `docker compose logs backend|db|frontend`; causa e correção para: porta ocupada no host (80 → `"8081:80"` no `frontend`; `127.0.0.1:8080` → `"127.0.0.1:8081:8080"` no `backend`, sem alterar o proxy interno `http://backend:8080`), `JWT_KEY` curta, migration em loop, senha do seed com menos de 6 caracteres.
7. `## Registro do último smoke test` — data e saída dos 5 comandos de verificação.

**Internal Logic:** documento operacional; nenhum segredo real.

**Dependencies:** nenhuma.

**Error Handling:** N/A.

## Data Structures

### Variáveis de ambiente (`.env` → Compose)

| Variável no `.env` | Obrigatória | Consumida como | Restrição |
|---|---|---|---|
| `POSTGRES_PASSWORD` | sim | `db.POSTGRES_PASSWORD` e `backend.ConnectionStrings__DefaultConnection` | não vazia; apenas letras e dígitos; `$` escapado como `$$` |
| `JWT_KEY` | sim | `backend.Jwt__Key` | ≥ 32 caracteres (HS256) |
| `SEED_ADMIN_EMAIL` | sim | `backend.SeedAdmin__Email` | formato de e-mail |
| `SEED_ADMIN_PASSWORD` | sim | `backend.SeedAdmin__Password` | ≥ 6 caracteres (política do Identity, Spec 30) |

### Variáveis fixas do `backend` (definidas no YAML, não no `.env`)

| Chave | Valor |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Jwt__Issuer` | `CaronaAlvinegra` |
| `Jwt__Audience` | `CaronaAlvinegra` |
| `Jwt__ExpirationMinutes` | `480` |
| `ConnectionStrings__DefaultConnection` | `Host=db;Port=5432;Database=carona_alvinegra;Username=postgres;Password=${POSTGRES_PASSWORD}` |

### Portas

| Serviço | Porta no container | Publicação no host |
|---|---|---|
| `db` | 5432 | nenhuma |
| `backend` | 8080 | `127.0.0.1:8080` |
| `frontend` | 80 | `80` (todas as interfaces) |

### Volumes

| Volume | Montagem | Conteúdo |
|---|---|---|
| `pgdata` | `db:/var/lib/postgresql/data` | dados do PostgreSQL (persistente) |

## Data Flow

### Primeiro deploy

1. Operador executa `cp .env.example .env`, preenche as 4 chaves e roda `docker compose up -d --build`.
2. Compose interpola o `.env`; se alguma chave faltar, aborta com mensagem `... ausente no .env` e nenhum container é criado.
3. Builds paralelos: backend (restore → publish → imagem aspnet) e frontend (`npm ci` → `vite build` com `VITE_API_URL=/` → imagem nginx).
4. `db` sobe; `pg_isready -U postgres -d carona_alvinegra` passa após `start_period` + até 5 tentativas de 10 s.
5. `backend` inicia (somente após `db` saudável), executa o bloco de migrations (até 10 × 3 s), roda o seed do Jones e passa a escutar em `0.0.0.0:8080`.
6. `frontend` inicia e escuta na porta 80.

### Requisição do SPA

1. Navegador acessa `http://<ip-do-vps>/` → nginx serve `/usr/share/nginx/html/index.html`.
2. Rota do React Router (`/login`, `/grupos`) → `try_files` não encontra arquivo → devolve `index.html` (200).
3. App chama `POST /api/auth/login` (mesma origem, `baseURL=/` via `VITE_API_URL=/`).
4. nginx encaminha para `http://backend:8080/api/auth/login` (caminho preservado).
5. API valida via Identity + PostgreSQL (`db:5432`) e retorna o token JWT.
6. Requisições seguintes enviam `Authorization: Bearer <token>`; nginx repassa o header sem alteração (nenhum CORS é necessário — mesma origem).

### Atualização (redeploy)

1. `git pull` no VPS.
2. `docker compose up -d --build` reconstrói as imagens alteradas e recria os containers.
3. `backend` reaplica migrations pendentes no startup (idempotente); o volume `pgdata` não é tocado.

### Rollback

1. `docker compose down` para e remove os containers; o volume `pgdata` permanece.
2. `git checkout <commit-anterior>` + `docker compose up -d --build` volta o código.
3. `docker compose down -v` remove também o volume (perda total de dados — usar apenas em reset).

## File / Module Layout

```
├── docker-compose.yml                 (novo)
├── .dockerignore                      (novo)
├── .env.example                       (novo)
├── .env                               (criado no deploy, gitignored)
├── .gitignore                         (editar: + .env, + !frontend/package-lock.json)
├── deploy.md                          (novo)
├── backend/Dockerfile                 (novo)
├── backend/src/CaronaAlvinegra.Api/Program.cs   (editar: bloco de migrations)
├── frontend/Dockerfile                (novo)
├── frontend/nginx.conf                (novo)
└── frontend/package-lock.json         (do Spec 90; se ausente, gerado com npm install e commitado)
```

Nenhum outro arquivo é criado ou modificado. Nenhum código de domínio, aplicação, controller, DTO ou componente React é alterado.

## Environment Preconditions

As validações de container exigem Docker Engine ≥ 24 com o plugin `docker compose` v2.

- **Com Docker disponível:** executar os comandos da tabela de Testing Strategy na íntegra; são a evidência de conclusão das Tasks 3, 4, 5 e 8.
- **Sem Docker no ambiente de execução (ex.: máquina de desenvolvimento Windows sem Docker Desktop):** a conclusão das Tasks 1–7 é atestada por (a) comparação byte-a-byte do arquivo criado com o skeleton correspondente neste `design.md` e (b) as validações textuais de cada task (`grep`/`git check-ignore`). A Task 8 permanece `[ ] pending` até ser executada em um ambiente com Docker (VPS), pois o smoke E2E não pode ser simulado.
- **Nenhuma validação pode ser marcada como concluída por suposição.** Se um comando não foi executado, a task correspondente permanece pendente.

## Spec Conflict Resolution

1. **Spec 10 — edge case "a API NÃO deve falhar na inicialização só porque o banco está fora".**
   `Migrate()` abre conexão no startup, portanto o comportamento tolerante não se aplica mais: em **todos** os ambientes, sem banco o processo falha após 10 tentativas (≈30 s) com erro claro. Decisão intencional: servir sem schema produziria erro 500 em todas as rotas de dados. No deploy, `depends_on: service_healthy` garante o banco antes do backend. O texto do Spec 10 não é alterado (specs concluídos são imutáveis); a supersessão fica registrada aqui para rastreabilidade.

2. **Spec 90 — `baseURL` default `http://localhost:5000`.**
   Resolução: o build de produção define `VITE_API_URL=/` (FR-002). Nenhuma alteração no código do Spec 90 é necessária. `VITE_API_URL=` (vazio) **não** é usado porque um fallback por `||` no código cairia no default `localhost:5000`; `/` funciona com `??` e com `||`.

3. **`.gitignore` — `package-lock.json` ignorado (bloco da ferramenta DsCode).**
   Resolução: negação pontual `!frontend/package-lock.json`; o lockfile da ferramenta continua ignorado.

## Testing Strategy

Validação manual e determinística (sem testes automatizados de container na v1). Executar na raiz, com `.env` preenchido.

| # | Comando | Resultado esperado |
|---|---|---|
| 1 | `docker compose config` | exit 0; exibe os 3 serviços e `127.0.0.1:8080` no backend |
| 2 | `docker compose build` | exit 0 (também válido a partir de clone limpo) |
| 3 | `docker compose up -d` | exit 0 |
| 4 | `docker compose ps` | 3 serviços `running`; `db` `healthy` |
| 5 | `curl -fsS http://127.0.0.1:8080/health` | `Healthy` (HTTP 200) |
| 6 | `curl -fsS -o /dev/null -w "%{http_code}" http://127.0.0.1/` | `200` |
| 7 | `curl -fsS -o /dev/null -w "%{http_code}" http://127.0.0.1/login` | `200` |
| 8 | `curl -fsS -X POST http://127.0.0.1/api/auth/login -H "Content-Type: application/json" -d '{"email":"<SEED_ADMIN_EMAIL>","password":"<SEED_ADMIN_PASSWORD>"}'` | HTTP 200 com `token` não vazio |
| 9 | `curl -fsS http://127.0.0.1/api/jogos -H "Authorization: Bearer <token>"` | HTTP 200 (array JSON) |
| 10 | `docker compose down && docker compose up -d` → repetir #9 | dados preservados |
| 11 | `docker image ls` | backend ≤ 350 MB; frontend ≤ 100 MB |
| 12 | `git ls-files \| grep -x ".env"` | saída vazia |

Os comandos 5–9 formam o smoke E2E (FR-009) e devem ser registrados em `deploy.md`.

## Migration / Rollback

- **Atualização:** `git pull` + `docker compose up -d --build`; migrations pendentes aplicadas no startup.
- **Rollback de código:** `git checkout <commit>` + `docker compose up -d --build`.
- **Parada:** `docker compose down` (mantém o volume `pgdata`).
- **Reset total:** `docker compose down -v` (apaga `pgdata`; o próximo `up` recria o schema e re-semeia o Jones).
- **Sem migrations destrutivas** introduzidas por este spec (nenhuma alteração de schema).
