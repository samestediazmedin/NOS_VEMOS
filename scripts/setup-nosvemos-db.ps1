param(
    [string]$ContainerName = "nosvemos-sqlserver",
    [string]$SaPassword = "",
    [switch]$AutoStartSqlContainer = $true
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if ([string]::IsNullOrWhiteSpace($SaPassword)) {
    $SaPassword = $env:SA_PASSWORD
}

if ([string]::IsNullOrWhiteSpace($SaPassword)) {
    $SaPassword = "Your_strong_password_123!"
}

function Test-RunningContainer {
    param([string]$Name)

    $names = docker ps --format "{{.Names}}"
    if ($LASTEXITCODE -ne 0) {
        throw "No se pudo consultar Docker. Verifica Docker Desktop en ejecucion."
    }

    return $names -contains $Name
}

function Start-SqlInfraContainer {
    Write-Host "==> Iniciando infraestructura SQL Server en Docker" -ForegroundColor Cyan
    docker compose --profile sqlserver --env-file "App_NosVemos_Infraestructura/.env" -f "App_NosVemos_Infraestructura/docker-compose.yml" up -d
    if ($LASTEXITCODE -ne 0) {
        throw "No se pudo iniciar SQL Server en Docker. Revisa DNS/proxy para mcr.microsoft.com y vuelve a intentar."
    }
}

$sqlFile = Join-Path $root "App_NosVemos_Infraestructura/sql/01-create-nosvemos-databases.sql"
if (-not (Test-Path $sqlFile)) {
    throw "No se encontro script SQL: $sqlFile"
}

if (-not (Test-RunningContainer -Name $ContainerName)) {
    if ($AutoStartSqlContainer) {
        Start-SqlInfraContainer
    }
}

if (-not (Test-RunningContainer -Name $ContainerName)) {
    throw "No existe contenedor activo '$ContainerName'. Levantalo y vuelve a ejecutar este script."
}

$containerSqlPath = "/tmp/01-create-nosvemos-databases.sql"

Write-Host "==> Copiando script SQL al contenedor $ContainerName" -ForegroundColor Cyan
docker cp $sqlFile "${ContainerName}:$containerSqlPath"
if ($LASTEXITCODE -ne 0) {
    throw "Fallo al copiar el script SQL al contenedor."
}

Write-Host "==> Creando bases de datos NOS_VEMOS" -ForegroundColor Cyan
docker exec $ContainerName /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SaPassword" -i $containerSqlPath -C
if ($LASTEXITCODE -ne 0) {
    throw "Fallo al ejecutar script SQL en SQL Server."
}

Write-Host "==> Bases creadas/verificadas OK" -ForegroundColor Green
Write-Host "Siguiente paso: iniciar microservicios para aplicar migraciones EF Core." -ForegroundColor Yellow
