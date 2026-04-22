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

## Configuracion OpenAI segura (sin hardcode)

- El servicio lee configuracion desde `OpenAI` en `appsettings.json`.
- La API key se toma desde variable de entorno `OPENAI_API_KEY` (o `OpenAI:ApiKey`).
- Si `OpenAI:Enabled=true` pero no hay key, el servicio usa fallback local heuristico.
- Si `OpenAI:RequireActivationPassword=true`, solo activa OpenAI cuando:
  - `OPENAI_OWNER_PASSWORD` y `OPENAI_ACTIVATION_PASSWORD` existen
  - ambos valores coinciden

Ejemplo PowerShell (sesion actual):

```powershell
$env:OPENAI_API_KEY="<tu_api_key>"
$env:OPENAI_OWNER_PASSWORD="<tu_password_dueno>"
$env:OPENAI_ACTIVATION_PASSWORD="<tu_password_dueno>"
$env:OpenAI__Enabled="true"
$env:OpenAI__Model="gpt-5.3-codex"
dotnet run --project App_NosVemos_Orquestador_IA/src/NosVemos.OrquestadorIA.Api
```

## Integracion con camara

- Cliente de prueba: `App_NosVemos_Movil/src/camara-ia.html`
- Ruta recomendada de acceso: `http://localhost:7000/api/v1/ia/analizar-camara`
