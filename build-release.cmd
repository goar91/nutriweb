@echo off
setlocal
pushd %~dp0

echo ═══════════════════════════════════════════════════════════
echo   COMPILACIÓN DE NUTRIWEB PARA DISTRIBUCIÓN
echo ═══════════════════════════════════════════════════════════
echo.

REM Detectar carpeta del escritorio
set "DESKTOP_PATH=%USERPROFILE%\Desktop"
if not exist "%DESKTOP_PATH%" set "DESKTOP_PATH=%USERPROFILE%\Escritorio"

REM Crear carpeta NutriWeb en el escritorio
set "OUTPUT_PATH=%DESKTOP_PATH%\NutriWeb_Cliente"
echo Preparando carpeta de destino...
echo Ubicacion: %OUTPUT_PATH%
if exist "%OUTPUT_PATH%" rd /s /q "%OUTPUT_PATH%"
mkdir "%OUTPUT_PATH%"
mkdir "%OUTPUT_PATH%\wwwroot"
mkdir "%OUTPUT_PATH%\wwwroot\browser"
mkdir "%OUTPUT_PATH%\database"
echo.

echo ═══════════════════════════════════════════════════════════
echo [1/4] CONFIGURANDO BASE DE DATOS
echo ═══════════════════════════════════════════════════════════
echo.

REM Copiar scripts de base de datos
echo Copiando scripts SQL...
copy "database\schema.sql" "%OUTPUT_PATH%\database\schema.sql" >nul
copy "database\add_planes_alimentacion.sql" "%OUTPUT_PATH%\database\add_planes_alimentacion.sql" >nul
copy "database\actualizar_a_4_semanas.sql" "%OUTPUT_PATH%\database\actualizar_a_4_semanas.sql" >nul

REM Copiar connection.local si existe
if exist "database\connection.local" copy "database\connection.local" "%OUTPUT_PATH%\database\connection.local" >nul
if not exist "database\connection.local" if exist "database\connection.local.example" copy "database\connection.local.example" "%OUTPUT_PATH%\database\connection.local" >nul

REM Copiar script de instalacion de base de datos
copy "templates\INSTALAR_BD.cmd" "%OUTPUT_PATH%\INSTALAR_BD.cmd" >nul
goto :after_instalar_bd

REM Crear script de instalación de base de datos
(
echo @echo off
echo echo ═══════════════════════════════════════════════════════════
echo echo   INSTALACION DE BASE DE DATOS NUTRIWEB
echo echo ═══════════════════════════════════════════════════════════
echo echo.
echo set /p PGPASSWORD="Ingrese password de PostgreSQL (usuario postgres): "
echo echo.
echo echo Creando base de datos...
echo "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -c "DROP DATABASE IF EXISTS nutriciondb;" 2^>nul
echo "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -c "CREATE DATABASE nutriciondb;"
echo if errorlevel 1 (
echo     echo [ERROR] No se pudo crear la base de datos
echo     pause
echo     exit /b 1
echo ^)
echo.
echo echo Ejecutando schema principal...
echo "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -f database\schema.sql
echo.
echo echo Agregando tablas de planes...
echo "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -f database\add_planes_alimentacion.sql
echo.
echo echo Actualizando a 4 semanas...
echo "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -f database\actualizar_a_4_semanas.sql
echo.
echo echo ✓ Base de datos instalada correctamente
echo echo   Database: nutriciondb
echo echo   Usuario: postgres
echo echo.
echo pause
) > "%OUTPUT_PATH%\INSTALAR_BD.cmd"
:after_instalar_bd

REM Crear configuracion por defecto con PostgreSQL local
set "CONN_STRING="
if exist "database\connection.local" (
  for /f "usebackq delims=" %%i in ("database\connection.local") do set "CONN_STRING=%%i"
)
if "%CONN_STRING%"=="" (
  set "CONN_STRING=Host=localhost;Port=5432;Database=nutriciondb;Username=postgres;Password="
)
setlocal EnableDelayedExpansion
(
echo {
echo   "Logging": {
echo     "LogLevel": {
echo       "Default": "Information",
echo       "Microsoft.AspNetCore": "Warning"
echo     }
echo   },
echo   "AllowedHosts": "*",
echo   "ConnectionStrings": {
echo     "NutritionDb": "!CONN_STRING!"
echo   },
echo   "Urls": "http://localhost:5000"
echo }
) > "%OUTPUT_PATH%\appsettings.json"
endlocal

echo ✓ Scripts de base de datos preparados
echo.

echo ═══════════════════════════════════════════════════════════
echo [2/4] COMPILANDO BACKEND
echo ═══════════════════════════════════════════════════════════
echo.
cd backend

REM Buscar dotnet.exe
set DOTNET_PATH=
if exist "C:\Program Files\dotnet\dotnet.exe" (
    set "DOTNET_PATH=C:\Program Files\dotnet\dotnet.exe"
) else if exist "C:\Program Files (x86)\dotnet\dotnet.exe" (
    set "DOTNET_PATH=C:\Program Files (x86)\dotnet\dotnet.exe"
) else if exist "%ProgramFiles%\dotnet\dotnet.exe" (
    set "DOTNET_PATH=%ProgramFiles%\dotnet\dotnet.exe"
) else (
    where dotnet >nul 2>&1
    if not errorlevel 1 (
        set "DOTNET_PATH=dotnet"
    )
)

if "%DOTNET_PATH%"=="" (
    echo [ERROR] No se encontro .NET SDK instalado
    echo Por favor instale .NET 10 SDK desde: https://dotnet.microsoft.com/download
    cd ..
    pause
    exit /b 1
)

echo Usando .NET en: %DOTNET_PATH%
"%DOTNET_PATH%" publish backend.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=true -p:DebugType=None -p:DebugSymbols=false -p:Debuggable=false -o "%OUTPUT_PATH%"
if errorlevel 1 (
    echo [ERROR] Fallo al compilar el backend
    cd ..
    pause
    exit /b 1
)

del /q "%OUTPUT_PATH%\*.pdb" 2>nul

cd ..
echo ✓ Backend compilado
echo.

echo ═══════════════════════════════════════════════════════════
echo [3/4] COMPILANDO FRONTEND
echo ═══════════════════════════════════════════════════════════
echo.
cd frontend
if not exist "node_modules" (
    echo Instalando dependencias...
    call npm install
)
call npm run build
if errorlevel 1 (
    echo [ERROR] Fallo al compilar el frontend
    cd ..
    pause
    exit /b 1
)
cd ..

echo Copiando archivos del frontend...
xcopy "frontend\dist\frontend\browser\*" "%OUTPUT_PATH%\wwwroot\browser\" /E /I /Y >nul
echo ✓ Frontend compilado e integrado
echo.

echo ═══════════════════════════════════════════════════════════
echo [4/4] CREANDO ARCHIVOS ADICIONALES
echo ═══════════════════════════════════════════════════════════
echo.

REM Copiar script de inicio
copy "templates\INICIAR.cmd" "%OUTPUT_PATH%\INICIAR.cmd" >nul
copy "templates\GENERAR_SEGURIDAD.cmd" "%OUTPUT_PATH%\GENERAR_SEGURIDAD.cmd" >nul
goto :after_iniciar

REM Crear script de inicio
(
echo @echo off
echo chcp 65001 ^>nul 2^>^&1
echo cd /d "%%~dp0"
echo title NutriWeb - Sistema de Nutricion
echo.
echo cls
echo echo ═══════════════════════════════════════════════════════════
echo echo   NUTRIWEB - INICIANDO SISTEMA COMPLETO
echo echo ═══════════════════════════════════════════════════════════
echo echo.
echo echo [1/3] Verificando PostgreSQL...
echo.
echo REM Intentar iniciar PostgreSQL si no esta corriendo
echo net start postgresql-x64-18 ^>nul 2^>^&1
echo if errorlevel 1 (
echo     net start postgresql-x64-17 ^>nul 2^>^&1
echo     if errorlevel 1 (
echo         net start postgresql-x64-16 ^>nul 2^>^&1
echo     ^)
echo ^)
echo.
echo REM Verificar si PostgreSQL esta corriendo
echo sc query postgresql-x64-18 ^| find "RUNNING" ^>nul 2^>^&1
echo if errorlevel 1 (
echo     sc query postgresql-x64-17 ^| find "RUNNING" ^>nul 2^>^&1
echo     if errorlevel 1 (
echo         sc query postgresql-x64-16 ^| find "RUNNING" ^>nul 2^>^&1
echo         if errorlevel 1 (
echo             echo [ERROR] PostgreSQL no esta instalado o no se puede iniciar
echo             echo.
echo             echo Por favor:
echo             echo 1. Instale PostgreSQL desde https://www.postgresql.org/download/
echo             echo 2. O inicie PostgreSQL manualmente
echo             echo 3. Ejecute INSTALAR_BD.cmd para crear la base de datos
echo             echo.
echo             pause
echo             exit /b 1
echo         ^)
echo     ^)
echo ^)
echo.
echo echo [OK] PostgreSQL esta corriendo
echo echo.
echo echo [2/3] Verificando base de datos...
echo timeout /t 1 /nobreak ^>nul
echo echo [OK] Base de datos lista
echo echo.
echo echo [3/3] Iniciando aplicacion...
echo echo.
echo echo ═══════════════════════════════════════════════════════════
echo echo   NUTRIWEB ACTIVO
echo echo ═══════════════════════════════════════════════════════════
echo echo.
echo echo URL: http://localhost:5000
echo echo.
echo echo Abriendo navegador en 3 segundos...
echo timeout /t 3 /nobreak ^>nul
echo start http://localhost:5000
echo echo.
echo echo El navegador se ha abierto. La aplicacion esta lista para usar.
echo echo.
echo echo IMPORTANTE: NO CIERRE ESTA VENTANA
echo echo            Para detener presione Ctrl+C
echo echo.
echo "%%~dp0backend.exe"
echo.
echo echo.
echo echo La aplicacion se ha detenido.
echo pause
) > "%OUTPUT_PATH%\INICIAR.cmd"
:after_iniciar

REM Crear archivo de instrucciones
echo ═══════════════════════════════════════════════════════════
echo   NUTRIWEB - GUIA DE INSTALACION Y USO
echo ═══════════════════════════════════════════════════════════
echo.
echo PASO 1: INICIAR LA APLICACION
echo --------------------------------
echo 1. Asegurese de tener PostgreSQL instalado
echo 2. Doble clic en: GENERAR_SEGURIDAD.cmd
echo 3. Doble clic en: INICIAR.cmd
echo 4. El sistema instalara la base de datos si falta
echo    y luego iniciara backend y frontend.
echo.
echo NOTA: INICIAR.cmd hace todo automaticamente.
echo       Solo necesita ejecutar este archivo.
echo.
echo CREDENCIALES POR DEFECTO:
echo --------------------------------
echo Usuario: admin
echo Password: admin
echo.
echo CONFIGURACION DE BASE DE DATOS:
echo --------------------------------
echo Si necesita cambiar la configuracion, edite:
echo appsettings.json
echo.
echo Base de datos por defecto:
echo - Host: localhost
echo - Puerto: 5432
echo - Database: nutriciondb
echo - Usuario: postgres
echo - Password: (segun appsettings.json)
echo.
echo CONTENIDO DE LA CARPETA:
echo --------------------------------
echo - INICIAR.cmd          Inicia la aplicacion
echo - GENERAR_SEGURIDAD.cmd Crea archivo de seguridad
echo - INSTALAR_BD.cmd      Instala o repara la base de datos
echo - backend.exe          Servidor de la aplicacion
echo - appsettings.json     Configuracion
echo - database/            Scripts SQL
echo - wwwroot/browser/     Frontend
echo.
echo INSTRUCCIONES RAPIDAS:
echo 1. Ejecutar: GENERAR_SEGURIDAD.cmd
echo 2. Ejecutar: INICIAR.cmd
echo 3. Opcional: INSTALAR_BD.cmd para reinstalar la base de datos
echo.
echo INICIAR.cmd hace todo automaticamente:
echo - Verifica e inicia PostgreSQL
echo - Instala la base de datos si falta
echo - Inicia backend y valida conexion con la base de datos
echo - Verifica el frontend y abre el navegador

echo.

echo.
echo ═══════════════════════════════════════════════════════════
echo   ✓ COMPILACION COMPLETADA EXITOSAMENTE
echo ═══════════════════════════════════════════════════════════
echo.
echo Carpeta creada en el Escritorio:
echo %OUTPUT_PATH%
echo.
goto :after_archivos
echo ARCHIVOS GENERADOS:
echo  ✓ backend.exe           Aplicacion principal
echo  ✓ INICIAR.cmd           Inicia la app y abre navegador
echo  ✓ INSTALAR_BD.cmd       Instala base de datos
echo  ✓ appsettings.json      Configuracion por defecto
echo  ✓ dPrimera vez: Ejecutar INSTALAR_BD.cmd
echo  2. Doble clic en: INICIAR.cmd
echo     - Inicia PostgreSQL automaticamente
echo     - Abre el navegador con la aplicacion
echo  3. Listo para usar!
echo.
echo INSTRUCCIONES PARA EL CLIENTE:
echo  1. Ejecutar: INSTALAR_BD.cmd (solo la primera vez)
echo  4. Usar la aplicacion en el navegador
echo.
:after_archivos
echo ARCHIVOS GENERADOS:
echo  - backend.exe           Aplicacion principal
echo  - INICIAR.cmd           Inicia la app y valida servicios
echo  - GENERAR_SEGURIDAD.cmd Crea archivo de seguridad
echo  - INSTALAR_BD.cmd       Instala o repara la base de datos
echo  - appsettings.json      Configuracion por defecto
echo.
echo USO RAPIDO:
echo  1. Doble clic en: GENERAR_SEGURIDAD.cmd (una vez)
echo  2. Doble clic en: INICIAR.cmd
echo     - Instala la base de datos si falta
echo     - Verifica backend y frontend
echo  3. Listo para usar!
echo.
echo INSTRUCCIONES PARA EL CLIENTE:
echo  1. Ejecutar: GENERAR_SEGURIDAD.cmd
echo  2. Ejecutar: INICIAR.cmd
echo  3. Opcional: INSTALAR_BD.cmd si necesita reinstalar la base de datos
echo  4. Usar la aplicacion en el navegador
echo.
echo Abriendo carpeta...
explorer "%OUTPUT_PATH%"
echo.

popd
endlocal
pause
