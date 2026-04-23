# Plan de implementacion Arduino (ejecutable)

Este plan aterriza la implementacion en tareas concretas, usando:

- ADR: `App_NosVemos_Documentacion/09-adr-integracion-arduino-bridge.md`
- Schema v1: `App_NosVemos_Documentacion/schemas/arduino-telemetria-v1.schema.json`

## Fase 1 - Bridge minimo viable

Objetivo: leer sensor y enviar telemetria valida al backend.

Tareas:

1. Crear proceso `bridge` (CLI/service) con lector serial.
2. Implementar parser de trama y validacion `checksum`.
3. Mapear lectura a contrato JSON v1.
4. Implementar cliente HTTP con reintentos exponenciales.
5. Agregar log local y cola temporal en memoria.

Criterios de salida:

- envia eventos validos al endpoint de ingestion,
- rechaza tramas invalidas,
- no pierde proceso ante desconexion puntual.

## Fase 2 - Ingestion backend

Objetivo: recibir eventos v1 y garantizar idempotencia + auditoria.

Tareas:

1. Crear endpoint de ingestion en backend (route dedicada).
2. Validar payload contra schema v1.
3. Aplicar idempotencia por `deviceId + sequence`.
4. Guardar `receivedAt`, estado y detalle de validacion.
5. Publicar evento de dominio solo para payload aceptado.

Criterios de salida:

- duplicados no generan doble persistencia,
- rechazos quedan auditados,
- latencia p95 definida y medida.

## Fase 3 - Integracion con dominio

Objetivo: conectar telemetria con decisiones de negocio.

Tareas:

1. Definir reglas dominio (umbral alerta proximidad, etc.).
2. Consumir evento de ingestion y derivar evento de negocio.
3. Integrar con Auditoria y Reportes.
4. (Opcional) Integrar Notificaciones por eventos criticos.

Criterios de salida:

- regla de negocio trazable de extremo a extremo,
- evidencia en auditoria y reporte basico.

## Fase 4 - End-to-end y endurecimiento

Objetivo: validar operacion real de forma estable.

Tareas:

1. Ejecutar pruebas: happy path, checksum invalido, duplicado, out-of-order.
2. Ejecutar prueba de reconexion bridge.
3. Ejecutar soak test (>= 2 horas).
4. Medir perdida de mensajes y latencia.
5. Ajustar umbrales de validacion y debounce.

Criterios de salida:

- tasa de rechazo explicada y aceptable,
- perdida de mensajes en limite objetivo,
- runbook basico de operacion disponible.

## Matriz minima de pruebas

1. `payload valido` -> 200 + persistencia + evento.
2. `checksum invalido` -> rechazo + auditoria.
3. `duplicado` -> idempotente.
4. `secuencia atrasada` -> politica aplicada (rechazo/degradado).
5. `bridge sin red` -> buffer + reenvio al reconectar.

## Definicion de terminado (MVP Arduino)

- contrato v1 activo en backend,
- bridge operativo con reconexion,
- idempotencia validada,
- pruebas E2E y soak en verde,
- documentacion tecnica y operativa actualizada.
