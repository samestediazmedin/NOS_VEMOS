import { useEffect, useState } from "react";
import Layout from "../../../components/layout/Layout";
import { getUsuarios } from "../services/usuariosService";

export default function Usuarios() {
  const [usuarios, setUsuarios] = useState([]);

  useEffect(() => {
    getUsuarios().then(setUsuarios);
  }, []);

  return (
    <Layout>
      <h2 className="text-xl mb-4">Usuarios</h2>

      <ul>
        {usuarios.map((u) => (
          <li key={u.id}>{u.email}</li>
        ))}
      </ul>
    </Layout>
  );
}