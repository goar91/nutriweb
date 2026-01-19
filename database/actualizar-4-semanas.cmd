@echo off
REM Script para actualizar base de datos a 4 semanas
echo Actualizando base de datos para soportar 4 semanas...
"C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -f actualizar_a_4_semanas.sql
echo.
echo Presiona cualquier tecla para continuar...
pause > nul
