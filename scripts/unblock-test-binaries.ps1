$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$targetDirs = @(
    "App_NosVemos_Autenticacion_Servicio/tests",
    "App_NosVemos_Usuarios_Servicio/tests",
    "App_NosVemos_NucleoNegocio_Servicio/tests",
    "App_NosVemos_Pasarela/tests"
)

$patterns = @("*.dll", "*.exe", "*.pdb")

$unblocked = 0
$errors = 0

foreach ($dir in $targetDirs) {
    if (-not (Test-Path $dir)) {
        continue
    }

    foreach ($pattern in $patterns) {
        $files = Get-ChildItem -Path $dir -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue
        foreach ($file in $files) {
            try {
                Unblock-File -Path $file.FullName -ErrorAction Stop
                $unblocked++
            }
            catch {
                $errors++
                Write-Warning "No se pudo desbloquear: $($file.FullName)"
            }
        }
    }
}

Write-Host "==> Resultado desbloqueo" -ForegroundColor Cyan
Write-Host "Archivos procesados/desbloqueados: $unblocked"
Write-Host "Errores: $errors"

if ($errors -gt 0) {
    Write-Host "Nota: si App Control/WDAC esta activo, puede bloquear la carga aunque el archivo este desbloqueado." -ForegroundColor Yellow
}

exit 0
