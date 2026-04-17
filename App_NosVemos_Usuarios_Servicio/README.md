# Usuarios Servicio

Responsable de perfiles de usuario, configuracion y estado de cuenta.

## Ejecutar

```bash
dotnet run --project App_NosVemos_Usuarios_Servicio/src/NosVemos.Usuarios.Api
```

## Endpoints base

- `GET /health`
- `GET /api/v1/usuarios`
- `GET /api/v1/usuarios/{id}`
- `POST /api/v1/usuarios`

## Funcionalidades

- Gestion de perfil basica.
- Consulta de usuarios por id.
- Alta de usuario operativo.
- Persistencia EF Core (modo InMemory por defecto; SQL Server configurable).
