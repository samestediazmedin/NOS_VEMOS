import { useEffect, useMemo, useState } from "react";
import { getEnrollmentHistory, getManagedUsers, getRecognitionHistory } from "../../services/api";
import type { EnrollmentRecord, ManagedUser, RecognitionEvent } from "../../types";

function resultClass(result: RecognitionEvent["result"]): string {
  if (result === "match") return "status-pill pill-ok";
  if (result === "fallback") return "status-pill pill-warn";
  return "status-pill pill-bad";
}

function enrollmentClass(status: string): string {
  if (status === "biometria_activa") return "status-pill pill-ok";
  if (status === "enrolamiento_en_progreso") return "status-pill pill-info";
  return "status-pill pill-warn";
}

export function HistoryPage({ token }: { token?: string }) {
  const [events, setEvents] = useState<RecognitionEvent[]>([]);
  const [enrollments, setEnrollments] = useState<EnrollmentRecord[]>([]);
  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [search, setSearch] = useState("");

  useEffect(() => {
    getRecognitionHistory(token).then(setEvents);
    getEnrollmentHistory().then(setEnrollments);
    getManagedUsers(token).then(setUsers);
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
        <p className="page-intro">Consulta eventos con trazabilidad por usuario, dispositivo y tiempo de respuesta.</p>
        <input
          placeholder="Buscar por usuario o dispositivo"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        {filtered.length === 0 ? (
          <div className="empty-state">No hay eventos para el filtro actual.</div>
        ) : (
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
                    <td>
                      <span className={resultClass(event.result)}>{event.result}</span>
                    </td>
                    <td>{(event.score * 100).toFixed(1)}%</td>
                    <td>{event.latencyMs} ms</td>
                    <td>{event.deviceId}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="card">
        <h3>Hora de ingreso por usuario</h3>
        <p className="page-intro">Registro de cuando cada usuario fue dado de alta en el sistema.</p>
        {users.length === 0 ? (
          <div className="empty-state">No hay usuarios registrados para mostrar ingreso.</div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Usuario</th>
                  <th>Correo</th>
                  <th>Hora de ingreso</th>
                </tr>
              </thead>
              <tbody>
                {users.map((user) => (
                  <tr key={user.id}>
                    <td>{user.nombre}</td>
                    <td>{user.email}</td>
                    <td>{user.createdAt ? new Date(user.createdAt).toLocaleString() : "No disponible"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="card">
        <h3>Historial de enrolamientos</h3>
        {enrollments.length === 0 ? (
          <div className="empty-state">No hay enrolamientos registrados.</div>
        ) : (
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
                    <td>
                      <span className={enrollmentClass(entry.status)}>{entry.status}</span>
                    </td>
                    <td>{entry.frontalScore}%</td>
                    <td>{entry.leftScore}%</td>
                    <td>{entry.rightScore}%</td>
                    <td>{entry.approvedBy}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
