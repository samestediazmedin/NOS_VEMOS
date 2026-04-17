# Infraestructura

Contiene componentes compartidos para desarrollo local:

- SQL Server
- RabbitMQ
- Seq
- Jaeger

Uso:

```bash
cp App_NosVemos_Infraestructura/.env.example .env
docker compose -f App_NosVemos_Infraestructura/docker-compose.yml up -d
```
