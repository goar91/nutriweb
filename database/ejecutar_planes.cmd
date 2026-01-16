@echo off
setlocal enabledelayedexpansion

cd /d %~dp0

:: Leer la cadena de conexión
for /f "delims=" %%i in (connection.local) do set connstr=%%i

:: Leer el contenido del SQL
set "sqlcontent="
for /f "delims=" %%i in ('type add_planes_alimentacion.sql') do (
    set "line=%%i"
    set "sqlcontent=!sqlcontent!!line!\n"
)

:: Ejecutar el SQL usando PowerShell Invoke-WebRequest
powershell -Command "$body = @{sql='%sqlcontent%'; connectionString='%connstr%'} | ConvertTo-Json; Invoke-WebRequest -Uri 'http://localhost:5000/api/nutrition/ejecutar-sql' -Method POST -Headers @{'Content-Type'='application/json'} -Body $body"

endlocal
