# Auditoria Servicio

Registra trazabilidad de acciones criticas, cambios y eventos de seguridad.

## Funcionalidades objetivo

- Registrar eventos de seguridad (login, denegaciones, cambios de rol).
- Registrar acciones administrativas y operativas de modulos criticos.
- Exponer consultas filtradas por usuario, modulo, accion y rango de fechas.
- Mantener politica de inmutabilidad de registros.

## Estado

- Worker de auditoria implementado para consumir eventos de dominio desde RabbitMQ.
- Registra en logs eventos `expediente.creado`, `expediente.cerrado`, `ia.camara.analizado`, `ia.rostro.reconocido` y `sensor.proximidad.detectada`.
- API de consulta implementada para listar eventos filtrados desde `AuditoriaDB`.

## Endpoints

- `GET /health`
- `GET /api/v1/auditoria/eventos`
  - Filtros opcionales: `routingKey`, `modulo`, `desde`, `hasta`, `limit` (max 500)
- `GET /api/v1/auditoria/movimientos`
- `POST /api/v1/auditoria/movimientos`
- `GET /api/v1/auditoria/asignaciones`
- `POST /api/v1/auditoria/asignaciones`

## Ejecucion local

```bash
dotnet run --project App_NosVemos_Auditoria_Servicio/src/NosVemos.Auditoria.Worker
```
