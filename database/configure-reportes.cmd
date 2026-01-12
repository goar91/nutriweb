@echo off
echo ============================================
echo Configurando esquema de reportes NutriWeb
echo ============================================
echo.

cd /d "%~dp0"

if not exist "connection.local" (
    echo ERROR: No se encuentra el archivo connection.local
    echo.
    echo Por favor crea el archivo database\connection.local con tu cadena de conexion:
    echo Host=localhost;Port=5432;Database=nutriciondb;Username=postgres;Password=TU_PASSWORD
    echo.
    pause
    exit /b 1
)

echo Leyendo cadena de conexion...
set /p CONN_STRING=<connection.local

echo.
echo Extrayendo parametros de conexion...
for /f "tokens=1,2 delims==" %%a in ("%CONN_STRING%") do (
    if "%%a"=="Host" set DB_HOST=%%b
    if "%%a"=="Port" set DB_PORT=%%b
    if "%%a"=="Database" set DB_NAME=%%b
    if "%%a"=="Username" set DB_USER=%%b
    if "%%a"=="Password" set DB_PASS=%%b
)

rem Limpiar punto y coma
set DB_HOST=%DB_HOST:;=%
set DB_PORT=%DB_PORT:;=%
set DB_NAME=%DB_NAME:;=%
set DB_USER=%DB_USER:;=%
set DB_PASS=%DB_PASS:;=%

echo Host: %DB_HOST%
echo Puerto: %DB_PORT%
echo Base de datos: %DB_NAME%
echo Usuario: %DB_USER%
echo.

echo Aplicando esquema de reportes...
echo.

set PGPASSWORD=%DB_PASS%
psql -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d %DB_NAME% -f reportes_schema.sql

if %errorlevel% equ 0 (
    echo.
    echo ============================================
    echo Esquema de reportes aplicado exitosamente!
    echo ============================================
) else (
    echo.
    echo ============================================
    echo ERROR: Hubo un problema al aplicar el esquema
    echo ============================================
)

echo.
pause
