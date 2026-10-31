import { useCallback, useEffect, useRef, useState } from 'react';
import { api, apiErrorMessage } from '../api/client';
import type {
  DesmarcarPagamento,
  Grupo,
  Jogo,
  Pagamento,
  Participante,
} from '../types';

export default function PagamentosPage() {
  const [jogos, setJogos] = useState<Jogo[]>([]);
  const [participantes, setParticipantes] = useState<Participante[]>([]);
  const [grupos, setGrupos] = useState<Grupo[]>([]);
  const [jogoId, setJogoId] = useState('');
  const [pagamentos, setPagamentos] = useState<Pagamento[]>([]);
  const [aviso, setAviso] = useState('');
  const [erro, setErro] = useState('');
  const [carregando, setCarregando] = useState(true);
  const [processando, setProcessando] = useState(false);
  const sequencia = useRef(0);

  const carregarBase = useCallback(async () => {
    setCarregando(true);

    try {
      const [jogosRes, participantesRes, gruposRes] = await Promise.all([
        api.get<Jogo[]>('/api/jogos'),
        api.get<Participante[]>('/api/participantes'),
        api.get<Grupo[]>('/api/grupos'),
      ]);

      setJogos(jogosRes.data);
      setParticipantes(participantesRes.data);
      setGrupos(gruposRes.data);
      setErro('');
    } catch (error) {
      setErro(apiErrorMessage(error));
    } finally {
      setCarregando(false);
    }
  }, []);

  const carregarPagamentos = useCallback(async (id: string) => {
    const atual = ++sequencia.current;

    if (!id) {
      setPagamentos([]);
      return;
    }

    try {
      const { data } = await api.get<Pagamento[]>(`/api/jogos/${id}/pagamentos`);

      if (atual === sequencia.current) {
        setPagamentos(data);
        setErro('');
      }
    } catch (error) {
      if (atual === sequencia.current) {
        setErro(apiErrorMessage(error));
      }
    }
  }, []);

  useEffect(() => {
    void carregarBase();
  }, [carregarBase]);

  useEffect(() => {
    setAviso('');
    void carregarPagamentos(jogoId);
  }, [jogoId, carregarPagamentos]);

  function nomeDoGrupo(id: string): string {
    return grupos.find((grupo) => grupo.id === id)?.nome ?? '—';
  }

  function pagamentoDe(participanteId: string): Pagamento | undefined {
    return pagamentos.find((pagamento) => pagamento.participanteId === participanteId);
  }

  async function marcar(participanteId: string) {
    const atual = ++sequencia.current;
    setProcessando(true);

    try {
      await api.post(`/api/jogos/${jogoId}/pagamentos/${participanteId}/marcar-pago`);

      if (atual === sequencia.current) {
        setAviso('');
        await carregarPagamentos(jogoId);
      }
    } catch (error) {
      if (atual === sequencia.current) {
        setErro(apiErrorMessage(error));
      }
    } finally {
      setProcessando(false);
    }
  }

  async function desmarcar(participanteId: string) {
    const atual = ++sequencia.current;
    setProcessando(true);

    try {
      const { data } = await api.post<DesmarcarPagamento>(
        `/api/jogos/${jogoId}/pagamentos/${participanteId}/desmarcar-pago`,
      );

      if (atual !== sequencia.current) {
        return;
      }

      setAviso(
        data.reembolsoDevido
          ? 'Pagamento desmarcado: reembolso devido.'
          : 'Pagamento desmarcado: sem reembolso.',
      );
      await carregarPagamentos(jogoId);
    } catch (error) {
      if (atual === sequencia.current) {
        setErro(apiErrorMessage(error));
      }
    } finally {
      setProcessando(false);
    }
  }

  return (
    <section>
      <h1>Pagamentos</h1>

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
      </div>

      {aviso && <p className="aviso">{aviso}</p>}

      {carregando ? (
        <p>Carregando…</p>
      ) : !jogoId ? (
        <p>Selecione um jogo para ver os pagamentos.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Participante</th>
              <th>Grupo</th>
              <th>Status</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            {participantes.length === 0 && (
              <tr>
                <td colSpan={4}>Nenhum participante cadastrado.</td>
              </tr>
            )}

            {participantes.map((participante) => {
              const pago = pagamentoDe(participante.id)?.pago ?? false;

              return (
                <tr key={participante.id}>
                  <td>{participante.nome}</td>
                  <td>{nomeDoGrupo(participante.grupoId)}</td>
                  <td>{pago ? 'Pago' : 'Não pago'}</td>
                  <td className="acoes">
                    {pago ? (
                      <button
                        type="button"
                        disabled={processando}
                        onClick={() => void desmarcar(participante.id)}
                      >
                        Desmarcar
                      </button>
                    ) : (
                      <button
                        type="button"
                        disabled={processando}
                        onClick={() => void marcar(participante.id)}
                      >
                        Marcar pago
                      </button>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </section>
  );
}
