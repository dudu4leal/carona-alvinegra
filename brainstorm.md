# Carona Alvinegra — Brainstorm

> Documento de brainstorm do projeto. Registra a visão, o problema, o modelo de
> domínio, as regras de negócio e o formato de saída definidos na conversa inicial.

## 1. Visão

Web app para o **Jones** organizar as vans dos jogos do **Botafogo** para a torcida
**"Carona Alvinegra" de Petrópolis**. Substitui o caderninho manual e a lógica de
alocação feita à mão, automatizando a distribuição dos torcedores nas vans.

## 2. Problema atual

Hoje o fluxo é manual:

1. O torcedor chama o Jones no WhatsApp: "tem vaga pro jogo X?"
2. Jones confere o caderninho.
3. Se tem vaga, o torcedor paga via Pix (direto pro Jones).
4. Jones anota o nome no caderninho (nome por nome).
5. Durante a janela aberta, Jones envia listas parciais no grupo do WhatsApp.
6. Ao fechar as vagas, envia a lista final no grupo.

Dores identificadas:

- Vai-e-vem de "tem vaga?" (sem visibilidade de disponibilidade).
- Caderninho físico: risco de erro e difícil de automatizar.
- Lógica de alocação (rota + grupos) feita manualmente — a parte mais difícil.
- Jones é ponto único de falha.

## 3. Decisões de escopo (v1)

- **Ferramenta do Jones (back-office)**, não um portal para o torcedor.
- O contato inicial ("tem vaga?") e o Pix **continuam no WhatsApp do Jones**.
- O sistema **apenas marca pago/não pago** (não processa Pix).
- **Web app responsivo** (navegador no celular/PC), não app nativo.

## 4. Modelo de domínio

| Conceito | Descrição |
|---|---|
| **Participante** | nome, WhatsApp, rota (Quitandinha/Bingen), grupo |
| **Grupo** | indivisível, máx. 15 pessoas |
| **Van** | 15 lugares, rota única, motorista terceirizado, 1 responsável ("Resp") |
| **Spin** | carro menor, 6 lugares, por rota, estado temporário |
| **Jogo** | competição (código tipo "BR-26" = Brasileirão 2026), adversário, data, horário de saída, ponto de embarque |
| **Pagamento** | só marca pago/não pago |
| **Lista de espera** | quem entrou mas ainda não tem vaga definida |

## 5. Regras de negócio

- **Van**: 15 pessoas + motorista, rota única (uma van não passa nas duas rotas no mesmo jogo).
- **Grupo**: indivisível, máx. 15 pessoas; grupo de 15 enche uma van e abre outra.
- **Abertura de van**: uma rota precisa de **≥ 12 pessoas** para justificar uma van.
- **Spin**: 6 lugares, usado quando a rota não fecha 12 pessoas.
- **Promoção**: `spin + lista de espera ≥ 12` → vira uma van nova e a spin deixa de existir.
- **Grupo que não encaixa** (ex.: família de 8 numa rota): o sistema **detecta e escala para o Jones** decidir (separar / esperar / não vai). O sistema NÃO divide grupo sozinho.
- **Cancelamento**:
  - < 48h antes do jogo → sem reembolso (salvo se outra pessoa repor a vaga).
  - > 48h antes do jogo → reembolso total.

## 6. O algoritmo (núcleo do produto)

Alocador **contínuo e automático**, por jogo e por rota:

1. Manter a fila de confirmados (cada um pertence a um grupo).
2. Empacotar **grupos inteiros** em vans de 15 (greedy / first-fit).
3. Sobra < 12 pessoas → **spin** (6 lugares).
4. Sobra que não encaixa na spin (7–11) → **escalar para o Jones**.
5. `spin + lista de espera ≥ 12` → **promover a van**.

Exemplo (rota Quitandinha):

```
Van 1 ─────────────── 15/15 ✅ (fechada)
Van 2 ─────────────── 15/15 ✅ (fechada)
Spin ────────────────  6/6  🚗 (rota não fechou 12)
Lista de espera: +6
   → 6 (spin) + 6 (espera) = 12 ≥ 12 → Van 3 (12/15), spin some
```

## 7. Formato de saída (lista oficial)

A lista final é copiável para colar direto no grupo do WhatsApp.

**Cabeçalho** (varia por jogo):

```
LISTA OFICIAL
BR-26
Data: 7-10, Quarta
Saída, 17:00
Embarque: Praça 14 bis ✈️
Botafogo x Vasco
```

**Bloco de van** (repete por van):

```
Van 2 🚐 🚐 ( Bingen)
1- ✅ Daodock( Resp)
2- ✅ Nicolle
...
15- ✅ Danielli
```

**Bloco spin**:

```
Spin
1- ✅ Jones
...
6- ✅ Guaraci Costa
```

**Rodapé fixo** (não muda entre jogos):

```
❌ Não é permiti atraso
❌ Não é permitido o uso de drogas.
💯 Volta será uma hora após o jogo, com tolerância de 10 minutos. Caso não
   apareça, o responsável da van está autorizado a partir sem você.
✅ O carona não é meu, é nosso então, cuide dele!
```

## 8. Pontos em aberto (para decidir depois)

- Nome/identidade do produto (logo, nome do app).
- Se um dia o torcedor passará a se auto-inscrever (aí reavalia app nativo).
- Detalhes de login/segurança do Jones.
- Onde hospedar (online vs. local).
