import Layout from "../components/layout/Layout";

export default function Dashboard() {
  return (
    <Layout>
      <h2 className="text-2xl font-bold mb-6">Resumen del sistema</h2>

      {/* Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">

        <div className="bg-white p-6 rounded-xl shadow">
          <h3 className="text-gray-500">Usuarios</h3>
          <p className="text-3xl font-bold mt-2">120</p>
        </div>

        <div className="bg-white p-6 rounded-xl shadow">
          <h3 className="text-gray-500">Expedientes</h3>
          <p className="text-3xl font-bold mt-2">58</p>
        </div>

        <div className="bg-white p-6 rounded-xl shadow">
          <h3 className="text-gray-500">Alertas IA</h3>
          <p className="text-3xl font-bold mt-2 text-red-500">12</p>
        </div>

      </div>

      {/* Actividad */}
      <div className="mt-8 bg-white p-6 rounded-xl shadow">
        <h3 className="text-lg font-semibold mb-4">Actividad reciente</h3>

        <ul className="space-y-2 text-gray-600">
          <li>✔ Usuario creado</li>
          <li>✔ Expediente cerrado</li>
          <li>⚠ Alerta IA detectada</li>
        </ul>
      </div>
    </Layout>
  );
}