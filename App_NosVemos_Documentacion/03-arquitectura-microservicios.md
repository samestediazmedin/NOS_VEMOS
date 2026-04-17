# Arquitectura propuesta

## Servicios iniciales

- `pasarela`: punto unico de entrada y ruteo.
- `autenticacion`: autenticacion, tokens y control de acceso.
- `usuarios`: gestion de perfiles y datos de usuario.
- `nucleonegocio`: logica principal del negocio.
- `reportes`: consultas agregadas y exportaciones basicas.
- `notificaciones`: correo/SMS/push por eventos.
- `auditoria`: trazabilidad de acciones y cambios criticos.

## Capa interna por servicio

- `API`: endpoints y validaciones de entrada.
- `Aplicacion`: casos de uso y orquestacion.
- `Dominio`: reglas del negocio.
- `Infraestructura`: SQL Server, mensajeria y clientes externos.

## Persistencia SQL Server

Recomendacion: una base por servicio (o esquema aislado por servicio).

Ejemplo:

- `AutenticacionDB`
- `UsuariosDB`
- `NucleoDB`
- `ReportesDB`
- `NotificacionesDB`
- `AuditoriaDB`

## Integracion

- Sincrona: REST para operaciones de consulta puntual.
- Asincrona: RabbitMQ para eventos de dominio.

## Diagrama textual

```text
Cliente Web/Mobile
        |
        v
       Pasarela
        |
  -------------------------------------------
  |         |          |         |          |
  v         v          v         v          v
Autent.  Usuarios   Nucleo    Reportes  Notificaciones
  |         |          |         |          |
  v         v          v         v          v
AutDB   UserDB     CoreDB    RepDB      NotiDB
             (SQL Server por servicio)

Nucleo --> RabbitMQ --> Notificaciones / Reportes / Auditoria
```
