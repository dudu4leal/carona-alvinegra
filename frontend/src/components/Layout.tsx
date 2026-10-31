import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export default function Layout() {
  const { logout } = useAuth();
  const navigate = useNavigate();

  function sair() {
    logout();
    navigate('/login', { replace: true });
  }

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    isActive ? 'ativo' : undefined;

  return (
    <div className="app">
      <header className="topo">
        <strong>Carona Alvinegra</strong>

        <nav>
          <NavLink to="/grupos" className={linkClass}>Grupos</NavLink>
          <NavLink to="/participantes" className={linkClass}>Participantes</NavLink>
          <NavLink to="/jogos" className={linkClass}>Jogos</NavLink>
          <NavLink to="/pagamentos" className={linkClass}>Pagamentos</NavLink>
          <NavLink to="/alocacao" className={linkClass}>Alocação</NavLink>
          <NavLink to="/lista" className={linkClass}>Lista oficial</NavLink>
        </nav>

        <button type="button" onClick={sair}>Sair</button>
      </header>

      <main className="conteudo">
        <Outlet />
      </main>
    </div>
  );
}
