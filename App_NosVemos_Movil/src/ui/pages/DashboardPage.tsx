import { useEffect, useState } from "react";
import { getDashboardMetrics } from "../../services/api";
import type { DashboardMetrics } from "../../types";

export function DashboardPage({ token }: { token?: string }) {
  const [metrics, setMetrics] = useState<DashboardMetrics | null>(null);

  useEffect(() => {
    getDashboardMetrics(token).then(setMetrics);
  }, [token]);

  if (!metrics) {
    return <div className="empty-state">Cargando dashboard operativo...</div>;
  }

  return (
    <div>
      <p className="page-intro">Vista rapida del turno con indicadores de reconocimiento, latencia y salud del sistema.</p>
      <div className="grid">
        <article className="card">
        <h3>Reconocimientos hoy</h3>
        <p className="kpi">{metrics.recognitionsToday}</p>
        <p className="metric-subtle">Eventos procesados en las ultimas horas.</p>
      </article>
      <article className="card">
        <h3>Activaciones exitosas</h3>
        <p className="kpi good">{metrics.successfulActivations}</p>
        <p className="metric-subtle">Coincidencias que habilitan acceso.</p>
      </article>
      <article className="card">
        <h3>Intentos fallidos</h3>
        <p className="kpi bad">{metrics.failedAttempts}</p>
        <p className="metric-subtle">Eventos no validados o fallback.</p>
      </article>
      <article className="card">
        <h3>Latencia promedio</h3>
        <p className="kpi">{metrics.avgLatencyMs} ms</p>
        <p className="metric-subtle">Tiempo desde captura hasta respuesta.</p>
      </article>

      <article className="card span2">
        <h3>Estado operativo</h3>
        <div className="status-row">
          <span>Camara</span>
          <strong className={metrics.cameraOnline ? "good" : "bad"}>{metrics.cameraOnline ? "Online" : "Offline"}</strong>
        </div>
        <div className="status-row">
          <span>Motor IA</span>
          <strong className={metrics.iaOnline ? "good" : "bad"}>{metrics.iaOnline ? "Online" : "Offline"}</strong>
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
    </div>
  );
}
