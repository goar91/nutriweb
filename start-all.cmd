@echo off
setlocal
pushd %~dp0

echo ============================================
echo  Iniciando NutriWeb
echo ============================================
echo.

REM Usa database\connection.local si NUTRITION_DB no está definido
if "%NUTRITION_DB%"=="" (
  if exist "database\connection.local" (
    for /f "usebackq delims=" %%i in ("database\connection.local") do set "NUTRITION_DB=%%i"
    echo [OK] Usando configuracion desde database\connection.local
  ) else (
    echo.
    echo [ADVERTENCIA] No se encontro la configuracion de la base de datos
    echo.
    echo Por favor ejecuta: backend\configure-db.cmd
    echo para configurar la conexion a PostgreSQL
    echo.
    pause
    exit /b 1
  )
)

echo.
echo Iniciando servicios...
echo - Backend: http://localhost:5000
echo - Frontend: http://localhost:4200
echo.

REM Inicia backend en http://localhost:5000
start "NutriWeb Backend" cmd /c "cd backend && dotnet run"

REM Espera 3 segundos para que el backend inicie
timeout /t 3 /nobreak > nul

REM Inicia frontend en http://localhost:4200
start "NutriWeb Frontend" cmd /c "cd frontend && npm start"

echo.
echo ============================================
echo  Aplicacion iniciada
echo ============================================
echo.
echo Accede a: http://localhost:4200
echo Usuario: admin
echo Password: admin
echo.
echo Presiona Ctrl+C en las ventanas de Backend y Frontend para detener
echo.

popd
endlocal
