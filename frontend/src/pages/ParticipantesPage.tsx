import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { api, apiErrorMessage } from '../api/client';
import type { Grupo, Participante, ParticipanteRequest } from '../types';

export default function ParticipantesPage() {
  const [participantes, setParticipantes] = useState<Participante[]>([]);
  const [grupos, setGrupos] = useState<Grupo[]>([]);
  const [nome, setNome] = useState('');
  const [whatsApp, setWhatsApp] = useState('');
  const [grupoId, setGrupoId] = useState('');
  const [editandoId, setEditandoId] = useState<string | null>(null);
  const [erro, setErro] = useState('');
  const [carregando, setCarregando] = useState(true);

  const carregar = useCallback(async () => {
    setCarregando(true);

    try {
      const [participantesRes, gruposRes] = await Promise.all([
        api.get<Participante[]>('/api/participantes'),
        api.get<Grupo[]>('/api/grupos'),
      ]);

      setParticipantes(participantesRes.data);
      setGrupos(gruposRes.data);
      setErro('');
    } catch (error) {
      setErro(apiErrorMessage(error));
    } finally {
      setCarregando(false);
    }
  }, []);

  useEffect(() => {
    void carregar();
  }, [carregar]);

  function limpar() {
    setNome('');
    setWhatsApp('');
    setGrupoId('');
    setEditandoId(null);
  }

  function editar(participante: Participante) {
    setEditandoId(participante.id);
    setNome(participante.nome);
    setWhatsApp(participante.whatsApp);
    setGrupoId(participante.grupoId);
  }

  function nomeDoGrupo(id: string): string {
    return grupos.find((grupo) => grupo.id === id)?.nome ?? '—';
  }

  async function salvar(event: FormEvent) {
    event.preventDefault();

    const payload: ParticipanteRequest = { nome, whatsApp, grupoId };

    try {
      if (editandoId) {
        await api.put(`/api/participantes/${editandoId}`, payload);
      } else {
        await api.post('/api/participantes', payload);
      }

      limpar();
      await carregar();
    } catch (error) {
      setErro(apiErrorMessage(error));
    }
  }

  async function excluir(id: string) {
    try {
      await api.delete(`/api/participantes/${id}`);
      await carregar();
    } catch (error) {
      setErro(apiErrorMessage(error));
    }
  }

  return (
    <section>
      <h1>Participantes</h1>

      {erro && <p className="erro">{erro}</p>}

      <form onSubmit={(event) => void salvar(event)} className="formulario">
        <label>
          Nome
          <input value={nome} onChange={(event) => setNome(event.target.value)} required />
        </label>

        <label>
          WhatsApp
          <input
            type="tel"
            value={whatsApp}
            onChange={(event) => setWhatsApp(event.target.value)}
            required
          />
        </label>

        <label>
          Grupo
          <select value={grupoId} onChange={(event) => setGrupoId(event.target.value)} required>
            <option value="">Selecione…</option>
            {grupos.map((grupo) => (
              <option key={grupo.id} value={grupo.id}>
                {grupo.nome} ({grupo.rota})
              </option>
            ))}
          </select>
        </label>

        <div className="acoes">
          <button type="submit">{editandoId ? 'Salvar' : 'Criar'}</button>
          {editandoId && (
            <button type="button" onClick={limpar}>Cancelar</button>
          )}
        </div>
      </form>

      {carregando ? (
        <p>Carregando…</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Nome</th>
              <th>WhatsApp</th>
              <th>Grupo</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            {participantes.length === 0 && (
              <tr>
                <td colSpan={4}>Nenhum participante cadastrado.</td>
              </tr>
            )}

            {participantes.map((participante) => (
              <tr key={participante.id}>
                <td>{participante.nome}</td>
                <td>{participante.whatsApp}</td>
                <td>{nomeDoGrupo(participante.grupoId)}</td>
                <td className="acoes">
                  <button type="button" onClick={() => editar(participante)}>Editar</button>
                  <button
                    type="button"
                    className="perigo"
                    onClick={() => void excluir(participante.id)}
                  >
                    Excluir
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
