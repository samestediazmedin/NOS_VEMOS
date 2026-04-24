import type { DashboardMetrics, EnrollmentRecord, RecognitionEvent } from "../types";

const BASE_URL = "http://localhost:7000";
const LOCAL_KEY = "nosvemos.local.recognition-events";

async function safeFetch<T>(path: string, token?: string): Promise<T | null> {
  try {
    const headers: Record<string, string> = {};
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }
    const response = await fetch(`${BASE_URL}${path}`, { headers });
    if (!response.ok) {
      return null;
    }
    return (await response.json()) as T;
  } catch {
    return null;
  }
}

async function safePostForm<T>(path: string, formData: FormData, token?: string): Promise<T | null> {
  try {
    const headers: Record<string, string> = {};
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }
    const response = await fetch(`${BASE_URL}${path}`, {
      method: "POST",
      headers,
      body: formData
    });
    if (!response.ok) {
      return null;
    }
    return (await response.json()) as T;
  } catch {
    return null;
  }
}

function loadLocalEvents(): RecognitionEvent[] {
  try {
    const raw = localStorage.getItem(LOCAL_KEY);
    if (!raw) return [];
    return JSON.parse(raw) as RecognitionEvent[];
  } catch {
    return [];
  }
}

function saveLocalEvents(events: RecognitionEvent[]): void {
  localStorage.setItem(LOCAL_KEY, JSON.stringify(events.slice(0, 200)));
}

function parseAuditPayload(payload: string): { userId: string | null; userName: string | null; score: number; reasonCode: string } {
  try {
    const data = JSON.parse(payload) as Record<string, unknown>;
    const biometria = (data.biometria ?? {}) as Record<string, unknown>;
    const userName = typeof biometria.usuarioDetectado === "string" ? biometria.usuarioDetectado : null;
    const userId = userName ? `USR-${userName.slice(0, 4).toUpperCase()}` : null;
    const score = typeof biometria.confianzaRostro === "number" ? biometria.confianzaRostro : 0;
    return { userId, userName, score, reasonCode: "AUDIT_EVENT" };
  } catch {
    return { userId: null, userName: null, score: 0, reasonCode: "AUDIT_RAW" };
  }
}

const mockEvents: RecognitionEvent[] = [
  {
    id: "evt-1001",
    createdAt: new Date(Date.now() - 1000 * 60 * 5).toISOString(),
    userId: "USR-0089",
    userName: "Juan Martinez",
    score: 0.93,
    threshold: 0.85,
    result: "match",
    deviceId: "DISP-NOR-001",
    latencyMs: 840,
    reasonCode: "FACE_MATCH_OK"
  },
  {
    id: "evt-1002",
    createdAt: new Date(Date.now() - 1000 * 60 * 12).toISOString(),
    userId: null,
    userName: null,
    score: 0.52,
    threshold: 0.85,
    result: "no_match",
    deviceId: "DISP-NOR-001",
    latencyMs: 1188,
    reasonCode: "FACE_NOT_FOUND"
  },
  {
    id: "evt-1003",
    createdAt: new Date(Date.now() - 1000 * 60 * 20).toISOString(),
    userId: "USR-0022",
    userName: "Ana Torres",
    score: 0.72,
    threshold: 0.85,
    result: "fallback",
    deviceId: "DISP-SUR-003",
    latencyMs: 1610,
    reasonCode: "MAX_ATTEMPTS_FALLBACK"
  }
];

const mockEnrollments: EnrollmentRecord[] = [
  {
    id: "enr-501",
    userId: "USR-0089",
    userName: "Juan Martinez",
    status: "biometria_activa",
    frontalScore: 92,
    leftScore: 89,
    rightScore: 90,
    approvedBy: "Admin Principal",
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24).toISOString()
  },
  {
    id: "enr-502",
    userId: "USR-0102",
    userName: "Maria Lopez",
    status: "biometria_rechazada",
    frontalScore: 58,
    leftScore: 61,
    rightScore: 57,
    approvedBy: "Admin Principal",
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 40).toISOString()
  }
];

export async function getRecognitionHistory(token?: string): Promise<RecognitionEvent[]> {
  const remote = await safeFetch<{ eventos: Array<{ id: string; fecha: string; routingKey: string; payload: string }> }>(
    "/api/v1/auditoria/eventos?modulo=ia&limit=100",
    token
  );

  const parsed =
    remote?.eventos.map((entry) => {
      const fields = parseAuditPayload(entry.payload);
      const result: RecognitionEvent["result"] =
        entry.routingKey === "ia.rostro.reconocido" ? (fields.score >= 0.85 ? "match" : "no_match") : "fallback";
      return {
        id: entry.id,
        createdAt: entry.fecha,
        userId: fields.userId,
        userName: fields.userName,
        score: fields.score,
        threshold: 0.85,
        result,
        deviceId: "DISP-NOR-001",
        latencyMs: 900,
        reasonCode: fields.reasonCode
      };
    }) ?? [];

  const local = loadLocalEvents();
  const merged = [...local, ...parsed];
  if (merged.length > 0) {
    return merged.sort((a, b) => Number(new Date(b.createdAt)) - Number(new Date(a.createdAt))).slice(0, 100);
  }
  return mockEvents;
}

export async function getEnrollmentHistory(): Promise<EnrollmentRecord[]> {
  return mockEnrollments;
}

export async function getDashboardMetrics(token?: string): Promise<DashboardMetrics> {
  const events = await getRecognitionHistory(token);
  const matches = events.filter((event) => event.result === "match");
  const failures = events.filter((event) => event.result !== "match");
  const avgLatency = events.length
    ? Math.round(events.reduce((acc, event) => acc + event.latencyMs, 0) / events.length)
    : 0;

  return {
    recognitionsToday: events.length,
    successfulActivations: matches.length,
    failedAttempts: failures.length,
    avgLatencyMs: avgLatency,
    cameraOnline: true,
    iaOnline: true,
    queueDepth: 2
  };
}

export interface CameraAnalysisResponse {
  id: string;
  fecha: string;
  evaluacion: { nivelRiesgo: string; recomendacion: string };
  biometria: { usuarioEsperado: string; usuarioDetectado: string; confianzaRostro: number };
  sensor: { distanciaCm: number; alertaProximidad: boolean };
}

export async function analyzeCameraFrame(params: {
  blob: Blob;
  usuarioEsperado: string;
  usuarioDetectado: string;
  confianzaRostro: number;
  distanciaCm?: number;
  token?: string;
}): Promise<CameraAnalysisResponse | null> {
  const formData = new FormData();
  formData.append("frame", params.blob, "captura.jpg");
  formData.append("usuarioEsperado", params.usuarioEsperado);
  formData.append("usuarioDetectado", params.usuarioDetectado);
  formData.append("confianzaRostro", String(params.confianzaRostro));
  formData.append("distanciaCm", String(params.distanciaCm ?? 0));

  return safePostForm<CameraAnalysisResponse>("/api/v1/ia/analizar-camara?contexto=camara_movil", formData, params.token);
}

export function appendLocalRecognitionEvent(event: RecognitionEvent): void {
  const events = loadLocalEvents();
  events.unshift(event);
  saveLocalEvents(events);
}
