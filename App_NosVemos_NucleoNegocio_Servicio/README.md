# NucleoNegocio Servicio

Contiene la logica central del negocio y la publicacion de eventos de dominio.

## Ejecutar

```bash
dotnet run --project App_NosVemos_NucleoNegocio_Servicio/src/NosVemos.NucleoNegocio.Api
```

## Endpoints base

- `GET /health`
- `GET /api/v1/expedientes`
- `POST /api/v1/expedientes`
- `POST /api/v1/expedientes/{id}/cerrar`

## Funcionalidades

- Apertura y consulta de expedientes.
- Cierre de expediente con cambio de estado.
- Base para sesiones, alertas y encuestas en siguientes iteraciones.
- Persistencia EF Core (modo InMemory por defecto; SQL Server configurable).
