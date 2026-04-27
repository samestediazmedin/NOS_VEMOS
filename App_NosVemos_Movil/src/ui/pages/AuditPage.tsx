import { useEffect, useState } from "react";
import { getRecognitionHistory } from "../../services/api";
import type { RecognitionEvent } from "../../types";

export function AuditPage({ token }: { token?: string }) {
  const [events, setEvents] = useState<RecognitionEvent[]>([]);

  useEffect(() => {
    getRecognitionHistory(token).then(setEvents);
  }, [token]);

  return (
    <section className="card">
      <h3>Auditoria biometrica</h3>
      <p className="page-intro">Linea de tiempo de eventos para evidencias de cumplimiento y revisiones operativas.</p>
      <p>
        Esta vista resume los eventos trazables para cumplimiento: quien fue detectado, con que score, en que
        dispositivo y cual fue la decision final.
      </p>
      {events.length === 0 ? (
        <div className="empty-state">No hay eventos de auditoria disponibles.</div>
      ) : (
        <ul className="audit-list">
          {events.map((event) => (
            <li key={event.id}>
              <strong>{new Date(event.createdAt).toLocaleTimeString()}</strong> - {event.userName ?? "Desconocido"} - {" "}
              <span className={event.result === "match" ? "status-pill pill-ok" : event.result === "fallback" ? "status-pill pill-warn" : "status-pill pill-bad"}>
                {event.result}
              </span>{" "}
              {event.reasonCode}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
