<!-- PIPELINE-CONTRACT: Task completion is determined by "**Status:** [x] done" at the end of each "### Task N" section.
     Checkboxes "- [ ]" under "**Validation:**" are human verification guides — the pipeline ignores them completely.
     To mark a task as done, add "**Status:** [x] done". To mark it as pending, use "**Status:** [ ] pending". -->
# Spec 100: Deploy (Docker + VPS) — Implementation Tasks

## Task Order

Tasks MUST be executed sequentially in numerical order. Each task depends on the completion of all preceding tasks.

Ordem justificada: `.dockerignore` e `.env.example`/`.gitignore` vêm antes dos builds (o contexto e os segredos precisam estar corretos antes de gerar qualquer imagem); `docker-compose.yml` vem depois dos Dockerfiles que ele referencia; a documentação vem depois de tudo que ela descreve; o smoke test é o último passo.

**Pré-condição de todas as validações:** criar os arquivos não exige nenhuma ferramenta. As validações marcadas com "(requer Docker)" exigem Docker Engine ≥ 24 com o plugin `docker compose` v2; as demais usam apenas `grep`, `git`, `npm` e `dotnet`. Ver "Environment Preconditions" no `design.md` para o critério objetivo quando Docker não estiver disponível no ambiente de execução.

## Tasks

### Task 1: Criar `.dockerignore` na raiz

**Objective:** Higiene do contexto de build dos dois containers.
**Requirements Covered:** FR-008
**Design References:** "Componente: `.dockerignore`"
**Actions:**
1. Criar `.dockerignore` na raiz do repositório com exatamente as entradas do skeleton do `design.md` (`.git`, `.gitignore`, `.dscode`, `dscode.cmd`, `dscode.mjs`, `node.exe`, `LICENSE`, `NOTICE`, `management`, `templates`, `node_modules`, `**/node_modules`, `**/bin`, `**/obj`, `**/dist`, `.env`, `**/.env`, `*.md`).
**Validation:**
- [ ] `.dockerignore` existe na raiz e contém `**/node_modules`, `**/bin`, `**/obj`, `**/dist`, `.env`, `*.md` e `node.exe`.
- [ ] Nenhuma entrada remove `backend/` ou `frontend/` do contexto (os Dockerfiles dependem deles).
**Status:** [ ] pending

### Task 2: Criar `.env.example` e ajustar `.gitignore`

**Objective:** Template de segredos versionável e segredos fora do git.
**Requirements Covered:** FR-005, NFR-001, NFR-006
**Design References:** "Componente: `.env` / `.env.example`", "Componente: `.gitignore` (edição na raiz)"
**Actions:**
1. Criar `.env.example` na raiz com exatamente as 4 chaves e os comentários do skeleton do `design.md` (`POSTGRES_PASSWORD`, `JWT_KEY`, `SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD`).
2. Acrescentar ao final do `.gitignore` (existente) o bloco do `design.md`:
   ```
   # --- Deploy (Spec 100) ---
   .env
   !frontend/package-lock.json
   ```
3. Não criar `.env` (ele é criado no VPS durante o deploy).
**Validation:**
- [ ] `.env.example` existe com as 4 chaves e sem valores reais; o valor de `JWT_KEY` tem ≥ 32 caracteres.
- [ ] `git check-ignore -v .env` retorna a linha do `.gitignore` (arquivo ignorado).
- [ ] `git check-ignore -v frontend/package-lock.json` mostra a regra `!frontend/package-lock.json` como última correspondência (a negação venceu; exit code 0).
- [ ] `.env` não está no índice do git (`git ls-files .env` retorna vazio).
**Status:** [ ] pending

### Task 3: Criar `backend/Dockerfile`

**Objective:** Imagem da API .NET 9.
**Requirements Covered:** FR-001
**Design References:** "Componente: `backend/Dockerfile`"
**Actions:**
1. Criar `backend/Dockerfile` com exatamente o conteúdo do skeleton do `design.md` (stage `build` em `sdk:9.0`, `dotnet restore backend/CaronaAlvinegra.sln`, `dotnet publish backend/src/CaronaAlvinegra.Api/CaronaAlvinegra.Api.csproj -c Release -o /app/publish --no-restore`; stage `runtime` em `aspnet:9.0`, `ENV ASPNETCORE_URLS=http://+:8080`, `EXPOSE 8080`, `ENTRYPOINT ["dotnet", "CaronaAlvinegra.Api.dll"]`).
2. Não alterar nenhum `.csproj` nem o `Program.cs` nesta task.
**Validation:**
- [ ] O arquivo contém dois `FROM`, o caminho explícito `src/CaronaAlvinegra.Api/CaronaAlvinegra.Api.csproj`, `ASPNETCORE_URLS=http://+:8080` e o `ENTRYPOINT` com `CaronaAlvinegra.Api.dll`.
- [ ] (requer Docker) `docker build -f backend/Dockerfile .` conclui com exit code 0.
**Status:** [ ] pending

### Task 4: Criar `frontend/nginx.conf` e `frontend/Dockerfile`

**Objective:** Imagem do SPA + proxy reverso na mesma origem.
**Requirements Covered:** FR-002, FR-003
**Design References:** "Componente: `frontend/nginx.conf`", "Componente: `frontend/Dockerfile`"
**Actions:**
1. Garantir `frontend/package-lock.json`: se não existir, executar `npm install` dentro de `frontend/` (Node 20+) para gerá-lo; se existir mas não estiver rastreado, executar `git add frontend/package-lock.json`; confirmar que `git check-ignore -v frontend/package-lock.json` mostra apenas a regra de negação.
2. Criar `frontend/nginx.conf` com exatamente o conteúdo do skeleton do `design.md` (`listen 80`, `server_name _`, `location /api/` com `proxy_pass http://backend:8080;` sem barra final e os 4 `proxy_set_header`, `location /` com `try_files $uri $uri/ /index.html;`).
3. Criar `frontend/Dockerfile` com exatamente o conteúdo do skeleton do `design.md` (stage `build` em `node:20-alpine` copiando `frontend/package.json` + `frontend/package-lock.json`, `npm ci`, `COPY frontend/ ./`, `ARG VITE_API_URL=/`, `ENV VITE_API_URL=$VITE_API_URL`, `npm run build`; stage `nginx:alpine` copiando `dist` e `nginx.conf`). O `COPY --from=build` usa `/app/dist`, salvo se `grep -n "outDir" frontend/vite.config.ts` retornar um diretório customizado — nesse caso usar exatamente esse caminho.
4. Não alterar nenhum arquivo do código React, `package.json` ou `vite.config.ts` nesta task (exceto a geração do lockfile no passo 1).
**Validation:**
- [ ] `frontend/nginx.conf` contém `proxy_pass http://backend:8080;` (sem `/` final) e `try_files $uri $uri/ /index.html;`.
- [ ] `frontend/package-lock.json` existe.
- [ ] `frontend/Dockerfile` contém `ARG VITE_API_URL=/` **antes** de `RUN npm run build`.
- [ ] (requer Docker) `docker build -f frontend/Dockerfile .` conclui com exit code 0.
**Status:** [ ] pending

### Task 5: Criar `docker-compose.yml`

**Objective:** Orquestração dos 3 serviços.
**Requirements Covered:** FR-004, NFR-005
**Design References:** "Componente: `docker-compose.yml`", "Data Structures"
**Actions:**
1. Criar `docker-compose.yml` na raiz com exatamente o conteúdo do skeleton do `design.md`: serviços `db`, `backend`, `frontend`; volume `pgdata`; `restart: unless-stopped` nos 3; healthcheck do `db`; `depends_on` do `backend` com `condition: service_healthy`; `ports` `127.0.0.1:8080:8080` (backend) e `80:80` (frontend); as 8 variáveis de ambiente do backend (incluindo `Jwt__ExpirationMinutes: "480"`).
2. Não incluir a chave `version:`.
**Validation:**
- [ ] (requer Docker) `docker compose config` conclui com exit code 0 com um `.env` temporário criado a partir de `.env.example` (remover o `.env` temporário ao final; nunca commitar).
- [ ] O YAML declara exatamente 3 serviços e o volume `pgdata`; `db` não tem `ports`; backend publica `127.0.0.1:8080:8080`.
**Status:** [ ] pending

### Task 6: Aplicar migrations no startup do backend

**Objective:** Garantir schema no banco antes de servir requisições.
**Requirements Covered:** FR-006
**Design References:** "Componente: Aplicação de migrations", "Spec Conflict Resolution"
**Actions:**
1. Editar `backend/src/CaronaAlvinegra.Api/Program.cs`: inserir o bloco C# exato do `design.md` imediatamente após `var app = builder.Build();` e antes de `app.UseCors();`, de `app.MapControllers()`/`app.Run()` e da chamada de seed do usuário admin.
2. Adicionar apenas os usings necessários (`Microsoft.EntityFrameworkCore` e o namespace de `ApplicationDbContext`).
**Validation:**
- [ ] `grep -n "Database.Migrate" backend/src/CaronaAlvinegra.Api/Program.cs` retorna uma ocorrência.
- [ ] `grep -n "maxAttempts = 10" backend/src/CaronaAlvinegra.Api/Program.cs` retorna uma ocorrência.
- [ ] A linha do `Migrate()` aparece depois de `builder.Build()` e antes de `UseCors`/`MapControllers`/`SeedAdminUserAsync`.
- [ ] `dotnet build backend/CaronaAlvinegra.sln` conclui sem erros.
**Status:** [ ] pending

### Task 7: Criar `deploy.md`

**Objective:** Runbook de deploy, verificação, atualização, rollback e diagnóstico.
**Requirements Covered:** FR-007, NFR-002, NFR-003, NFR-004
**Design References:** "Componente: `deploy.md`", "Testing Strategy"
**Actions:**
1. Criar `deploy.md` na raiz com as 7 seções obrigatórias **nesta ordem**: Pré-requisitos, Primeiro deploy, Verificação, Atualização (redeploy), Rollback, Diagnóstico, Registro do último smoke test.
2. Em "Verificação", incluir os comandos 5–9 da tabela do "Testing Strategy" do `design.md`, com o resultado esperado de cada um.
3. Em "Diagnóstico", incluir causa e correção para: porta ocupada no host (80 e `127.0.0.1:8080`), `JWT_KEY` com menos de 32 caracteres, migration falhando em loop, senha do seed com menos de 6 caracteres.
4. Não incluir nenhum valor real de segredo.
**Validation:**
- [ ] `deploy.md` existe na raiz e contém as 7 seções na ordem exigida.
- [ ] Contém `docker compose up -d --build`, `docker compose down -v` (com aviso de perda de dados) e `curl -fsS http://127.0.0.1:8080/health`.
- [ ] Nenhum valor de `JWT_KEY`/`SEED_ADMIN_PASSWORD`/`POSTGRES_PASSWORD` real aparece no documento.
**Status:** [ ] pending

### Task 8: Executar e registrar o smoke test E2E

**Objective:** Provar que a v1 está acessível em produção (V7).
**Requirements Covered:** FR-009, NFR-001, NFR-002, NFR-003
**Design References:** "Testing Strategy", "Data Flow"
**Actions:**
1. Em um ambiente com Docker (VPS ou máquina de desenvolvimento com Docker), criar `.env` a partir de `.env.example` com valores válidos e executar `docker compose up -d --build`, aguardando até 300 s.
2. Executar os comandos 4–9 da tabela do "Testing Strategy" do `design.md` (ps, health, SPA, fallback, login, lista autenticada) e registrar data + saída na seção "Registro do último smoke test" do `deploy.md`.
3. Executar o ciclo de persistência (comando 10: `docker compose down` + `up -d` → repetir a lista autenticada) e registrar o resultado.
4. Executar `git ls-files | grep -x ".env"` (deve ser vazio) e registrar.
**Validation:**
- [ ] `/health` retorna `Healthy`; `/` e `/login` retornam 200; login retorna 200 com `token`; `/api/jogos` com token retorna 200.
- [ ] Dados persistem após `down` + `up -d`.
- [ ] `.env` não está rastreado pelo git.
- [ ] A seção "Registro do último smoke test" do `deploy.md` está preenchida com data e saídas.
**Status:** [ ] pending
