# Carona Alvinegra — Decisões e Plano SDD

> Consolidado da conversa inicial (pré-SDD). Complementa o `brainstorm.md`
> (visão, domínio e regras de negócio) com as **decisões técnicas** e o
> **plano de trabalho** em SDD.
>
> Data: 2026-10-08

## 1. Stack técnica

| Camada | Decisão |
|---|---|
| Backend | C# **.NET 9** + **Entity Framework Core**, estruturado em **DDD tático** |
| Frontend | **React + TypeScript** |
| Banco de dados | **PostgreSQL** |
| Autenticação | **ASP.NET Core Identity + JWT** (login/senha) |
| API | REST |
| Deploy | **VPS Linux + Docker** |
| Estrutura | `backend/` (solução C#) + `frontend/` (app React) |

Notas:

- DDD tático: entidades, value objects, aggregates, repositories e as camadas
  Domain / Application / Infrastructure / API. **Sem** CQRS/event sourcing
  (desnecessário para o tamanho do projeto).

## 2. Escopo da v1

Entra na v1:

- Back-office do Jones: participantes, grupos, jogos, pagamentos.
- **Algoritmo de alocação automática** (van / spin / promoção / escalar Jones).
- Geração da **lista oficial** (formato WhatsApp).
- Login com usuário e senha.

Fora da v1 (futuro):

- Processamento de Pix.
- Portal do torcedor / auto-inscrição.
- App nativo.

## 3. Estrutura do repositório

```
carona-alvinegra/
├── brainstorm.md          ← visão, domínio e regras (semente dos specs)
├── decisoes.md            ← este documento (decisões + plano)
├── management/            ← SDD (roadmap, vision, specs, arch, adr, lessons)
├── backend/               ← solução .NET 9 (DDD)
├── frontend/              ← app React + TypeScript
└── dscode/                ← ferramenta DsCode (gitignored, NÃO commitada)
```

## 4. Fluxo SDD

```
1. /spec-plan        → cria management/roadmap.md  (status "planned")
2. /spec-new <n>     → cria requirements/design/task (status "created")
3. /spec-verify <n>  → valida e auto-corrige specs  (status "verified")
4. /spec-implement <n> → implementa (branch → merge) (status "implemented")
5. /spec-audit <n>   → audita e fecha (feature live) (status "audited")
```

## 5. Specs previstos

O `/spec-plan` formaliza numeração e dependências. Preview:

1. Fundação do backend (.NET 9 + DDD + EF Core + PostgreSQL + migrations)
2. Modelo de domínio (Participante, Grupo, Van, Spin, Jogo, Pagamento, Lista de espera)
3. Autenticação (Identity + JWT)
4. CRUD de Participantes e Grupos
5. Gestão de Jogos
6. Marcação de pagamento (pago/não pago)
7. **Algoritmo de alocação** (van ≥12 / spin / promoção / escalar Jones) — o núcleo
8. Geração da lista oficial (formato WhatsApp)
9. Frontend React + TypeScript (shell + telas)
10. Deploy (Docker + VPS + PostgreSQL)

## 6. Pré-requisito operacional

A sessão atual do DsCode está presa à pasta `dscode/`. Para o SDD funcionar
corretamente (Read/Write/Edit/Grep/Glob + índice de arquivos cobrindo o projeto),
**reiniciar o DsCode a partir da pasta pai**:

```bash
cd C:\Users\dudsl\Documents\GitHub\carona-alvinegra
dscode\dscode.cmd
```

Adicionar `.gitignore` no pai com `dscode/`, `node_modules/` e `node.exe`.

## 7. Em aberto (adiado — não bloqueia o SDD)

- Nome/identidade do produto (logo, nome do app).
- Auto-inscrição do torcedor (um dia).
- Detalhes avançados de login/segurança.
