import { useEffect, useMemo, useRef, useState } from "react";
import { analyzeCameraFrame } from "../../services/api";

type Capture = {
  angle: "frontal" | "izquierda" | "derecha";
  quality: number;
  createdAt: string;
};

const steps: Capture["angle"][] = ["frontal", "izquierda", "derecha"];

function label(angle: Capture["angle"]): string {
  if (angle === "frontal") return "Frontal";
  if (angle === "izquierda") return "30 grados izquierda";
  return "30 grados derecha";
}

export function CapturePage() {
  const [captures, setCaptures] = useState<Capture[]>([]);
  const [consent, setConsent] = useState(false);
  const [status, setStatus] = useState("Camara detenida");
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const streamRef = useRef<MediaStream | null>(null);

  const next = useMemo(() => steps.find((angle) => !captures.some((capture) => capture.angle === angle)), [captures]);
  const done = captures.length === 3;

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

  async function saveCapture(): Promise<void> {
    if (!next) return;
    const video = videoRef.current;
    const canvas = canvasRef.current;
    if (!video || !canvas || !video.videoWidth) {
      setStatus("Inicia la camara antes de capturar");
      return;
    }

    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    const context = canvas.getContext("2d");
    if (!context) return;
    context.drawImage(video, 0, 0, canvas.width, canvas.height);

    const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, "image/jpeg", 0.9));
    if (!blob) {
      setStatus("No se pudo generar imagen");
      return;
    }

    setStatus("Analizando captura con IA...");
    const response = await analyzeCameraFrame({
      blob,
      usuarioEsperado: "juan.martinez",
      usuarioDetectado: "juan.martinez",
      confianzaRostro: 0.92,
      distanciaCm: 48
    });

    if (!response) {
      setStatus("No hubo respuesta de API, captura guardada en modo local");
    } else {
      setStatus(`API OK - Riesgo ${response.evaluacion.nivelRiesgo}`);
    }

    const quality = response ? Math.round((response.biometria.confianzaRostro || 0.8) * 100) : Math.round(82 + Math.random() * 15);
    setCaptures((prev) => [...prev, { angle: next, quality, createdAt: new Date().toLocaleTimeString() }]);
  }

  function recapture(angle: Capture["angle"]): void {
    setCaptures((prev) => prev.filter((capture) => capture.angle !== angle));
  }

  return (
    <div className="grid split">
      <section className="card">
        <h3>Enrolamiento biometrico</h3>
        <p>Usuario: USR-0089 - Juan Martinez</p>
        <label className="checkbox">
          <input type="checkbox" checked={consent} onChange={(e) => setConsent(e.target.checked)} />
          Usuario acepta consentimiento de biometria.
        </label>
        <div className="camera-box">
          <div className="face-guide" />
          <span>Vista camara (simulada)</span>
          <video ref={videoRef} autoPlay playsInline className="camera-video" />
          <canvas ref={canvasRef} hidden />
        </div>
        <p>{status}</p>
        <p>
          Paso actual: <strong>{next ? label(next) : "Completado"}</strong>
        </p>
        <button type="button" onClick={startCamera}>
          Iniciar camara
        </button>
        <button disabled={!consent || !next} onClick={saveCapture}>
          Capturar foto
        </button>
      </section>

      <section className="card">
        <h3>Control de 3 fotos</h3>
        {steps.map((angle) => {
          const capture = captures.find((item) => item.angle === angle);
          return (
            <div className="status-row" key={angle}>
              <span>{label(angle)}</span>
              {capture ? (
                <span>
                  {capture.quality}% <button onClick={() => recapture(angle)}>recapturar</button>
                </span>
              ) : (
                <span>Pendiente</span>
              )}
            </div>
          );
        })}
        {done && <p className="ok">Enrolamiento listo para aprobacion del admin.</p>}
      </section>
    </div>
  );
}
