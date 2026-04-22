# Bitacora de sesion - 2026-04-20

## Resumen de avances

- Se reforzo seguridad JWT en Pasarela, Usuarios y NucleoNegocio.
- Se implemento hash de password con BCrypt en Autenticacion.
- Se agregaron pruebas de autorizacion/integracion en Auth, Usuarios, Nucleo y Pasarela (con limitacion local de App Control en Pasarela).
- Se habilito CI en GitHub Actions (`.github/workflows/ci.yml`).
- Se incorporaron scripts de verificacion y soporte local:
  - `scripts/verify-security.ps1`
  - `scripts/unblock-test-binaries.ps1`
  - `scripts/smoke-e2e-db.ps1`
- Se activo persistencia SQL por defecto para Auth, Usuarios, Nucleo, IA y Auditoria.
- Se agregaron migraciones EF para Auth, Usuarios, Nucleo, IA y Auditoria.
- Se implemento publicacion de eventos de dominio:
  - Nucleo: `expediente.creado`, `expediente.cerrado`
  - IA: `ia.camara.analizado`, `ia.rostro.reconocido`, `sensor.proximidad.detectada`
- Se implemento `Auditoria.Worker` para consumo de eventos y persistencia en `AuditoriaDB`.
- Se agrego endpoint de consulta de auditoria:
  - `GET /api/v1/auditoria/eventos`
- Se conecto auditoria en Pasarela y en la vista de prueba frontend (`camara-ia.html`).
- Se documento backlog por historias de usuario en:
  - `App_NosVemos_Documentacion/07-historias-usuario-y-backlog-funcional.md`

## Estado de infraestructura

- Docker daemon activo, pero pull de SQL Server bloqueado por resolucion DNS/proxy hacia `eastus.data.mcr.microsoft.com`.
- Pendiente normalizar configuracion de red/proxy de Docker para levantar stack completo.

## Estado de git

- Commit realizado y enviado a `origin/DEVELOP`:
  - `2911eb4` - "Fortalece seguridad, persistencia SQL y trazabilidad por eventos"

## Siguiente punto para continuar manana

1. Resolver pull de imagen SQL Server en Docker (DNS/proxy).
2. Levantar infraestructura completa y ejecutar `scripts/smoke-e2e-db.ps1`.
3. Mejorar auditoria:
   - paginacion (`page`, `pageSize`)
   - endpoint detalle (`/api/v1/auditoria/eventos/{id}`)
   - vista frontend de auditoria con filtros.
