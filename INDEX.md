# 📚 ÍNDICE DE DOCUMENTACIÓN - NUTRIWEB

## 🎯 INICIO RÁPIDO

Si es tu primera vez configurando el proyecto, sigue este orden:

1. **[RESUMEN_CONFIGURACION.md](RESUMEN_CONFIGURACION.md)** - ⭐ EMPIEZA AQUÍ
   - Resumen ejecutivo de lo que se ha configurado
   - Estado actual del proyecto
   - Próximos pasos críticos

2. **[CONFIGURACION_COMPLETA.md](CONFIGURACION_COMPLETA.md)** - Guía detallada
   - Información completa de configuración
   - Troubleshooting
   - Requisitos y verificaciones

3. **Instalar .NET 10 SDK** (si aún no lo has hecho)
   - Descargar de: https://dotnet.microsoft.com/download/dotnet/10.0
   - Verificar con: `dotnet --version`

4. **Ejecutar la aplicación** (ver sección "Ejecución" abajo)

---

## ✅ ESTADO DE CONFIGURACIÓN

### Base de Datos PostgreSQL ✅ COMPLETADA
- **15 tablas** principales creadas
- **1 vista** optimizada
- **43 índices** para rendimiento
- **3 triggers** automatizados
- **3 funciones** de utilidad
- **2 pacientes** de prueba
- **1 usuario** administrador

### Scripts Creados ✅
- `database/setup_complete_database.sql` - Script SQL consolidado
- `database/setup_database.ps1` - Automatización PowerShell
- `database/setup_database.cmd` - Script batch
- `verificar-sistema.cmd` - Verificación del sistema

### Pendiente ⚠️
- Instalar .NET 10 SDK
- Ejecutar `dotnet restore` en backend
- Ejecutar `npm install` en frontend
- Cambiar contraseñas por defecto

---

## 📖 DOCUMENTACIÓN PRINCIPAL

### Configuración y Setup
- **[RESUMEN_CONFIGURACION.md](RESUMEN_CONFIGURACION.md)** - Resumen ejecutivo ⭐
- **[CONFIGURACION_COMPLETA.md](CONFIGURACION_COMPLETA.md)** - Guía completa detallada
- **[README.md](README.md)** - Información general del proyecto
- **[SOLUCION.md](SOLUCION.md)** - Estructura de la solución

### Base de Datos
- **[database/COMANDOS_VERIFICACION.md](database/COMANDOS_VERIFICACION.md)** - Comandos útiles de verificación
- **[database/VERIFICACION_DB.md](database/VERIFICACION_DB.md)** - Verificación de base de datos
- **[database/setup_complete_database.sql](database/setup_complete_database.sql)** - Script SQL completo

### Características Específicas
- **[AUTH0_SETUP.md](AUTH0_SETUP.md)** - Configuración de autenticación Auth0
- **[PLANES_NUTRICIONALES.md](PLANES_NUTRICIONALES.md)** - Planes nutricionales
- **[AUTOCOMPLETADO_CEDULA.md](AUTOCOMPLETADO_CEDULA.md)** - Autocompletado de cédula
- **[OPTIMIZACIONES.md](OPTIMIZACIONES.md)** - Optimizaciones realizadas

### Cambios y Versiones
- **[CHANGELOG.md](CHANGELOG.md)** - Registro de cambios

---

## 🚀 EJECUCIÓN

### Opción 1: Ejecución Automática (Recomendado)
```powershell
# Desde la raíz del proyecto
.\start-all.cmd
```

### Opción 2: Ejecución Manual

#### Backend
```powershell
cd backend
dotnet restore    # Primera vez
dotnet run        # http://localhost:5000
```

#### Frontend
```powershell
cd frontend
npm install       # Primera vez
npm start         # http://localhost:4200
```

---

## 🔍 VERIFICACIÓN

### Verificar Sistema Completo
```powershell
.\verificar-sistema.cmd
```

### Verificar Base de Datos Manualmente
```powershell
$env:PGPASSWORD = "030762"
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "
SELECT 'Tablas' as tipo, COUNT(*)::text as total 
FROM information_schema.tables 
WHERE table_schema = 'public' AND table_type = 'BASE TABLE';
"
```

Ver más comandos en: [database/COMANDOS_VERIFICACION.md](database/COMANDOS_VERIFICACION.md)

---

## 🔐 CREDENCIALES

### Base de Datos PostgreSQL
```
Host: localhost
Port: 5432
Database: nutriciondb
Username: postgres
Password: 030762
```

### Usuario Administrador del Sistema
```
Username: admin
Password: admin
```

⚠️ **IMPORTANTE**: Cambiar estas credenciales en producción

---

## 📦 SCRIPTS DISPONIBLES

### Base de Datos
| Script | Descripción |
|--------|-------------|
| `database/setup_complete_database.sql` | Script SQL completo de configuración |
| `database/setup_database.ps1` | Script PowerShell automatizado |
| `database/setup_database.cmd` | Script batch alternativo |

### Sistema
| Script | Descripción |
|--------|-------------|
| `verificar-sistema.cmd` | Verificar configuración completa |
| `start-all.cmd` | Iniciar backend y frontend |
| `stop-all.cmd` | Detener todos los servicios |

---

## 🗂️ ESTRUCTURA DE BASE DE DATOS

### Tablas Principales (15)
1. `pacientes` - Información de pacientes
2. `historias_clinicas` - Historias clínicas
3. `antecedentes` - Antecedentes médicos
4. `habitos` - Hábitos de vida
5. `signos_vitales` - Signos vitales
6. `datos_antropometricos` - Medidas antropométricas
7. `valores_bioquimicos` - Análisis bioquímicos
8. `recordatorio_24h` - Recordatorio 24 horas
9. `frecuencia_consumo` - Frecuencia de consumo
10. `usuarios` - Usuarios del sistema
11. `sesiones` - Sesiones activas
12. `logs_acceso` - Logs de acceso
13. `auditoria` - Auditoría
14. `planes_nutricionales` - Planes nutricionales
15. `alimentacion_semanal` - Alimentación semanal

### Vistas (1)
- `vista_historias_completas` - Consultas optimizadas

### Funciones (3)
- `actualizar_fecha_modificacion()` - Actualiza fechas automáticamente
- `actualizar_fecha_modificacion_plan()` - Actualiza fechas de planes
- `limpiar_sesiones_expiradas()` - Limpia sesiones antiguas

---

## 🛠️ REQUISITOS DE SOFTWARE

### Instalado ✅
- PostgreSQL 18
- Node.js 18+

### Pendiente ⚠️
- .NET 10 SDK - [Descargar aquí](https://dotnet.microsoft.com/download/dotnet/10.0)

### Verificar Instalaciones
```powershell
# PostgreSQL
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" --version

# Node.js
node --version
npm --version

# .NET (después de instalar)
dotnet --version
```

---

## 🔄 RECONFIGURAR BASE DE DATOS

Si necesitas recrear la base de datos desde cero:

```powershell
cd database

# Configurar contraseña
$env:PGPASSWORD = "030762"

# Eliminar base de datos existente
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -c "DROP DATABASE IF EXISTS nutriciondb;"

# Crear nueva base de datos
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -c "CREATE DATABASE nutriciondb;"

# Ejecutar script completo
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -f "setup_complete_database.sql"

# Limpiar variable de entorno
$env:PGPASSWORD = $null
```

---

## 📞 SOLUCIÓN DE PROBLEMAS

### Problemas Comunes

| Problema | Solución | Documentación |
|----------|----------|---------------|
| PostgreSQL no responde | Verificar servicio en Windows | [CONFIGURACION_COMPLETA.md](CONFIGURACION_COMPLETA.md#solución-de-problemas) |
| Error de conexión | Revisar credenciales en appsettings.json | [CONFIGURACION_COMPLETA.md](CONFIGURACION_COMPLETA.md#error-de-conexión-en-el-backend) |
| dotnet no encontrado | Instalar .NET 10 SDK | [RESUMEN_CONFIGURACION.md](RESUMEN_CONFIGURACION.md#instalar-net-10-sdk) |
| Puerto en uso | Cambiar puerto en configuración | [CONFIGURACION_COMPLETA.md](CONFIGURACION_COMPLETA.md#solución-de-problemas) |

---

## 📊 DIAGRAMA DE FLUJO DE CONFIGURACIÓN

```
1. ✅ PostgreSQL Instalado
        ↓
2. ✅ Base de datos creada (nutriciondb)
        ↓
3. ✅ Tablas y estructura configurada (setup_complete_database.sql)
        ↓
4. ⚠️ Instalar .NET 10 SDK
        ↓
5. ⏳ dotnet restore (backend)
        ↓
6. ⏳ npm install (frontend)
        ↓
7. ⏳ Ejecutar aplicación (start-all.cmd)
        ↓
8. 🎉 Aplicación funcionando
```

---

## 🎯 ACCESOS RÁPIDOS

### URLs de la Aplicación
- **Backend API**: http://localhost:5000
- **Frontend**: http://localhost:4200
- **Swagger/API Docs**: http://localhost:5000/swagger (si está configurado)

### Comandos de un Solo Paso
```powershell
# Verificar todo
.\verificar-sistema.cmd

# Iniciar todo
.\start-all.cmd

# Detener todo
.\stop-all.cmd

# Verificar base de datos
cd database
# Ver COMANDOS_VERIFICACION.md para comandos específicos
```

---

## 📅 INFORMACIÓN DE VERSIÓN

- **Fecha de configuración**: 18 de enero de 2026
- **Versión de base de datos**: 1.0
- **PostgreSQL**: 18
- **Angular**: 21
- **.NET**: 10
- **Node.js**: 18+

---

## 🎓 TUTORIALES PASO A PASO

### Para Nuevos Desarrolladores
1. Leer [RESUMEN_CONFIGURACION.md](RESUMEN_CONFIGURACION.md)
2. Instalar .NET 10 SDK
3. Ejecutar `verificar-sistema.cmd`
4. Seguir checklist en [CONFIGURACION_COMPLETA.md](CONFIGURACION_COMPLETA.md#checklist-de-configuración)

### Para Mantenimiento de Base de Datos
1. Consultar [database/COMANDOS_VERIFICACION.md](database/COMANDOS_VERIFICACION.md)
2. Usar scripts en carpeta `database/`
3. Revisar logs en PostgreSQL

### Para Desarrollo Frontend/Backend
1. Ver [README.md](README.md)
2. Configurar Auth0 según [AUTH0_SETUP.md](AUTH0_SETUP.md)
3. Revisar [PLANES_NUTRICIONALES.md](PLANES_NUTRICIONALES.md) para funcionalidades

---

**🚀 ¡Todo está listo para comenzar a desarrollar!**

**Siguiente paso**: Instalar .NET 10 SDK y ejecutar `.\start-all.cmd`
