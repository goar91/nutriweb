@echo off
chcp 65001 >nul 2>&1
setlocal EnableExtensions
set "SEC_DIR=%ProgramData%\NutriWeb"
set "SEC_FILE=%SEC_DIR%\security.key"
if not "%NUTRIWEB_SECURITY_FILE%"=="" set "SEC_FILE=%NUTRIWEB_SECURITY_FILE%"
for %%I in ("%SEC_FILE%") do set "SEC_DIR=%%~dpI"

if not exist "%SEC_DIR%" mkdir "%SEC_DIR%" >nul 2>&1

powershell -NoProfile -Command "$guid=[guid]::NewGuid().ToString('N'); Set-Content -Path '%SEC_FILE%' -Value $guid -NoNewline"
if errorlevel 1 (
  echo [ERROR] No se pudo crear el archivo de seguridad.
  pause
  exit /b 1
)

echo [OK] Archivo de seguridad creado en:
echo %SEC_FILE%
pause
