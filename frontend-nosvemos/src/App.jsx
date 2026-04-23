import { BrowserRouter, Routes, Route } from "react-router-dom";
import Login from "./modules/auth/pages/Login";
import Dashboard from "./pages/Dashboard";
import Usuarios from "./modules/usuarios/pages/Usuarios";
import Expedientes from "./modules/expedientes/pages/Expedientes";
import IA from "./modules/ia/pages/IA";
import ProtectedRoute from "./core/routes/ProtectedRoute";

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Login />} />

        <Route
          path="/dashboard"
          element={<ProtectedRoute><Dashboard /></ProtectedRoute>}
        />

        <Route
          path="/usuarios"
          element={<ProtectedRoute><Usuarios /></ProtectedRoute>}
        />

        <Route
          path="/expedientes"
          element={<ProtectedRoute><Expedientes /></ProtectedRoute>}
        />

        <Route
          path="/ia"
          element={<ProtectedRoute><IA /></ProtectedRoute>}
        />
      </Routes>
    </BrowserRouter>
  );
}