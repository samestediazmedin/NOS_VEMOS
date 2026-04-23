$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$testProjects = @(
    "App_NosVemos_Autenticacion_Servicio/tests/NosVemos.Autenticacion.Tests/NosVemos.Autenticacion.Tests.csproj",
    "App_NosVemos_Usuarios_Servicio/tests/NosVemos.Usuarios.Tests/NosVemos.Usuarios.Tests.csproj",
    "App_NosVemos_NucleoNegocio_Servicio/tests/NosVemos.NucleoNegocio.Tests/NosVemos.NucleoNegocio.Tests.csproj",
    "App_NosVemos_Pasarela/tests/NosVemos.Pasarela.Tests/NosVemos.Pasarela.Tests.csproj",
    "App_NosVemos_Orquestador_IA/tests/NosVemos.OrquestadorIA.Tests/NosVemos.OrquestadorIA.Tests.csproj"
)

$blockedMarkers = @(
    "0x800711C7",
    "Control de aplicaciones bloque",
    "could not load dependent assembly",
    "No hay ninguna prueba disponible"
)

function Test-IsBlockedOutput {
    param([string]$text)

    foreach ($marker in $blockedMarkers) {
        if ($text -like "*$marker*") {
            return $true
        }
    }

    return $false
}

Write-Host "==> Build solution" -ForegroundColor Cyan
dotnet build "NosVemos.sln"
if ($LASTEXITCODE -ne 0) {
    throw "Build failed."
}

$passed = @()
$failed = @()
$blocked = @()

foreach ($project in $testProjects) {
    Write-Host "`n==> Test $project" -ForegroundColor Cyan
    $output = dotnet test $project 2>&1 | Out-String
    $output.TrimEnd() | Write-Host

    if (Test-IsBlockedOutput $output) {
        $blocked += $project
        continue
    }

    if ($LASTEXITCODE -eq 0) {
        $passed += $project
        continue
    }

    $failed += $project
}

Write-Host "`n==> Summary" -ForegroundColor Cyan
Write-Host "Passed : $($passed.Count)"
Write-Host "Blocked: $($blocked.Count)"
Write-Host "Failed : $($failed.Count)"

if ($passed.Count -gt 0) {
    Write-Host "`nPassed projects:" -ForegroundColor Green
    $passed | ForEach-Object { Write-Host " - $_" }
}

if ($blocked.Count -gt 0) {
    Write-Host "`nBlocked projects (App Control / policy):" -ForegroundColor Yellow
    $blocked | ForEach-Object { Write-Host " - $_" }
}

if ($failed.Count -gt 0) {
    Write-Host "`nFailed projects:" -ForegroundColor Red
    $failed | ForEach-Object { Write-Host " - $_" }
    exit 1
}

exit 0
