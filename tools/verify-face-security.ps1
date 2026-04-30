param(
    [string]$BaseUrl = "http://localhost:7000",
    [string]$ImagePath = "",
    [double]$Threshold = 0.92
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ImagePath)) {
    $ImagePath = Join-Path $env:TEMP "nosvemos-face-check.jpg"
    Add-Type -AssemblyName System.Drawing
    $bmp = New-Object System.Drawing.Bitmap 96, 96
    for ($x = 0; $x -lt 96; $x++) {
        for ($y = 0; $y -lt 96; $y++) {
            $c = if (($x + $y) % 2 -eq 0) { [System.Drawing.Color]::FromArgb(25, 120, 210) } else { [System.Drawing.Color]::FromArgb(220, 235, 248) }
            $bmp.SetPixel($x, $y, $c)
        }
    }
    $bmp.Save($ImagePath, [System.Drawing.Imaging.ImageFormat]::Jpeg)
    $bmp.Dispose()
}

if (-not (Test-Path $ImagePath)) {
    throw "No existe la imagen: $ImagePath"
}

Add-Type -AssemblyName System.Net.Http
$httpClient = [System.Net.Http.HttpClient]::new()

function Invoke-FaceCheck {
    param(
        [string]$ExpectedUser,
        [string]$Context
    )

    $form = [System.Net.Http.MultipartFormDataContent]::new()
    $bytes = [System.IO.File]::ReadAllBytes($ImagePath)
    $fileContent = [System.Net.Http.ByteArrayContent]::new($bytes)
    $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("image/jpeg")

    $form.Add($fileContent, "frame", "frame.jpg")
    $form.Add([System.Net.Http.StringContent]::new($ExpectedUser), "usuarioEsperado")
    $form.Add([System.Net.Http.StringContent]::new(""), "usuarioDetectado")
    $form.Add([System.Net.Http.StringContent]::new("0"), "confianzaRostro")
    $form.Add([System.Net.Http.StringContent]::new("40"), "distanciaCm")
    $form.Add([System.Net.Http.StringContent]::new($Threshold.ToString([System.Globalization.CultureInfo]::InvariantCulture)), "umbralReconocimiento")

    $response = $httpClient.PostAsync("$BaseUrl/api/v1/ia/rostro/verificar?contexto=$Context", $form).Result
    $body = $response.Content.ReadAsStringAsync().Result

    if (-not $response.IsSuccessStatusCode) {
        throw "Error HTTP $($response.StatusCode): $body"
    }

    return ($body | ConvertFrom-Json)
}

Write-Host "==> Verificacion seguridad facial" -ForegroundColor Cyan
Write-Host "BaseUrl: $BaseUrl"
Write-Host "Imagen : $ImagePath"
Write-Host "Umbral : $Threshold"

$cases = @(
    @{ Name = "1N - usuario desconocido"; Expected = ""; Context = "qa_unknown" },
    @{ Name = "1:1 - usuario esperado inexistente"; Expected = "usuario_no_registrado"; Context = "qa_expected_missing" }
)

foreach ($case in $cases) {
    Write-Host "`nCaso: $($case.Name)" -ForegroundColor Yellow
    $result = Invoke-FaceCheck -ExpectedUser $case.Expected -Context $case.Context
    $security = $result.seguridad
    Write-Host "- coincide       : $($security.coincide)"
    Write-Host "- accesoPermitido: $($security.accesoPermitido)"
    Write-Host "- confianza      : $($security.confianza)"
    Write-Host "- motivo         : $($security.motivo)"
}

Write-Host "`nOK. Revisa que casos desconocidos denieguen acceso." -ForegroundColor Green
