import { Link } from "react-router-dom";

export default function Sidebar() {
  return (
    <div className="w-64 bg-gray-900 text-white flex flex-col p-4">
      <h2 className="text-2xl font-bold mb-8">NOS VEMOS</h2>

      <nav className="space-y-3">
        <Link to="/dashboard" className="block hover:bg-gray-700 p-2 rounded">
          📊 Dashboard
        </Link>

        <Link to="/usuarios" className="block hover:bg-gray-700 p-2 rounded">
          👤 Usuarios
        </Link>

        <Link to="/expedientes" className="block hover:bg-gray-700 p-2 rounded">
          📁 Expedientes
        </Link>

        <Link to="/ia" className="block hover:bg-gray-700 p-2 rounded">
          🤖 IA
        </Link>
      </nav>
    </div>
  );
}