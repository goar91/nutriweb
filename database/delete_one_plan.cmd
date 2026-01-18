@echo off
echo Eliminando un plan nutricional de la base de datos...
echo.

set PGPASSWORD=030762

echo Listando planes existentes:
echo ================================
psql -h localhost -U postgres -d nutriciondb -c "SELECT id, fecha_inicio, fecha_creacion FROM planes_nutricionales ORDER BY fecha_creacion DESC LIMIT 3;"

echo.
echo Eliminando el plan mas reciente...
psql -h localhost -U postgres -d nutriciondb -c "DELETE FROM planes_nutricionales WHERE id = (SELECT id FROM planes_nutricionales ORDER BY fecha_creacion DESC LIMIT 1);"

echo.
echo Planes restantes:
echo ================================
psql -h localhost -U postgres -d nutriciondb -c "SELECT id, fecha_inicio, fecha_creacion FROM planes_nutricionales ORDER BY fecha_creacion DESC;"

echo.
echo Proceso completado.
pause
