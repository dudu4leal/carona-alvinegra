# Architecture Decision Records (ADR)

This file records architecture decisions that affect the project. Each ADR describes a decision, its context, the rationale behind it, and its consequences. ADRs are numbered sequentially (ADR-001, ADR-002, ...) and never renumbered after creation.

---

## ADR-001: Backend em C# .NET 9 com DDD tático

**Date:** 2026-10-08
**Status:** accepted

**Context:** Back-office web para alocar torcedores em vans, com um algoritmo de alocação como núcleo. É preciso um backend com modelo de domínio rico (regras de van/spin/promoção) e persistência relacional.

**Decision:** Backend em C# .NET 9 + Entity Framework Core, estruturado em DDD tático (camadas Domain / Application / Infrastructure / API). Sem CQRS/event sourcing.

**Rationale:** DDD tático modela bem as regras de negócio (aggregates de Grupo/Van/Spin/Jogo). .NET 9 + EF Core é estável e bem suportado para o tamanho do projeto. CQRS/event sourcing seria complexidade desnecessária.

**Consequences:** Camadas claras e testáveis; curva de estrutura um pouco maior em relação a um CRUD simples, mas justificada pelo algoritmo de alocação.

---

## ADR-002: Frontend em React + TypeScript

**Date:** 2026-10-08
**Status:** accepted

**Context:** O Jones precisa de uma interface responsiva (celular e PC) para o back-office.

**Decision:** Frontend em React + TypeScript, consumindo a API REST.

**Rationale:** React + TS é maduro, tipado e produtivo; atende responsividade sem app nativo.

**Consequences:** Build/transpilação adicional; tipagem compartilhada entre camadas via contratos de API.

---

## ADR-003: PostgreSQL como banco de dados

**Date:** 2026-10-08
**Status:** accepted

**Context:** Persistência relacional de participantes, grupos, jogos, pagamentos e alocações.

**Decision:** PostgreSQL, acessado via EF Core.

**Rationale:** Relacional robusto, com integridade referencial e suporte no EF Core; adequado ao domínio (poucas entidades, forte consistência).

**Consequences:** Requer instância PostgreSQL no deploy (Docker Compose).

---

## ADR-004: Autenticação com ASP.NET Core Identity + JWT

**Date:** 2026-10-08
**Status:** accepted

**Context:** A v1 precisa de login com usuário e senha para o Jones (único usuário).

**Decision:** ASP.NET Core Identity (login/senha) + JWT para autorização nas rotas da API.

**Rationale:** Identity fornece login/hash de senha pronto; JWT é o padrão para APIs REST stateless.

**Consequences:** Rotas protegidas por token; usuário único semeado na inicialização.

---

## ADR-005: Deploy em VPS Linux + Docker

**Date:** 2026-10-08
**Status:** accepted

**Context:** O app precisa ser publicado para uso real do Jones.

**Decision:** Deploy em VPS Linux via Docker (Docker Compose para backend + frontend + PostgreSQL).

**Rationale:** Docker simplifica o empacotamento e a reprodutibilidade; VPS Linux é suficiente para a v1 (poucos usuários).

**Consequences:** Infraestrutura mínima gerenciada por Docker Compose; sem orquestração (Kubernetes) na v1.
