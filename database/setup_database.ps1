# ============================================
# Script PowerShell para configurar la base de datos PostgreSQL
# NutriWeb
# ============================================

$ErrorActionPreference = "Continue"

# Configuración
$env:PGPASSWORD = "030762"
$psqlPath = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
$dbName = "nutriciondb"
$userName = "postgres"
$scriptPath = Join-Path $PSScriptRoot "setup_complete_database.sql"

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Configuración de Base de Datos NutriWeb" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Verificar que existe psql
if (-not (Test-Path $psqlPath)) {
    Write-Host "✗ Error: No se encontró PostgreSQL en $psqlPath" -ForegroundColor Red
    Write-Host "Por favor verifica la instalación de PostgreSQL" -ForegroundColor Yellow
    exit 1
}

# Verificar que existe el script SQL
if (-not (Test-Path $scriptPath)) {
    Write-Host "✗ Error: No se encontró el script setup_complete_database.sql" -ForegroundColor Red
    exit 1
}

# Crear la base de datos si no existe
Write-Host "[1/2] Creando base de datos '$dbName'..." -ForegroundColor Yellow
$createDbResult = & $psqlPath -U $userName -c "CREATE DATABASE $dbName;" 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Base de datos creada exitosamente" -ForegroundColor Green
} else {
    if ($createDbResult -match "already exists") {
        Write-Host "ℹ Base de datos ya existe, continuando..." -ForegroundColor Cyan
    } else {
        Write-Host "ℹ Continuando con la configuración..." -ForegroundColor Cyan
    }
}

Write-Host ""
Write-Host "[2/2] Ejecutando script de configuración completo..." -ForegroundColor Yellow
$setupResult = & $psqlPath -U $userName -d $dbName -f $scriptPath 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "============================================" -ForegroundColor Green
    Write-Host "✓ Base de datos configurada exitosamente!" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Información de la base de datos:" -ForegroundColor Cyan
    Write-Host "  Nombre: $dbName" -ForegroundColor White
    Write-Host "  Host: localhost" -ForegroundColor White
    Write-Host "  Puerto: 5432" -ForegroundColor White
    Write-Host ""
    Write-Host "Credenciales por defecto:" -ForegroundColor Cyan
    Write-Host "  Usuario: admin" -ForegroundColor White
    Write-Host "  Password: admin" -ForegroundColor White
    Write-Host ""
    Write-Host "⚠ IMPORTANTE: Cambiar la contraseña en producción" -ForegroundColor Yellow
    Write-Host "============================================" -ForegroundColor Green
    
    # Mostrar resumen de tablas creadas
    Write-Host ""
    Write-Host "Verificando tablas creadas..." -ForegroundColor Yellow
    & $psqlPath -U $userName -d $dbName -c "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name;" -t
} else {
    Write-Host ""
    Write-Host "✗ Error al configurar la base de datos" -ForegroundColor Red
    Write-Host "Detalles del error:" -ForegroundColor Yellow
    Write-Host $setupResult -ForegroundColor Red
    exit 1
}

# Limpiar la contraseña de la variable de entorno
$env:PGPASSWORD = $null

Write-Host ""
Write-Host "Presiona cualquier tecla para continuar..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
