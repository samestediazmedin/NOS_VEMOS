import { useState } from "react";
import Layout from "../../../components/layout/Layout";
import { analizar } from "../services/iaService";

export default function IA() {
  const [resultado, setResultado] = useState(null);

  const handleFile = async (e) => {
    const file = e.target.files[0];
    const res = await analizar(file);
    setResultado(res);
  };

  return (
    <Layout>
      <h2 className="text-xl mb-4">IA Cámara</h2>

      <input type="file" onChange={handleFile} />

      {resultado && (
        <div className="mt-4">
          <p>Riesgo: {resultado.riesgo}</p>
          <p>{resultado.recomendacion}</p>
        </div>
      )}
    </Layout>
  );
}