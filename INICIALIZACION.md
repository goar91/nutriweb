# 🚀 INICIALIZACIÓN DE NUTRIWEB

## ⚠️ REQUISITO CRÍTICO: .NET 10 SDK

**La aplicación NO puede ejecutarse sin .NET 10 SDK instalado.**

### Instalar .NET 10 SDK

1. **Descargar** desde: https://dotnet.microsoft.com/download/dotnet/10.0
2. Buscar la sección **".NET 10.0 SDK"** (NO Runtime)
3. Descargar el instalador para Windows x64
4. Ejecutar el instalador
5. **Reiniciar la terminal** después de la instalación
6. Verificar instalación:
   ```powershell
   dotnet --version
   # Debe mostrar: 10.0.x
   ```

---

## ✅ ESTADO ACTUAL DEL SISTEMA

### Verificado ✅
- ✅ PostgreSQL 18 - **Ejecutándose** (servicio postgresql-x64-18)
- ✅ Base de datos nutriciondb - **Configurada** con 15 tablas
- ✅ Node.js v24.13.0 - **Instalado**
- ✅ Scripts de base de datos - **Creados**

### Pendiente ⚠️
- ❌ .NET 10 SDK - **NO INSTALADO** (requerido para backend)
- ⏳ Dependencias del backend - Pendiente (requiere .NET SDK)
- ⏳ Dependencias del frontend - Pendiente

---

## 📋 PASOS PARA INICIALIZAR

### Una vez instalado .NET 10 SDK:

#### Opción 1: Script Automático (Recomendado)

```cmd
inicializar.cmd
```

Este script:
1. Verifica .NET SDK y Node.js
2. Restaura dependencias del backend (`dotnet restore`)
3. Instala dependencias del frontend (`npm install`)
4. Confirma que todo está listo

#### Opción 2: Manual

```powershell
# 1. Restaurar dependencias del backend
cd backend
dotnet restore

# 2. Instalar dependencias del frontend
cd ../frontend
npm install

# 3. Volver a la raíz
cd ..
```

---

## 🚀 EJECUTAR LA APLICACIÓN

Después de inicializar:

### Opción A: Ejecutar Todo (start-all.cmd)
```cmd
start-all.cmd
```

### Opción B: Manual - Backend
```powershell
cd backend
dotnet run
# Backend disponible en: http://localhost:5000
```

### Opción C: Manual - Frontend (en otra terminal)
```cmd
cd frontend
npm start
# Frontend disponible en: http://localhost:4200
```

---

## 🔍 VERIFICACIONES

### Verificar Sistema Completo
```cmd
verificar-sistema.cmd
```

### Verificar Base de Datos
```powershell
$env:PGPASSWORD = "030762"
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -c "SELECT COUNT(*) FROM pacientes;"
```

---

## 📊 RESUMEN DE DEPENDENCIAS

### Backend (.NET 10)
Paquetes en `backend/backend.csproj`:
- Microsoft.AspNetCore.OpenApi 10.0.1
- Npgsql 8.0.5 (PostgreSQL driver)
- System.Text.Encoding.CodePages 9.0.0

### Frontend (Angular 21)
Principales paquetes en `frontend/package.json`:
- @angular/core 21.0.0
- @angular/common 21.0.0
- @angular/forms 21.0.0
- @angular/platform-browser 21.0.0

---

## 🐛 SOLUCIÓN DE PROBLEMAS

### Error: "dotnet no se reconoce"
**Causa**: .NET SDK no está instalado o no está en el PATH
**Solución**: 
1. Instalar .NET 10 SDK
2. Reiniciar la terminal
3. Verificar con: `dotnet --version`

### Error: "npm no se reconoce" o "ejecución de scripts deshabilitada"
**Solución**: Usar archivos .cmd en lugar de comandos npm directamente
```cmd
cd frontend
call npm install
```

### Error: "Cannot connect to PostgreSQL"
**Solución**:
1. Verificar servicio PostgreSQL:
   ```powershell
   Get-Service postgresql-x64-18
   ```
2. Si está detenido, iniciarlo:
   ```powershell
   Start-Service postgresql-x64-18
   ```

### Puerto 5000 o 4200 ya en uso
**Backend (5000)**:
- Editar `backend/Properties/launchSettings.json`
- Cambiar el puerto en las URLs

**Frontend (4200)**:
```cmd
cd frontend
npm start -- --port 4300
```

---

## 📂 ESTRUCTURA POST-INICIALIZACIÓN

Después de ejecutar la inicialización:

```
nutriweb-1/
├── backend/
│   ├── bin/          # ✅ Binarios compilados
│   └── obj/          # ✅ Archivos de compilación
├── frontend/
│   └── node_modules/ # ✅ Dependencias npm instaladas
└── ...
```

---

## ⏱️ TIEMPOS ESTIMADOS

- **Descarga .NET SDK**: 2-5 minutos
- **Instalación .NET SDK**: 1-2 minutos
- **dotnet restore**: 30-60 segundos
- **npm install**: 2-5 minutos (primera vez)

**Total**: ~10-15 minutos (primera vez)

---

## 🎯 CHECKLIST DE INICIALIZACIÓN

- [ ] .NET 10 SDK descargado
- [ ] .NET 10 SDK instalado
- [ ] Terminal reiniciada
- [ ] `dotnet --version` funciona
- [ ] `dotnet restore` ejecutado en backend/
- [ ] `npm install` ejecutado en frontend/
- [ ] PostgreSQL ejecutándose
- [ ] Base de datos nutriciondb verificada

---

## 🔐 RECORDATORIO DE CREDENCIALES

### Base de Datos
```
Host: localhost
Port: 5432
Database: nutriciondb
Username: postgres
Password: 030762
```

### Usuario Admin
```
Username: admin
Password: admin
```

---

## 📚 DOCUMENTACIÓN RELACIONADA

- **[INDEX.md](INDEX.md)** - Índice completo de documentación
- **[RESUMEN_CONFIGURACION.md](RESUMEN_CONFIGURACION.md)** - Estado de configuración
- **[CONFIGURACION_COMPLETA.md](CONFIGURACION_COMPLETA.md)** - Guía detallada

---

## 🆘 AYUDA RÁPIDA

Si tienes problemas, ejecuta:
```cmd
verificar-sistema.cmd
```

Este script te dirá exactamente qué falta o qué está mal configurado.

---

**Siguiente paso**: Instalar .NET 10 SDK y ejecutar `inicializar.cmd`
