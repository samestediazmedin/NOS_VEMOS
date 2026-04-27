import { useEffect, useRef, useState } from "react";
import { analyzeCameraFrame, appendLocalRecognitionEvent } from "../../services/api";

type MatchState = "idle" | "processing" | "match" | "no-match";

export function RecognitionPage() {
  const [state, setState] = useState<MatchState>("idle");
  const [score, setScore] = useState(0);
  const [attempts, setAttempts] = useState(0);
  const [status, setStatus] = useState("Camara detenida");
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const streamRef = useRef<MediaStream | null>(null);

  useEffect(() => {
    return () => {
      streamRef.current?.getTracks().forEach((track) => track.stop());
    };
  }, []);

  async function startCamera(): Promise<void> {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: "user" }, audio: false });
      streamRef.current = stream;
      if (videoRef.current) {
        videoRef.current.srcObject = stream;
      }
      setStatus("Camara activa");
    } catch (error) {
      setStatus(`No se pudo iniciar camara: ${(error as Error).message}`);
    }
  }

  async function runRecognition(): Promise<void> {
    setState("processing");
    const video = videoRef.current;
    const canvas = canvasRef.current;
    if (!video || !canvas || !video.videoWidth) {
      setState("idle");
      setStatus("Inicia la camara antes de reconocer");
      return;
    }

    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    const context = canvas.getContext("2d");
    if (!context) return;
    context.drawImage(video, 0, 0, canvas.width, canvas.height);
    const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, "image/jpeg", 0.9));
    if (!blob) {
      setState("idle");
      setStatus("No se pudo generar captura");
      return;
    }

    const start = performance.now();
    const response = await analyzeCameraFrame({
      blob,
      usuarioEsperado: "juan.martinez",
      usuarioDetectado: "juan.martinez",
      confianzaRostro: Number((0.7 + Math.random() * 0.28).toFixed(2)),
      distanciaCm: 46
    });
    const latencyMs = Math.round(performance.now() - start);

    const value = response?.biometria?.confianzaRostro ?? Number(Math.random().toFixed(2));
    setScore(value);
    if (value >= 0.85) {
      setState("match");
      setAttempts(0);
      setStatus("Acceso aprobado");
    } else {
      setState("no-match");
      setAttempts((prev) => prev + 1);
      setStatus("Sin coincidencia, revisar fallback");
    }

    appendLocalRecognitionEvent({
      id: crypto.randomUUID(),
      createdAt: new Date().toISOString(),
      userId: "USR-0089",
      userName: "Juan Martinez",
      score: value,
      threshold: 0.85,
      result: value >= 0.85 ? "match" : attempts + 1 >= 3 ? "fallback" : "no_match",
      deviceId: "DISP-NOR-001",
      latencyMs,
      reasonCode: response ? "API_ANALYSIS" : "LOCAL_SIMULATION"
    });
  }

  return (
    <div className="grid split">
      <section className="card">
        <h3>Reconocimiento en vivo</h3>
        <p className="page-intro">Valida identidad en tiempo real y ejecuta fallback cuando superas intentos fallidos.</p>
        <div className="camera-box">
          <div className={state === "match" ? "frame ok" : state === "no-match" ? "frame bad" : "frame"} />
          <span>{state === "processing" ? "Analizando rostro..." : "Camara activa"}</span>
          <video ref={videoRef} autoPlay playsInline className="camera-video" />
          <canvas ref={canvasRef} hidden />
        </div>
        <span className="status-pill pill-info">{status}</span>
        <div className="actions">
          <button type="button" className="btn-secondary" onClick={startCamera}>
            Iniciar camara
          </button>
          <button className="btn-primary" onClick={runRecognition} disabled={state === "processing" || attempts >= 3}>
            Iniciar reconocimiento
          </button>
        </div>
      </section>

      <section className="card">
        <h3>Resultado</h3>
        <div className="status-row">
          <span>Umbral</span>
          <strong>0.85</strong>
        </div>
        <div className="status-row">
          <span>Score</span>
          <strong>{score}</strong>
        </div>
        <div className="status-row">
          <span>Intentos fallidos</span>
          <strong>{attempts}/3</strong>
        </div>
        {state === "match" && <p className="ok">Match valido: activar dispositivo.</p>}
        {state === "no-match" && <p className="warn">No coincide: reintentar o aplicar fallback seguro.</p>}
        {attempts >= 3 && <p className="bad">Bloqueado: usar PIN o credencial secundaria.</p>}
      </section>
    </div>
  );
}
