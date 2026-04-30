import type { BiometricCapture, DashboardMetrics, EnrollmentRecord, ManagedUser, RecognitionEvent } from "../types";

const BASE_URL = "http://localhost:7000";
const LOCAL_KEY = "nosvemos.local.recognition-events";
const LOCAL_USERS_KEY = "nosvemos.local.managed-users";
const LOCAL_BIOMETRIC_KEY = "nosvemos.local.biometric-enrollments";

interface LocalBiometricEnrollment {
  userEmail: string;
  userName: string;
  captures: BiometricCapture[];
  estado: ManagedUser["biometricStatus"];
  createdAt: string;
  approvedBy: string;
}

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

async function safePostJson<T>(path: string, body: object, token?: string): Promise<T | null> {
  try {
    const headers: Record<string, string> = { "Content-Type": "application/json" };
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }
    const response = await fetch(`${BASE_URL}${path}`, {
      method: "POST",
      headers,
      body: JSON.stringify(body)
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

function loadLocalUsers(): ManagedUser[] {
  try {
    const raw = localStorage.getItem(LOCAL_USERS_KEY);
    if (!raw) return [];
    return JSON.parse(raw) as ManagedUser[];
  } catch {
    return [];
  }
}

function saveLocalUsers(users: ManagedUser[]): void {
  localStorage.setItem(LOCAL_USERS_KEY, JSON.stringify(users.slice(0, 200)));
}

function loadLocalBiometricEnrollments(): LocalBiometricEnrollment[] {
  try {
    const raw = localStorage.getItem(LOCAL_BIOMETRIC_KEY);
    if (!raw) return [];
    return JSON.parse(raw) as LocalBiometricEnrollment[];
  } catch {
    return [];
  }
}

function saveLocalBiometricEnrollments(entries: LocalBiometricEnrollment[]): void {
  localStorage.setItem(LOCAL_BIOMETRIC_KEY, JSON.stringify(entries.slice(0, 200)));
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

const mockManagedUsers: ManagedUser[] = [
  {
    id: "usr-admin-1",
    nombre: "Administrador General",
    email: "admin@nosvemos.local",
    activo: true,
    source: "local",
    biometricStatus: "biometria_activa",
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 72).toISOString()
  },
  {
    id: "usr-demo-1",
    nombre: "Juan Martinez",
    email: "juan@nosvemos.local",
    activo: true,
    source: "local",
    biometricStatus: "biometria_activa",
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 36).toISOString()
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
  biometria: { usuarioEsperado: string; usuarioDetectado: string; usuarioDetectadoNombre?: string; confianzaRostro: number };
  seguridad?: {
    usuarioDetectado: string | null;
    confianza: number;
    segundaMejorConfianza: number;
    umbralExactitud: number;
    margenMinimo: number;
    coincide: boolean;
    esExacto: boolean;
    accesoPermitido: boolean;
    motivo: string;
  };
  sensor: { distanciaCm: number; alertaProximidad: boolean };
}

export async function analyzeCameraFrame(params: {
  blob: Blob;
  usuarioEsperado: string;
  usuarioDetectado: string;
  confianzaRostro: number;
  distanciaCm?: number;
  umbralReconocimiento?: number;
  token?: string;
}): Promise<CameraAnalysisResponse | null> {
  const formData = new FormData();
  formData.append("frame", params.blob, "captura.jpg");
  formData.append("usuarioEsperado", params.usuarioEsperado);
  formData.append("usuarioDetectado", params.usuarioDetectado);
  formData.append("confianzaRostro", String(params.confianzaRostro));
  formData.append("distanciaCm", String(params.distanciaCm ?? 0));
  formData.append("umbralReconocimiento", String(params.umbralReconocimiento ?? 0.82));

  return safePostForm<CameraAnalysisResponse>("/api/v1/ia/analizar-camara?contexto=camara_movil", formData, params.token);
}

export async function enrollBiometricSample(params: {
  blob: Blob;
  userId: string;
  userName: string;
  angle: "frontal" | "izquierda" | "derecha";
  quality: number;
  token?: string;
}): Promise<boolean> {
  const formData = new FormData();
  formData.append("frame", params.blob, "enrolamiento.jpg");
  formData.append("userId", params.userId);
  formData.append("userName", params.userName);
  formData.append("angle", params.angle);
  formData.append("quality", String(params.quality));

  const response = await safePostForm<{ status: string }>("/api/v1/ia/enrolar-rostro", formData, params.token);
  return response?.status === "enrolled";
}

export function appendLocalRecognitionEvent(event: RecognitionEvent): void {
  const events = loadLocalEvents();
  events.unshift(event);
  saveLocalEvents(events);
}

export async function getManagedUsers(token?: string): Promise<ManagedUser[]> {
  const enrollmentByEmail = new Map(
    loadLocalBiometricEnrollments().map((entry) => [entry.userEmail.toLowerCase(), entry.estado] as const)
  );

  const remote = await safeFetch<Array<{ id: string; nombre: string; email: string; activo: boolean; createdAt?: string }>>("/api/v1/usuarios", token);
  if (remote && remote.length > 0) {
    return remote.map((item) => ({
      ...item,
      source: "api",
      biometricStatus: enrollmentByEmail.get(item.email.toLowerCase()) ?? "pendiente_biometria",
      createdAt: item.createdAt
    }));
  }

  const localUsers = loadLocalUsers();
  if (localUsers.length > 0) {
    return localUsers;
  }

  saveLocalUsers(mockManagedUsers);
  return mockManagedUsers;
}

export async function createManagedUser(
  payload: { nombre: string; email: string; biometricStatus?: ManagedUser["biometricStatus"] },
  token?: string
): Promise<{ ok: boolean; message: string }> {
  const remote = await safePostJson<{ id: string }>("/api/v1/usuarios", { Nombre: payload.nombre, Email: payload.email }, token);
  if (remote?.id) {
    return { ok: true, message: "Usuario creado en API de Usuarios." };
  }

  const localUsers = loadLocalUsers();
  const exists = localUsers.some((entry) => entry.email.toLowerCase() === payload.email.toLowerCase());
  if (exists) {
    return { ok: false, message: "Ya existe un usuario con ese correo." };
  }

  localUsers.unshift({
    id: crypto.randomUUID(),
    nombre: payload.nombre,
    email: payload.email,
    activo: true,
    source: "local",
    biometricStatus: payload.biometricStatus ?? "pendiente_biometria",
    createdAt: new Date().toISOString()
  });
  saveLocalUsers(localUsers);
  return { ok: true, message: "Usuario creado en almacenamiento local de demo." };
}

export function saveUserBiometricEnrollment(payload: {
  userEmail: string;
  userName: string;
  captures: BiometricCapture[];
  approvedBy: string;
}): void {
  const enrollments = loadLocalBiometricEnrollments();
  const filtered = enrollments.filter((item) => item.userEmail.toLowerCase() !== payload.userEmail.toLowerCase());
  filtered.unshift({
    userEmail: payload.userEmail,
    userName: payload.userName,
    captures: payload.captures,
    estado: "biometria_activa",
    createdAt: new Date().toISOString(),
    approvedBy: payload.approvedBy
  });
  saveLocalBiometricEnrollments(filtered);

  const users = loadLocalUsers();
  const index = users.findIndex((item) => item.email.toLowerCase() === payload.userEmail.toLowerCase());
  if (index >= 0) {
    users[index] = { ...users[index], biometricStatus: "biometria_activa" };
    saveLocalUsers(users);
  }
}

export interface SensorTriggerEvent {
  id: string;
  createdAt: string;
  distanceCm: number;
}

export async function getLatestSensorTrigger(token?: string): Promise<SensorTriggerEvent | null> {
  const remote = await safeFetch<{
    eventos: Array<{ id: string; fecha: string; routingKey: string; payload: string }>;
  }>("/api/v1/auditoria/eventos?routingKey=sensor.proximidad.detectada&limit=1", token);

  const first = remote?.eventos?.[0];
  if (!first) {
    return null;
  }

  let distanceCm = 0;
  try {
    const data = JSON.parse(first.payload) as { distanciaCm?: number };
    distanceCm = typeof data.distanciaCm === "number" ? data.distanciaCm : 0;
  } catch {
    distanceCm = 0;
  }

  return {
    id: first.id,
    createdAt: first.fecha,
    distanceCm
  };
}
