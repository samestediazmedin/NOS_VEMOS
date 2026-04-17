# Requisitos funcionales y no funcionales

## Requisitos funcionales

RF-01. Registro, inicio de sesion y recuperacion de cuenta.

RF-02. Gestion de usuarios (perfil, estado, roles y permisos).

RF-03. Gestion de entidades del negocio principal (crear, consultar, actualizar, cancelar).

RF-04. Emision de eventos de negocio para notificaciones y auditoria.

RF-05. Generacion de reportes operativos basicos por fechas/estado.

RF-06. Registro de auditoria de acciones criticas.

## Requisitos no funcionales

RNF-01. Seguridad: JWT, cifrado de datos sensibles, HTTPS obligatorio.

RNF-02. Rendimiento: latencia p95 menor a 300 ms para endpoints criticos del MVP.

RNF-03. Escalabilidad: despliegue independiente por servicio.

RNF-04. Observabilidad: logs estructurados, trazas distribuidas, metricas base.

RNF-05. Mantenibilidad: arquitectura por capas y versionado de API.

RNF-06. Resiliencia: reintentos y manejo de fallos transitorios en integraciones.

## Criterios de aceptacion del MVP

- Login y autorizacion funcionando en entorno local.
- Al menos 1 flujo completo del Nucleo de Negocio operativo.
- Persistencia en SQL Server con migraciones aplicables.
- Un evento de negocio publicado y consumido correctamente.
- Logs y trazas visibles en Seq y Jaeger.
