import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import axios from 'axios';
import { api, apiErrorMessage } from '../api/client';
import type { Grupo, GrupoRequest, Rota } from '../types';

export default function GruposPage() {
  const [grupos, setGrupos] = useState<Grupo[]>([]);
  const [nome, setNome] = useState('');
  const [rota, setRota] = useState<Rota>('Quitandinha');
  const [editandoId, setEditandoId] = useState<string | null>(null);
  const [erro, setErro] = useState('');
  const [carregando, setCarregando] = useState(true);

  const carregar = useCallback(async () => {
    setCarregando(true);

    try {
      const { data } = await api.get<Grupo[]>('/api/grupos');
      setGrupos(data);
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
    setRota('Quitandinha');
    setEditandoId(null);
  }

  function editar(grupo: Grupo) {
    setEditandoId(grupo.id);
    setNome(grupo.nome);
    setRota(grupo.rota);
  }

  async function salvar(event: FormEvent) {
    event.preventDefault();

    const payload: GrupoRequest = { nome, rota };

    try {
      if (editandoId) {
        await api.put(`/api/grupos/${editandoId}`, payload);
      } else {
        await api.post('/api/grupos', payload);
      }

      limpar();
      await carregar();
    } catch (error) {
      setErro(apiErrorMessage(error));
    }
  }

  async function excluir(id: string) {
    try {
      await api.delete(`/api/grupos/${id}`);
      await carregar();
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 409) {
        setErro('Não é possível excluir um grupo que possui participantes.');
      } else {
        setErro(apiErrorMessage(error));
      }
    }
  }

  return (
    <section>
      <h1>Grupos</h1>

      {erro && <p className="erro">{erro}</p>}

      <form onSubmit={(event) => void salvar(event)} className="formulario">
        <label>
          Nome
          <input value={nome} onChange={(event) => setNome(event.target.value)} required />
        </label>

        <label>
          Rota
          <select value={rota} onChange={(event) => setRota(event.target.value as Rota)}>
            <option value="Quitandinha">Quitandinha</option>
            <option value="Bingen">Bingen</option>
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
              <th>Rota</th>
              <th>Participantes</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            {grupos.length === 0 && (
              <tr>
                <td colSpan={4}>Nenhum grupo cadastrado.</td>
              </tr>
            )}

            {grupos.map((grupo) => (
              <tr key={grupo.id}>
                <td>{grupo.nome}</td>
                <td>{grupo.rota}</td>
                <td>{grupo.participantes.length}</td>
                <td className="acoes">
                  <button type="button" onClick={() => editar(grupo)}>Editar</button>
                  <button type="button" className="perigo" onClick={() => void excluir(grupo.id)}>
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
