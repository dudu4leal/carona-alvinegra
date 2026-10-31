# Spec 90: Frontend React + TypeScript — Design

## Design Approach

App SPA em **Vite + React + TypeScript**, consumindo a API REST. Estado local + `AuthContext` (sem Redux). Um cliente HTTP centralizado injeta o JWT. Cada tela segue o mesmo padrão: carregar lista → exibir → formulário de criar/editar → ações de excluir.

## Architecture Decisions

- **Vite** como build/dev server (rápido, padrão moderno).
- **React Router** para navegação e rotas protegidas.
- **Axios** como cliente HTTP, com interceptor de token.
- **Token no `localStorage`**, removido no logout.
- **CSS simples** (sem framework UI pesado), com layout responsivo.
- **`tsconfig.json` único** (inclui `src`) e `ProtectedRoute` definido em `App.tsx` (sem arquivo extra).
- **Respostas assíncronas ordenadas por sequência:** telas que disparam GET + ação (Alocação/Pagamentos) descartam respostas antigas via contador `useRef`.

## Component / Module Breakdown

### Componente: `api/client.ts`

**Purpose:** Cliente HTTP com token.

**Interface:** `api.get/post/put/delete` (instância axios).

**Internal Logic:** `baseURL` de `import.meta.env.VITE_API_URL` (default `http://localhost:5000`). Interceptor adiciona `Authorization: Bearer <token>`; em resposta 401, limpa o token e redireciona para `/login` — exceto no próprio login, cujo 401 vira a mensagem "E-mail ou senha inválidos." na tela.

**Dependencies:** `axios`.

**Error Handling:** 401 → logout; demais erros → propaga mensagem.

### Componente: `auth/AuthContext.tsx`

**Purpose:** Sessão do usuário.

**Interface:** `{ token, login(email, senha), logout() }` via `useAuth()` (a presença do token em `localStorage` determina a autenticação).

**Internal Logic:** `login` chama `POST /api/auth/login`, guarda o token em `localStorage` e no estado. `logout` limpa.

**Dependencies:** `api/client.ts`.

**Error Handling:** credenciais inválidas → erro exibido na tela.

### Componente: `components/Layout.tsx`

**Purpose:** Shell com menu e `Outlet` do Router.

**Internal Logic:** Menu com links para as telas e botão Sair. Rotas: `/grupos`, `/participantes`, `/jogos`, `/pagamentos`, `/alocacao`, `/lista` (protegidas via `ProtectedRoute`); `/login` é pública; `/` redireciona para `/grupos` (protegida). `ProtectedRoute` verifica o token e redireciona para `/login` sem token.

**Dependencies:** `AuthContext`, React Router.

**Error Handling:** N/A.

### Componente: Páginas (Login, Grupos, Participantes, Jogos, Pagamentos, Alocação, Lista)

**Purpose:** Telas do back-office.

**Internal Logic:**
- `LoginPage`: formulário e-mail/senha → `login()`.
- `GruposPage`: tabela + form (nome, rota) + excluir (com tratamento de 409).
- `ParticipantesPage`: tabela + form (nome, whatsapp, grupo) + excluir; carrega `GET /api/grupos` para a seleção.
- `JogosPage`: tabela + form (código, adversário, data, saída, embarque) + excluir.
- `PagamentosPage`: seleção de jogo → carrega `GET /api/participantes`, `GET /api/grupos` e `GET /api/jogos/{id}/pagamentos`, cruza por `participanteId` e exibe **uma linha por participante** (nome + grupo) com estado pago/não pago (ausência de registro = não pago) + botões marcar/desmarcar; ao desmarcar, mostra `reembolsoDevido`.
- `AlocacaoPage`: seleção de jogo → botão "Alocar" → exibe `Alocacao[]` (um bloco por rota, com vans/spin/escalados/pendentes); `GET .../alocacao` também retorna array (404 → estado vazio, não erro); carrega `GET /api/grupos` para exibir nomes.
- `ListaPage`: seleção de jogo → botão "Gerar lista" → exibe texto + botão copiar.

**Dependencies:** `api/client.ts`, DTOs/tipos.

**Error Handling:** exibe mensagens de erro da API (400/404/409).

### Componente: Tipos (`types/`)

**Purpose:** Tipos TypeScript espelhando os DTOs do backend.

**Interface:** `Grupo`, `Participante`, `Jogo`, `Pagamento`, `DesmarcarPagamento`, `Alocacao`, `VanAlocada`, `SpinAlocado`, `ListaOficial`.

**Internal Logic:** N/A.

**Dependencies:** Nenhuma.

**Error Handling:** N/A.

## Data Flow

1. Usuário acessa `/` → `ProtectedRoute` verifica token → sem token, `/login`.
2. `LoginPage` → `login()` → `POST /api/auth/login` → token em `localStorage`.
3. Telas usam `api/client.ts` (com token) para CRUD/operações.
4. `ListaPage` copia o texto retornado por `GET /api/jogos/{id}/lista`.

## Data Structures

- Tipos espelhando DTOs:
```ts
type Rota = 'Quitandinha' | 'Bingen';
interface Grupo { id: string; nome: string; rota: Rota; participantes: Participante[]; }
interface Participante { id: string; nome: string; whatsApp: string; grupoId: string; }
interface Jogo { id: string; competicaoCodigo: string; adversario: string; data: string; horarioSaida: string; pontoEmbarque: string; }
interface VanAlocada { id: string; motorista: string; responsavelId: string; grupoIds: string[]; ocupacao: number; }
interface SpinAlocado { id: string; grupoIds: string[]; ocupacao: number; }
interface Alocacao { id: string; jogoId: string; rota: Rota; vans: VanAlocada[]; spin?: SpinAlocado | null; gruposEscalados: string[]; gruposPendentes: string[]; }
// GET/POST /api/jogos/{id}/alocar|alocacao retornam Alocacao[] (uma por rota: Quitandinha, Bingen)
type AlocacaoLista = Alocacao[];
interface Pagamento { id: string; participanteId: string; jogoId: string; pago: boolean; }
interface DesmarcarPagamento { reembolsoDevido: boolean; }
interface ListaOficial { texto: string; }
```

## File / Module Layout

```
frontend/
├── package.json
├── vite.config.ts
├── tsconfig.json
├── index.html
└── src/
    ├── main.tsx
    ├── App.tsx                    (rotas)
    ├── api/client.ts              (novo)
    ├── auth/AuthContext.tsx       (novo)
    ├── components/Layout.tsx      (novo)
    ├── pages/
    │   ├── LoginPage.tsx
    │   ├── GruposPage.tsx
    │   ├── ParticipantesPage.tsx
    │   ├── JogosPage.tsx
    │   ├── PagamentosPage.tsx
    │   ├── AlocacaoPage.tsx
    │   └── ListaPage.tsx
    └── types/index.ts             (novo)
```

## Testing Strategy

- **Build/typecheck:** `npm run build` com exit code 0 (validação principal; o script do Vite já inclui typecheck).
- **Testes manuais:** login, CRUD de cada tela, alocação e cópia da lista.
- Testes de componente (Vitest/RTL) são opcionais na v1 (KISS); não bloqueiam.

## Migration / Rollback

- **Sem migration** (frontend não usa banco).
- **Rollback:** N/A.
