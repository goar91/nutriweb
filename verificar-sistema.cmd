@echo off
REM ============================================
REM Script de Verificacion Rapida - NutriWeb
REM ============================================

echo.
echo ========================================
echo  VERIFICACION DE CONFIGURACION NUTRIWEB
echo ========================================
echo.

REM Verificar PostgreSQL
echo [1/5] Verificando PostgreSQL...
"C:\Program Files\PostgreSQL\18\bin\psql.exe" --version >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo   ✓ PostgreSQL 18 instalado
) else (
    echo   ✗ PostgreSQL no encontrado
    goto error
)

REM Verificar Node.js
echo [2/5] Verificando Node.js...
node --version >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo   ✓ Node.js instalado
) else (
    echo   ✗ Node.js no encontrado
    goto error
)

REM Verificar .NET
echo [3/5] Verificando .NET SDK...
dotnet --version >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo   ✓ .NET SDK instalado
) else (
    echo   ✗ .NET SDK no encontrado
    goto error
)

REM Verificar Base de Datos
echo [4/5] Verificando base de datos nutriciondb...
SET PGPASSWORD=030762
"C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "SELECT 1;" >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo   ✓ Base de datos accesible
) else (
    echo   ✗ No se puede conectar a la base de datos
    goto error
)

REM Verificar tablas
echo [5/5] Verificando tablas...
SET PGPASSWORD=030762
"C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';" -t >temp_count.txt 2>&1
set /p TABLE_COUNT=<temp_count.txt
del temp_count.txt
echo   ✓ Tablas encontradas: %TABLE_COUNT%

echo.
echo ========================================
echo  RESUMEN DE CONFIGURACION
echo ========================================
echo.

SET PGPASSWORD=030762
echo Base de datos: nutriciondb
"C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "SELECT COUNT(*) as pacientes FROM pacientes; SELECT COUNT(*) as usuarios FROM usuarios;" -t

echo.
echo ========================================
echo  ✓ SISTEMA LISTO PARA USAR
echo ========================================
echo.
echo Proximos pasos:
echo   1. cd backend
echo   2. dotnet restore
echo   3. dotnet run
echo.
echo   En otra terminal:
echo   1. cd frontend
echo   2. npm install
echo   3. npm start
echo.
echo O ejecutar: start-all.cmd
echo.
goto end

:error
echo.
echo ========================================
echo  ✗ ERROR EN LA VERIFICACION
echo ========================================
echo.
echo Revisa CONFIGURACION_COMPLETA.md para mas informacion
echo.

:end
SET PGPASSWORD=
pause
