import type { ReactNode } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import { useAuth } from './auth/AuthContext';
import Layout from './components/Layout';
import LoginPage from './pages/LoginPage';
import GruposPage from './pages/GruposPage';
import ParticipantesPage from './pages/ParticipantesPage';
import JogosPage from './pages/JogosPage';
import PagamentosPage from './pages/PagamentosPage';
import AlocacaoPage from './pages/AlocacaoPage';
import ListaPage from './pages/ListaPage';

function ProtectedRoute({ children }: { children: ReactNode }) {
  const { token } = useAuth();

  return token ? <>{children}</> : <Navigate to="/login" replace />;
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route
        element={
          <ProtectedRoute>
            <Layout />
          </ProtectedRoute>
        }
      >
        <Route path="/" element={<Navigate to="/grupos" replace />} />
        <Route path="/grupos" element={<GruposPage />} />
        <Route path="/participantes" element={<ParticipantesPage />} />
        <Route path="/jogos" element={<JogosPage />} />
        <Route path="/pagamentos" element={<PagamentosPage />} />
        <Route path="/alocacao" element={<AlocacaoPage />} />
        <Route path="/lista" element={<ListaPage />} />
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
