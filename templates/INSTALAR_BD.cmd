@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "MODE=%DB_ACTION%"
if "%MODE%"=="" set "MODE=install"

if /i "%MODE%"=="check" (
  call :load_config
  call :find_psql
  call :test_db
  exit /b %ERRORLEVEL%
)

echo ========================================
echo  INSTALACION DE BASE DE DATOS NUTRIWEB
echo ========================================
echo.

call :load_config
call :find_psql
call :test_server
if errorlevel 1 exit /b 1

call :ensure_db
if errorlevel 1 exit /b 1

call :run_scripts
if errorlevel 1 exit /b 1

echo.
echo [OK] Base de datos instalada correctamente
echo   Database: %DB_NAME%
echo   Usuario: %DB_USER%
echo.
exit /b 0

:load_config
set "DB_HOST=localhost"
set "DB_PORT=5432"
set "DB_NAME=nutriciondb"
set "DB_USER=postgres"
set "DB_PASS="
set "CONN_FROM_FILE="

if exist "database\\connection.local" (
  set "CONN_FROM_FILE=1"
  for /f "usebackq delims=" %%i in (`powershell -NoProfile -Command "$ErrorActionPreference='Stop'; $cs=Get-Content -Raw 'database\\connection.local'; if (-not $cs) { exit 0 }; $dict=@{}; foreach ($p in $cs -split ';') { if ($p -match '=') { $kv=$p -split '=',2; $dict[$kv[0].ToLower()]=$kv[1] } }; if ($dict.ContainsKey('host')) { 'DB_HOST=' + $dict['host'] }; if ($dict.ContainsKey('port')) { 'DB_PORT=' + $dict['port'] }; if ($dict.ContainsKey('database')) { 'DB_NAME=' + $dict['database'] }; if ($dict.ContainsKey('username')) { 'DB_USER=' + $dict['username'] }; if ($dict.ContainsKey('user id')) { 'DB_USER=' + $dict['user id'] }; if ($dict.ContainsKey('password')) { 'DB_PASS=' + $dict['password'] }"` ) do set "%%i"
)

if not defined CONN_FROM_FILE if exist "appsettings.json" (
  for /f "usebackq delims=" %%i in (`powershell -NoProfile -Command "$ErrorActionPreference='Stop'; $json=Get-Content -Raw 'appsettings.json' | ConvertFrom-Json; $cs=$json.ConnectionStrings.NutritionDb; if (-not $cs) { exit 0 }; $dict=@{}; foreach ($p in $cs -split ';') { if ($p -match '=') { $kv=$p -split '=',2; $dict[$kv[0].ToLower()]=$kv[1] } }; if ($dict.ContainsKey('host')) { 'DB_HOST=' + $dict['host'] }; if ($dict.ContainsKey('port')) { 'DB_PORT=' + $dict['port'] }; if ($dict.ContainsKey('database')) { 'DB_NAME=' + $dict['database'] }; if ($dict.ContainsKey('username')) { 'DB_USER=' + $dict['username'] }; if ($dict.ContainsKey('user id')) { 'DB_USER=' + $dict['user id'] }; if ($dict.ContainsKey('password')) { 'DB_PASS=' + $dict['password'] }"` ) do set "%%i"
)

if "%DB_PASS%"=="" (
  set /p DB_PASS="Ingrese password de PostgreSQL (usuario %DB_USER%): "
)

exit /b 0

:find_psql
set "PSQL="
for %%V in (18 17 16 15 14 13) do (
  if not defined PSQL if exist "C:\Program Files\PostgreSQL\%%V\bin\psql.exe" set "PSQL=C:\Program Files\PostgreSQL\%%V\bin\psql.exe"
)
if "%PSQL%"=="" (
  for /f "delims=" %%p in ('where psql 2^>nul') do (
    set "PSQL=%%p"
    goto :psql_found
  )
)
:psql_found
if "%PSQL%"=="" (
  echo [ERROR] No se encontro psql.exe. Instale PostgreSQL.
  exit /b 1
)
exit /b 0

:test_server
set "PGPASSWORD=%DB_PASS%"
"%PSQL%" -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d postgres -c "SELECT 1;" >nul 2>&1
if errorlevel 1 (
  echo [ERROR] No se pudo conectar al servidor PostgreSQL
  exit /b 1
)
exit /b 0

:test_db
set "PGPASSWORD=%DB_PASS%"
"%PSQL%" -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d %DB_NAME% -c "SELECT 1;" >nul 2>&1
if errorlevel 1 (
  exit /b 1
)
exit /b 0

:ensure_db
set "PGPASSWORD=%DB_PASS%"
"%PSQL%" -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='%DB_NAME%';" > "%TEMP%\nutriweb_dbcheck.txt" 2>nul
set /p DB_EXISTS=<"%TEMP%\nutriweb_dbcheck.txt"
del "%TEMP%\nutriweb_dbcheck.txt" >nul 2>&1

if "%DB_EXISTS%"=="1" (
  echo [OK] Base de datos ya existe: %DB_NAME%
) else (
  echo Creando base de datos %DB_NAME%...
  "%PSQL%" -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d postgres -c "CREATE DATABASE %DB_NAME%;"
  if errorlevel 1 (
    echo [ERROR] No se pudo crear la base de datos
    exit /b 1
  )
)

exit /b 0

:run_scripts
set "PGPASSWORD=%DB_PASS%"
echo Ejecutando schema principal...
"%PSQL%" -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d %DB_NAME% -f database\schema.sql
if errorlevel 1 exit /b 1

echo Agregando tablas de planes...
"%PSQL%" -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d %DB_NAME% -f database\add_planes_alimentacion.sql
if errorlevel 1 exit /b 1

echo Actualizando a 4 semanas...
"%PSQL%" -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d %DB_NAME% -f database\actualizar_a_4_semanas.sql
if errorlevel 1 exit /b 1

exit /b 0
