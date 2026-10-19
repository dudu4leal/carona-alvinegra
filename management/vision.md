# Carona Alvinegra Vision

## Product Vision

Web app (back-office) para o **Jones** organizar as vans dos jogos do **Botafogo** para a torcida **"Carona Alvinegra" de Petrópolis**. Substitui o caderninho manual e a lógica de alocação feita à mão, automatizando a distribuição dos torcedores nas vans.

## Target Audience

O **Jones** (organizador), único usuário da v1. Usa o sistema pelo celular e pelo computador para cadastrar participantes, grupos e jogos, marcar pagamentos, rodar a alocação automática e gerar a lista oficial para colar no grupo do WhatsApp. Os torcedores continuam interagindo com o Jones pelo WhatsApp (a v1 não tem portal do torcedor nem processamento de Pix).

## Value Proposition

- **Gestão centralizada:** participantes, grupos, jogos e pagamentos em um só lugar (substitui o caderninho).
- **Alocação automática:** distribuição de grupos em vans/spins seguindo as regras (van ≥12, spin 6, promoção, escalar Jones).
- **Lista oficial pronta:** geração da lista copiável para o WhatsApp no formato oficial.
- **Acesso seguro:** login com usuário e senha (único usuário na v1).

---

## Value Blocks

<!-- Value blocks are added incrementally by /spec-plan and /spec-new. Do not edit manually. -->

### V1: Back-office do Jones
Substitui o caderninho manual e a lógica de alocação feita à mão por um sistema web centralizado. O Jones cadastra participantes, grupos, jogos e marca pagamentos em um único lugar, eliminando o vai-e-vem de "tem vaga?" e o risco de erro do caderninho físico. É uma ferramenta de back-office (só o Jones usa); o contato inicial e o Pix continuam no WhatsApp dele.

**Delivered by:** Spec 40 (crud-participantes-grupos), Spec 50 (gestao-jogos), Spec 60 (marcacao-pagamento)

### V2: Alocação automática de vans
O núcleo do produto: distribuir automaticamente os grupos confirmados em vans (15 lugares) e spins (6 lugares), por jogo e por rota. Mantém a fila de confirmados, empacota grupos inteiros (greedy/first-fit), abre van quando a rota tem ≥12 pessoas, usa spin quando sobra <12, promove spin+lista de espera ≥12 para uma van nova e, quando um grupo não encaixa (ex.: família de 8), escala para o Jones decidir — o sistema nunca divide grupo sozinho.

**Delivered by:** Spec 70 (algoritmo-alocacao)

### V3: Lista oficial para WhatsApp
Gera a lista oficial do jogo no formato exato para colar no grupo do WhatsApp: cabeçalho (competição, data, horário de saída, ponto de embarque, confronto), blocos de van (com responsável marcado como "Resp") e bloco spin, além do rodapé fixo (regras de atraso, drogas, horário de volta e o lema da carona).

**Delivered by:** Spec 80 (lista-oficial), Spec 110 (atribuicao-responsavel-van)

### V4: Acesso autenticado
Protege o back-office com login e senha. Apenas o Jones acessa o sistema (único usuário na v1), autenticado via ASP.NET Core Identity e autorizado nas rotas da API por JWT. Evita que terceiros alterem participantes, pagamentos ou alocações.

**Delivered by:** Spec 30 (autenticacao)

### V5: Fundação técnica e modelo de domínio
Estabelece a fundação técnica e o modelo de domínio: backend .NET 9 em DDD tático (camadas Domain / Application / Infrastructure / API) com EF Core e PostgreSQL, além das entidades do domínio — Participante, Grupo, Van, Spin, Jogo, Pagamento e Lista de espera. É a base sobre a qual todos os outros specs constroem.

**Delivered by:** Spec 10 (fundacao-backend), Spec 20 (modelo-dominio)

### V6: Frontend responsivo
Interface web responsiva (React + TypeScript) que o Jones usa no celular e no PC: shell com navegação e telas para participantes, grupos, jogos, pagamentos, alocação e a lista oficial. Consome a API REST do backend.

**Delivered by:** Spec 90 (frontend-react)

### V7: Deploy em produção
Publica o sistema para uso real do Jones em um VPS Linux, empacotado com Docker (Docker Compose orquestrando backend, frontend e PostgreSQL). Torna a v1 acessível em produção.

**Delivered by:** Spec 100 (deploy)
