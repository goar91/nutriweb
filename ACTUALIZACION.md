# Guía de Actualización - Nuevas Funcionalidades

## 🎉 Cambios Implementados

### ✅ Nuevos Componentes Frontend

1. **Ver Pacientes** (`/pacientes`)
   - Lista completa de pacientes
   - Búsqueda y filtrado
   - Ver detalles de paciente
   - Visualización de historias clínicas por paciente
   - Eliminación de pacientes

2. **Detalle de Paciente** (`/pacientes/:id`)
   - Información personal completa
   - Lista de todas sus historias clínicas
   - Datos antropométricos de cada consulta

3. **Reportes** (`/reportes`)
   - Estadísticas generales del sistema
   - Reporte de pacientes con filtros de fecha
   - Reporte de historias clínicas con filtros
   - Exportación a CSV

### ✅ Nuevos Endpoints Backend

```
GET  /api/pacientes                          - Listar pacientes
GET  /api/pacientes/{id}                     - Detalle de paciente
GET  /api/pacientes/{id}/historias           - Historias de paciente
DELETE /api/pacientes/{id}                   - Eliminar paciente
GET  /api/reportes/estadisticas              - Estadísticas generales
GET  /api/reportes/pacientes                 - Reporte de pacientes
GET  /api/reportes/historias                 - Reporte de historias
```

### ✅ Nuevas Tablas y Vistas en Base de Datos

```sql
-- Tabla de reportes
reportes

-- Vistas para reportes
vista_resumen_pacientes
vista_estadisticas_generales
vista_historias_recientes
```

## 📦 Archivos Nuevos/Modificados

### Nuevos Archivos

```
frontend/src/app/components/pacientes/
├── pacientes.component.ts
├── pacientes.component.html
└── pacientes.component.scss

frontend/src/app/components/paciente-detalle/
├── paciente-detalle.component.ts
├── paciente-detalle.component.html
└── paciente-detalle.component.scss

frontend/src/app/components/reportes/
├── reportes.component.ts
├── reportes.component.html
└── reportes.component.scss

database/
├── reportes_schema.sql
└── configure-reportes.cmd

README_OFFLINE.md
ACTUALIZACION.md (este archivo)
```

### Archivos Modificados

```
backend/Program.cs                           - Nuevos endpoints
frontend/src/app/app.routes.ts              - Nuevas rutas
frontend/src/app/components/navbar/navbar.component.ts - Nuevos enlaces
frontend/src/environments/environment.prod.ts - Configuración offline
```

## 🚀 Pasos para Actualizar

### 1. Actualizar Base de Datos

**Opción A - Usando el script (Windows):**
```bash
cd database
.\configure-reportes.cmd
```

**Opción B - Manual:**
```bash
# Ingresar a PostgreSQL
psql -U postgres -d nutriciondb

# Ejecutar el esquema
\i database/reportes_schema.sql

# Verificar
SELECT * FROM vista_estadisticas_generales;
```

### 2. Actualizar Backend

El backend ya está actualizado con los nuevos endpoints. Solo necesitas reiniciarlo:

```bash
cd backend
dotnet run
```

### 3. Actualizar Frontend

```bash
cd frontend
npm install  # Por si acaso
npm start
```

## 🎯 Verificación

1. Accede a `http://localhost:4200`
2. Inicia sesión
3. Verifica que en el navbar aparezcan:
   - Dashboard
   - Nueva Historia
   - **Ver Pacientes** ← NUEVO
   - **Reportes** ← NUEVO

4. Prueba cada funcionalidad:
   - Click en "Ver Pacientes" → Deberías ver la lista de pacientes
   - Click en "Reportes" → Deberías ver las estadísticas

## 📊 Funcionalidades Nuevas en Detalle

### Ver Pacientes
- **Lista de pacientes**: Muestra todos los pacientes registrados
- **Búsqueda**: Filtra por nombre, cédula, email o teléfono
- **Detalles**: Click en el ícono 👁️ para ver detalles completos
- **Historias**: Ver todas las consultas de un paciente
- **Eliminar**: Click en el ícono 🗑️ (con confirmación)

### Reportes

#### Estadísticas Generales
- Total de pacientes
- Total de historias clínicas
- Pacientes registrados este mes
- Historias registradas este mes
- Distribución por sexo

#### Reporte de Pacientes
- Filtros por fecha de registro
- Total de historias por paciente
- Última fecha de consulta
- Exportación a CSV

#### Reporte de Historias Clínicas
- Filtros por fecha de consulta
- Datos del paciente y de la consulta
- Valores antropométricos (IMC, peso, talla)
- Exportación a CSV

## 🔒 Configuración Offline

El sistema ahora está 100% configurado para funcionar sin internet:

1. **Sin Auth0**: Autenticación completamente local
2. **Sin CDNs**: Todas las dependencias en node_modules
3. **Base de datos local**: PostgreSQL en tu máquina
4. **API local**: Backend .NET en localhost
5. **Frontend local**: Angular servido desde localhost

## 🛠️ Resolución de Problemas

### Las nuevas páginas no aparecen

```bash
# Limpiar y reinstalar frontend
cd frontend
rm -rf node_modules .angular
npm install
npm start
```

### Error en base de datos

```bash
# Verificar que las vistas existan
psql -U postgres -d nutriciondb -c "\dv"

# Deberías ver:
# vista_resumen_pacientes
# vista_estadisticas_generales
# vista_historias_recientes
```

### Error 404 en endpoints

```bash
# Verificar que el backend esté corriendo
# Deberías ver algo como:
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: http://localhost:5000
```

## 📝 Notas Importantes

1. **Backup**: Se recomienda hacer backup de la base de datos antes de aplicar cambios
   ```bash
   pg_dump -U postgres nutriciondb > backup_antes_reportes.sql
   ```

2. **Datos de prueba**: Si no tienes pacientes, crea algunos desde "Nueva Historia"

3. **Exportación CSV**: Los reportes se exportan en formato UTF-8 compatible con Excel

## 🎉 ¡Listo!

Tu sistema NutriWeb ahora cuenta con:
- ✅ Gestión completa de pacientes
- ✅ Reportes y estadísticas
- ✅ Exportación de datos
- ✅ Modo 100% offline
- ✅ Sin dependencias de internet

Para más información, consulta [README_OFFLINE.md](README_OFFLINE.md)
