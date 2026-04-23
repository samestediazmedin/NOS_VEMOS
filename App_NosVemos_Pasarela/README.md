# Pasarela

Punto unico de entrada para clientes web y moviles.

Funciones:

- Enrutamiento
- Autenticacion centralizada
- Control de acceso
- Politicas transversales

## Ejecutar

```bash
dotnet run --project App_NosVemos_Pasarela/src/NosVemos.Pasarela.Api
```

## Rutas configuradas

- `/api/v1/autenticacion/*` -> `http://localhost:7001`
- `/api/v1/usuarios/*` -> `http://localhost:7002`
- `/api/v1/expedientes/*` -> `http://localhost:7003`
- `/api/v1/ia/*` -> `http://localhost:7004`
- `/api/v1/auditoria/*` -> `http://localhost:7005`

## Funcionalidades

- Punto unico de entrada para web y movil.
- Proxy reverso con YARP para enrutar por dominio funcional.
- Facilita desacoplar clientes de cambios internos de cada microservicio.
