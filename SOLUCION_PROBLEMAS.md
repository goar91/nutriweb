# SOLUCIÓN DE PROBLEMAS - NUTRIWEB

## El backend no se inicia al ejecutar INICIAR.cmd

### Verificaciones básicas:

1. **Verificar que todos los archivos existen:**
   - backend.exe
   - appsettings.json
   - wwwroot/browser/ (carpeta con archivos del frontend)

2. **Verificar que PostgreSQL está instalado y ejecutándose:**
   ```cmd
   net start postgresql-x64-18
   ```

3. **Verificar la base de datos:**
   - Ejecutar INSTALAR_BD.cmd
   - Verificar que la base "nutriciondb" existe

### Pruebas de diagnóstico:

#### Prueba 1: Ejecutar backend manualmente
```cmd
cd C:\Users\<TU_USUARIO>\Desktop\NutriWeb_Cliente
backend.exe
```

**Mensajes esperados:**
- "NUTRIWEB - Sistema de Nutrición"
- "Now listening on: http://localhost:5000"

#### Prueba 2: Verificar que el puerto 5000 está libre
```cmd
netstat -ano | findstr :5000
```

Si el puerto está ocupado, matar el proceso o cambiar el puerto en appsettings.json.

### Problemas comunes:

#### Error: Backend se cierra inmediatamente
- Verificar PostgreSQL está ejecutándose
- Revisar appsettings.json - verificar connection string
- Ejecutar backend.exe desde CMD para ver errores

#### Error: "Connection refused" o errores de base de datos
- Verificar PostgreSQL en localhost:5432
- Verificar usuario y contraseña: postgres/postgres
- Ejecutar INSTALAR_BD.cmd

#### Frontend muestra pantalla en blanco
- Verificar que wwwroot/browser/ contiene archivos
- Abrir navegador y presionar F12 (DevTools)
- Revisar errores en la consola
- Verificar que el backend está ejecutándose (http://localhost:5000)

### Solución rápida:

Si todo falla, ejecutar en este orden:

1. Reiniciar PostgreSQL:
```cmd
net stop postgresql-x64-18
net start postgresql-x64-18
```

2. Reinstalar base de datos:
```cmd
INSTALAR_BD.cmd
```

3. Iniciar aplicación:
```cmd
INICIAR.cmd
```

### Archivos de log:

Por defecto, .NET registra errores en la consola. Para capturar logs:

```cmd
backend.exe > output.log 2>&1
```

Luego revisar output.log para errores detallados.

### Contacto de soporte:

Si ninguna solución funciona, enviar:
- Contenido de output.log
- Screenshot del error
- Versión de Windows
- Versión de PostgreSQL instalada
