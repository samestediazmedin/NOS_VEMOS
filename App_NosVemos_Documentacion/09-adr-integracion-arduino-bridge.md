# ADR-001 - Integracion Arduino mediante bridge intermedio

- Estado: Aprobado (propuesto para implementacion v1)
- Fecha: 2026-04-22
- Contexto: Integracion de datos de sensores Arduino con plataforma NOS_VEMOS

## Decision

Se adopta una arquitectura con `bridge` intermedio entre Arduino y backend:

```text
Arduino -> Bridge local (serial/WiFi + validacion) -> API Ingestion -> Eventos dominio
```

No se permite acoplar firmware Arduino directamente a microservicios de negocio.

## Motivo

- Centralizar validaciones de trama y calidad de dato.
- Manejar reconexion, buffering y reintentos sin afectar backend.
- Desacoplar cambios de firmware de cambios de dominio.
- Facilitar soporte multi-dispositivo y evolucion de protocolos.

## Consecuencias positivas

- Menor ruido de datos en servicios de negocio.
- Mejor trazabilidad e idempotencia por `deviceId + sequence`.
- Menor complejidad en `NucleoNegocio` y `Orquestador IA`.
- Posibilidad de evolucionar transporte (serial, WiFi, MQTT) sin romper contrato.

## Consecuencias negativas

- Un componente adicional para desplegar y monitorear.
- Mayor esfuerzo inicial de integracion y pruebas.

## Alternativas consideradas

1. Arduino -> API directa
   - Rechazada por acoplamiento fuerte y falta de controles intermedios.
2. Arduino -> Broker directo (sin bridge)
   - Rechazada en v1 por menor control de validacion/normalizacion en edge.

## Reglas vinculantes para implementacion

- El contrato oficial v1 sera `schemas/arduino-telemetria-v1.schema.json`.
- El bridge debe validar: formato, checksum, rango y secuencia.
- El backend debe aplicar idempotencia por `deviceId + sequence`.
- Todo rechazo o degradacion debe quedar en auditoria.

## Criterio de revision futura

Revisar este ADR cuando:

- se agreguen mas de 3 tipos de sensores,
- se requiera OTA o control remoto del firmware,
- se migre ingestion a broker/event-stream dedicado.
