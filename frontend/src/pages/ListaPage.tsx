import { useCallback, useEffect, useState } from 'react';
import { api, apiErrorMessage } from '../api/client';
import type { Jogo, ListaOficial } from '../types';

export default function ListaPage() {
  const [jogos, setJogos] = useState<Jogo[]>([]);
  const [jogoId, setJogoId] = useState('');
  const [texto, setTexto] = useState('');
  const [erro, setErro] = useState('');
  const [copiado, setCopiado] = useState(false);
  const [carregando, setCarregando] = useState(true);
  const [gerando, setGerando] = useState(false);

  const carregarJogos = useCallback(async () => {
    setCarregando(true);

    try {
      const { data } = await api.get<Jogo[]>('/api/jogos');
      setJogos(data);
      setErro('');
    } catch (error) {
      setErro(apiErrorMessage(error));
    } finally {
      setCarregando(false);
    }
  }, []);

  useEffect(() => {
    void carregarJogos();
  }, [carregarJogos]);

  async function gerar() {
    if (!jogoId) {
      return;
    }

    setGerando(true);
    setCopiado(false);

    try {
      const { data } = await api.get<ListaOficial>(`/api/jogos/${jogoId}/lista`);
      setTexto(data.texto);
      setErro('');
    } catch (error) {
      setTexto('');
      setErro(apiErrorMessage(error));
    } finally {
      setGerando(false);
    }
  }

  async function copiar() {
    try {
      await navigator.clipboard.writeText(texto);
      setCopiado(true);
    } catch {
      setErro('Não foi possível copiar automaticamente. Selecione o texto e copie manualmente.');
    }
  }

  return (
    <section>
      <h1>Lista oficial</h1>

      {erro && <p className="erro">{erro}</p>}

      <div className="formulario">
        <label>
          Jogo
          <select
            value={jogoId}
            onChange={(event) => {
              setJogoId(event.target.value);
              setTexto('');
              setCopiado(false);
            }}
          >
            <option value="">Selecione…</option>
            {jogos.map((jogo) => (
              <option key={jogo.id} value={jogo.id}>
                {jogo.competicaoCodigo} — {jogo.adversario}
              </option>
            ))}
          </select>
        </label>

        <button type="button" onClick={() => void gerar()} disabled={!jogoId || gerando}>
          {gerando ? 'Gerando…' : 'Gerar lista'}
        </button>

        <button type="button" onClick={() => void copiar()} disabled={!texto}>
          Copiar
        </button>
      </div>

      {copiado && <p className="aviso">Lista copiada.</p>}

      {carregando ? (
        <p>Carregando…</p>
      ) : texto ? (
        <pre className="lista">{texto}</pre>
      ) : (
        <p>Selecione um jogo e clique em "Gerar lista".</p>
      )}
    </section>
  );
}
