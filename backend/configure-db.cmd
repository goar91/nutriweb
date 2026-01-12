@echo off
REM Script para probar la conexión a PostgreSQL y configurar NutriWeb

echo ============================================
echo  NutriWeb - Configuracion de Base de Datos
echo ============================================
echo.

set "PSQL=C:\Program Files\PostgreSQL\18\bin\psql.exe"
set "DB=nutriciondb"
set "USER=postgres"

REM Preguntar por la contraseña
echo Por favor ingresa la contrasena de PostgreSQL:
set /p "PASSWORD=Contrasena: "

REM Probar conexión
echo.
echo Probando conexion a PostgreSQL...
set PGPASSWORD=%PASSWORD%
"%PSQL%" -U %USER% -d %DB% -c "SELECT version();" > nul 2>&1

if %ERRORLEVEL% EQU 0 (
    echo [OK] Conexion exitosa!
    echo.
    
    REM Guardar la contraseña en el archivo de configuración
    echo Guardando configuracion...
    
    REM Crear archivo connection.local
    if not exist "..\database" mkdir "..\database"
    echo Host=localhost;Port=5432;Database=nutriciondb;Username=postgres;Password=%PASSWORD%;Pooling=true;Trust Server Certificate=true > "..\database\connection.local"
    
    echo [OK] Configuracion guardada en database\connection.local
    echo.
    
    REM Verificar usuarios existentes
    echo Verificando usuarios en la base de datos...
    "%PSQL%" -U %USER% -d %DB% -c "SELECT username, nombre, email, rol, activo FROM usuarios ORDER BY fecha_creacion;"
    
    echo.
    echo ============================================
    echo  Configuracion completada exitosamente
    echo ============================================
    echo.
    echo Ahora puedes ejecutar start-all.cmd para iniciar la aplicacion
    echo.
    
) else (
    echo.
    echo [ERROR] No se pudo conectar a PostgreSQL
    echo.
    echo Por favor verifica:
    echo  1. Que PostgreSQL este ejecutandose
    echo  2. Que la contrasena sea correcta
    echo  3. Que la base de datos 'nutriciondb' exista
    echo.
    echo Ejecuta este script nuevamente con la contrasena correcta
    echo.
)

pause
