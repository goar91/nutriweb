@echo off
setlocal
pushd %~dp0

echo ============================================
echo  Deteniendo NutriWeb
echo ============================================
echo.

echo Deteniendo procesos de Node.js (Frontend)...
taskkill /F /IM node.exe >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo [OK] Procesos de Node.js detenidos
) else (
    echo [INFO] No se encontraron procesos de Node.js
)

echo.
echo Deteniendo procesos de .NET (Backend)...
taskkill /F /IM dotnet.exe >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo [OK] Procesos de .NET detenidos
) else (
    echo [INFO] No se encontraron procesos de .NET
)

echo.
echo ============================================
echo  Aplicacion detenida
echo ============================================
echo.

popd
endlocal
pause
