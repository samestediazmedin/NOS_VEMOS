import { useMemo, useState, type FormEvent } from "react";
import type { AppModule, Role } from "../types";
import { DashboardPage } from "./pages/DashboardPage";
import { RecognitionPage } from "./pages/RecognitionPage";
import { HistoryPage } from "./pages/HistoryPage";
import { AuditPage } from "./pages/AuditPage";
import { UsersAdminPage } from "./pages/UsersAdminPage";

const ROLE_STORAGE_KEY = "nosvemos.role";

function isRole(value: string | null): value is Role {
  return value === "Admin";
}

function Login({ onLogin }: { onLogin: (role: Role) => void }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const canLogin = email.trim().length > 0 && password.trim().length > 0;

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!canLogin) return;

    const validEmail = email.trim().toLowerCase() === "admin@nosvemos.local";
    const validPassword = password === "Admin123*";
    if (!validEmail || !validPassword) {
      setError("Credenciales invalidas. Usa la cuenta administradora del proyecto.");
      return;
    }

    setError("");
    onLogin("Admin");
  };

  return (
    <div className="login-shell">
      <div className="login-card">
        <span className="login-chip">Modo demo</span>
        <h1>NOS VEMOS</h1>
        <p>Panel centralizado de administracion, usuarios y control biometrico.</p>

        <form className="login-form" onSubmit={handleSubmit}>
          <span className="role-title">Perfil habilitado: Administrador</span>

          <label htmlFor="email">Correo</label>
          <input
            id="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="admin@nosvemos.local"
            autoComplete="email"
          />

          <label htmlFor="password">Contrasena</label>
          <input
            id="password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="********"
            autoComplete="current-password"
          />

          <button type="submit" className="btn-primary" disabled={!canLogin}>
            Entrar
          </button>
        </form>

        {error ? <small className="bad">{error}</small> : null}
        <small>Cuenta administradora: admin@nosvemos.local</small>
      </div>
    </div>
  );
}

const modules: Array<{ id: AppModule; label: string }> = [
  { id: "dashboard", label: "Dashboard" },
  { id: "usuarios", label: "Gestion de usuarios" },
  { id: "reconocimiento", label: "Reconocimiento Vivo" },
  { id: "historial", label: "Historial" },
  { id: "auditoria", label: "Auditoria" }
];

export function App() {
  const [role, setRole] = useState<Role | null>(() => {
    const storedRole = localStorage.getItem(ROLE_STORAGE_KEY);
    return isRole(storedRole) ? storedRole : null;
  });
  const [token] = useState("demo-token");
  const [module, setModule] = useState<AppModule>("dashboard");

  const handleLogin = (nextRole: Role) => {
    setRole(nextRole);
    setModule("dashboard");
    localStorage.setItem(ROLE_STORAGE_KEY, nextRole);
  };

  const handleLogout = () => {
    setRole(null);
    setModule("dashboard");
    localStorage.removeItem(ROLE_STORAGE_KEY);
  };

  const title = useMemo(() => modules.find((entry) => entry.id === module)?.label ?? "NOS VEMOS", [module]);

  if (!role) {
    return <Login onLogin={handleLogin} />;
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <strong>NOS VEMOS</strong>
          <span>{role}</span>
        </div>
        <nav>
          {modules.map((item) => (
            <button
              key={item.id}
              className={item.id === module ? "nav-btn active" : "nav-btn"}
              onClick={() => setModule(item.id)}
            >
              {item.label}
            </button>
          ))}
        </nav>
      </aside>

      <section className="content">
        <header className="topbar">
          <h2>{title}</h2>
          <div className="topbar-right">
            <span className="status-pill pill-info">Sesion activa</span>
            <button className="ghost" onClick={handleLogout}>
              Salir
            </button>
          </div>
        </header>

        <main>
          {module === "dashboard" && <DashboardPage token={token} />}
          {module === "usuarios" && <UsersAdminPage token={token} />}
          {module === "reconocimiento" && <RecognitionPage />}
          {module === "historial" && <HistoryPage token={token} />}
          {module === "auditoria" && <AuditPage token={token} />}
        </main>
      </section>
    </div>
  );
}
