import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { api, apiErrorMessage } from '../api/client';
import type { Jogo, JogoRequest } from '../types';

export default function JogosPage() {
  const [jogos, setJogos] = useState<Jogo[]>([]);
  const [competicaoCodigo, setCompeticaoCodigo] = useState('');
  const [adversario, setAdversario] = useState('');
  const [data, setData] = useState('');
  const [horarioSaida, setHorarioSaida] = useState('');
  const [pontoEmbarque, setPontoEmbarque] = useState('');
  const [editandoId, setEditandoId] = useState<string | null>(null);
  const [erro, setErro] = useState('');
  const [carregando, setCarregando] = useState(true);

  const carregar = useCallback(async () => {
    setCarregando(true);

    try {
      const { data: jogosApi } = await api.get<Jogo[]>('/api/jogos');
      setJogos(jogosApi);
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
    setCompeticaoCodigo('');
    setAdversario('');
    setData('');
    setHorarioSaida('');
    setPontoEmbarque('');
    setEditandoId(null);
  }

  function editar(jogo: Jogo) {
    setEditandoId(jogo.id);
    setCompeticaoCodigo(jogo.competicaoCodigo);
    setAdversario(jogo.adversario);
    setData(jogo.data.slice(0, 10));
    setHorarioSaida(jogo.horarioSaida.slice(0, 5));
    setPontoEmbarque(jogo.pontoEmbarque);
  }

  function formatarData(valor: string): string {
    const [ano, mes, dia] = valor.slice(0, 10).split('-');
    return ano && mes && dia ? `${dia}/${mes}/${ano}` : valor;
  }

  async function salvar(event: FormEvent) {
    event.preventDefault();

    const payload: JogoRequest = {
      competicaoCodigo,
      adversario,
      data,
      horarioSaida,
      pontoEmbarque,
    };

    try {
      if (editandoId) {
        await api.put(`/api/jogos/${editandoId}`, payload);
      } else {
        await api.post('/api/jogos', payload);
      }

      limpar();
      await carregar();
    } catch (error) {
      setErro(apiErrorMessage(error));
    }
  }

  async function excluir(id: string) {
    try {
      await api.delete(`/api/jogos/${id}`);
      await carregar();
    } catch (error) {
      setErro(apiErrorMessage(error));
    }
  }

  return (
    <section>
      <h1>Jogos</h1>

      {erro && <p className="erro">{erro}</p>}

      <form onSubmit={(event) => void salvar(event)} className="formulario">
        <label>
          Código
          <input
            value={competicaoCodigo}
            onChange={(event) => setCompeticaoCodigo(event.target.value)}
            placeholder="BR-26"
            required
          />
        </label>

        <label>
          Adversário
          <input value={adversario} onChange={(event) => setAdversario(event.target.value)} required />
        </label>

        <label>
          Data
          <input type="date" value={data} onChange={(event) => setData(event.target.value)} required />
        </label>

        <label>
          Saída
          <input
            type="time"
            value={horarioSaida}
            onChange={(event) => setHorarioSaida(event.target.value)}
            required
          />
        </label>

        <label>
          Embarque
          <input
            value={pontoEmbarque}
            onChange={(event) => setPontoEmbarque(event.target.value)}
            required
          />
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
              <th>Código</th>
              <th>Adversário</th>
              <th>Data</th>
              <th>Saída</th>
              <th>Embarque</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            {jogos.length === 0 && (
              <tr>
                <td colSpan={6}>Nenhum jogo cadastrado.</td>
              </tr>
            )}

            {jogos.map((jogo) => (
              <tr key={jogo.id}>
                <td>{jogo.competicaoCodigo}</td>
                <td>{jogo.adversario}</td>
                <td>{formatarData(jogo.data)}</td>
                <td>{jogo.horarioSaida.slice(0, 5)}</td>
                <td>{jogo.pontoEmbarque}</td>
                <td className="acoes">
                  <button type="button" onClick={() => editar(jogo)}>Editar</button>
                  <button type="button" className="perigo" onClick={() => void excluir(jogo.id)}>
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
