# Solución de Problemas - NutriWeb

## Problema Identificado

**Error reportado:** 
- No se crean registros en la tabla `logs_acceso`
- Error de autenticación: "la autentificación password falló para el usuario postgres"

## Causa Raíz

1. **Error de conexión a base de datos:** La contraseña configurada en el código ("postgres") no coincidía con la contraseña real de PostgreSQL ("030762")

2. **Falta de logging:** El código no estaba registrando los intentos de login en la tabla `logs_acceso`

## Soluciones Implementadas

### 1. Configuración de Base de Datos

✅ **Creado script `backend/configure-db.cmd`**
- Solicita la contraseña de PostgreSQL al usuario
- Verifica la conexión
- Guarda la configuración en `database/connection.local`
- Muestra los usuarios existentes

**Uso:**
```cmd
cd backend
configure-db.cmd
```

### 2. Sistema de Logging Completo

✅ **Agregado registro en `logs_acceso` para:**

#### Login Exitoso
```csharp
INSERT INTO logs_acceso (usuario_id, username, accion, ip_address, user_agent, exitoso, mensaje)
VALUES (@uid, @username, 'login', @ip, @ua, true, 'Login exitoso')
```

#### Login Fallido - Usuario No Encontrado
```csharp
INSERT INTO logs_acceso (username, accion, ip_address, user_agent, exitoso, mensaje)
VALUES (@username, 'login', @ip, @ua, false, 'Usuario no encontrado')
```

#### Login Fallido - Contraseña Incorrecta
```csharp
INSERT INTO logs_acceso (usuario_id, username, accion, ip_address, user_agent, exitoso, mensaje)
VALUES (@uid, @username, 'login', @ip, @ua, false, 'Contraseña incorrecta')
```

#### Registro Exitoso de Usuario
```csharp
INSERT INTO logs_acceso (usuario_id, username, accion, ip_address, user_agent, exitoso, mensaje)
VALUES (@uid, @username, 'register', @ip, @ua, true, 'Usuario registrado exitosamente')
```

#### Registro Fallido - Usuario Duplicado
```csharp
INSERT INTO logs_acceso (username, accion, ip_address, user_agent, exitoso, mensaje)
VALUES (@username, 'register', @ip, @ua, false, 'Usuario o email ya existe')
```

### 3. Script de Inicio Mejorado

✅ **Actualizado `start-all.cmd`**
- Verifica la existencia de `database/connection.local` antes de iniciar
- Muestra mensajes claros sobre el estado de la aplicación
- Guía al usuario si falta la configuración

## Archivos Modificados

1. **`backend/Program.cs`**
   - ✅ Agregado logging en endpoint `/api/auth/login`
   - ✅ Agregado logging en endpoint `/api/auth/register`
   - ✅ Manejo de errores con try-catch para no interrumpir el flujo

2. **`backend/configure-db.cmd`** (nuevo)
   - ✅ Script interactivo para configurar la conexión a PostgreSQL

3. **`start-all.cmd`**
   - ✅ Verificación de configuración antes de iniciar
   - ✅ Mensajes mejorados

4. **`database/connection.local`** (generado)
   - ✅ Contiene la cadena de conexión con la contraseña correcta

## Verificación del Sistema

### 1. Verificar Logging

Después de hacer login, ejecuta:

```sql
SELECT 
    id,
    usuario_id,
    username,
    accion,
    exitoso,
    mensaje,
    fecha_hora,
    ip_address
FROM logs_acceso
ORDER BY fecha_hora DESC
LIMIT 10;
```

**Resultado esperado:**
```
 id | usuario_id | username | accion | exitoso |       mensaje        |      fecha_hora       | ip_address
----+------------+----------+--------+---------+---------------------+----------------------+------------
  1 | uuid...    | admin    | login  | t       | Login exitoso       | 2026-01-11 ...       | 127.0.0.1
```

### 2. Verificar Usuario Admin

```sql
SELECT username, nombre, email, rol, activo, fecha_creacion
FROM usuarios
WHERE username = 'admin';
```

**Resultado esperado:**
```
 username |    nombre     |       email        |  rol  | activo |   fecha_creacion
----------+---------------+--------------------+-------+--------+---------------------
 admin    | Administrador | admin@nutriweb.com | admin | t      | 2026-01-11 ...
```

## Cómo Usar la Aplicación

### Primera Vez

1. **Configurar Base de Datos**
   ```cmd
   cd c:\ProyectoWeb\NutriWeb\nutriweb\backend
   configure-db.cmd
   ```
   - Ingresa la contraseña: `030762`

2. **Iniciar Aplicación**
   ```cmd
   cd c:\ProyectoWeb\NutriWeb\nutriweb
   start-all.cmd
   ```

3. **Acceder**
   - URL: http://localhost:4200
   - Usuario: `admin`
   - Contraseña: `admin`

### Verificar Logs de Acceso

**Desde PowerShell:**
```powershell
$env:PGPASSWORD = "030762"
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb `
  -c "SELECT username, accion, exitoso, mensaje, fecha_hora FROM logs_acceso ORDER BY fecha_hora DESC LIMIT 10;"
```

**Desde psql:**
```cmd
psql -U postgres -d nutriciondb
```
```sql
SELECT * FROM logs_acceso ORDER BY fecha_hora DESC LIMIT 10;
```

## Características de Seguridad Implementadas

### 1. Hash de Contraseñas
- Algoritmo: **PBKDF2** con SHA256
- Iteraciones: 100,000
- Salt aleatorio de 16 bytes
- Formato: `PBKDF2$salt$hash`

### 2. Migración Automática
- Las contraseñas en texto plano se convierten a hash automáticamente en el primer login exitoso
- El usuario `admin` con contraseña `admin` se actualizará a hash PBKDF2 al iniciar sesión

### 3. Registro de Auditoría
- Todos los intentos de login (exitosos y fallidos)
- Registro de creación de usuarios
- IP address y user agent
- Timestamp automático
- Mensaje descriptivo del resultado

## Problemas Conocidos y Mejoras Futuras

### Mejoras Sugeridas

1. **IP Address Real:**
   - Actualmente usa `127.0.0.1` hardcoded
   - Mejorar para capturar la IP real del cliente desde HttpContext

2. **User Agent Real:**
   - Actualmente usa "Backend" hardcoded
   - Capturar el User-Agent del request HTTP

3. **Sesiones Persistentes:**
   - Actualmente las sesiones están en memoria (se pierden al reiniciar)
   - Implementar almacenamiento en base de datos para sesiones persistentes

4. **Cleanup de Sesiones:**
   - Implementar tarea programada para limpiar sesiones expiradas
   - Usar la función `limpiar_sesiones_expiradas()` que ya existe en la BD

### Ejemplo de Mejora - IP Real

```csharp
// En lugar de:
logCmd.Parameters.AddWithValue("ip", "127.0.0.1");

// Usar:
var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
logCmd.Parameters.AddWithValue("ip", ipAddress);
```

### Ejemplo de Mejora - User Agent Real

```csharp
// En lugar de:
logCmd.Parameters.AddWithValue("ua", "Backend");

// Usar:
var userAgent = httpContext.Request.Headers.UserAgent.ToString();
logCmd.Parameters.AddWithValue("ua", string.IsNullOrEmpty(userAgent) ? "unknown" : userAgent);
```

## Testing

### Probar Login Exitoso

```bash
# Con curl (si está disponible)
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}'
```

**Respuesta esperada:**
```json
{
  "success": true,
  "token": "abc123...",
  "user": {
    "id": "uuid...",
    "username": "admin",
    "nombre": "Administrador",
    "email": "admin@nutriweb.com"
  }
}
```

### Probar Login Fallido

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"incorrect"}'
```

**Respuesta esperada:**
```json
{
  "success": false,
  "error": "Credenciales inválidas"
}
```

### Probar Registro de Usuario

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "nutricionista1",
    "password": "password123",
    "email": "nutricionista1@nutriweb.com",
    "nombre": "María García"
  }'
```

**Respuesta esperada:**
```json
{
  "success": true,
  "token": "xyz789...",
  "user": {
    "id": "uuid...",
    "username": "nutricionista1",
    "nombre": "María García",
    "email": "nutricionista1@nutriweb.com"
  }
}
```

## Contacto y Soporte

Si encuentras algún problema:

1. Verifica que PostgreSQL esté corriendo
2. Ejecuta `backend\configure-db.cmd` para validar la conexión
3. Revisa los logs en la consola del backend
4. Verifica los registros en `logs_acceso`:
   ```sql
   SELECT * FROM logs_acceso WHERE exitoso = false ORDER BY fecha_hora DESC;
   ```

---

**Fecha de actualización:** 11 de enero de 2026  
**Versión:** 1.1
