@echo off
chcp 65001 >nul 2>&1
setlocal EnableExtensions
cd /d "%~dp0"
title NutriWeb - Sistema de Nutricion
cls
set "SEC_FILE=%ProgramData%\NutriWeb\security.key"
if not "%NUTRIWEB_SECURITY_FILE%"=="" set "SEC_FILE=%NUTRIWEB_SECURITY_FILE%"
if not exist "%SEC_FILE%" (
  echo [ERROR] Archivo de seguridad no encontrado.
  echo Ejecute GENERAR_SEGURIDAD.cmd para crearlo.
  pause
  exit /b 1
)
echo ========================================
echo  NUTRIWEB - INICIANDO SISTEMA COMPLETO
echo ========================================
echo.

echo [1/4] Verificando PostgreSQL...
for %%V in (18 17 16 15 14 13) do (
  net start postgresql-x64-%%V >nul 2>&1
)

set "PG_RUNNING="
for %%V in (18 17 16 15 14 13) do (
  sc query postgresql-x64-%%V | find "RUNNING" >nul 2>&1
  if not errorlevel 1 set "PG_RUNNING=1"
)

if not defined PG_RUNNING (
  echo [ERROR] PostgreSQL no esta instalado o no se puede iniciar
  echo.
  echo 1. Instale PostgreSQL desde https://www.postgresql.org/download/
  echo 2. O inicie PostgreSQL manualmente
  echo.
  pause
  exit /b 1
)
echo [OK] PostgreSQL esta corriendo
echo.

echo [2/4] Verificando base de datos...
set "DB_ACTION=check"
call "%~dp0INSTALAR_BD.cmd"
if errorlevel 1 (
  echo [INFO] Base de datos no encontrada. Instalando...
  set "DB_ACTION=install"
  call "%~dp0INSTALAR_BD.cmd"
  if errorlevel 1 (
    echo [ERROR] No se pudo instalar la base de datos
    pause
    exit /b 1
  )
)
set "DB_ACTION="
echo [OK] Base de datos lista
echo.

echo [3/4] Iniciando backend...
start "NutriWeb Backend" "%~dp0backend.exe"

echo Esperando backend y conexion a la base de datos...
set "STATUS_URL=http://localhost:5000/api/nutrition/status"
set "MAX_RETRIES=30"
set "WAIT_SECONDS=2"
for /l %%i in (1,1,%MAX_RETRIES%) do (
  powershell -NoProfile -Command "try { $r = Invoke-WebRequest -UseBasicParsing '%STATUS_URL%' -TimeoutSec 3; if ($r.StatusCode -eq 200) { exit 0 } else { exit 1 } } catch { exit 1 }"
  if not errorlevel 1 goto backend_ok
  timeout /t %WAIT_SECONDS% /nobreak >nul
)
echo [ERROR] No se pudo validar la conexion entre backend y base de datos
pause
exit /b 1

:backend_ok
echo [OK] Backend y base de datos listos
echo.

echo [4/4] Verificando frontend...
powershell -NoProfile -Command "try { $r = Invoke-WebRequest -UseBasicParsing 'http://localhost:5000/' -TimeoutSec 3; if ($r.StatusCode -eq 200 -and $r.Content -match '<app-root') { exit 0 } else { exit 1 } } catch { exit 1 }"
if errorlevel 1 (
  echo [ERROR] No se pudo validar el frontend
  pause
  exit /b 1
)

echo [OK] Frontend listo
echo.
echo Abriendo navegador...
start http://localhost:5000

echo.
echo IMPORTANTE: No cierre la ventana del backend.
echo Para detener, cierre la ventana "NutriWeb Backend".
echo.
pause
endlocal
