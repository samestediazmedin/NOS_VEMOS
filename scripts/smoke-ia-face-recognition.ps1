param(
    [string]$GatewayUrl = "http://localhost:7000",
    [string]$Email = "",
    [string]$Password = "Pass123*"
)

$ErrorActionPreference = "Stop"

function New-SyntheticFaceImage {
    param(
        [string]$Path,
        [string]$Variant
    )

    Add-Type -AssemblyName System.Drawing
    $bmp = New-Object System.Drawing.Bitmap 128, 128
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::FromArgb(238, 210, 186))
    $g.FillEllipse([System.Drawing.Brushes]::Black, 40, 44, 12, 12)
    $g.FillEllipse([System.Drawing.Brushes]::Black, 76, 44, 12, 12)
    $g.DrawArc((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(120, 40, 30), 4)), 40, 62, 48, 28, 15, 150)
    $g.DrawEllipse((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(80, 40, 20), 3)), 28, 20, 72, 92)

    if ($Variant -eq "other") {
        $g.Clear([System.Drawing.Color]::FromArgb(208, 222, 245))
        $g.FillEllipse([System.Drawing.Brushes]::Black, 34, 44, 14, 14)
        $g.FillEllipse([System.Drawing.Brushes]::Black, 80, 44, 14, 14)
        $g.DrawArc((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(20, 20, 120), 4)), 35, 58, 56, 26, 200, 150)
        $g.DrawRectangle((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(30, 80, 120), 3)), 24, 18, 80, 96)
    }

    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Jpeg)
    $g.Dispose()
    $bmp.Dispose()
}

function Invoke-AuthJson {
    param(
        [string]$Method,
        [string]$Url,
        [object]$Body,
        [hashtable]$Headers
    )

    $payload = $Body | ConvertTo-Json -Depth 10
    return Invoke-RestMethod -Method $Method -Uri $Url -ContentType "application/json" -Headers $Headers -Body $payload
}

function Invoke-Multipart {
    param(
        [string]$Url,
        [string]$Token,
        [string]$ImagePath,
        [hashtable]$Fields
    )

    $form = [System.Net.Http.MultipartFormDataContent]::new()
    $bytes = [System.IO.File]::ReadAllBytes($ImagePath)
    $fileContent = [System.Net.Http.ByteArrayContent]::new($bytes)
    $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("image/jpeg")
    $form.Add($fileContent, "frame", [System.IO.Path]::GetFileName($ImagePath))

    foreach ($kv in $Fields.GetEnumerator()) {
        $form.Add([System.Net.Http.StringContent]::new([string]$kv.Value), [string]$kv.Key)
    }

    $http = [System.Net.Http.HttpClient]::new()
    $http.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $Token)
    $response = $http.PostAsync($Url, $form).Result
    $body = $response.Content.ReadAsStringAsync().Result
    if (-not $response.IsSuccessStatusCode) {
        throw "Error HTTP $($response.StatusCode): $body"
    }

    return $body | ConvertFrom-Json
}

if ([string]::IsNullOrWhiteSpace($Email)) {
    $Email = "ia-smoke-$(Get-Date -Format 'yyyyMMddHHmmss')@nosvemos.local"
}

Write-Host "==> Registro/Login para pruebas IA" -ForegroundColor Cyan
$null = Invoke-AuthJson -Method "POST" -Url "$GatewayUrl/api/v1/autenticacion/registro" -Body @{ Email = $Email; Password = $Password } -Headers @{}
$login = Invoke-AuthJson -Method "POST" -Url "$GatewayUrl/api/v1/autenticacion/login" -Body @{ Email = $Email; Password = $Password } -Headers @{}

if ([string]::IsNullOrWhiteSpace($login.access_token)) {
    throw "No se obtuvo token de autenticacion."
}

$samePath = Join-Path $env:TEMP "nosvemos-face-same.jpg"
$otherPath = Join-Path $env:TEMP "nosvemos-face-other.jpg"
New-SyntheticFaceImage -Path $samePath -Variant "same"
New-SyntheticFaceImage -Path $otherPath -Variant "other"

Write-Host "==> Entrenando perfil de rostro" -ForegroundColor Cyan
$train = Invoke-Multipart -Url "$GatewayUrl/api/v1/ia/rostro/entrenar" -Token $login.access_token -ImagePath $samePath -Fields @{ usuario = "usuario_demo" }
Write-Host "Muestras entrenadas: $($train.muestras)" -ForegroundColor Green

Write-Host "==> Verificando imagen del mismo usuario" -ForegroundColor Cyan
$sameVerify = Invoke-Multipart -Url "$GatewayUrl/api/v1/ia/rostro/verificar" -Token $login.access_token -ImagePath $samePath -Fields @{ usuarioEsperado = "usuario_demo" }

Write-Host "==> Verificando imagen de usuario diferente" -ForegroundColor Cyan
$otherVerify = Invoke-Multipart -Url "$GatewayUrl/api/v1/ia/rostro/verificar" -Token $login.access_token -ImagePath $otherPath -Fields @{ usuarioEsperado = "usuario_demo" }

Write-Host "Resultado mismo usuario: confianza=$($sameVerify.confianza) coincide=$($sameVerify.coincide) exacto=$($sameVerify.esExacto)" -ForegroundColor Green
Write-Host "Resultado usuario distinto: confianza=$($otherVerify.confianza) coincide=$($otherVerify.coincide) exacto=$($otherVerify.esExacto)" -ForegroundColor Yellow

if (-not $sameVerify.coincide) {
    throw "La verificacion del mismo usuario no coincidio."
}

if ($otherVerify.esExacto) {
    throw "La verificacion del usuario distinto se marco como exacta; revisar umbral/modelo."
}

Write-Host "==> Smoke IA reconocimiento por pixeles completado OK" -ForegroundColor Green
