import { useState } from 'react';
import type { FormEvent } from 'react';
import axios from 'axios';
import { Navigate, useNavigate } from 'react-router-dom';
import { apiErrorMessage } from '../api/client';
import { useAuth } from '../auth/AuthContext';

export default function LoginPage() {
  const { token, login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const [erro, setErro] = useState('');
  const [enviando, setEnviando] = useState(false);

  if (token) {
    return <Navigate to="/grupos" replace />;
  }

  async function entrar(event: FormEvent) {
    event.preventDefault();
    setEnviando(true);
    setErro('');

    try {
      await login(email, senha);
      navigate('/grupos', { replace: true });
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 401) {
        setErro('E-mail ou senha inválidos.');
      } else {
        setErro(apiErrorMessage(error));
      }
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="login">
      <form onSubmit={(event) => void entrar(event)} className="cartao">
        <h1>Carona Alvinegra</h1>

        <label>
          E-mail
          <input
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            autoComplete="username"
            required
          />
        </label>

        <label>
          Senha
          <input
            type="password"
            value={senha}
            onChange={(event) => setSenha(event.target.value)}
            autoComplete="current-password"
            required
          />
        </label>

        {erro && <p className="erro">{erro}</p>}

        <button type="submit" disabled={enviando}>
          {enviando ? 'Entrando…' : 'Entrar'}
        </button>
      </form>
    </div>
  );
}
