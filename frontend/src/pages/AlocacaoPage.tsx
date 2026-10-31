import { useCallback, useEffect, useRef, useState } from 'react';
import axios from 'axios';
import { api, apiErrorMessage, definirResponsavelVan } from '../api/client';
import type { Alocacao, Grupo, Jogo, VanAlocada } from '../types';

const SEM_RESPONSAVEL = '00000000-0000-0000-0000-000000000000';

interface RascunhoVan {
  motorista: string;
  responsavelId: string;
}

function montarRascunhos(alocacoes: Alocacao[]): Record<string, RascunhoVan> {
  const rascunhos: Record<string, RascunhoVan> = {};

  for (const alocacao of alocacoes) {
    for (const van of alocacao.vans) {
      rascunhos[van.id] = {
        motorista: van.motorista,
        responsavelId: van.responsavelId === SEM_RESPONSAVEL ? '' : van.responsavelId,
      };
    }
  }

  return rascunhos;
}

export default function AlocacaoPage() {
  const [jogos, setJogos] = useState<Jogo[]>([]);
  const [grupos, setGrupos] = useState<Grupo[]>([]);
  const [jogoId, setJogoId] = useState('');
  const [alocacoes, setAlocacoes] = useState<Alocacao[]>([]);
  const [rascunhos, setRascunhos] = useState<Record<string, RascunhoVan>>({});
  const [erro, setErro] = useState('');
  const [carregando, setCarregando] = useState(true);
  const [alocando, setAlocando] = useState(false);
  const [salvandoVan, setSalvandoVan] = useState<string | null>(null);
  const sequencia = useRef(0);

  const carregarBase = useCallback(async () => {
    setCarregando(true);

    try {
      const [jogosRes, gruposRes] = await Promise.all([
        api.get<Jogo[]>('/api/jogos'),
        api.get<Grupo[]>('/api/grupos'),
      ]);

      setJogos(jogosRes.data);
      setGrupos(gruposRes.data);
      setErro('');
    } catch (error) {
      setErro(apiErrorMessage(error));
    } finally {
      setCarregando(false);
    }
  }, []);

  const carregarAlocacao = useCallback(async (id: string) => {
    const atual = ++sequencia.current;

    if (!id) {
      setAlocacoes([]);
      setRascunhos({});
      return;
    }

    try {
      const { data } = await api.get<Alocacao[]>(`/api/jogos/${id}/alocacao`);

      if (atual !== sequencia.current) {
        return;
      }

      setAlocacoes(data);
      setRascunhos(montarRascunhos(data));
      setErro('');
    } catch (error) {
      if (atual !== sequencia.current) {
        return;
      }

      if (axios.isAxiosError(error) && error.response?.status === 404) {
        setAlocacoes([]);
        setRascunhos({});
        setErro('');
      } else {
        setAlocacoes([]);
        setRascunhos({});
        setErro(apiErrorMessage(error));
      }
    }
  }, []);

  useEffect(() => {
    void carregarBase();
  }, [carregarBase]);

  useEffect(() => {
    void carregarAlocacao(jogoId);
  }, [jogoId, carregarAlocacao]);

  function nomesDosGrupos(ids: string[]): string {
    if (ids.length === 0) {
      return '—';
    }

    return ids
      .map((id) => grupos.find((grupo) => grupo.id === id)?.nome ?? id)
      .join(', ');
  }

  function opcoesDeResponsavel(van: VanAlocada): { id: string; label: string }[] {
    const opcoes: { id: string; label: string }[] = [];

    for (const grupoId of van.grupoIds) {
      const grupo = grupos.find((g) => g.id === grupoId);

      if (!grupo) {
        continue;
      }

      for (const participante of grupo.participantes) {
        opcoes.push({
          id: participante.id,
          label: `${participante.nome} (${grupo.nome})`,
        });
      }
    }

    return opcoes;
  }

  function atualizarRascunho(van: VanAlocada, campo: keyof RascunhoVan, valor: string) {
    setRascunhos((atuais) => {
      const atual = atuais[van.id] ?? {
        motorista: van.motorista,
        responsavelId: van.responsavelId === SEM_RESPONSAVEL ? '' : van.responsavelId,
      };

      return {
        ...atuais,
        [van.id]: { ...atual, [campo]: valor },
      };
    });
  }

  async function alocar() {
    if (!jogoId) {
      return;
    }

    const atual = ++sequencia.current;
    setAlocando(true);

    try {
      const { data } = await api.post<Alocacao[]>(`/api/jogos/${jogoId}/alocar`);

      if (atual === sequencia.current) {
        setAlocacoes(data);
        setRascunhos(montarRascunhos(data));
        setErro('');
      }
    } catch (error) {
      if (atual === sequencia.current) {
        setErro(apiErrorMessage(error));
      }
    } finally {
      setAlocando(false);
    }
  }

  async function salvarResponsavel(alocacaoId: string, van: VanAlocada) {
    const rascunho = rascunhos[van.id];

    if (!rascunho) {
      return;
    }

    setSalvandoVan(van.id);

    try {
      const resposta = await definirResponsavelVan(jogoId, van.id, {
        motorista: rascunho.motorista,
        responsavelId: rascunho.responsavelId || SEM_RESPONSAVEL,
      });

      setAlocacoes((atuais) =>
        atuais.map((alocacao) =>
          alocacao.id !== alocacaoId
            ? alocacao
            : {
                ...alocacao,
                vans: alocacao.vans.map((v) => (v.id === van.id ? resposta : v)),
              },
        ),
      );
      setRascunhos((atuais) => ({
        ...atuais,
        [van.id]: {
          motorista: resposta.motorista,
          responsavelId: resposta.responsavelId === SEM_RESPONSAVEL ? '' : resposta.responsavelId,
        },
      }));
      setErro('');
    } catch (error) {
      setErro(apiErrorMessage(error));
    } finally {
      setSalvandoVan(null);
    }
  }

  return (
    <section>
      <h1>Alocação</h1>

      {erro && <p className="erro">{erro}</p>}

      <div className="formulario">
        <label>
          Jogo
          <select value={jogoId} onChange={(event) => setJogoId(event.target.value)}>
            <option value="">Selecione…</option>
            {jogos.map((jogo) => (
              <option key={jogo.id} value={jogo.id}>
                {jogo.competicaoCodigo} — {jogo.adversario}
              </option>
            ))}
          </select>
        </label>

        <button type="button" onClick={() => void alocar()} disabled={!jogoId || alocando}>
          {alocando ? 'Alocando…' : 'Alocar'}
        </button>
      </div>

      {carregando ? (
        <p>Carregando…</p>
      ) : !jogoId ? (
        <p>Selecione um jogo para alocar.</p>
      ) : alocacoes.length === 0 ? (
        <p>Nenhuma alocação. Clique em "Alocar".</p>
      ) : (
        alocacoes.map((alocacao) => (
          <article key={alocacao.id} className="cartao">
            <h2>Rota {alocacao.rota}</h2>

            {alocacao.vans.length === 0 ? (
              <p>Nenhuma van.</p>
            ) : (
              alocacao.vans.map((van, indice) => {
                const rascunho = rascunhos[van.id] ?? {
                  motorista: van.motorista,
                  responsavelId:
                    van.responsavelId === SEM_RESPONSAVEL ? '' : van.responsavelId,
                };
                const opcoes = opcoesDeResponsavel(van);
                const responsavelForaDaLista =
                  rascunho.responsavelId !== '' &&
                  !opcoes.some((opcao) => opcao.id === rascunho.responsavelId);

                return (
                  <div key={van.id} className="bloco">
                    <h3>Van {indice + 1}</h3>
                    <p>Ocupação: {van.ocupacao}</p>
                    <p>Grupos: {nomesDosGrupos(van.grupoIds)}</p>

                    <div className="formulario">
                      <label>
                        Motorista
                        <input
                          value={rascunho.motorista}
                          onChange={(event) =>
                            atualizarRascunho(van, 'motorista', event.target.value)
                          }
                          maxLength={100}
                        />
                      </label>

                      <label>
                        Responsável
                        <select
                          value={rascunho.responsavelId}
                          onChange={(event) =>
                            atualizarRascunho(van, 'responsavelId', event.target.value)
                          }
                        >
                          <option value="">— sem responsável —</option>
                          {opcoes.map((opcao) => (
                            <option key={opcao.id} value={opcao.id}>
                              {opcao.label}
                            </option>
                          ))}
                          {responsavelForaDaLista && (
                            <option value={rascunho.responsavelId}>
                              Responsável atual (fora dos grupos da van)
                            </option>
                          )}
                        </select>
                      </label>

                      <button
                        type="button"
                        onClick={() => void salvarResponsavel(alocacao.id, van)}
                        disabled={salvandoVan === van.id}
                      >
                        {salvandoVan === van.id ? 'Salvando…' : 'Salvar'}
                      </button>
                    </div>
                  </div>
                );
              })
            )}

            {alocacao.spin && (
              <div className="bloco">
                <h3>Spin</h3>
                <p>Ocupação: {alocacao.spin.ocupacao}</p>
                <p>Grupos: {nomesDosGrupos(alocacao.spin.grupoIds)}</p>
              </div>
            )}

            <p>Escalados: {nomesDosGrupos(alocacao.gruposEscalados)}</p>
            <p>Pendentes (não pagos): {nomesDosGrupos(alocacao.gruposPendentes)}</p>
          </article>
        ))
      )}
    </section>
  );
}
