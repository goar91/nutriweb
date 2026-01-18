@echo off
echo ============================================
echo Compilando NutriWeb para Release
echo ============================================
echo.

REM Navegar al directorio del frontend
cd frontend

echo [1/4] Instalando dependencias del frontend...
call npm install
if errorlevel 1 (
    echo ERROR: No se pudieron instalar las dependencias del frontend
    pause
    exit /b 1
)

echo.
echo [2/4] Compilando frontend en modo produccion...
call npm run build
if errorlevel 1 (
    echo ERROR: Fallo la compilacion del frontend
    pause
    exit /b 1
)

echo.
echo [3/4] Copiando archivos del frontend al backend...
REM Limpiar directorio wwwroot/browser anterior
if exist "..\backend\wwwroot\browser" rmdir /s /q "..\backend\wwwroot\browser"

REM Copiar archivos compilados del frontend
xcopy /E /I /Y "dist\frontend\browser\*" "..\backend\wwwroot\browser\"
if errorlevel 1 (
    echo ERROR: No se pudieron copiar los archivos del frontend
    pause
    exit /b 1
)

REM Navegar al directorio del backend
cd ..\backend

echo.
echo [4/4] Publicando backend para Windows x64...
dotnet publish -c Release -r win-x64 --self-contained -o ..\publish\win-x64
if errorlevel 1 (
    echo ERROR: Fallo la publicacion del backend
    cd ..
    pause
    exit /b 1
)

cd ..

echo.
echo ============================================
echo Compilacion completada exitosamente!
echo ============================================
echo.
echo El ejecutable se encuentra en: publish\win-x64\backend.exe
echo.
pause
