# Architecture

## 1. Technical Architecture

- **Stack:** Backend C# **.NET 9** + **Entity Framework Core** (DDD tático); Frontend **React + TypeScript**.
- **Database:** **PostgreSQL**.
- **Deployment:** **VPS Linux + Docker**.
- **Platform:** Web (responsive — navegador no celular e no PC).

## 2. Conceptual Architecture

### Layers

Backend estruturado em **DDD tático**, com as camadas:

1. **Domain** — entidades, value objects, aggregates e regras de negócio (sem dependências externas).
2. **Application** — casos de uso e orquestração (sem referências de infraestrutura).
3. **Infrastructure** — EF Core, repositórios, Identity e persistência (PostgreSQL).
4. **API** — controllers REST (contrato HTTP consumido pelo frontend).

Frontend **React + TypeScript** consome a API REST. **Sem** CQRS/event sourcing (desnecessário para o tamanho do projeto).

### Fundamental Principles

These principles MUST be followed by ALL specs and implementations:

1. **Extreme Detail & Determinism:** All specs must be created, maintained, and evolved with extreme detail, total completeness, and absolute determinism. No decision may be left pending for implementation time. Every spec must be implementable 100% by autonomous AI.

2. **KISS (Keep It Simple, Stupid):** All implementations must be as simple as possible. Avoid unnecessary abstractions, patterns, or complexity. The AI must be able to implement, maintain, and evolve the code easily.

3. **DRY (Don't Repeat Yourself):** Avoid duplication whenever possible, provided it does not conflict with KISS. Simplicity takes precedence over deduplication when they conflict.

4. **AI-First Documentation:** All spec documents (requirements.md, design.md, task.md) are written exclusively for AI engines (especially DeepSeek V4 Pro). Human readability is secondary. Precision, completeness, and determinism are paramount.
