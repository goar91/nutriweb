# Ejecucion local con un solo ejecutable

Este flujo permite entregar un unico ejecutable que:
- verifica/instala la base de datos
- inicia el backend
- sirve el frontend
- abre el navegador automaticamente
- cierra la app al hacer logout (en modo local)

## Construir el ejecutable

1. Asegura que .NET 10 SDK y Node.js esten instalados.
2. Ejecuta:
   ```cmd
   build-release.cmd
   ```
3. El ejecutable resultante quedara en la carpeta creada en el Escritorio.

## Ejecutar en el cliente

1. Copia los archivos generados al cliente (backend.exe + wwwroot + appsettings.json + license.key).
2. Ajusta la cadena de conexion si es necesario en `appsettings.json` o `database/connection.local`.
3. Ejecuta `backend.exe`.

El backend abrira el navegador y servira el frontend desde `http://localhost:5000`.

## Variables de entorno utiles

- `NUTRIWEB_NO_BROWSER=1` evita abrir el navegador automaticamente.
- `NUTRIWEB_BROWSER_URL=http://localhost:5000` cambia el URL a abrir.
- `NUTRIWEB_SKIP_DB_BOOTSTRAP=1` omite la instalacion automatica de la BD.

## Notas

- El instalador de BD usa `setup_complete_database.sql` embebido. Si deseas reemplazarlo,
  coloca un archivo con ese nombre en `database/` junto al ejecutable.
