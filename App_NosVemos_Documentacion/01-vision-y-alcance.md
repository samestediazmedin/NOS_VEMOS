# Vision y alcance

## Vision

Construir una plataforma empresarial basada en microservicios, escalable y mantenible, con seguridad desde el diseno y preparada para crecer por modulos.

## Objetivo general

Disenar e implementar una solucion backend que separe responsabilidades por dominio de negocio, permita despliegue independiente por servicio y use SQL Server como base transaccional robusta.

## Objetivos especificos

- Implementar autenticacion y autorizacion basada en JWT.
- Definir servicios de usuarios, negocio central, reportes y notificaciones.
- Habilitar trazabilidad y observabilidad desde la primera version.
- Preparar CI/CD y convenciones tecnicas para escalar el sistema.

## Alcance del MVP

Incluye:

- API Gateway basico.
- Servicio de Autenticacion.
- Servicio de Usuarios.
- Servicio de Nucleo de Negocio.
- SQL Server y mensajeria base (RabbitMQ) disponibles.

No incluye en MVP:

- Kubernetes en produccion.
- Alta disponibilidad avanzada multi-region.
- BI avanzada y analitica historica compleja.
- SSO corporativo completo (se deja preparado para fase futura).
