import { useEffect, useMemo, useState } from "react";
import { getEnrollmentHistory, getRecognitionHistory } from "../../services/api";
import type { EnrollmentRecord, RecognitionEvent } from "../../types";

export function HistoryPage({ token }: { token?: string }) {
  const [events, setEvents] = useState<RecognitionEvent[]>([]);
  const [enrollments, setEnrollments] = useState<EnrollmentRecord[]>([]);
  const [search, setSearch] = useState("");

  useEffect(() => {
    getRecognitionHistory(token).then(setEvents);
    getEnrollmentHistory().then(setEnrollments);
  }, [token]);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return events;
    return events.filter((event) => {
      const text = `${event.userId ?? ""} ${event.userName ?? ""} ${event.deviceId}`.toLowerCase();
      return text.includes(term);
    });
  }, [events, search]);

  return (
    <div className="stack">
      <section className="card">
        <h3>Historial de reconocimientos</h3>
        <input
          placeholder="Buscar por usuario o dispositivo"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Fecha</th>
                <th>Usuario</th>
                <th>Resultado</th>
                <th>Score</th>
                <th>Latencia</th>
                <th>Dispositivo</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((event) => (
                <tr key={event.id}>
                  <td>{new Date(event.createdAt).toLocaleString()}</td>
                  <td>{event.userName ?? "Desconocido"}</td>
                  <td>{event.result}</td>
                  <td>{(event.score * 100).toFixed(1)}%</td>
                  <td>{event.latencyMs} ms</td>
                  <td>{event.deviceId}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="card">
        <h3>Historial de enrolamientos</h3>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Usuario</th>
                <th>Estado</th>
                <th>Frontal</th>
                <th>Izquierda</th>
                <th>Derecha</th>
                <th>Aprobado por</th>
              </tr>
            </thead>
            <tbody>
              {enrollments.map((entry) => (
                <tr key={entry.id}>
                  <td>{entry.userName}</td>
                  <td>{entry.status}</td>
                  <td>{entry.frontalScore}%</td>
                  <td>{entry.leftScore}%</td>
                  <td>{entry.rightScore}%</td>
                  <td>{entry.approvedBy}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
