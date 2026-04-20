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

4. Configurar secreto JWT para todos los servicios (PowerShell):

```powershell
$env:Jwt__SecretKey="NOS_VEMOS_DEV_SECRET_KEY_CHANGE_ME"
```

En cmd.exe:

```bat
set Jwt__SecretKey=NOS_VEMOS_DEV_SECRET_KEY_CHANGE_ME
```

5. Ejecutar verificacion de seguridad (build + tests, detecta proyectos bloqueados por politica):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify-security.ps1
```

Si algunos proyectos quedan bloqueados por politica de Windows, intenta primero:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\unblock-test-binaries.ps1
```

Y luego vuelve a correr:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify-security.ps1
```

## CI

El repositorio incluye pipeline en GitHub Actions: `.github/workflows/ci.yml`.

- Restaura dependencias de `NosVemos.sln`.
- Compila en modo `Release`.
- Ejecuta `scripts/verify-security.ps1` para validar build y pruebas de seguridad.

## Smoke test E2E con base de datos

Con infraestructura y microservicios arriba, puedes validar flujo completo (registro, login, usuarios, expediente, IA y auditoria persistida en SQL Server):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-e2e-db.ps1
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
dotnet run --project App_NosVemos_Auditoria_Servicio/src/NosVemos.Auditoria.Worker
```

Puertos por defecto:

- Pasarela: `http://localhost:7000`
- Autenticacion: `http://localhost:7001`
- Usuarios: `http://localhost:7002`
- NucleoNegocio: `http://localhost:7003`
- Orquestador IA: `http://localhost:7004`

Camara IA de prueba (frontend simple): `App_NosVemos_Movil/src/camara-ia.html`

Nota: `Autenticacion`, `Usuarios`, `NucleoNegocio`, `OrquestadorIA` y `Auditoria` estan configurados para persistencia SQL Server por defecto en desarrollo local. Si necesitas trabajar sin SQL temporalmente, puedes habilitar modo en memoria en cada servicio cambiando `UseInMemoryDatabase=true` donde aplique.

Eventos de dominio: `NucleoNegocio` publica `expediente.creado` y `expediente.cerrado`; `OrquestadorIA` publica `ia.camara.analizado`, `ia.rostro.reconocido` y `sensor.proximidad.detectada` en RabbitMQ (`nosvemos.domain.events`), y `Auditoria.Worker` los consume para trazabilidad.
