# DsCode Roadmap

## Spec Statuses

| Status | Meaning |
|---|---|---|---|
| `proposed` | Idea stage, no spec written yet |
| `planned` | Roadmap entry created via /spec-plan |
| `created` | Spec documents created via /spec-new |
| `verified` | Spec documents verified via /spec-verify |
| `implemented` | Code implemented via /spec-implement |
| `in-progress` | Under active development on a feature branch |
| `audited` | Final stage — implementation audited via /spec-audit. Feature is live. |
| `discarded` | Intentionally abandoned |

---

## Specs

| # | Name | Status | References |
|---|---|---|---|
| 10 | fundacao-backend | audited | V5 |
| 20 | modelo-dominio | audited | V5 |
| 30 | autenticacao | verified | V4 |
| 40 | crud-participantes-grupos | verified | V1 |
| 50 | gestao-jogos | verified | V1 |
| 60 | marcacao-pagamento | verified | V1 |
| 70 | algoritmo-alocacao | verified | V2 |
| 80 | lista-oficial | verified | V3 |
| 90 | frontend-react | verified | V6 |
| 100 | deploy | verified | V7 |
| 110 | atribuicao-responsavel-van | verified | V3 |

---

## Dependency Graph

```
10 (fundacao-backend)
  └─> 20 (modelo-dominio)
        ├─> 30 (autenticacao)
        ├─> 40 (crud-participantes-grupos)
        ├─> 50 (gestao-jogos)
        │     └─> 60 (marcacao-pagamento)
        │           └─> 70 (algoritmo-alocacao)
        │                 └─> 80 (lista-oficial)
        └─> 90 (frontend-react)  [depende de 30, 40, 50, 60, 70, 80]
              └─> 110 (atribuicao-responsavel-van)  [depende de 40, 70, 80, 90]
                    └─> 100 (deploy)                [depende de 10..110]
```
