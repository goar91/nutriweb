@echo off
chcp 65001 >nul 2>&1
title NutriWeb - Prueba Local
cls

echo ═══════════════════════════════════════════════════════════
echo   NUTRIWEB - PRUEBA LOCAL
echo ═══════════════════════════════════════════════════════════
echo.
echo Este script ejecuta el backend directamente desde
echo la carpeta de desarrollo para propósitos de prueba.
echo.
echo ═══════════════════════════════════════════════════════════
echo.

cd /d "%~dp0backend"

echo Iniciando backend en modo Debug...
echo.
echo Backend: http://localhost:5000
echo.
echo Presione Ctrl+C para detener
echo.

timeout /t 2 /nobreak >nul
start http://localhost:5000

dotnet run --project backend.csproj

pause
