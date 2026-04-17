# Autenticacion Servicio

Responsable de login, registro, JWT, renovacion de token y control de acceso.

## Ejecutar

```bash
dotnet run --project App_NosVemos_Autenticacion_Servicio/src/NosVemos.Autenticacion.Api
```

## Endpoints base

- `GET /health`
- `POST /api/v1/autenticacion/registro`
- `POST /api/v1/autenticacion/login`

## Funcionalidades

- Registro de usuarios de acceso.
- Login con emision de JWT.
- Base inicial de roles (`Administrador`, `Usuario`).
- Persistencia EF Core (modo InMemory por defecto; SQL Server configurable).
