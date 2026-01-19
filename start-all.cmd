@echo off
setlocal
pushd %~dp0

echo ============================================
echo  Iniciando NutriWeb (Modo Desarrollo)
echo ============================================
echo.

REM Verificar e iniciar PostgreSQL
echo [1/4] Verificando PostgreSQL...
net start postgresql-x64-18 >nul 2>&1
if errorlevel 1 (
    net start postgresql-x64-17 >nul 2>&1
    if errorlevel 1 (
        net start postgresql-x64-16 >nul 2>&1
    )
)

sc query postgresql-x64-18 | find "RUNNING" >nul 2>&1
if errorlevel 1 (
    sc query postgresql-x64-17 | find "RUNNING" >nul 2>&1
    if errorlevel 1 (
        sc query postgresql-x64-16 | find "RUNNING" >nul 2>&1
        if errorlevel 1 (
            echo [ERROR] PostgreSQL no esta corriendo
            echo Por favor inicie PostgreSQL manualmente
            pause
            exit /b 1
        )
    )
)
echo [OK] PostgreSQL activo

echo.
echo [2/4] Verificando configuracion de base de datos...

echo.
echo [2/4] Verificando configuracion de base de datos...

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
}
echo [OK] Configuracion de BD lista

echo.
echo [3/4] Iniciando backend

echo.
echo.
echo [3/4] Iniciando backend...
echo - Backend: http://localhost:5000
echo - Frontend: http://localhost:4200
echo.

REM Inicia backend en http://localhost:5000
start "NutriWeb Backend" cmd /c "cd backend && dotnet run"

REM Espera 3 segundos para que el backend inicie
echo [OK] Backend iniciado
echo.
echo [4/4] Iniciando frontend...

REM Inicia frontend en http://localhost:4200
start "NutriWeb Frontend" cmd /c "cd frontend && npm start"

REM Espera 8 segundos para que el frontend esté listo
timeout /t 8 /nobreak > nul

echo [OK] Frontend iniciado
echo.
echo ============================================
echo  NutriWeb listo para desarrollo
echo ============================================

REM Inicia frontend en http://localhost:4200
start "NutriWeb Frontend" cmd /c "cd frontend && npm start"

REM Espera 8 segundos para que el frontend esté listo
echo Esperando a que los servicios inicien...
timeout /t 8 /nobreak > nul

REM Abre el navegador automáticamente
echo Abriendo navegador...
start http://localhost:4200

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
