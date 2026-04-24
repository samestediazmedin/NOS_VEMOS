import { useEffect, useState } from "react";
import { getDashboardMetrics } from "../../services/api";
import type { DashboardMetrics } from "../../types";

export function DashboardPage({ token }: { token?: string }) {
  const [metrics, setMetrics] = useState<DashboardMetrics | null>(null);

  useEffect(() => {
    getDashboardMetrics(token).then(setMetrics);
  }, [token]);

  if (!metrics) {
    return <p>Cargando dashboard...</p>;
  }

  return (
    <div className="grid">
      <article className="card">
        <h3>Reconocimientos hoy</h3>
        <p className="kpi">{metrics.recognitionsToday}</p>
      </article>
      <article className="card">
        <h3>Activaciones exitosas</h3>
        <p className="kpi good">{metrics.successfulActivations}</p>
      </article>
      <article className="card">
        <h3>Intentos fallidos</h3>
        <p className="kpi bad">{metrics.failedAttempts}</p>
      </article>
      <article className="card">
        <h3>Latencia promedio</h3>
        <p className="kpi">{metrics.avgLatencyMs} ms</p>
      </article>

      <article className="card span2">
        <h3>Estado operativo</h3>
        <div className="status-row">
          <span>Camara</span>
          <strong>{metrics.cameraOnline ? "Online" : "Offline"}</strong>
        </div>
        <div className="status-row">
          <span>Motor IA</span>
          <strong>{metrics.iaOnline ? "Online" : "Offline"}</strong>
        </div>
        <div className="status-row">
          <span>Cola eventos</span>
          <strong>{metrics.queueDepth}</strong>
        </div>
      </article>
      <article className="card span2">
        <h3>Objetivo del turno</h3>
        <p>
          Priorizar flujo de camara: enrolamiento de 3 fotos con calidad minima, reconocimiento menor a 2 segundos y
          activacion segura de dispositivo.
        </p>
      </article>
    </div>
  );
}
