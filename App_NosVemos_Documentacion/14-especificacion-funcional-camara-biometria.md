# Especificacion funcional - Camara y biometria facial

Este documento define el alcance funcional, UX, reglas de negocio, APIs y criterios de QA para el flujo completo de camara del sistema NOS VEMOS.

## 1. Objetivo

Habilitar identificacion biometrica facial de usuarios inscritos para activar dispositivos de forma rapida y trazable, con control administrativo y fallback seguro.

## 2. Alcance funcional

Incluye:

- Enrolamiento biometrico asistido por administrador (3 fotos obligatorias).
- Validacion de calidad de captura antes de guardar.
- Generacion y almacenamiento de plantilla facial.
- Reconocimiento facial en vivo para activacion de dispositivo.
- Registro de eventos en auditoria.
- Fallback por metodo secundario cuando no hay coincidencia.

No incluye (fuera de alcance inicial):

- Reentrenamiento manual de modelos desde UI.
- Alta masiva biometrica por lote.

## 3. Roles

- `Admin`: ejecuta alta biometrica, aprobacion, baja biometrica y override controlado.
- `Operador`: usa reconocimiento en vivo para operacion diaria.
- `Auditor`: consulta trazabilidad y evidencias.
- `Usuario`: titular de la identidad biometrica.

## 4. Estados del usuario biometrico

- `pendiente_biometria`: usuario registrado sin plantilla facial activa.
- `enrolamiento_en_progreso`: captura parcial (1 o 2 fotos validas).
- `biometria_activa`: plantilla facial validada y habilitada para reconocimiento.
- `biometria_rechazada`: intento de enrolamiento fallido por calidad/validacion.
- `biometria_suspendida`: identidad deshabilitada por seguridad o mantenimiento.

## 5. Flujo E2E (camara)

1. Usuario se registra e inicia sesion.
2. Admin abre modulo de camara y selecciona usuario.
3. Sistema guia la captura de 3 fotos: frontal, semiperfil izquierdo, semiperfil derecho.
4. Cada foto pasa validacion de calidad en tiempo real.
5. Si las 3 fotos cumplen umbral, se genera plantilla facial y se guarda.
6. Usuario cambia a `biometria_activa`.
7. En operacion, camara captura rostro y la IA compara contra plantilla.
8. Si `score >= umbral`, se activa dispositivo y se audita el evento.
9. Si `score < umbral`, se permite reintento (max N) y luego fallback seguro.

## 6. UX/UI requerida

### 6.1 Pantalla: Alta biometrica (Admin)

Componentes minimos:

- Video preview en vivo.
- Mascara/guia facial centrada.
- Indicadores de calidad: iluminacion, nitidez, pose, rostro detectado.
- Progreso de capturas: `1/3`, `2/3`, `3/3`.
- Acciones: `Capturar`, `Recapturar`, `Cancelar`, `Guardar enrolamiento`.
- Mensajeria clara de error (ejemplo: `Baja iluminacion, acerquese a una fuente de luz`).

### 6.2 Pantalla: Resultado de enrolamiento

- Resumen de calidad por foto.
- Estado final: `Aprobado` o `Requiere nueva captura`.
- Metadata: admin responsable, fecha, dispositivo de captura.

### 6.3 Pantalla: Reconocimiento en vivo

- Preview camara.
- Estado de pipeline: `Detectando`, `Analizando`, `Coincidencia`, `Sin coincidencia`.
- Score mostrado en porcentaje y decision final.
- Indicador de latencia por intento.
- Acciones: `Reintentar`, `Activar por fallback` (si rol/autorizacion aplica).

### 6.4 Pantalla: Auditoria biometrica

- Filtros por usuario, fecha, dispositivo, resultado, operador.
- Tabla de eventos con detalle tecnico.
- Exportacion CSV/PDF.

## 7. Reglas de negocio

1. `RN-01`: El enrolamiento requiere 3 fotos validas obligatorias.
2. `RN-02`: Si una foto no cumple calidad minima, no se puede cerrar enrolamiento.
3. `RN-03`: Solo `Admin` puede aprobar/suspender biometria.
4. `RN-04`: El reconocimiento para activacion de dispositivo requiere `biometria_activa`.
5. `RN-05`: Debe existir umbral configurable de coincidencia por entorno.
6. `RN-06`: Maximo de reintentos por sesion de reconocimiento (default: 3).
7. `RN-07`: Al superar maximo de reintentos se exige fallback (PIN/credencial secundaria).
8. `RN-08`: Todo evento biometrico debe registrarse en auditoria.
9. `RN-09`: Cambios de estado biometrico son inmutables en bitacora (append-only).
10. `RN-10`: Sin consentimiento biometrico explicito no se permite captura.

## 8. Historias de usuario (con criterios)

### 8.1 Enrolamiento

- `BIO-01` (P1): Como admin, quiero capturar 3 fotos guiadas para habilitar biometria del usuario.
  - Criterios:
    - No permite finalizar con menos de 3 fotos validas.
    - Muestra calidad por captura y permite recaptura.

- `BIO-02` (P1): Como admin, quiero aprobar o rechazar enrolamiento segun calidad final para garantizar precision.
  - Criterios:
    - Estado final queda en `biometria_activa` o `biometria_rechazada`.
    - Se registra quien aprobo/rechazo y por que.

### 8.2 Reconocimiento y activacion

- `BIO-03` (P1): Como operador, quiero reconocer al usuario por camara para activar el dispositivo sin friccion.
  - Criterios:
    - Si `score >= umbral`, activa dispositivo.
    - Respuesta visual en menos de 2 segundos p95.

- `BIO-04` (P1): Como operador, quiero fallback seguro cuando falla reconocimiento para no detener la operacion.
  - Criterios:
    - Limita reintentos y habilita metodo secundario.
    - Registra motivo de fallback en auditoria.

### 8.3 Seguridad y auditoria

- `BIO-05` (P1): Como auditor, quiero trazabilidad completa de capturas, decisiones y activaciones para cumplimiento.
  - Criterios:
    - Consulta filtrable por fecha, usuario, operador y resultado.
    - Exportacion de reporte.

## 9. Criterios Gherkin

```gherkin
Feature: Enrolamiento biometrico por admin

  Scenario: Enrolamiento exitoso con 3 fotos validas
    Given un usuario en estado "pendiente_biometria"
    And un admin autenticado en el modulo de camara
    When captura foto frontal valida
    And captura foto semiperfil izquierdo valida
    And captura foto semiperfil derecho valida
    And confirma guardar enrolamiento
    Then el sistema genera plantilla facial
    And cambia el estado del usuario a "biometria_activa"
    And registra evento en auditoria

  Scenario: Enrolamiento rechazado por calidad insuficiente
    Given un usuario en estado "pendiente_biometria"
    When al menos una captura no cumple calidad minima
    Then el sistema impide cerrar enrolamiento
    And muestra recomendacion de recaptura
```

```gherkin
Feature: Reconocimiento y activacion de dispositivo

  Scenario: Activacion automatica por coincidencia valida
    Given un usuario con estado "biometria_activa"
    When la camara detecta rostro y el score es mayor o igual al umbral
    Then el sistema activa el dispositivo
    And registra evento de activacion exitosa en auditoria

  Scenario: Fallback por no coincidencia
    Given un intento de reconocimiento
    When el score es menor al umbral en 3 reintentos
    Then el sistema bloquea nuevos reintentos temporales
    And solicita metodo secundario autorizado
    And registra evento de fallback en auditoria
```

## 10. Contratos API propuestos

### 10.1 Enrolamiento

- `POST /api/v1/ia/biometria/enrolamientos`
  - Request: `userId`, `capturas[]` (3 imagenes + metadata), `consentimiento=true`.
  - Response `201`: `enrolamientoId`, `estado`, `qualitySummary`, `createdAt`.

- `POST /api/v1/ia/biometria/enrolamientos/{enrolamientoId}/aprobar`
  - Request: `aprobadoPor`, `comentario`.
  - Response `200`: `estado=biometria_activa`.

- `POST /api/v1/ia/biometria/enrolamientos/{enrolamientoId}/rechazar`
  - Request: `rechazadoPor`, `motivo`.
  - Response `200`: `estado=biometria_rechazada`.

### 10.2 Reconocimiento

- `POST /api/v1/ia/biometria/reconocimientos`
  - Request: `deviceId`, `frame` o `imagen`, `sessionId`.
  - Response `200`: `resultado`, `score`, `userId?`, `latencyMs`, `decision`.

- `POST /api/v1/ia/biometria/reconocimientos/{id}/activar-dispositivo`
  - Request: `metodo` (`face_match` o `fallback`), `operatorId`.
  - Response `200`: `activationId`, `status`.

### 10.3 Auditoria

- `GET /api/v1/auditoria/biometria/eventos?userId=&resultado=&desde=&hasta=&deviceId=`
  - Response `200`: lista paginada de eventos.

## 11. Modelo de datos minimo

- `BiometricProfile`
  - `Id`, `UserId`, `Estado`, `TemplateVersion`, `QualityScorePromedio`, `CreatedAt`, `UpdatedAt`.
- `BiometricSample`
  - `Id`, `ProfileId`, `PoseType`, `QualityScore`, `CaptureDevice`, `CapturedAt`.
- `RecognitionEvent`
  - `Id`, `SessionId`, `DeviceId`, `MatchResult`, `Score`, `LatencyMs`, `ReasonCode`, `CreatedAt`.
- `DeviceActivation`
  - `Id`, `RecognitionEventId`, `Metodo`, `Status`, `OperatorId`, `CreatedAt`.

## 12. NFR (no funcionales)

- Latencia reconocimiento p95 <= 2000 ms.
- Disponibilidad del flujo de reconocimiento >= 99.5%.
- Trazabilidad: 100% de eventos biometrico-operativos auditados.
- Seguridad: JWT valido + RBAC para operaciones criticas.

## 13. Checklist QA (lista ejecutable)

### 13.1 QA funcional

- [ ] Alta biometrica no finaliza con menos de 3 fotos.
- [ ] Recaptura disponible por cada paso.
- [ ] Usuario queda en `biometria_activa` tras aprobacion.
- [ ] Reconocimiento exitoso activa dispositivo.
- [ ] Fallo tras 3 intentos deriva a fallback.

### 13.2 QA UX

- [ ] Mensajes de error son claros y accionables.
- [ ] Estado visual de pipeline se actualiza sin bloqueos.
- [ ] Responsive correcto en desktop/tablet/mobile.

### 13.3 QA seguridad

- [ ] Endpoint de enrolamiento solo accesible por `Admin`.
- [ ] Sin token valido, API responde `401`.
- [ ] Sin permisos de rol, API responde `403`.
- [ ] Eventos criticos quedan registrados en auditoria.

### 13.4 QA rendimiento

- [ ] p95 de reconocimiento dentro de objetivo (< 2000 ms).
- [ ] Reintentos no degradan el servicio de forma acumulativa.

## 14. Plan de implementacion sugerido

1. Backend enrolamiento + validacion calidad + estados.
2. Backend reconocimiento + activacion + auditoria de eventos.
3. UI admin de captura 3-fotos con guia de calidad.
4. UI operativa de reconocimiento en vivo.
5. Fallback y politicas de seguridad.
6. Pruebas E2E con escenarios reales de iluminacion y angulos.

## 15. Definicion de terminado (DoD)

Se considera terminado cuando:

- Historias `BIO-01` a `BIO-05` cumplen criterios.
- Flujos Gherkin pasan en pruebas automatizadas.
- Auditoria registra 100% de eventos del flujo.
- Documentacion API y UX queda actualizada.
