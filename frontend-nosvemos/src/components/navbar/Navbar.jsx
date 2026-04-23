import { logout } from "../../core/auth/authStore";

export default function Navbar() {
  return (
    <div className="bg-white shadow p-4 flex justify-between items-center">
      <h1 className="font-semibold text-gray-700">Dashboard</h1>

      <button
        onClick={logout}
        className="text-red-500 font-semibold"
      >
        Cerrar sesión
      </button>
    </div>
  );
}