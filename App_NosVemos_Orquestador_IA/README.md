# Orquestador IA

Coordina flujos con componentes de IA y servicios internos.

## Ejecutar

```bash
dotnet run --project App_NosVemos_Orquestador_IA/src/NosVemos.OrquestadorIA.Api
```

## Endpoints base

- `GET /health`
- `POST /api/v1/ia/analizar-camara` (multipart con campo `frame`)
- `GET /api/v1/ia/analisis`
- `GET /api/v1/ia/analisis/{id}`

Puerto por defecto: `http://localhost:7004`

## Funcionalidades actuales

- Analiza capturas de camara y calcula brillo/contraste de la imagen.
- Clasifica el nivel de riesgo (`Alto`, `Medio`, `Bajo`) con una recomendacion automatica.
- Guarda historial de analisis en memoria para consulta rapida desde UI o backoffice.

## Integracion con camara

- Cliente de prueba: `App_NosVemos_Movil/src/camara-ia.html`
- Ruta recomendada de acceso: `http://localhost:7000/api/v1/ia/analizar-camara`
