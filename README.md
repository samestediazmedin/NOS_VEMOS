# NOS_VEMOS - Arquitectura de Microservicios

Proyecto organizado por microservicios en carpetas raiz, con nombres en espanol y estructura similar al esquema solicitado.

## Estructura principal

```text
NOS_VEMOS/
|-- App_NosVemos_Autenticacion_Servicio/
|-- App_NosVemos_Usuarios_Servicio/
|-- App_NosVemos_NucleoNegocio_Servicio/
|-- App_NosVemos_Notificaciones_Servicio/
|-- App_NosVemos_Reportes_Servicio/
|-- App_NosVemos_Auditoria_Servicio/
|-- App_NosVemos_Orquestador_IA/
|-- App_NosVemos_Pasarela/
|-- App_NosVemos_Infraestructura/
|-- App_NosVemos_Movil/
|-- App_NosVemos_Documentacion/
|-- .gitignore
`-- README.md
```

## Convencion por microservicio

Cada microservicio contiene:

- `src/`: codigo fuente por capas (`API`, `Aplicacion`, `Dominio`, `Infraestructura`).
- `tests/`: pruebas unitarias e integracion.
- `deploy/`: artefactos de despliegue del servicio.
- `README.md`: responsabilidades y alcance.

## Levantar infraestructura local

1. Copiar variables de entorno:

```bash
cp App_NosVemos_Infraestructura/.env.example App_NosVemos_Infraestructura/.env
```

2. Iniciar servicios base:

```bash
docker compose --env-file App_NosVemos_Infraestructura/.env -f App_NosVemos_Infraestructura/docker-compose.yml up -d
```

Para incluir SQL Server en contenedor:

```bash
docker compose --profile sqlserver --env-file App_NosVemos_Infraestructura/.env -f App_NosVemos_Infraestructura/docker-compose.yml up -d
```

3. Verificar estado:

```bash
docker compose --env-file App_NosVemos_Infraestructura/.env -f App_NosVemos_Infraestructura/docker-compose.yml ps
```

## Endpoints locales

- SQL Server: `localhost,1433`
- RabbitMQ UI: `http://localhost:15673`
- Seq: `http://localhost:5341`
- Jaeger UI: `http://localhost:16686`

## Ejecutar microservicios base

```bash
dotnet run --project App_NosVemos_Autenticacion_Servicio/src/NosVemos.Autenticacion.Api
dotnet run --project App_NosVemos_Usuarios_Servicio/src/NosVemos.Usuarios.Api
dotnet run --project App_NosVemos_NucleoNegocio_Servicio/src/NosVemos.NucleoNegocio.Api
dotnet run --project App_NosVemos_Orquestador_IA/src/NosVemos.OrquestadorIA.Api
dotnet run --project App_NosVemos_Pasarela/src/NosVemos.Pasarela.Api
```

Puertos por defecto:

- Pasarela: `http://localhost:7000`
- Autenticacion: `http://localhost:7001`
- Usuarios: `http://localhost:7002`
- NucleoNegocio: `http://localhost:7003`
- Orquestador IA: `http://localhost:7004`

Camara IA de prueba (frontend simple): `App_NosVemos_Movil/src/camara-ia.html`

Nota: los servicios API arrancan en modo `UseInMemoryDatabase=true` para funcionar incluso sin SQL Server. Cuando SQL Server este disponible, cambia ese valor a `false` en los `appsettings.json` de cada servicio.



## Base de Datos - Control Acceso
 Requisitos
SQL Server
SQL Server Management Studio (SSMS)


## Crear la base de datos

Ejecutar el archivo:

01_create_database.sql

Contenido esperado:

CREATE DATABASE ControlAccesoFacial;
GO

USE ControlAccesoFacial;
GO

## Crear las tablas

Ejecutar: el scrip.sql 

02_tables.sql

Este archivo contiene:

Tabla Usuarios
Tabla Rostros
Tabla Accesos



##Conexión a la base de datos
Servidor: localhost
Base de datos: ControlAccesoFacial
Usuario: según configuración local
