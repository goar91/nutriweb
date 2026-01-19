@echo off
REM ============================================
REM Script de Inicializacion - NutriWeb
REM ============================================

echo.
echo ========================================
echo  INICIALIZACION DE NUTRIWEB
echo ========================================
echo.

REM Verificar .NET SDK
echo [1/5] Verificando .NET SDK...
dotnet --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo   X .NET SDK no esta instalado
    echo.
    echo   ACCION REQUERIDA:
    echo   Por favor descarga e instala .NET 10 SDK desde:
    echo   https://dotnet.microsoft.com/download/dotnet/10.0
    echo.
    echo   Despues de instalar, ejecuta este script nuevamente.
    echo.
    pause
    exit /b 1
)
echo   ✓ .NET SDK instalado

REM Verificar Node.js
echo [2/5] Verificando Node.js...
node --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo   X Node.js no esta instalado
    echo.
    echo   ACCION REQUERIDA:
    echo   Por favor descarga e instala Node.js desde:
    echo   https://nodejs.org/
    echo.
    pause
    exit /b 1
)
echo   ✓ Node.js instalado

REM Restaurar dependencias del backend
echo [3/5] Restaurando dependencias del backend...
cd backend
dotnet restore
if %ERRORLEVEL% NEQ 0 (
    echo   X Error al restaurar dependencias del backend
    cd ..
    pause
    exit /b 1
)
cd ..
echo   ✓ Dependencias del backend restauradas

REM Instalar dependencias del frontend
echo [4/5] Instalando dependencias del frontend...
cd frontend
call npm install
if %ERRORLEVEL% NEQ 0 (
    echo   X Error al instalar dependencias del frontend
    cd ..
    pause
    exit /b 1
)
cd ..
echo   ✓ Dependencias del frontend instaladas

echo.
echo ========================================
echo  ✓ INICIALIZACION COMPLETADA
echo ========================================
echo.
echo La aplicacion esta lista para ejecutarse.
echo.
echo Para iniciar la aplicacion:
echo   1. Ejecuta: start-all.cmd
echo.
echo O manualmente:
echo   - Backend:  cd backend  ^&  dotnet run
echo   - Frontend: cd frontend ^&  npm start
echo.
pause
