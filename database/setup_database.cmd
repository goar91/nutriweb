@echo off
REM ============================================
REM Script para configurar la base de datos PostgreSQL
REM NutriWeb
REM ============================================

SET PGPASSWORD=030762
SET PSQL="C:\Program Files\PostgreSQL\18\bin\psql.exe"
SET DBNAME=nutriciondb
SET USERNAME=postgres

echo.
echo ============================================
echo Configuracion de Base de Datos NutriWeb
echo ============================================
echo.

REM Crear la base de datos si no existe
echo [1/2] Creando base de datos nutriciondb...
%PSQL% -U %USERNAME% -c "CREATE DATABASE %DBNAME%;" 2>nul
if %ERRORLEVEL% EQU 0 (
    echo ✓ Base de datos creada exitosamente
) else (
    echo ℹ Base de datos ya existe, continuando...
)

echo.
echo [2/2] Ejecutando script de configuracion completo...
%PSQL% -U %USERNAME% -d %DBNAME% -f "%~dp0setup_complete_database.sql"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ============================================
    echo ✓ Base de datos configurada exitosamente!
    echo ============================================
    echo.
    echo Credenciales por defecto:
    echo   Usuario: admin
    echo   Password: admin
    echo.
    echo IMPORTANTE: Cambiar la contraseña en produccion
    echo ============================================
) else (
    echo.
    echo ✗ Error al configurar la base de datos
    echo Por favor revisa los mensajes de error anteriores
)

SET PGPASSWORD=
pause
