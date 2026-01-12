# 🏥 NutriWeb - Sistema de Gestión Nutricional

Sistema completo de gestión nutricional con autenticación local, gestión de pacientes, historias clínicas y reportes. **100% Offline** - No requiere conexión a internet.

## ✨ Características

- ✅ **Autenticación local** con sesiones en memoria (sin servicios externos)
- ✅ **Gestión de pacientes** - Ver, crear, editar y eliminar pacientes
- ✅ **Historias clínicas** completas con datos antropométricos, bioquímicos, hábitos y más
- ✅ **Reportes y estadísticas** - Visualización de datos y exportación a CSV
- ✅ **Base de datos PostgreSQL local**
- ✅ **100% Offline** - Funciona sin conexión a internet

## 📋 Requisitos Previos

- Node.js (v18 o superior)
- .NET SDK 10.0
- PostgreSQL 14 o superior
- Visual Studio Code (recomendado)

## 🚀 Instalación y Configuración

### 1. Configurar Base de Datos PostgreSQL

```bash
# Ingresar a PostgreSQL como superusuario
psql -U postgres

# Crear la base de datos
CREATE DATABASE nutriciondb;

# Conectarse a la base de datos
\c nutriciondb

# Ejecutar el esquema principal
\i database/schema.sql

# Ejecutar el esquema de reportes
\i database/reportes_schema.sql

# Salir de PostgreSQL
\q
```

### 2. Configurar Conexión a Base de Datos

Crear el archivo `database/connection.local`:

```
Host=localhost;Port=5432;Database=nutriciondb;Username=postgres;Password=TU_PASSWORD
```

**Importante**: Reemplaza `TU_PASSWORD` con tu contraseña de PostgreSQL.

### 3. Instalar Dependencias del Backend

```bash
cd backend
dotnet restore
```

### 4. Instalar Dependencias del Frontend

```bash
cd frontend
npm install
```

### 5. Crear Usuario Inicial

Ejecutar en PostgreSQL:

```sql
INSERT INTO usuarios (username, email, nombre, password_hash, rol, activo)
VALUES (
  'admin',
  'admin@nutriweb.local',
  'Administrador',
  'admin123', -- Esta contraseña se encriptará automáticamente en el primer login
  'nutricionista',
  true
);
```

## 🎯 Ejecución del Proyecto

### Opción 1: Iniciar todo con un comando (Windows)

```bash
.\start-all.cmd
```

Este comando iniciará automáticamente:
- Backend en `http://localhost:5000`
- Frontend en `http://localhost:4200`

### Opción 2: Iniciar manualmente

#### Terminal 1 - Backend:
```bash
cd backend
dotnet run
```

#### Terminal 2 - Frontend:
```bash
cd frontend
npm start
```

## 🔐 Acceso al Sistema

1. Abre tu navegador en `http://localhost:4200`
2. Inicia sesión con:
   - **Usuario**: `admin`
   - **Contraseña**: `admin123`

## 📱 Funcionalidades Principales

### 1. Dashboard
- Resumen de actividad reciente
- Acceso rápido a funciones principales

### 2. Nueva Historia Clínica
- Registro completo de pacientes
- Datos personales y antecedentes
- Signos vitales y datos antropométricos
- Valores bioquímicos
- Recordatorio de 24 horas
- Frecuencia de consumo de alimentos

### 3. Ver Pacientes
- Lista completa de pacientes registrados
- Búsqueda por nombre, cédula, email o teléfono
- Ver detalles de cada paciente
- Visualizar historias clínicas por paciente
- Eliminar pacientes

### 4. Reportes
- **Estadísticas Generales**: Vista general del sistema
- **Reporte de Pacientes**: Lista filtrable por fechas
- **Reporte de Historias Clínicas**: Lista de consultas con datos antropométricos
- **Exportación a CSV**: Descarga de reportes para análisis externo

## 🗂️ Estructura del Proyecto

```
nutriweb/
├── backend/              # API .NET Core
│   ├── Program.cs        # Endpoints de API
│   ├── appsettings.json  # Configuración
│   └── bin/             # Binarios compilados
│
├── frontend/            # Aplicación Angular
│   ├── src/
│   │   ├── app/
│   │   │   ├── components/
│   │   │   │   ├── dashboard/
│   │   │   │   ├── login/
│   │   │   │   ├── navbar/
│   │   │   │   ├── pacientes/          # ✨ NUEVO
│   │   │   │   ├── paciente-detalle/   # ✨ NUEVO
│   │   │   │   └── reportes/           # ✨ NUEVO
│   │   │   ├── guards/
│   │   │   ├── interceptors/
│   │   │   └── services/
│   │   └── environments/
│   └── package.json
│
├── database/
│   ├── schema.sql           # Esquema principal
│   ├── reportes_schema.sql  # ✨ NUEVO - Esquema de reportes
│   └── connection.local     # Configuración de conexión (crear)
│
└── start-all.cmd           # Script de inicio
```

## 🔒 Seguridad

- **Autenticación local** con tokens de sesión
- **Hashing de contraseñas** con PBKDF2 + SHA256
- **Sesiones en memoria** del servidor
- **Interceptor de autenticación** en el frontend
- **Guards de rutas** para proteger páginas
- **Logs de acceso** en base de datos

## 🌐 Modo 100% Offline

El sistema está diseñado para funcionar completamente sin conexión a internet:

- ✅ **Sin CDNs externos**: Todas las dependencias están incluidas localmente
- ✅ **Sin servicios en la nube**: Autenticación y datos completamente locales
- ✅ **Base de datos local**: PostgreSQL en tu máquina
- ✅ **Backend local**: API .NET en localhost
- ✅ **Frontend local**: Angular servido localmente

## 📊 Endpoints de API

### Autenticación
- `POST /api/auth/login` - Iniciar sesión
- `POST /api/auth/register` - Registrar usuario
- `POST /api/auth/logout` - Cerrar sesión
- `GET /api/auth/verify` - Verificar sesión

### Pacientes
- `GET /api/pacientes` - Listar todos los pacientes
- `GET /api/pacientes/{id}` - Obtener detalles de paciente
- `GET /api/pacientes/{id}/historias` - Obtener historias de paciente
- `DELETE /api/pacientes/{id}` - Eliminar paciente

### Historias Clínicas
- `POST /api/nutrition/history` - Crear nueva historia clínica

### Reportes
- `GET /api/reportes/estadisticas` - Estadísticas generales
- `GET /api/reportes/pacientes?fechaDesde&fechaHasta` - Reporte de pacientes
- `GET /api/reportes/historias?fechaDesde&fechaHasta` - Reporte de historias

## 🛠️ Tecnologías Utilizadas

### Backend
- .NET 10.0
- ASP.NET Core Minimal APIs
- Npgsql (PostgreSQL connector)
- PBKDF2 para hashing de contraseñas

### Frontend
- Angular 21
- TypeScript
- SCSS
- Signals API
- Standalone Components

### Base de Datos
- PostgreSQL 14+
- Vistas materializadas para reportes
- Índices optimizados
- Triggers para auditoría

## 🐛 Solución de Problemas

### Error de conexión a base de datos
```bash
# Verificar que PostgreSQL esté corriendo
sudo systemctl status postgresql  # Linux
# o
net start postgresql-x64-14       # Windows

# Verificar credenciales en database/connection.local
```

### Error de puerto en uso
```bash
# Cambiar el puerto del backend en backend/Program.cs
app.Urls.Clear();
app.Urls.Add("http://localhost:5001");  # Usar otro puerto

# Actualizar apiUrl en frontend/src/app/services/*.ts
```

### Error al compilar Angular
```bash
cd frontend
rm -rf node_modules package-lock.json
npm install
```

## 📝 Notas Adicionales

- Las contraseñas se encriptan automáticamente al primer uso
- Los reportes se pueden exportar a CSV para análisis externo
- El sistema mantiene logs de todos los accesos e intentos de login
- Las sesiones expiran al cerrar el navegador o hacer logout

## 📄 Licencia

Este proyecto es de uso privado y educativo.

## 👥 Soporte

Para problemas o preguntas, verifica:
1. Los logs del backend en la consola
2. Los errores del frontend en las herramientas de desarrollador del navegador
3. Los logs de PostgreSQL

---

**¡Listo para usar!** El sistema está completamente configurado para funcionar sin conexión a internet.
