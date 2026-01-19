# 🔍 COMANDOS DE VERIFICACIÓN DE BASE DE DATOS

Este documento contiene comandos útiles para verificar y consultar la base de datos NutriWeb.

## 🔐 Configurar Credenciales

Antes de ejecutar cualquier comando, configurar la contraseña:

```powershell
$env:PGPASSWORD = "030762"
```

## 📊 VERIFICACIONES BÁSICAS

### Verificar Conexión
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "SELECT current_database(), current_user;"
```

### Listar Todas las Tablas
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT table_name, 
       (SELECT COUNT(*) FROM information_schema.columns WHERE table_name = t.table_name) as columnas
FROM information_schema.tables t
WHERE table_schema = 'public' 
  AND table_type = 'BASE TABLE'
ORDER BY table_name;
"
```

### Contar Registros en Cada Tabla
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT 'pacientes' as tabla, COUNT(*) as registros FROM pacientes
UNION ALL SELECT 'historias_clinicas', COUNT(*) FROM historias_clinicas
UNION ALL SELECT 'antecedentes', COUNT(*) FROM antecedentes
UNION ALL SELECT 'habitos', COUNT(*) FROM habitos
UNION ALL SELECT 'signos_vitales', COUNT(*) FROM signos_vitales
UNION ALL SELECT 'datos_antropometricos', COUNT(*) FROM datos_antropometricos
UNION ALL SELECT 'valores_bioquimicos', COUNT(*) FROM valores_bioquimicos
UNION ALL SELECT 'recordatorio_24h', COUNT(*) FROM recordatorio_24h
UNION ALL SELECT 'frecuencia_consumo', COUNT(*) FROM frecuencia_consumo
UNION ALL SELECT 'usuarios', COUNT(*) FROM usuarios
UNION ALL SELECT 'sesiones', COUNT(*) FROM sesiones
UNION ALL SELECT 'logs_acceso', COUNT(*) FROM logs_acceso
UNION ALL SELECT 'auditoria', COUNT(*) FROM auditoria
UNION ALL SELECT 'planes_nutricionales', COUNT(*) FROM planes_nutricionales
UNION ALL SELECT 'alimentacion_semanal', COUNT(*) FROM alimentacion_semanal;
"
```

## 👥 CONSULTAS DE USUARIOS

### Ver Todos los Usuarios
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT id, username, email, nombre, rol, activo, fecha_creacion 
FROM usuarios 
ORDER BY fecha_creacion DESC;
"
```

### Ver Usuario Administrador
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT * FROM usuarios WHERE username = 'admin';
"
```

## 🏥 CONSULTAS DE PACIENTES

### Ver Todos los Pacientes
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT id, numero_cedula, nombre, edad_cronologica, sexo, telefono, email 
FROM pacientes 
ORDER BY nombre;
"
```

### Ver Pacientes con sus Historias
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT 
    p.nombre,
    p.numero_cedula,
    COUNT(h.id) as total_historias
FROM pacientes p
LEFT JOIN historias_clinicas h ON p.id = h.paciente_id
GROUP BY p.id, p.nombre, p.numero_cedula
ORDER BY total_historias DESC;
"
```

## 📋 CONSULTAS DE HISTORIAS CLÍNICAS

### Ver Últimas Historias Clínicas
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT 
    h.fecha_consulta,
    p.nombre as paciente,
    h.motivo_consulta,
    h.diagnostico
FROM historias_clinicas h
INNER JOIN pacientes p ON h.paciente_id = p.id
ORDER BY h.fecha_consulta DESC
LIMIT 10;
"
```

### Ver Historias Completas (usando la vista)
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT * FROM vista_historias_completas LIMIT 5;
"
```

## 💊 CONSULTAS DE PLANES NUTRICIONALES

### Ver Planes Activos
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT 
    pn.id,
    p.nombre as paciente,
    pn.fecha_inicio,
    pn.fecha_fin,
    pn.objetivo,
    pn.calorias_diarias,
    pn.activo
FROM planes_nutricionales pn
INNER JOIN historias_clinicas h ON pn.historia_id = h.id
INNER JOIN pacientes p ON h.paciente_id = p.id
WHERE pn.activo = true
ORDER BY pn.fecha_inicio DESC;
"
```

### Ver Alimentación Semanal de un Plan
```powershell
# Reemplazar PLAN_ID con el ID del plan
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT 
    semana,
    dia_semana,
    CASE dia_semana
        WHEN 1 THEN 'Lunes'
        WHEN 2 THEN 'Martes'
        WHEN 3 THEN 'Miércoles'
        WHEN 4 THEN 'Jueves'
        WHEN 5 THEN 'Viernes'
        WHEN 6 THEN 'Sábado'
        WHEN 7 THEN 'Domingo'
    END as dia_nombre,
    desayuno,
    almuerzo,
    cena
FROM alimentacion_semanal
WHERE plan_id = 'PLAN_ID'
ORDER BY semana, dia_semana;
"
```

## 📊 CONSULTAS DE ÍNDICES Y ESTRUCTURA

### Ver Todos los Índices
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'public'
ORDER BY tablename, indexname;
"
```

### Ver Triggers
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT 
    trigger_name,
    event_manipulation,
    event_object_table,
    action_statement
FROM information_schema.triggers
WHERE trigger_schema = 'public'
ORDER BY event_object_table;
"
```

### Ver Funciones
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT 
    routine_name,
    routine_type,
    data_type as return_type
FROM information_schema.routines
WHERE routine_schema = 'public'
ORDER BY routine_name;
"
```

## 🔧 COMANDOS DE MANTENIMIENTO

### Ver Tamaño de la Base de Datos
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT pg_size_pretty(pg_database_size('nutriciondb')) as tamaño_db;
"
```

### Ver Tamaño de Cada Tabla
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as tamaño
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
"
```

### Limpiar Sesiones Expiradas
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT limpiar_sesiones_expiradas();
"
```

### Ver Sesiones Activas
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT 
    s.id,
    u.username,
    s.ip_address,
    s.fecha_inicio,
    s.fecha_expiracion,
    s.activa
FROM sesiones s
INNER JOIN usuarios u ON s.usuario_id = u.id
ORDER BY s.fecha_inicio DESC;
"
```

## 🔍 CONSULTAS DE AUDITORÍA

### Ver Últimos Registros de Auditoría
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT 
    a.tabla,
    a.accion,
    u.username,
    a.fecha_accion
FROM auditoria a
LEFT JOIN usuarios u ON a.usuario_id = u.id
ORDER BY a.fecha_accion DESC
LIMIT 20;
"
```

### Ver Logs de Acceso
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT 
    accion,
    username,
    ip_address,
    exitoso,
    mensaje,
    fecha_hora
FROM logs_acceso
ORDER BY fecha_hora DESC
LIMIT 20;
"
```

## 🧪 INSERTAR DATOS DE PRUEBA

### Crear un Nuevo Paciente de Prueba
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
INSERT INTO pacientes (numero_cedula, nombre, edad_cronologica, sexo, telefono, email)
VALUES ('1111111111', 'Paciente Test', '30', 'M', '0999999999', 'test@example.com')
RETURNING id, nombre;
"
```

### Crear una Historia Clínica de Prueba
```powershell
# Reemplazar PACIENTE_ID con el ID del paciente
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
INSERT INTO historias_clinicas (paciente_id, fecha_consulta, motivo_consulta, diagnostico)
VALUES ('PACIENTE_ID', CURRENT_DATE, 'Consulta de prueba', 'Diagnóstico de prueba')
RETURNING id, fecha_consulta;
"
```

## 🗑️ LIMPIEZA DE DATOS

### Eliminar Datos de Prueba
```powershell
# ⚠️ PRECAUCIÓN: Esto eliminará datos
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
DELETE FROM pacientes WHERE email LIKE '%@example.com';
"
```

### Vaciar una Tabla Específica
```powershell
# ⚠️ PRECAUCIÓN: Esto eliminará TODOS los datos de la tabla
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
TRUNCATE TABLE logs_acceso CASCADE;
"
```

## 📥 BACKUP Y RESTORE

### Crear Backup
```powershell
& "C:\Program Files\PostgreSQL\18\bin\pg_dump.exe" -U postgres -d nutriciondb -F c -b -v -f "nutriciondb_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').backup"
```

### Restaurar Backup
```powershell
# Primero crear la base de datos vacía
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -c "DROP DATABASE IF EXISTS nutriciondb;"
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -c "CREATE DATABASE nutriciondb;"

# Restaurar el backup
& "C:\Program Files\PostgreSQL\18\bin\pg_restore.exe" -U postgres -d nutriciondb -v "ruta_al_archivo.backup"
```

## 🔐 GESTIÓN DE SEGURIDAD

### Cambiar Contraseña de Usuario Admin
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
UPDATE usuarios 
SET password_hash = 'NUEVA_CONTRASEÑA_HASHEADA' 
WHERE username = 'admin';
"
```

### Crear Nuevo Usuario del Sistema
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
INSERT INTO usuarios (username, email, nombre, password_hash, rol, activo)
VALUES ('nuevo_usuario', 'usuario@nutriweb.com', 'Nombre Usuario', 'password_hash', 'nutricionista', true)
RETURNING id, username, email;
"
```

---

## 💡 TIPS ÚTILES

### Abrir psql Interactivo
```powershell
$env:PGPASSWORD = "030762"
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb
```

### Comandos Útiles en psql Interactivo
```sql
\dt                 -- Listar tablas
\d nombre_tabla     -- Describir tabla
\dv                 -- Listar vistas
\df                 -- Listar funciones
\di                 -- Listar índices
\du                 -- Listar usuarios de PostgreSQL
\l                  -- Listar bases de datos
\q                  -- Salir
```

### Formatear Salida
```powershell
# Añadir -x para formato expandido
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -x -c "SELECT * FROM usuarios LIMIT 1;"

# Añadir -H para salida HTML
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -H -c "SELECT * FROM pacientes;"
```

---

**Nota**: Recuerda siempre configurar `$env:PGPASSWORD = "030762"` antes de ejecutar los comandos, o cambiar la contraseña por la que uses en tu sistema.
