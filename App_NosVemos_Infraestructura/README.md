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
