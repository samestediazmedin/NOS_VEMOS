# Especificacion API de ingestion Arduino (v1)

Este documento define el contrato HTTP para ingestion de telemetria Arduino.

## 1. Endpoint

- Metodo: `POST`
- Ruta interna sugerida: `/api/v1/telemetria/ingestion`
- Ruta via Pasarela (cuando se publique): `/api/v1/telemetria/ingestion`
- Auth sugerida: `Bearer JWT` (rol tecnico: `DeviceBridge` o `Administrador`)

## 2. Headers requeridos

- `Content-Type: application/json`
- `X-Request-Id: <uuid>` (recomendado para trazabilidad)

Headers opcionales:

- `Idempotency-Key: <deviceId:sequence>`

Nota: aunque llegue `Idempotency-Key`, el backend debe validar idempotencia real por `deviceId + sequence` del payload.

## 3. Body (payload)

El body debe cumplir exactamente el schema:

- `App_NosVemos_Documentacion/schemas/arduino-telemetria-v1.schema.json`

## 4. Respuestas

### 202 Accepted (aceptado)

```json
{
  "status": "accepted",
  "eventId": "f1b8c9cc-0d9c-45fa-9d5d-8128c80b24f7",
  "deviceId": "arduino-001",
  "sequence": 1287,
  "receivedAt": "2026-04-22T21:00:00Z"
}
```

### 200 OK (duplicado idempotente)

```json
{
  "status": "duplicate",
  "eventId": "f1b8c9cc-0d9c-45fa-9d5d-8128c80b24f7",
  "deviceId": "arduino-001",
  "sequence": 1287,
  "receivedAt": "2026-04-22T21:00:00Z"
}
```

### 400 BadRequest (error de contrato)

```json
{
  "status": "rejected",
  "code": "SCHEMA_VALIDATION_ERROR",
  "message": "Payload invalido para contrato arduino-telemetria-v1.",
  "errors": [
    {
      "field": "quality.confidence",
      "issue": "must be <= 1"
    }
  ],
  "receivedAt": "2026-04-22T21:00:00Z"
}
```

### 401 Unauthorized

```json
{
  "status": "rejected",
  "code": "UNAUTHORIZED",
  "message": "Token invalido o ausente.",
  "receivedAt": "2026-04-22T21:00:00Z"
}
```

### 409 Conflict (secuencia invalida)

```json
{
  "status": "rejected",
  "code": "SEQUENCE_CONFLICT",
  "message": "Sequence menor al ultimo valor aceptado para el dispositivo.",
  "deviceId": "arduino-001",
  "sequence": 1200,
  "lastAcceptedSequence": 1287,
  "receivedAt": "2026-04-22T21:00:00Z"
}
```

### 422 UnprocessableEntity (dato tecnicamente invalido)

```json
{
  "status": "rejected",
  "code": "DATA_QUALITY_REJECTED",
  "message": "Checksum invalido o valor fuera de rango.",
  "deviceId": "arduino-001",
  "sequence": 1288,
  "receivedAt": "2026-04-22T21:00:00Z"
}
```

### 500 InternalServerError

```json
{
  "status": "error",
  "code": "INGESTION_INTERNAL_ERROR",
  "message": "Error interno procesando telemetria.",
  "requestId": "c6f6a449-2205-48fa-a06f-8f4f27f7086f",
  "receivedAt": "2026-04-22T21:00:00Z"
}
```

## 5. Reglas backend obligatorias

1. Validar schema JSON antes de reglas de negocio.
2. Validar idempotencia por `deviceId + sequence`.
3. Guardar `receivedAt` de servidor.
4. Auditar todo `rejected`, `duplicate` y `accepted`.
5. Publicar evento de dominio solo en `accepted`.

## 6. Timeouts y reintentos (cliente bridge)

- Timeout sugerido por request: 5 segundos.
- Reintentos: 3 (backoff 0.5s, 1s, 2s).
- En `400/409/422`: no reintentar automaticamente (error funcional).
- En `500/timeout`: reintentar.

## 7. Versionado

- Version actual: v1.
- Cambio no compatible => nuevo schema y nueva ruta o header de version.
