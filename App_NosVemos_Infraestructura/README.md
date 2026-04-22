# Infraestructura

Contiene componentes compartidos para desarrollo local:

- SQL Server
- RabbitMQ
- Seq
- Jaeger

Uso:

```bash
cp App_NosVemos_Infraestructura/.env.example App_NosVemos_Infraestructura/.env
docker compose --env-file App_NosVemos_Infraestructura/.env -f App_NosVemos_Infraestructura/docker-compose.yml up -d
```

## Base de datos SQL Server (NOS_VEMOS)

El lugar correcto para artefactos de base de datos de infraestructura es:

- `App_NosVemos_Infraestructura/sql/`

Script incluido:

- `App_NosVemos_Infraestructura/sql/01-create-nosvemos-databases.sql`

Para crear las bases del proyecto en el contenedor SQL Server:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\setup-nosvemos-db.ps1
```

Opcionalmente puedes evitar el autoarranque del contenedor SQL:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\setup-nosvemos-db.ps1 -AutoStartSqlContainer:$false
```

El script crea estas bases si no existen:

- `AutenticacionDB`
- `UsuariosDB`
- `NucleoDB`
- `IADB`
- `AuditoriaDB`

Despues, al iniciar cada microservicio, EF Core aplica migraciones automaticamente.

Si quieres incluir SQL Server por contenedor (cuando MCR este accesible):

```bash
docker compose --profile sqlserver --env-file App_NosVemos_Infraestructura/.env -f App_NosVemos_Infraestructura/docker-compose.yml up -d
```

Puertos por defecto en esta base:

- SQL Server: `1433`
- RabbitMQ AMQP: `5673`
- RabbitMQ UI: `15673`
- Seq: `5341`
- Jaeger UI: `16686`
