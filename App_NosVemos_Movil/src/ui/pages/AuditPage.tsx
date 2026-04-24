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
      <p>
        Esta vista resume los eventos trazables para cumplimiento: quien fue detectado, con que score, en que
        dispositivo y cual fue la decision final.
      </p>
      <ul className="audit-list">
        {events.map((event) => (
          <li key={event.id}>
            <strong>{new Date(event.createdAt).toLocaleTimeString()}</strong> - {event.userName ?? "Desconocido"} -{" "}
            {event.result} - {event.reasonCode}
          </li>
        ))}
      </ul>
    </section>
  );
}
