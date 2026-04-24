# Movil

Frontend operativo de NOS VEMOS para flujo biometrico por camara.

## Modulos incluidos

- Dashboard operativo (KPIs de reconocimiento y estado de servicios).
- Captura asistida de enrolamiento biometrico (3 fotos requeridas).
- Reconocimiento facial en vivo con control de intentos y fallback.
- Historial de reconocimientos y enrolamientos.
- Auditoria biometrica resumida.

## Ejecutar local

1. `npm install`
2. `npm run dev`
3. Abrir `http://localhost:5173`

## Build

- `npm run build`

## Integracion API

- Base esperada: `http://localhost:7000`
- Endpoint de lectura usado para historial/auditoria:
  - `/api/v1/auditoria/biometria/eventos`

Si el endpoint no responde, la app usa datos mock para mantener operativa la interfaz.

## Archivo legado

- `src/camara-ia.html` se conserva como prueba standalone de captura y envio de frame.
