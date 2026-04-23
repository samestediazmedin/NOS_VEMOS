import { useEffect, useState } from "react";
import Layout from "../../../components/layout/Layout";
import { getExpedientes } from "../services/expedientesService";

export default function Expedientes() {
  const [data, setData] = useState([]);

  useEffect(() => {
    getExpedientes().then(setData);
  }, []);

  return (
    <Layout>
      <h2 className="text-xl mb-4">Expedientes</h2>

      {data.map((e) => (
        <div key={e.id}>{e.estado}</div>
      ))}
    </Layout>
  );
}