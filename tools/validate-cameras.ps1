param(
    [string]$IaBaseUrl = "http://localhost:7004",
    [string[]]$Cameras = @("camara_1", "camara_2"),
    [int]$AttemptsPerUser = 3,
    [double]$Threshold = 0.82,
    [string]$DatasetPath = "",
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($DatasetPath)) {
    $DatasetPath = ".\dataset"
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = ".\artifacts"
}

function Clamp-Int {
    param(
        [int]$Value,
        [int]$Min,
        [int]$Max
    )

    if ($Value -lt $Min) { return $Min }
    if ($Value -gt $Max) { return $Max }
    return $Value
}

function New-ValidationImageJpeg {
    param(
        [string]$Path,
        [int]$Seed,
        [string]$Label
    )

    Add-Type -AssemblyName System.Drawing
    $bmp = New-Object System.Drawing.Bitmap 112, 112
    $rng = [System.Random]::new($Seed)
    $baseR = 30 + $rng.Next(0, 140)
    $baseG = 30 + $rng.Next(0, 140)
    $baseB = 30 + $rng.Next(0, 140)

    for ($x = 0; $x -lt 112; $x++) {
        for ($y = 0; $y -lt 112; $y++) {
            $noise = $rng.Next(-24, 25)
            $r = Clamp-Int -Value ($baseR + $noise + [int]($x / 9)) -Min 0 -Max 255
            $g = Clamp-Int -Value ($baseG + $noise + [int]($y / 9)) -Min 0 -Max 255
            $b = Clamp-Int -Value ($baseB + $noise + [int](($x + $y) / 20)) -Min 0 -Max 255
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($r, $g, $b))
        }
    }

    $graphics = [System.Drawing.Graphics]::FromImage($bmp)
    $font = New-Object System.Drawing.Font("Arial", 10)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $graphics.DrawString($Label, $font, $brush, 5, 5)
    $graphics.Dispose()

    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Jpeg)
    $bmp.Dispose()
}

function Invoke-AnalyzeCamera {
    param(
        [System.Net.Http.HttpClient]$Client,
        [string]$BaseUrl,
        [string]$Camera,
        [string]$ImagePath,
        [string]$ExpectedUser,
        [double]$ThresholdValue
    )

    $form = [System.Net.Http.MultipartFormDataContent]::new()
    $bytes = [System.IO.File]::ReadAllBytes($ImagePath)
    $fileContent = [System.Net.Http.ByteArrayContent]::new($bytes)
    $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("image/jpeg")
    $form.Add($fileContent, "frame", "validacion.jpg")
    $form.Add([System.Net.Http.StringContent]::new($ExpectedUser), "usuarioEsperado")
    $form.Add([System.Net.Http.StringContent]::new(""), "usuarioDetectado")
    $form.Add([System.Net.Http.StringContent]::new("0"), "confianzaRostro")
    $form.Add([System.Net.Http.StringContent]::new("50"), "distanciaCm")
    $form.Add([System.Net.Http.StringContent]::new($ThresholdValue.ToString([System.Globalization.CultureInfo]::InvariantCulture)), "umbralReconocimiento")

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $response = $Client.PostAsync("$BaseUrl/api/v1/ia/analizar-camara?contexto=$Camera", $form).Result
    $stopwatch.Stop()

    $body = $response.Content.ReadAsStringAsync().Result
    if (-not $response.IsSuccessStatusCode) {
        throw "Fallo analisis ($Camera): $($response.StatusCode) - $body"
    }

    $payload = $body | ConvertFrom-Json
    $detectedUser = ""
    $confidence = 0.0
    if ($null -ne $payload -and $null -ne $payload.biometria) {
        if ($null -ne $payload.biometria.usuarioDetectado) {
            $detectedUser = [string]$payload.biometria.usuarioDetectado
        }
        if ($null -ne $payload.biometria.confianzaRostro) {
            $confidence = [double]$payload.biometria.confianzaRostro
        }
    }

    return [PSCustomObject]@{
        DetectedUser = $detectedUser
        Confidence = $confidence
        LatencyMs = [int]$stopwatch.ElapsedMilliseconds
    }
}

function Get-P95 {
    param([int[]]$Values)

    if ($null -eq $Values -or $Values.Count -eq 0) {
        return 0
    }

    $sorted = $Values | Sort-Object
    $index = [Math]::Ceiling($sorted.Count * 0.95) - 1
    if ($index -lt 0) { $index = 0 }
    if ($index -ge $sorted.Count) { $index = $sorted.Count - 1 }
    return [int]$sorted[$index]
}

function New-Metrics {
    param(
        [string]$Name,
        [object[]]$Rows
    )

    $allRows = @($Rows)
    $total = $allRows.Count
    $correct = @($allRows | Where-Object { $_.Correct }).Count
    $positives = @($allRows | Where-Object { -not $_.IsNegative })
    $negatives = @($allRows | Where-Object { $_.IsNegative })

    $positiveTotal = $positives.Count
    $negativeTotal = $negatives.Count
    $falseRejects = @($positives | Where-Object { -not $_.Correct }).Count
    $falseAccepts = @($negatives | Where-Object { -not $_.Correct }).Count

    $accuracy = if ($total -gt 0) { [Math]::Round((100.0 * $correct / $total), 2) } else { 0 }
    $frr = if ($positiveTotal -gt 0) { [Math]::Round((100.0 * $falseRejects / $positiveTotal), 2) } else { 0 }
    $far = if ($negativeTotal -gt 0) { [Math]::Round((100.0 * $falseAccepts / $negativeTotal), 2) } else { 0 }
    $latencyAvg = if ($total -gt 0) { [Math]::Round((($Rows | Measure-Object -Property LatencyMs -Average).Average), 2) } else { 0 }
    $latencyP95 = Get-P95 -Values ($Rows | ForEach-Object { [int]$_.LatencyMs })

    return [PSCustomObject]@{
        Camera = $Name
        Total = $total
        Correct = $correct
        Accuracy = $accuracy
        PositiveTotal = $positiveTotal
        NegativeTotal = $negativeTotal
        FalseRejects = $falseRejects
        FalseAccepts = $falseAccepts
        FRR = $frr
        FAR = $far
        AvgLatencyMs = $latencyAvg
        P95LatencyMs = $latencyP95
    }
}

Write-Host "==> Validacion de reconocimiento (2 camaras)" -ForegroundColor Cyan
Write-Host "Base URL: $IaBaseUrl"
Write-Host "Camaras: $($Cameras -join ', ')"
Write-Host "Umbral: $Threshold"

$healthUrl = "$IaBaseUrl/health"
try {
    $null = Invoke-RestMethod -Method GET -Uri $healthUrl
}
catch {
    throw "No se pudo conectar al Orquestador IA en $IaBaseUrl. Inicia el servicio primero."
}

$enrolled = Invoke-RestMethod -Method GET -Uri "$IaBaseUrl/api/v1/ia/rostros/enrolados"
if ($null -eq $enrolled -or $enrolled.Count -eq 0) {
    throw "No hay usuarios enrolados. Primero registra perfiles desde el flujo de alta biometrica."
}

Add-Type -AssemblyName System.Net.Http
$httpClient = [System.Net.Http.HttpClient]::new()

$tmpRoot = Join-Path $env:TEMP "nosvemos-ia-validate"
if (-not (Test-Path $tmpRoot)) {
    New-Item -Path $tmpRoot -ItemType Directory | Out-Null
}

$rows = @()
$angles = @("frontal", "izquierda", "derecha")

foreach ($camera in $Cameras) {
    Write-Host "`n==> Camara: $camera" -ForegroundColor Yellow

    foreach ($entry in $enrolled) {
        $userId = [string]$entry.userId
        $userName = [string]$entry.userName
        if ([string]::IsNullOrWhiteSpace($userId)) {
            continue
        }

        for ($attempt = 1; $attempt -le $AttemptsPerUser; $attempt++) {
            $angle = $angles[($attempt - 1) % $angles.Count]
            $seed = [Math]::Abs(("$camera-$userId-$attempt-$angle".GetHashCode()))
            $syntheticPath = Join-Path $tmpRoot "$camera-$userId-$attempt-$angle.jpg"
            New-ValidationImageJpeg -Path $syntheticPath -Seed $seed -Label "$userId-$angle"

            $result = Invoke-AnalyzeCamera -Client $httpClient -BaseUrl $IaBaseUrl -Camera $camera -ImagePath $syntheticPath -ExpectedUser $userId -ThresholdValue $Threshold
            $correct = ($result.DetectedUser -eq $userId)
            $rows += [PSCustomObject]@{
                Camera = $camera
                Source = "synthetic"
                Case = "positive"
                ExpectedUser = $userId
                DetectedUser = $result.DetectedUser
                Confidence = [Math]::Round($result.Confidence, 4)
                LatencyMs = $result.LatencyMs
                Correct = $correct
                IsNegative = $false
                ImagePath = $syntheticPath
            }
        }

        $datasetUserPath = Join-Path (Join-Path $DatasetPath $camera) $userId
        if (Test-Path $datasetUserPath) {
            $userImages = Get-ChildItem -Path $datasetUserPath -File -Include *.jpg, *.jpeg, *.png
            foreach ($img in $userImages) {
                $result = Invoke-AnalyzeCamera -Client $httpClient -BaseUrl $IaBaseUrl -Camera $camera -ImagePath $img.FullName -ExpectedUser $userId -ThresholdValue $Threshold
                $correct = ($result.DetectedUser -eq $userId)
                $rows += [PSCustomObject]@{
                    Camera = $camera
                    Source = "dataset"
                    Case = "positive"
                    ExpectedUser = $userId
                    DetectedUser = $result.DetectedUser
                    Confidence = [Math]::Round($result.Confidence, 4)
                    LatencyMs = $result.LatencyMs
                    Correct = $correct
                    IsNegative = $false
                    ImagePath = $img.FullName
                }
            }
        }
    }

    for ($negAttempt = 1; $negAttempt -le $AttemptsPerUser; $negAttempt++) {
        $seed = [Math]::Abs(("$camera-neg-$negAttempt".GetHashCode()))
        $negPath = Join-Path $tmpRoot "$camera-neg-$negAttempt.jpg"
        New-ValidationImageJpeg -Path $negPath -Seed $seed -Label "neg-$camera-$negAttempt"

        $negResult = Invoke-AnalyzeCamera -Client $httpClient -BaseUrl $IaBaseUrl -Camera $camera -ImagePath $negPath -ExpectedUser "" -ThresholdValue $Threshold
        $negCorrect = [string]::IsNullOrWhiteSpace($negResult.DetectedUser)
        $rows += [PSCustomObject]@{
            Camera = $camera
            Source = "synthetic"
            Case = "negative"
            ExpectedUser = ""
            DetectedUser = $negResult.DetectedUser
            Confidence = [Math]::Round($negResult.Confidence, 4)
            LatencyMs = $negResult.LatencyMs
            Correct = $negCorrect
            IsNegative = $true
            ImagePath = $negPath
        }
    }

    $datasetNegPath = Join-Path (Join-Path $DatasetPath "negativos") $camera
    if (Test-Path $datasetNegPath) {
        $negImages = Get-ChildItem -Path $datasetNegPath -File -Include *.jpg, *.jpeg, *.png
        foreach ($img in $negImages) {
            $negResult = Invoke-AnalyzeCamera -Client $httpClient -BaseUrl $IaBaseUrl -Camera $camera -ImagePath $img.FullName -ExpectedUser "" -ThresholdValue $Threshold
            $negCorrect = [string]::IsNullOrWhiteSpace($negResult.DetectedUser)
            $rows += [PSCustomObject]@{
                Camera = $camera
                Source = "dataset"
                Case = "negative"
                ExpectedUser = ""
                DetectedUser = $negResult.DetectedUser
                Confidence = [Math]::Round($negResult.Confidence, 4)
                LatencyMs = $negResult.LatencyMs
                Correct = $negCorrect
                IsNegative = $true
                ImagePath = $img.FullName
            }
        }
    }
}

if ($rows.Count -eq 0) {
    throw "No se pudieron ejecutar casos de validacion."
}

$metrics = @()
foreach ($camera in $Cameras) {
    $cameraRows = $rows | Where-Object { $_.Camera -eq $camera }
    $metrics += New-Metrics -Name $camera -Rows $cameraRows
}
$metrics += New-Metrics -Name "global" -Rows $rows

Write-Host "`n==> Reporte por camara" -ForegroundColor Cyan
foreach ($m in $metrics) {
    Write-Host "[$($m.Camera)] total=$($m.Total) acc=$($m.Accuracy)% FRR=$($m.FRR)% FAR=$($m.FAR)% avg=$($m.AvgLatencyMs)ms p95=$($m.P95LatencyMs)ms"
}

$sourceBreakdown = $rows |
    Group-Object -Property Source |
    ForEach-Object {
        [PSCustomObject]@{
            Source = $_.Name
            Total = $_.Count
            Accuracy = [Math]::Round((100.0 * (($_.Group | Where-Object { $_.Correct }).Count) / $_.Count), 2)
        }
    }

Write-Host "`n==> Fuentes evaluadas" -ForegroundColor Cyan
foreach ($s in $sourceBreakdown) {
    Write-Host "[$($s.Source)] total=$($s.Total) acc=$($s.Accuracy)%"
}

if (-not (Test-Path $OutputDir)) {
    New-Item -Path $OutputDir -ItemType Directory | Out-Null
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$jsonPath = Join-Path $OutputDir "ia-validation-report-$stamp.json"
$csvPath = Join-Path $OutputDir "ia-validation-results-$stamp.csv"

$report = [PSCustomObject]@{
    generatedAt = (Get-Date).ToString("o")
    iaBaseUrl = $IaBaseUrl
    cameras = $Cameras
    threshold = $Threshold
    attemptsPerUser = $AttemptsPerUser
    datasetPath = $DatasetPath
    metrics = $metrics
    sourceBreakdown = $sourceBreakdown
}

$report | ConvertTo-Json -Depth 8 | Set-Content -Path $jsonPath -Encoding UTF8
$rows | Export-Csv -Path $csvPath -NoTypeInformation -Encoding UTF8

Write-Host "`nReporte JSON: $jsonPath" -ForegroundColor Green
Write-Host "Detalle CSV: $csvPath" -ForegroundColor Green
Write-Host "Validacion completada." -ForegroundColor Green
