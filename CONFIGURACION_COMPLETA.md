# ✅ CONFIGURACIÓN COMPLETA DE NUTRIWEB

## 🎉 Base de Datos Configurada Exitosamente

La base de datos **nutriciondb** ha sido creada y configurada con todas las tablas necesarias.

### 📊 Tablas Creadas (16 tablas + 1 vista)

1. **pacientes** - Información personal de los pacientes
2. **historias_clinicas** - Historias clínicas nutricionales
3. **antecedentes** - Antecedentes médicos
4. **habitos** - Hábitos de vida
5. **signos_vitales** - Signos vitales
6. **datos_antropometricos** - Medidas antropométricas
7. **valores_bioquimicos** - Resultados de análisis bioquímicos
8. **recordatorio_24h** - Recordatorio de alimentación 24 horas
9. **frecuencia_consumo** - Frecuencia de consumo de alimentos
10. **usuarios** - Usuarios del sistema (nutricionistas y administradores)
11. **sesiones** - Sesiones activas de usuarios
12. **logs_acceso** - Registro de accesos al sistema
13. **auditoria** - Registro de auditoría de cambios
14. **planes_nutricionales** - Planes nutricionales asignados
15. **alimentacion_semanal** - Alimentación detallada por día
16. **vista_historias_completas** - Vista para consultas optimizadas

---

## 🔧 REQUISITOS PREVIOS

### Software Necesario

- ✅ **PostgreSQL 18** (Instalado en: `C:\Program Files\PostgreSQL\18\`)
- ✅ **Node.js 18+** y npm
- ✅ **.NET 10 SDK**

### Verificación de Instalaciones

```powershell
# Verificar PostgreSQL
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" --version

# Verificar Node.js
node --version

# Verificar .NET
dotnet --version
```

---

## 📦 CONFIGURACIÓN DE BASE DE DATOS

### Información de Conexión

```json
Host: localhost
Port: 5432
Database: nutriciondb
Username: postgres
Password: 030762
```

### Cadena de Conexión (appsettings.json)

```json
"ConnectionStrings": {
  "NutritionDb": "Host=localhost;Port=5432;Database=nutriciondb;Username=postgres;Password=030762;Pooling=true;Trust Server Certificate=true"
}
```

### Credenciales de Administrador del Sistema

```
Usuario: admin
Password: admin
```

⚠️ **IMPORTANTE**: Cambiar estas credenciales en producción

### Scripts de Base de Datos Disponibles

- **setup_complete_database.sql** - Script completo de creación de todas las tablas
- **setup_database.ps1** - Script PowerShell para configuración automatizada
- **setup_database.cmd** - Script batch alternativo

### Reconfigurar Base de Datos (si es necesario)

```powershell
# Desde el directorio database/
cd database

# Opción 1: Usar PowerShell
$env:PGPASSWORD = "030762"
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -f "setup_complete_database.sql"

# Opción 2: Ejecutar script PowerShell (requiere permisos)
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\setup_database.ps1
```

---

## 🚀 PASOS PARA EJECUTAR LA APLICACIÓN

### 1. Backend (.NET 10)

```powershell
cd backend

# Restaurar dependencias
dotnet restore

# Ejecutar el backend
dotnet run
```

El backend estará disponible en: **http://localhost:5000**

### 2. Frontend (Angular 21)

```powershell
cd frontend

# Instalar dependencias (primera vez)
npm install

# Ejecutar el frontend
npm start
```

El frontend estará disponible en: **http://localhost:4200**

### 3. Ejecutar Todo (Automático)

Desde la raíz del proyecto:

```powershell
# Iniciar backend y frontend juntos
.\start-all.cmd
```

---

## 📁 ESTRUCTURA DEL PROYECTO

```
nutriweb-1/
├── backend/                    # Backend .NET 10
│   ├── appsettings.json       # Configuración (incluye ConnectionString)
│   ├── Program.cs             # Punto de entrada
│   └── wwwroot/               # Archivos estáticos del frontend compilado
├── frontend/                   # Frontend Angular 21
│   ├── src/                   # Código fuente
│   ├── package.json           # Dependencias npm
│   └── angular.json           # Configuración Angular
├── database/                   # Scripts de base de datos
│   ├── setup_complete_database.sql  # ✅ Script completo nuevo
│   ├── setup_database.ps1     # Script PowerShell de configuración
│   ├── setup_database.cmd     # Script batch de configuración
│   ├── schema.sql             # Schema original
│   ├── add_auth_tables.sql    # Tablas de autenticación
│   └── add_planes_alimentacion.sql  # Tablas de planes
└── README.md                   # Documentación general
```

---

## 🔐 CONFIGURACIÓN DE SEGURIDAD

### Variables de Entorno Recomendadas (Producción)

Crear un archivo `.env` o configurar variables de entorno:

```env
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DB=nutriciondb
POSTGRES_USER=postgres
POSTGRES_PASSWORD=TU_CONTRASEÑA_SEGURA
ADMIN_USERNAME=admin
ADMIN_PASSWORD=TU_CONTRASEÑA_ADMIN_SEGURA
```

### Cambiar Contraseña de Administrador

```sql
-- Conectarse a la base de datos
psql -U postgres -d nutriciondb

-- Actualizar contraseña (usar hash bcrypt en producción)
UPDATE usuarios 
SET password_hash = 'NUEVA_CONTRASEÑA_HASHEADA' 
WHERE username = 'admin';
```

---

## 📊 VERIFICACIÓN DE LA INSTALACIÓN

### Verificar Tablas Creadas

```powershell
$env:PGPASSWORD = "030762"
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name;"
```

### Verificar Datos de Prueba

```powershell
$env:PGPASSWORD = "030762"
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "SELECT * FROM pacientes;"
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "SELECT * FROM usuarios;"
```

---

## 🛠️ SOLUCIÓN DE PROBLEMAS

### Error: "psql no se reconoce"

Agregar PostgreSQL al PATH o usar la ruta completa:
```powershell
$env:Path += ";C:\Program Files\PostgreSQL\18\bin"
```

### Error: "La base de datos ya existe"

Esto es normal si ya ejecutaste el script antes. Continúa con la configuración de tablas.

### Error de Conexión en el Backend

1. Verificar que PostgreSQL esté ejecutándose:
   - Abrir "Servicios" de Windows
   - Buscar "postgresql-x64-18"
   - Verificar que esté iniciado

2. Verificar la cadena de conexión en `backend/appsettings.json`

3. Verificar credenciales de PostgreSQL

### El Frontend no Conecta con el Backend

1. Verificar que el backend esté ejecutándose en `http://localhost:5000`
2. Verificar configuración de CORS en `Program.cs`
3. Verificar la URL de la API en `frontend/src/environments/environment.ts`

---

## 📚 DOCUMENTACIÓN ADICIONAL

- [README.md](README.md) - Documentación general del proyecto
- [AUTH0_SETUP.md](AUTH0_SETUP.md) - Configuración de Auth0
- [PLANES_NUTRICIONALES.md](PLANES_NUTRICIONALES.md) - Documentación de planes nutricionales
- [database/VERIFICACION_DB.md](database/VERIFICACION_DB.md) - Verificación de base de datos

---

## ✅ CHECKLIST DE CONFIGURACIÓN

- [x] PostgreSQL 18 instalado
- [x] Base de datos `nutriciondb` creada
- [x] 16 tablas creadas exitosamente
- [x] Vista `vista_historias_completas` creada
- [x] Índices y triggers configurados
- [x] Usuario administrador creado (admin/admin)
- [x] Pacientes de prueba insertados
- [x] Cadena de conexión configurada en `appsettings.json`
- [ ] Dependencias del backend restauradas (`dotnet restore`)
- [ ] Dependencias del frontend instaladas (`npm install`)
- [ ] Backend ejecutándose en localhost:5000
- [ ] Frontend ejecutándose en localhost:4200
- [ ] Contraseñas de producción cambiadas (⚠️ PENDIENTE)

---

## 🎯 PRÓXIMOS PASOS

1. **Instalar dependencias del proyecto**
   ```powershell
   cd backend
   dotnet restore
   
   cd ../frontend
   npm install
   ```

2. **Ejecutar la aplicación**
   ```powershell
   # Desde la raíz
   .\start-all.cmd
   ```

3. **Acceder a la aplicación**
   - Abrir navegador en: http://localhost:4200
   - Iniciar sesión con: admin / admin

4. **Configurar para producción**
   - Cambiar contraseñas
   - Configurar Auth0 (ver AUTH0_SETUP.md)
   - Configurar variables de entorno
   - Crear certificados SSL

---

## 📞 SOPORTE

Para más información o problemas, revisar:
- Logs del backend: Terminal donde ejecutaste `dotnet run`
- Logs del frontend: Terminal donde ejecutaste `npm start`
- Logs de PostgreSQL: `C:\Program Files\PostgreSQL\18\data\log\`

---

**Fecha de configuración**: 18 de enero de 2026
**Versión de la base de datos**: 1.0
**Estado**: ✅ Completamente configurada y lista para usar
