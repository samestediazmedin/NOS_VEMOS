import { useEffect, useRef, useState } from "react";
import { analyzeCameraFrame, appendLocalRecognitionEvent } from "../../services/api";

type MatchState = "idle" | "processing" | "match" | "no-match";

export function RecognitionPage() {
  const [state, setState] = useState<MatchState>("idle");
  const [score, setScore] = useState(0);
  const [attempts, setAttempts] = useState(0);
  const [status, setStatus] = useState("Camara detenida");
  const [cameraReady, setCameraReady] = useState(false);
  const [lastLatencyMs, setLastLatencyMs] = useState(0);

  const videoRef = useRef<HTMLVideoElement | null>(null);
  const captureCanvasRef = useRef<HTMLCanvasElement | null>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const runningRef = useRef(false);

  useEffect(() => {
    return () => {
      streamRef.current?.getTracks().forEach((track) => track.stop());
    };
  }, []);

  useEffect(() => {
    if (!cameraReady) {
      return;
    }

    const timer = setInterval(async () => {
      if (!streamRef.current?.active || runningRef.current || state === "processing" || state === "match" || attempts >= 3) {
        return;
      }

      runningRef.current = true;
      await runRecognition();
      runningRef.current = false;
    }, 2200);

    return () => clearInterval(timer);
  }, [cameraReady, state, attempts]);

  async function startCamera(): Promise<boolean> {
    if (streamRef.current?.active) {
      setCameraReady(true);
      setStatus("Camara preparada");
      return true;
    }

    try {
      const stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: "user" }, audio: false });
      streamRef.current = stream;
      if (videoRef.current) {
        videoRef.current.srcObject = stream;
      }
      setCameraReady(true);
      setStatus("Camara preparada");
      return true;
    } catch (error) {
      setCameraReady(false);
      setStatus(`No se pudo iniciar camara: ${(error as Error).message}`);
      return false;
    }
  }

  async function runRecognition(): Promise<void> {
    if (!cameraReady || !streamRef.current?.active) {
      const ready = await startCamera();
      if (!ready) {
        return;
      }
    }

    setState("processing");
    const video = videoRef.current;
    const canvas = captureCanvasRef.current;
    if (!video || !canvas || !video.videoWidth) {
      setState("idle");
      setStatus("Camara sin imagen. Espera 1 segundo y reintenta.");
      return;
    }

    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    const context = canvas.getContext("2d");
    if (!context) {
      setState("idle");
      return;
    }

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
      usuarioEsperado: "",
      usuarioDetectado: "",
      confianzaRostro: 0,
      distanciaCm: 46
    });
    const latencyMs = Math.round(performance.now() - start);
    setLastLatencyMs(latencyMs);

    const security = response?.seguridad;
    const detectedUser = security?.usuarioDetectado ?? response?.biometria?.usuarioDetectado ?? "";
    const detectedName = response?.biometria?.usuarioDetectadoNombre ?? detectedUser;
    const value = security?.confianza ?? response?.biometria?.confianzaRostro ?? 0;
    const isMatch = security?.accesoPermitido ?? (detectedUser.length > 0 && value >= 0.82);
    const denyReason = security?.motivo ?? "Usuario no reconocido";

    setScore(value);
    if (isMatch) {
      setState("match");
      setAttempts(0);
      setStatus(`Detectado: ${detectedName}`);
    } else {
      setState("no-match");
      setAttempts((prev) => prev + 1);
      setStatus(`ACCESO DENEGADO: ${denyReason}`);
    }

    appendLocalRecognitionEvent({
      id: crypto.randomUUID(),
      createdAt: new Date().toISOString(),
      userId: detectedUser || null,
      userName: response?.biometria?.usuarioDetectadoNombre ?? null,
      score: value,
      threshold: security?.umbralExactitud ?? 0.82,
      result: isMatch ? "match" : attempts + 1 >= 3 ? "fallback" : "no_match",
      deviceId: "DISP-NOR-001",
      latencyMs,
      reasonCode: isMatch ? "ACCESS_GRANTED" : security?.motivo?.toUpperCase().replace(/\s+/g, "_") ?? "ACCESS_DENIED_UNKNOWN_USER"
    });
  }

  function resetRecognition(): void {
    setAttempts(0);
    setScore(0);
    setState("idle");
    setStatus(cameraReady ? "Camara preparada" : "Camara detenida");
  }

  const statusClass =
    attempts >= 3 ? "status-pill pill-bad" : state === "match" ? "status-pill pill-ok" : state === "no-match" ? "status-pill pill-warn" : "status-pill pill-info";

  const overlayMessage = state === "processing" ? "Analizando rostro..." : "Alinea el rostro en el marco";
  const canEnter = state === "match";
  const denied = state === "no-match";

  return (
    <div className="recognition-layout">
      <section className="card recognition-camera-card">
        <h3>Reconocimiento en vivo</h3>
        <p className="page-intro">Identificacion automatica 1:N en tiempo real con fallback cuando superas intentos fallidos.</p>
        <div className="camera-box recognition-camera-box">
          <div className={state === "match" ? "frame ok" : state === "no-match" ? "frame bad" : "frame"} />
          <span className="camera-overlay-label">{overlayMessage}</span>
          {canEnter && <span className="camera-access-ok">Puede ingresar</span>}
          {denied && <span className="camera-access-denied">ACCESO DENEGADO</span>}
          <video ref={videoRef} autoPlay playsInline className="camera-video" />
          <canvas ref={captureCanvasRef} hidden />
        </div>
        <div className="recognition-status-line">
          <span className={statusClass}>{status}</span>
          {cameraReady && <span className="status-pill pill-info">Escaneo automatico activo</span>}
        </div>
      </section>

      <div className="recognition-bottom">
        <section className="card recognition-controls-card">
          <h3>Control operativo</h3>
          <p className="page-intro">Prepara la camara una sola vez. El reconocimiento se ejecuta automaticamente.</p>
          <div className="actions recognition-actions">
            <button type="button" className="btn-primary" onClick={startCamera}>
              {cameraReady ? "Camara preparada" : "Preparar camara"}
            </button>
            <button className="btn-secondary" onClick={runRecognition} disabled={!cameraReady || state === "processing" || attempts >= 3}>
              Identificar ahora
            </button>
            <button type="button" className="ghost" onClick={resetRecognition}>
              Reiniciar intentos
            </button>
          </div>
        </section>

        <section className="card recognition-metrics-card">
          <h3>Resultado IA</h3>
          <div className="status-row">
            <span>Umbral</span>
            <strong>0.82</strong>
          </div>
          <div className="status-row">
            <span>Score</span>
            <strong>{score}</strong>
          </div>
          <div className="status-row">
            <span>Intentos fallidos</span>
            <strong>{attempts}/3</strong>
          </div>
          <div className="status-row">
            <span>Latencia</span>
            <strong>{lastLatencyMs} ms</strong>
          </div>
          {state === "match" && <p className="ok">Match valido: activar dispositivo.</p>}
          {state === "no-match" && <p className="warn">No coincide: reintentar o aplicar fallback seguro.</p>}
          {attempts >= 3 && <p className="bad">Bloqueado: usar PIN o credencial secundaria.</p>}
        </section>
      </div>
    </div>
  );
}
