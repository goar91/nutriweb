# Guía de Distribución y Protección de NutriWeb

## 🔒 Sistema de Protección Implementado

### Características de Seguridad

1. **Compilación a Ejecutable Nativo**
   - Backend compilado a ejecutable .exe de un solo archivo
   - Código C# compilado (no es código fuente)
   - Frontend ofuscado y minimizado

2. **Protección del Código**
   - Backend: Código compilado en binario nativo
   - Frontend: JavaScript ofuscado con build optimizer
   - Sin archivos de código fuente en la distribución
   - Símbolos de depuración eliminados

## 📦 Proceso de Empaquetado

### Paso 1: Compilar la Aplicación para Distribución

```cmd
build-release.cmd
```

Este comando:
- ✅ Compila el frontend en modo producción con ofuscación
- ✅ Compila el backend a ejecutable nativo (.exe)
- ✅ Empaqueta todo en un solo archivo ejecutable
- ✅ Genera la carpeta `publish\dist\` con los archivos listos

**Archivos generados:**
```
publish\dist\
├── backend.exe              (Ejecutable principal - ~90MB)
├── appsettings.json         (Configuración)
├── connection.txt           (Ejemplo de configuración DB)
├── LEEME.txt               (Instrucciones para el cliente)
└── wwwroot\
    └── browser\            (Frontend integrado)
```

### Paso 2: Preparar Paquete para el Cliente

**Copiar estos archivos a una carpeta limpia:**

```
NutriWeb_v1.0\
├── backend.exe              ← Del paso 1
├── appsettings.json         ← Del paso 1 (editar configuración DB)
├── LEEME.txt               ← Del paso 1
└── wwwroot\                ← Del paso 1 (carpeta completa)
```

### Paso 3: Configurar Base de Datos

Editar `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=nutriciondb;Username=postgres;Password=CONTRASEÑA_CLIENTE"
  }
}
```

### Paso 4: Entregar al Cliente

**Empaqueta la carpeta en un ZIP:**
```
NutriWeb_v1.0.zip
```

## 🛡️ Nivel de Protección

### Backend (Alto)
- ✅ **Código Compilado**: Binario nativo Windows x64
- ✅ **Sin Código Fuente**: No se incluyen archivos .cs
- ✅ **Ofuscación Nativa**: El compilador .NET ya ofusca significativamente
- ✅ **Single File**: Todo en un solo .exe
- ✅ **Sin Símbolos**: DebugSymbols=false
- ⚠️ **Reversible**: Con herramientas avanzadas (ILSpy, dnSpy) pero requiere expertise

### Frontend (Medio-Alto)
- ✅ **Minificado**: Variables y funciones renombradas
- ✅ **Ofuscado**: Build optimizer de Angular
- ✅ **Tree-shaking**: Código no usado eliminado
- ✅ **Hashing**: Nombres de archivos aleatorios
- ⚠️ **Visible en Navegador**: El JavaScript siempre es visible en algún nivel

### Base de Datos (No Incluida)
- ❌ **No se distribuye**: El cliente debe tener PostgreSQL
- ✅ **Schema Scripts**: Proporcionados pero no ejecutables sin conocimiento
- ✅ **Datos Propietarios**: No se incluyen datos del cliente original

## 🚀 Instrucciones para el Cliente Final

### Requisitos del Sistema

- Windows 10/11 (64 bits)
- PostgreSQL 18 o superior
- 200 MB de espacio en disco
- 4 GB de RAM mínimo

### Instalación

1. **Instalar PostgreSQL** (si no está instalado)
   - Descargar de: https://www.postgresql.org/download/
   - Durante instalación, recordar usuario y contraseña

2. **Crear Base de Datos**
   ```sql
   CREATE DATABASE nutriciondb;
   ```

3. **Ejecutar Scripts de Base de Datos**
   - Proporcionar script `schema.sql` al cliente
   - Ejecutar en PostgreSQL usando pgAdmin o psql

4. **Configurar Conexión**
   - Editar `appsettings.json`
   - Colocar datos de PostgreSQL del cliente

5. **Iniciar Aplicación**
   - Doble clic en `backend.exe`
   - Abrir navegador en: http://localhost:5000
   - Usuario: `admin` / Contraseña: `admin`

## 📋 Checklist de Distribución

Antes de entregar al cliente:

- [ ] Compilado con `build-release.cmd`
- [ ] `appsettings.json` configurado (o instrucciones claras)
- [ ] Probado que `backend.exe` inicia correctamente
- [ ] `LEEME.txt` incluido con instrucciones
- [ ] Scripts de base de datos proporcionados
- [ ] Usuario/contraseña de prueba documentados
- [ ] Contacto de soporte proporcionado

## 🆘 Soporte Post-Venta

### Problemas Comunes


**1. "No puede conectar a la base de datos"**
- Verificar que PostgreSQL esté corriendo
- Revisar `appsettings.json`
- Verificar firewall/antivirus

**2. "Puerto 5000 en uso"**
- Cambiar puerto en `appsettings.json`:
  ```json
  "Urls": "http://localhost:5001"
  ```

## 📝 Notas Legales

**Recomendaciones:**
1. Especificar términos de soporte
2. Definir política de actualizaciones
3. Establecer cláusulas de no redistribución

## 🔄 Actualizaciones

### Para Enviar Actualización al Cliente:

1. Compilar nueva versión: `build-release.cmd`
2. Enviar solo `backend.exe` actualizado
3. Instruir al cliente: "Reemplazar backend.exe"

### Changelog de Versiones

Mantener registro:
```
v1.0 - Enero 2026
- Release inicial
- Planes de 4 semanas

v1.1 - Próxima
- [Mejoras futuras]
```

---

**Última actualización**: 18 de enero de 2026  
**Desarrollador**: [Tu Nombre/Empresa]  
**Contacto**: [Tu Email/Teléfono]
