# Analisis de integracion Arduino (pre-implementacion)

Este documento define como integrar un flujo con Arduino y sensores antes de escribir codigo productivo, con foco en validacion de datos y pruebas de integracion.

## 1. Objetivo

Integrar telemetria/sensores de Arduino con la plataforma NOS_VEMOS asegurando:

- calidad de dato (no basura, no duplicados, no valores fuera de rango),
- trazabilidad completa (quien envio, cuando, que se guardo),
- resiliencia ante desconexion/intermitencia,
- base tecnica para escalar a varios dispositivos.

## 2. Alcance inicial

Incluye:

- captura de lectura desde Arduino (USB serial o WiFi, segun modulo),
- proceso intermedio de validacion (edge bridge),
- envio de eventos validados a backend,
- almacenamiento y auditoria de eventos relevantes.

No incluye (en esta fase):

- control remoto avanzado del firmware,
- OTA,
- panel de operaciones en tiempo real completo.

## 3. Arquitectura recomendada

Recomendacion: no conectar Arduino directo al microservicio final. Usar un proceso intermedio (bridge) para filtrar/normalizar.

```text
Arduino + sensores
   -> Bridge local (lector serial/WiFi + validacion)
      -> Pasarela/API de ingestion
         -> NucleoNegocio / Orquestador IA
            -> RabbitMQ eventos
               -> Auditoria / Reportes / Notificaciones
```

### Por que un bridge

- desacopla firmware del backend,
- permite reintentos y buffer local,
- centraliza validaciones tecnicas,
- evita contaminar servicios de negocio con ruido de hardware.

## 4. Contrato de dato sugerido (evento telemetria)

```json
{
  "deviceId": "arduino-001",
  "sensorType": "proximidad",
  "value": 42.3,
  "unit": "cm",
  "capturedAt": "2026-04-22T20:10:00Z",
  "sequence": 1287,
  "quality": {
    "signal": "ok",
    "confidence": 0.94
  },
  "meta": {
    "firmwareVersion": "1.0.0",
    "bridgeVersion": "1.0.0",
    "source": "serial"
  },
  "checksum": "A1F9"
}
```

Campos criticos:

- `deviceId`, `sensorType`, `value`, `capturedAt`, `sequence`, `checksum`.

## 5. Proceso de verificacion de datos (obligatorio)

### Capa A: firmware Arduino

- validar rango fisico antes de transmitir (ej. distancia > 0 y < max sensor),
- descartar lecturas NaN/inestables,
- incrementar `sequence` por mensaje,
- calcular `checksum` por trama,
- aplicar frecuencia estable (ej. 2-5 Hz segun sensor).

### Capa B: bridge (ingestion local)

- validar estructura de trama (parse estricto),
- verificar `checksum`,
- validar monotonia de `sequence` por `deviceId`,
- detectar duplicado por `(deviceId, sequence)`,
- aplicar reglas de sanidad:
  - outlier hard: fuera de rango tecnico,
  - outlier soft: cambio brusco contra ventana corta,
  - debounce para evitar rebotes.

### Capa C: backend

- validar esquema JSON,
- validar idempotencia por `deviceId + sequence`,
- sellar `receivedAt` servidor,
- registrar errores de validacion en auditoria,
- publicar solo eventos aprobados a dominio.

## 6. Regla de decision de eventos

- evento `aceptado`: pasa todas las validaciones.
- evento `rechazado`: checksum invalido, formato invalido o rango imposible.
- evento `degradado`: pasa, pero con flag de baja confianza (ej. señal debil/outlier suave).

Esto evita perder informacion operativa sin contaminar decisiones criticas.

## 7. Riesgos y mitigaciones

- **Desconexion serial/WiFi**: reconexion automatica + cola local en bridge.
- **Deriva de sensor**: rutina de calibracion y umbrales versionados.
- **Doble envio**: idempotencia en backend por `(deviceId, sequence)`.
- **Desfase horario**: `capturedAt` y `receivedAt` con control de drift.
- **Falsos positivos**: debounce + ventana movil + reglas de confianza.

## 8. Pruebas de integracion que se deben ejecutar

1. **Happy path**
   - Arduino envia lecturas validas, backend persiste y publica evento.
2. **Checksum invalido**
   - Trama debe rechazarse y auditarse.
3. **Duplicados**
   - mismo `(deviceId, sequence)` no debe duplicar persistencia/evento.
4. **Out of order**
   - secuencia atrasada: marcar, rechazar o degradar segun politica.
5. **Desconexion/reconexion**
   - bridge recupera y continua sin caida del sistema.
6. **Soak test (>= 2h)**
   - estabilidad, memoria, latencia, perdida de mensajes.

## 9. Plan de implementacion (fases)

### Fase 0 - Preparacion (documental)

- cerrar contrato de dato,
- definir rangos por sensor,
- definir politica de idempotencia y errores.

### Fase 1 - Bridge minimo

- lector serial/WiFi,
- parser + checksum + validaciones base,
- envio HTTP al backend.

### Fase 2 - Ingestion backend

- endpoint dedicado de telemetria,
- persistencia + auditoria + publicacion de eventos.

### Fase 3 - End-to-end

- integrar con Nucleo/IA segun caso,
- pruebas de integracion y carga.

### Fase 4 - Operacion

- metricas (rechazo, latencia, disponibilidad),
- alertas de calidad de dato,
- documentacion operativa.

## 10. Definicion de listo para implementar

Se inicia implementacion cuando esten cerrados:

- contrato de dato v1,
- rangos y reglas por sensor,
- estrategia de idempotencia,
- plan de pruebas de integracion,
- criterios de aceptacion tecnica.

## 11. Recomendacion final

Antes de implementar firmware definitivo, ejecutar una prueba piloto con 1 Arduino y 1 sensor (proximidad) para validar todo el pipeline y ajustar reglas de calidad de dato. Despues escalar a otros sensores.

## 12. Artefactos tecnicos asociados

- ADR de arquitectura: `App_NosVemos_Documentacion/09-adr-integracion-arduino-bridge.md`
- Contrato tecnico v1: `App_NosVemos_Documentacion/schemas/arduino-telemetria-v1.schema.json`
