import { useState } from "react";
import { login } from "../services/authService";
import "./login.css";

export default function Login() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const handleLogin = async () => {
    try {
      await login(email, password);
      window.location.href = "/dashboard";
    } catch {
      alert("Credenciales inválidas");
    }
  };

  return (
    <div className="login-container">

      <div className="login-card">
        <h2>LOGIN</h2>
        <p>Plataforma empresarial</p>

        <input
          placeholder="correo@empresa.com"
          onChange={(e) => setEmail(e.target.value)}
        />

        <input
          type="password"
          placeholder="********"
          onChange={(e) => setPassword(e.target.value)}
        />

        <button onClick={handleLogin}>Ingresar</button>
      </div>

    </div>
  );
}