# Spec 100: Deploy (Docker + VPS) — Design

## Design Approach

Empacotar a aplicação em três contêineres orquestrados por **Docker Compose**: `db` (PostgreSQL), `backend` (API .NET) e `frontend` (nginx servindo o SPA). O nginx faz proxy de `/api` para o backend, eliminando CORS em produção. Segredos ficam em `.env` (gitignored).

## Architecture Decisions

- **Multi-stage build** no backend (SDK→runtime) para imagem enxuta.
- **nginx** no frontend servindo estático + proxy reverso (mesma origem).
- **`depends_on` com `condition: service_healthy`** para garantir o banco antes do backend.
- **Migrations aplicadas no startup** do backend (mais simples; evita passo manual).

## Component / Module Breakdown

### Componente: `backend/Dockerfile`

**Purpose:** Imagem da API.

**Interface (esqueleto):**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY backend/ ./
RUN dotnet restore
RUN dotnet publish CaronaAlvinegra.Api -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "CaronaAlvinegra.Api.dll"]
```

**Internal Logic:** Publica a API e a executa no runtime.

**Dependencies:** SDK/runtime .NET 9.

**Error Handling:** N/A.

### Componente: `frontend/Dockerfile`

**Purpose:** Imagem do SPA.

**Interface (esqueleto):**
```dockerfile
FROM node:20-alpine AS build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

**Internal Logic:** Build do Vite e serve via nginx.

**Dependencies:** Node 20, nginx.

**Error Handling:** N/A.

### Componente: `frontend/nginx.conf`

**Purpose:** Servir SPA + proxy `/api`.

**Interface (esqueleto):**
```nginx
server {
  listen 80;
  location /api/ {
    proxy_pass http://backend:8080;
    proxy_set_header Host $host;
  }
  location / {
    root /usr/share/nginx/html;
    try_files $uri /index.html;
  }
}
```

**Internal Logic:** Encaminha `/api/*` ao backend; demais rotas caem no SPA.

**Dependencies:** serviço `backend`.

**Error Handling:** N/A.

### Componente: `docker-compose.yml`

**Purpose:** Orquestração.

**Interface (esqueleto):**
```yaml
services:
  db:
    image: postgres:16
    environment:
      POSTGRES_DB: carona_alvinegra
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes: [pgdata:/var/lib/postgresql/data]
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  backend:
    build: { context: ., dockerfile: backend/Dockerfile }
    environment:
      ConnectionStrings__DefaultConnection: "Host=db;Port=5432;Database=carona_alvinegra;Username=postgres;Password=${POSTGRES_PASSWORD}"
      Jwt__Key: ${JWT_KEY}
      Jwt__Issuer: CaronaAlvinegra
      Jwt__Audience: CaronaAlvinegra
      SeedAdmin__Email: ${SEED_ADMIN_EMAIL}
      SeedAdmin__Password: ${SEED_ADMIN_PASSWORD}
    depends_on:
      db: { condition: service_healthy }
    ports: ["8080:8080"]

  frontend:
    build: { context: ., dockerfile: frontend/Dockerfile }
    ports: ["80:80"]
    depends_on: [backend]

volumes:
  pgdata:
```

**Internal Logic:** Sobe os 3 serviços com healthcheck e segredos via `.env`.

**Dependencies:** Docker Compose.

**Error Handling:** Falha se `.env` ausente.

### Componente: `.env` / `.env.example`

**Purpose:** Segredos.

**Interface (`.env.example`):**
```
POSTGRES_PASSWORD=changeme
JWT_KEY=super-secret-key-com-32+-caracteres
SEED_ADMIN_EMAIL=jones@caronaalvinegra.local
SEED_ADMIN_PASSWORD=Trocar123!
```

**Internal Logic:** `.env` real com valores de produção (gitignored).

**Dependencies:** Nenhuma.

**Error Handling:** N/A.

### Componente: Aplicação de migrations

**Purpose:** Garantir schema no banco.

**Interface:** Trecho no `Program.cs` (startup) ou passo documentado.

**Internal Logic:** Resolve o escopo do `ApplicationDbContext` e chama `db.Database.Migrate()` antes de servir.

**Dependencies:** `ApplicationDbContext`.

**Error Handling:** Logar erro e não derrubar o app (ou falhar claro).

## Data Flow

1. `docker compose up -d --build` no VPS.
2. `db` sobe e passa no healthcheck.
3. `backend` sobe, aplica migrations e serve a API na porta 8080.
4. `frontend` (nginx) serve o SPA na porta 80 e faz proxy `/api` → backend.

## Data Structures

- Volume `pgdata` para persistência do PostgreSQL.

## File / Module Layout

```
├── docker-compose.yml          (novo)
├── .env.example                (novo)
├── .env                        (local, gitignored)
├── backend/Dockerfile          (novo)
├── frontend/Dockerfile         (novo)
└── frontend/nginx.conf         (novo)
```

## Testing Strategy

- **Validação local:** `docker compose up -d --build` e verificar `GET /health`, login e acesso ao SPA.
- **Sem testes automatizados** de container na v1 (validação manual).

## Migration / Rollback

- **Rollback:** `docker compose down` (mantém volume `pgdata`); `docker compose down -v` para limpar dados.
