# Alineacion de microservicios para mecanismo de cerradura (ESP32/Arduino)

Este documento alinea el contexto de `fake_backend.py` con la arquitectura real de NOS VEMOS, separando responsabilidades por microservicio y manteniendo un flujo biometrico compatible con hardware (ESP32-CAM + Arduino Mega).

## 1) Objetivo

Reemplazar el backend monolitico simulado por un flujo distribuido entre Pasarela, Orquestador IA, Nucleo de Negocio y Auditoria, sin perder el comportamiento operativo del mecanismo de acceso.

## 2) Contrato del simulador y mapeo a microservicios

Contrato observado en `fake_backend.py`:

- `POST /verify`
- `GET /comando/{device_id}`
- `POST /enroll/{registro_id}`
- `POST /admin/aprobar`

Mapeo propuesto en NOS VEMOS:

| Endpoint simulador | Dominio real | Microservicio responsable | Ruta recomendada via Pasarela |
|---|---|---|---|
| `POST /verify` | Verificacion facial 1:N | `Orquestador IA` | `POST /api/v1/ia/analizar-camara` |
| `GET /comando/{device_id}` | Consulta de accion para actuador | `NucleoNegocio` | `GET /api/v1/dispositivos/{deviceId}/comandos` |
| `POST /enroll/{registro_id}` | Enrolamiento biometrico por muestras | `Orquestador IA` | `POST /api/v1/ia/enrolar-rostro` |
| `POST /admin/aprobar` | Aprobacion administrativa de alta | `NucleoNegocio` + `Auditoria` | `POST /api/v1/expedientes/{id}/aprobar-acceso` |

## 3) Responsabilidades por microservicio

### Pasarela (`App_NosVemos_Pasarela`)

- Punto unico de entrada para firmware y frontend.
- Valida JWT para panel/admin y token de dispositivo para hardware.
- Enruta a IA, negocio y auditoria.

### Orquestador IA (`App_NosVemos_Orquestador_IA`)

- Analiza frame de camara y produce decision biometrica.
- Mantiene enrolamiento de muestras faciales por usuario.
- Publica eventos de reconocimiento para trazabilidad.

### Nucleo de Negocio (`App_NosVemos_NucleoNegocio_Servicio`)

- Gestiona estado de acceso del mecanismo (permitido/denegado/pendiente).
- Entrega comandos al dispositivo (`abrir`, `capturar_registro`, `ninguno`).
- Orquesta reglas de negocio (reintentos, fallback, aprobaciones).

### Auditoria (`App_NosVemos_Auditoria_Servicio`)

- Registra eventos de verificacion, aprobacion y accion del actuador.
- Permite consulta por dispositivo, usuario y ventana de tiempo.

## 4) Flujo operativo recomendado

1. ESP32-CAM envia frame a `POST /api/v1/ia/analizar-camara`.
2. IA responde reconocimiento (`usuarioDetectado`, `confianzaRostro`).
3. Nucleo decide si generar comando de apertura para `device_id`.
4. Arduino/ESP32 consulta comando en `GET /api/v1/dispositivos/{deviceId}/comandos`.
5. Si hay nuevo usuario aprobado, dispositivo recibe `capturar_registro` y envia 3 muestras a `POST /api/v1/ia/enrolar-rostro`.
6. Todos los eventos se registran en Auditoria.

## 5) Estado actual vs gap minimo

Ya disponible:

- IA: analisis de camara y enrolamiento de rostro.
- Pasarela: rutas para `ia`, `expedientes`, `auditoria`.
- Auditoria: persistencia y consulta de eventos.

Pendiente para cerrar el caso "cerradura":

- Endpoint formal de comandos por `device_id` en Nucleo.
- Token de dispositivo para hardware (separado de JWT humano).
- Regla de negocio explicita de `puede_ingresar` -> comando `abrir`.

## 6) Compatibilidad con firmware existente

Para no romper el firmware actual durante transicion:

- Mantener un adaptador temporal de contrato (bridge) que traduzca:
  - `/verify` -> `/api/v1/ia/analizar-camara`
  - `/comando/{device_id}` -> `/api/v1/dispositivos/{deviceId}/comandos`
  - `/enroll/{registro_id}` -> `/api/v1/ia/enrolar-rostro`
- Retirar el bridge cuando el firmware consuma rutas versionadas de Pasarela.

## 7) Criterios de aceptacion para el mecanismo

- La camara identifica automaticamente (1:N) y responde decision en tiempo operativo.
- El actuador recibe un comando unico y trazable por cada intento valido.
- Cada apertura/denegacion queda auditada con `device_id`, usuario, score y timestamp.
- El flujo de alta (3 capturas) queda integrado al enrolamiento de IA.
