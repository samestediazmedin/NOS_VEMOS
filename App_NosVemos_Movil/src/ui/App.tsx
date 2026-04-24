import { useMemo, useState } from "react";
import type { AppModule, Role } from "../types";
import { DashboardPage } from "./pages/DashboardPage";
import { CapturePage } from "./pages/CapturePage";
import { RecognitionPage } from "./pages/RecognitionPage";
import { HistoryPage } from "./pages/HistoryPage";
import { AuditPage } from "./pages/AuditPage";

function Login({ onLogin }: { onLogin: (role: Role) => void }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  return (
    <div className="login-shell">
      <div className="login-card">
        <h1>NOS VEMOS</h1>
        <p>Control biometrico por camara</p>
        <label>
          Correo
          <input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="admin@nosvemos.local" />
        </label>
        <label>
          Contrasena
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="********" />
        </label>
        <button onClick={() => onLogin("Admin")} disabled={!email || !password}>
          Entrar
        </button>
      </div>
    </div>
  );
}

const modules: Array<{ id: AppModule; label: string }> = [
  { id: "dashboard", label: "Dashboard" },
  { id: "captura", label: "Captura Asistida" },
  { id: "reconocimiento", label: "Reconocimiento Vivo" },
  { id: "historial", label: "Historial" },
  { id: "auditoria", label: "Auditoria" }
];

export function App() {
  const [role, setRole] = useState<Role | null>(null);
  const [token] = useState("demo-token");
  const [module, setModule] = useState<AppModule>("dashboard");

  const title = useMemo(() => modules.find((entry) => entry.id === module)?.label ?? "NOS VEMOS", [module]);

  if (!role) {
    return <Login onLogin={setRole} />;
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
          <button className="ghost" onClick={() => setRole(null)}>
            Salir
          </button>
        </header>

        <main>
          {module === "dashboard" && <DashboardPage token={token} />}
          {module === "captura" && <CapturePage />}
          {module === "reconocimiento" && <RecognitionPage />}
          {module === "historial" && <HistoryPage token={token} />}
          {module === "auditoria" && <AuditPage token={token} />}
        </main>
      </section>
    </div>
  );
}
