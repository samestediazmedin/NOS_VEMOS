export type Role = "Admin";

export type AppModule =
  | "dashboard"
  | "usuarios"
  | "reconocimiento"
  | "historial"
  | "auditoria";

export interface ManagedUser {
  id: string;
  nombre: string;
  email: string;
  activo: boolean;
  source: "api" | "local";
  biometricStatus: "pendiente_biometria" | "biometria_activa";
  createdAt?: string;
}

export type FaceAngle = "frontal" | "izquierda" | "derecha";

export interface BiometricCapture {
  angle: FaceAngle;
  quality: number;
  createdAt: string;
}

export interface RecognitionEvent {
  id: string;
  createdAt: string;
  userId: string | null;
  userName: string | null;
  score: number;
  threshold: number;
  result: "match" | "no_match" | "fallback";
  deviceId: string;
  latencyMs: number;
  reasonCode: string;
}

export interface EnrollmentRecord {
  id: string;
  userId: string;
  userName: string;
  status: "biometria_activa" | "biometria_rechazada" | "enrolamiento_en_progreso";
  frontalScore: number;
  leftScore: number;
  rightScore: number;
  approvedBy: string;
  createdAt: string;
}

export interface DashboardMetrics {
  recognitionsToday: number;
  successfulActivations: number;
  failedAttempts: number;
  avgLatencyMs: number;
  cameraOnline: boolean;
  iaOnline: boolean;
  queueDepth: number;
}
