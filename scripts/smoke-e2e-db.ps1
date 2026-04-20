$ErrorActionPreference = "Stop"

function New-TestImageJpeg {
    param(
        [string]$Path
    )

    Add-Type -AssemblyName System.Drawing
    $bmp = New-Object System.Drawing.Bitmap 64, 64
    for ($x = 0; $x -lt 64; $x++) {
        for ($y = 0; $y -lt 64; $y++) {
            $color = if (($x + $y) % 2 -eq 0) { [System.Drawing.Color]::FromArgb(30, 180, 90) } else { [System.Drawing.Color]::FromArgb(20, 70, 140) }
            $bmp.SetPixel($x, $y, $color)
        }
    }
    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Jpeg)
    $bmp.Dispose()
}

function Invoke-Json {
    param(
        [string]$Method,
        [string]$Url,
        [object]$Body,
        [hashtable]$Headers
    )

    $payload = if ($null -ne $Body) { $Body | ConvertTo-Json -Depth 10 } else { $null }
    return Invoke-RestMethod -Method $Method -Uri $Url -ContentType "application/json" -Headers $Headers -Body $payload
}

Write-Host "==> E2E smoke: inicio" -ForegroundColor Cyan

$gateway = "http://localhost:7000"
$email = "smoke-$(Get-Date -Format 'yyyyMMddHHmmss')@nosvemos.local"
$password = "Pass123*"

Write-Host "==> Registro" -ForegroundColor Cyan
$null = Invoke-Json -Method "POST" -Url "$gateway/api/v1/autenticacion/registro" -Body @{ Email = $email; Password = $password } -Headers @{}

Write-Host "==> Login" -ForegroundColor Cyan
$login = Invoke-Json -Method "POST" -Url "$gateway/api/v1/autenticacion/login" -Body @{ Email = $email; Password = $password } -Headers @{}
$token = $login.access_token
if ([string]::IsNullOrWhiteSpace($token)) {
    throw "No se obtuvo access_token en login."
}
$authHeaders = @{ Authorization = "Bearer $token" }

Write-Host "==> Usuarios (GET)" -ForegroundColor Cyan
$null = Invoke-RestMethod -Method GET -Uri "$gateway/api/v1/usuarios" -Headers $authHeaders

Write-Host "==> Expediente (POST)" -ForegroundColor Cyan
$codigo = "EXP-SMOKE-$(Get-Date -Format 'HHmmss')"
$expediente = Invoke-Json -Method "POST" -Url "$gateway/api/v1/expedientes" -Body @{ Codigo = $codigo } -Headers $authHeaders

if ($null -eq $expediente.id) {
    throw "No se obtuvo ID de expediente creado."
}

Write-Host "==> IA (POST analizar-camara)" -ForegroundColor Cyan
$tmpJpg = Join-Path $env:TEMP "nosvemos-smoke.jpg"
New-TestImageJpeg -Path $tmpJpg

$form = [System.Net.Http.MultipartFormDataContent]::new()
$bytes = [System.IO.File]::ReadAllBytes($tmpJpg)
$fileContent = [System.Net.Http.ByteArrayContent]::new($bytes)
$fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("image/jpeg")
$form.Add($fileContent, "frame", "captura.jpg")
$form.Add([System.Net.Http.StringContent]::new("usuario_demo"), "usuarioEsperado")
$form.Add([System.Net.Http.StringContent]::new("usuario_demo"), "usuarioDetectado")
$form.Add([System.Net.Http.StringContent]::new("0.93"), "confianzaRostro")
$form.Add([System.Net.Http.StringContent]::new("42"), "distanciaCm")

$httpClient = [System.Net.Http.HttpClient]::new()
$httpClient.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $token)
$iaResponse = $httpClient.PostAsync("$gateway/api/v1/ia/analizar-camara?contexto=smoke", $form).Result
if (-not $iaResponse.IsSuccessStatusCode) {
    $err = $iaResponse.Content.ReadAsStringAsync().Result
    throw "Fallo IA: $($iaResponse.StatusCode) - $err"
}

Write-Host "==> Esperando auditoria async" -ForegroundColor Cyan
Start-Sleep -Seconds 3

Write-Host "==> Validacion SQL en contenedor" -ForegroundColor Cyan
$queries = @(
    "SELECT COUNT(1) AS total_auth FROM AutenticacionDB.dbo.Usuarios;",
    "SELECT COUNT(1) AS total_usuarios FROM UsuariosDB.dbo.Usuarios;",
    "SELECT COUNT(1) AS total_expedientes FROM NucleoDB.dbo.Expedientes;",
    "SELECT COUNT(1) AS total_ia FROM IADB.dbo.Analisis;",
    "SELECT COUNT(1) AS total_auditoria FROM AuditoriaDB.dbo.Eventos;"
)

foreach ($q in $queries) {
    docker exec nosvemos-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Your_strong_password_123!" -Q $q -C
}

Write-Host "==> E2E smoke completado OK" -ForegroundColor Green
