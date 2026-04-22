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

## Ejecucion local

```bash
dotnet run --project App_NosVemos_Auditoria_Servicio/src/NosVemos.Auditoria.Worker
```

## Consulta de eventos

Endpoint disponible:

- `GET /api/v1/auditoria/eventos?routingKey=<key>&from=<utc>&to=<utc>&take=50`

Ejemplo por pasarela:

```bash
curl -H "Authorization: Bearer <token>" "http://localhost:7000/api/v1/auditoria/eventos?take=20"
```
