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
cp App_NosVemos_Infraestructura/.env.example .env
```

2. Iniciar servicios base:

```bash
docker compose -f App_NosVemos_Infraestructura/docker-compose.yml up -d
```

3. Verificar estado:

```bash
docker compose -f App_NosVemos_Infraestructura/docker-compose.yml ps
```

## Endpoints locales

- SQL Server: `localhost,1433`
- RabbitMQ UI: `http://localhost:15672`
- Seq: `http://localhost:5341`
- Jaeger UI: `http://localhost:16686`
