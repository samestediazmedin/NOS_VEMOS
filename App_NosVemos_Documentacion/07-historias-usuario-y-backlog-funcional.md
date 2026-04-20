# Historias de usuario y backlog funcional

Este documento organiza, en orden y por microservicio, lo que ya esta implementado y lo que falta por cerrar para llegar a un producto operable.

## Convenciones

- Prioridad: `P1` (critica), `P2` (alta), `P3` (media), `P4` (baja).
- Estado: `Hecho`, `En curso`, `Pendiente`.
- Formato: `Como <rol>, quiero <objetivo>, para <valor>.`

## 1) Autenticacion Servicio

### Historias implementadas (Hecho)

- `AUTH-01` (P1): Como usuario, quiero registrarme con email y password para poder acceder al sistema.
  - Criterios:
    - Registro crea usuario nuevo.
    - No permite duplicados por email.
    - Password se almacena con hash BCrypt.

- `AUTH-02` (P1): Como usuario, quiero iniciar sesion para obtener un token JWT y consumir APIs protegidas.
  - Criterios:
    - Login correcto devuelve `access_token`.
    - Login incorrecto devuelve `401`.

- `AUTH-03` (P1): Como equipo tecnico, quiero pruebas de integracion de auth para evitar regresiones en login/registro.
  - Criterios:
    - Pruebas de registro exitoso, registro duplicado y login correcto.
    - Prueba de password incorrecto.

### Historias pendientes

- `AUTH-04` (P1): Como administrador, quiero revocar sesiones/tokens para cortar accesos comprometidos.
- `AUTH-05` (P2): Como usuario, quiero recuperar password de forma segura.
- `AUTH-06` (P2): Como seguridad, quiero refresh token con expiracion y rotacion.

## 2) Usuarios Servicio

### Historias implementadas (Hecho)

- `USR-01` (P1): Como operador, quiero consultar usuarios para gestionar personas activas.
- `USR-02` (P1): Como operador, quiero crear usuarios desde API.
- `USR-03` (P1): Como seguridad, quiero que el modulo usuarios este protegido con JWT.
- `USR-04` (P2): Como QA, quiero pruebas de autorizacion (sin token y con token).

### Historias pendientes

- `USR-05` (P1): Como operador, quiero editar datos de usuario.
- `USR-06` (P1): Como operador, quiero desactivar/reactivar usuario.
- `USR-07` (P2): Como auditor, quiero historial de cambios por usuario.

## 3) NucleoNegocio Servicio

### Historias implementadas (Hecho)

- `NUC-01` (P1): Como operador, quiero crear expedientes para iniciar un caso.
- `NUC-02` (P1): Como operador, quiero cerrar expedientes para finalizar un caso.
- `NUC-03` (P1): Como seguridad, quiero proteger expedientes con JWT.
- `NUC-04` (P1): Como arquitectura, quiero publicar eventos de dominio al crear/cerrar expediente.
  - Eventos:
    - `expediente.creado`
    - `expediente.cerrado`

### Historias pendientes

- `NUC-05` (P1): Como operador, quiero actualizar estado intermedio del expediente.
- `NUC-06` (P2): Como operador, quiero adjuntar observaciones/bitacora.
- `NUC-07` (P2): Como negocio, quiero reglas de validacion por tipo de expediente.

## 4) Pasarela (Gateway)

### Historias implementadas (Hecho)

- `GTW-01` (P1): Como cliente, quiero un solo punto de entrada para todas las APIs.
- `GTW-02` (P1): Como seguridad, quiero rutas protegidas por JWT en gateway.
  - Publico: `/api/v1/autenticacion/*`
  - Protegido: `/api/v1/usuarios/*`, `/api/v1/expedientes/*`, `/api/v1/ia/*`
- `GTW-03` (P2): Como QA, quiero pruebas de autorizacion en gateway.

### Historias pendientes

- `GTW-04` (P1): Como seguridad, quiero rate limiting por cliente/token.
- `GTW-05` (P2): Como plataforma, quiero circuit breaker y timeout por ruta.
- `GTW-06` (P2): Como observabilidad, quiero correlation-id propagado.

## 5) Orquestador IA

### Historias implementadas (Hecho)

- `IA-01` (P1): Como operador, quiero enviar una captura de camara y recibir evaluacion de riesgo.
- `IA-02` (P1): Como sistema, quiero registrar analisis de brillo/contraste para evaluar condiciones de captura.
- `IA-03` (P1): Como plataforma, quiero emitir eventos de IA para auditoria y trazabilidad.
  - Eventos:
    - `ia.camara.analizado`
    - `ia.rostro.reconocido`
    - `sensor.proximidad.detectada`

### Historias pendientes

- `IA-04` (P1): Como seguridad, quiero verificacion biometrica real con modelo de reconocimiento facial.
- `IA-05` (P1): Como negocio, quiero persistencia de analisis IA en base de datos (no solo memoria).
- `IA-06` (P2): Como operador, quiero historial filtrable de analisis por usuario/fecha/riesgo.

## 6) Auditoria Servicio

### Historias implementadas (Hecho)

- `AUD-01` (P1): Como auditor, quiero consumir eventos de negocio para tener trazabilidad operativa.
- `AUD-02` (P1): Como auditor, quiero recibir eventos de IA/camara/rostro/sensor.

### Historias pendientes

- `AUD-03` (P1): Como auditor, quiero persistir eventos en `AuditoriaDB`.
- `AUD-04` (P1): Como auditor, quiero consultar eventos por modulo, usuario, tipo y fecha.
- `AUD-05` (P2): Como cumplimiento, quiero inmutabilidad y retencion configurable.

## 7) Frontend (Movil/Web)

### Historias implementadas (Hecho)

- `FE-01` (P2): Como operador, quiero una vista de prueba para capturar camara y enviar a IA.
- `FE-02` (P2): Como operador, quiero enviar metadatos de rostro y proximidad en la captura.

### Historias pendientes

- `FE-03` (P1): Como usuario, quiero login visual con manejo de token y sesion.
- `FE-04` (P1): Como operador, quiero dashboard de expedientes y usuarios.
- `FE-05` (P1): Como operador, quiero panel IA con historial, filtros y alertas.
- `FE-06` (P2): Como auditor, quiero panel de eventos en tiempo casi real.

## 8) Historias transversales (DevOps, datos, calidad)

### Historias implementadas (Hecho)

- `OPS-01` (P1): CI base en GitHub Actions (restore + build + verify-security).
- `OPS-02` (P1): Script de verificacion de seguridad y estado de pruebas.
- `DAT-01` (P1): Migraciones iniciales EF para Auth/Usuarios/Nucleo.

### Historias pendientes

- `OPS-03` (P1): Como DevOps, quiero despliegue automatizado por ambiente.
- `OPS-04` (P2): Como SRE, quiero metricas y trazas distribuidas completas.
- `QAL-01` (P1): Como QA, quiero suite de pruebas E2E de flujo completo.
- `SEC-01` (P1): Como seguridad, quiero rotacion de secretos y politicas por entorno.

## 9) Orden recomendado de ejecucion (backlog por fases)

### Fase A - Cerrar persistencia y auditoria (P1)

1. `AUD-03`: Persistir eventos en AuditoriaDB.
2. `AUD-04`: API de consulta de auditoria.
3. `IA-05`: Persistencia de analisis IA.

### Fase B - Frontend operativo (P1)

1. `FE-03`: Login y manejo de sesion.
2. `FE-04`: Vistas usuarios/expedientes.
3. `FE-05`: Vista de analisis IA con historial.

### Fase C - Seguridad y resiliencia (P1/P2)

1. `GTW-04`: Rate limiting.
2. `GTW-05`: Timeout/reintentos/circuit breaker.
3. `SEC-01`: Gestion de secretos por entorno.

### Fase D - Calidad y operacion continua (P2)

1. `QAL-01`: E2E.
2. `OPS-03`: CD por ambiente.
3. `OPS-04`: Observabilidad completa.

## 10) Definicion de terminado (DoD) por historia

Una historia se considera cerrada solo si cumple todo:

- API/funcionalidad implementada y probada.
- Criterios de aceptacion cumplidos.
- Pruebas automatizadas (unit/integration segun aplique).
- Documentacion actualizada (`README` + este backlog).
- Sin secretos hardcodeados.
