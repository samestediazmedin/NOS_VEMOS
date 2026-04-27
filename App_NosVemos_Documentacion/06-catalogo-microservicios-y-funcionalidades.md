# Catalogo de microservicios y funcionalidades

Este documento consolida que hace cada microservicio, su estado actual y la ruta de integracion con los demas modulos.

## Resumen por microservicio

| Microservicio | Carpeta | Rol principal | Estado actual |
|---|---|---|---|
| Pasarela | `App_NosVemos_Pasarela` | Entrada unica y ruteo entre servicios | Implementado (MVP) |
| Autenticacion | `App_NosVemos_Autenticacion_Servicio` | Registro, login y token JWT | Implementado (MVP) |
| Usuarios | `App_NosVemos_Usuarios_Servicio` | Gestion de perfiles y consulta de usuarios | Implementado (MVP) |
| NucleoNegocio | `App_NosVemos_NucleoNegocio_Servicio` | Expedientes y estados del flujo principal | Implementado (MVP) |
| Orquestador IA | `App_NosVemos_Orquestador_IA` | Analisis de imagen de camara y recomendaciones | Implementado (MVP IA) |
| Notificaciones | `App_NosVemos_Notificaciones_Servicio` | Mensajes por eventos (correo/SMS/push) | Definido (pendiente API) |
| Reportes | `App_NosVemos_Reportes_Servicio` | Analitica y exportaciones operativas | Definido (pendiente API) |
| Auditoria | `App_NosVemos_Auditoria_Servicio` | Trazabilidad y log inmutable | Implementado (Worker + API) |

## Funcionalidades implementadas en MVP

### Pasarela

- Rutea solicitudes a servicios de negocio.
- Rutas activas:
  - `/api/v1/autenticacion/*` -> `7001`
  - `/api/v1/usuarios/*` -> `7002`
  - `/api/v1/expedientes/*` -> `7003`
  - `/api/v1/ia/*` -> `7004`

### Autenticacion

- Registro y login de usuarios.
- Emision de JWT.
- Roles base (`Administrador`, `Usuario`).

### Usuarios

- Listado de usuarios.
- Consulta de usuario por id.
- Creacion de usuario.

### NucleoNegocio

- Creacion de expediente.
- Consulta de expedientes.
- Cierre de expediente.

### Orquestador IA + Camara

- Recibe imagen de camara (`multipart/form-data`, campo `frame`).
- Calcula metricas visuales (brillo/contraste).
- Genera clasificacion de riesgo (`Alto`, `Medio`, `Bajo`).
- Devuelve recomendacion automatica por contexto.
- Guarda historial de analisis en memoria.
- Soporte cliente de prueba: `App_NosVemos_Movil/src/camara-ia.html`.

## Persistencia

En los servicios MVP (`Autenticacion`, `Usuarios`, `NucleoNegocio`):

- Provider principal preparado: SQL Server.
- Modo activo por defecto: `UseInMemoryDatabase=true` para asegurar ejecucion local inmediata.
- Para usar SQL Server real: cambiar `UseInMemoryDatabase=false` en los `appsettings.json` correspondientes.

## Integracion esperada en siguientes fases

### Notificaciones

- Consumir eventos de NucleoNegocio y Orquestador IA.
- Gestionar plantillas y reintentos.

### Reportes

- Construir modelos de lectura por eventos.
- Exponer dashboard y exportaciones.

### Auditoria

- Registrar eventos criticos de todos los servicios.
- Exponer consultas filtradas por actor/modulo/fecha.

## Endpoints MVP consolidados

- Pasarela: `http://localhost:7000`
- Autenticacion: `http://localhost:7001`
- Usuarios: `http://localhost:7002`
- NucleoNegocio: `http://localhost:7003`
- Orquestador IA: `http://localhost:7004`
- Auditoria: `http://localhost:7005`

Endpoints importantes:

- `POST /api/v1/autenticacion/login`
- `GET /api/v1/usuarios`
- `POST /api/v1/expedientes`
- `POST /api/v1/ia/analizar-camara`
- `GET /api/v1/ia/analisis`
- `GET /api/v1/auditoria/eventos`
