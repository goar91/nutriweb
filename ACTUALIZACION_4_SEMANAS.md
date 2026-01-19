# Actualización del Sistema de Planes Alimenticios: De 2 a 4 Semanas

## Cambios Implementados

### 1. Base de Datos
- ✅ **Script SQL creado**: `database/actualizar_a_4_semanas.sql`
- ✅ **Script ejecutable**: `database/actualizar-4-semanas.cmd`
- **Cambio**: Modificado el constraint de la tabla `alimentacion_semanal` para aceptar semanas 1, 2, 3 y 4 (anteriormente solo 1 y 2)

### 2. Backend (.NET)
- ✅ **Archivo modificado**: `backend/Program.cs`
- **Cambios realizados**:
  - Agregadas propiedades `Semana3` y `Semana4` en `GuardarPlanAlimentacionRequest`
  - Endpoint **POST** `/api/planes`: Ahora guarda las 4 semanas
  - Endpoint **PUT** `/api/planes/{planId}`: Ahora actualiza las 4 semanas
  - Endpoint **GET** `/api/planes/{planId}`: Ahora devuelve las 4 semanas
  - Endpoint **DELETE** `/api/planes/{planId}`: **NUEVO** - Permite eliminar planes con confirmación

### 3. Frontend (Angular)
- ✅ **Servicio modificado**: `frontend/src/app/services/planes.service.ts`
  - Actualizada interfaz `PlanAlimentacion` para incluir `semana3` y `semana4` (opcionales)
  - Método `eliminarPlan()` ya existía

- ✅ **Componente modificado**: `frontend/src/app/components/planes-alimentacion/planes-alimentacion.component.ts`
  - **Título**: Cambiado de "Dos Semanas" a "Cuatro Semanas"
  - **Tabs de navegación**: Agregadas pestañas para Semana 3 y Semana 4
  - **Variables**: Agregadas `semana3` y `semana4`
  - **Métodos actualizados**:
    - `getPlanActual()`: Soporta 4 semanas con switch statement
    - `getPlanSemana()`: Devuelve la semana correcta (1-4)
    - `limpiarPlan()`: Limpia la semana actual (1-4)
    - `onHistoriaChange()`: Inicializa las 4 semanas vacías
    - `guardarPlan()`: Envía las 4 semanas al backend
    - `cargarPlan()`: Carga las 4 semanas desde el backend
    - `imprimirTodasSemanas()`: Renombrado de `imprimirAmbasSemanas()`, imprime las 4 semanas
  - **Nuevo método**: `eliminarPlan()` con confirmación y actualización automática de la lista
  - **Botón eliminar**: Agregado en cada plan guardado con icono de papelera
  - **Estilos CSS**: Agregados para `.btn-delete` (rojo) y `.plan-actions` (contenedor flex)

## Instrucciones de Actualización

### Paso 1: Actualizar la Base de Datos

**Opción A - Usando el script .cmd (Recomendado):**
```cmd
cd database
actualizar-4-semanas.cmd
```
Cuando solicite la contraseña, ingrese: `030762`

**Opción B - Manualmente con psql:**
```cmd
cd database
"C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -d nutriciondb -f actualizar_a_4_semanas.sql
```

### Paso 2: Verificar la Actualización de la Base de Datos
```sql
-- En psql, verificar que el constraint se actualizó correctamente:
SELECT constraint_name, check_clause
FROM information_schema.check_constraints
WHERE constraint_name = 'alimentacion_semanal_semana_check';

-- Debe mostrar: CHECK (semana = ANY (ARRAY[1, 2, 3, 4]))
```

### Paso 3: Compilar y Ejecutar
No se requiere ninguna acción adicional. Los cambios en el frontend y backend ya están implementados.

```cmd
# Iniciar todo el sistema
start-all.cmd
```

## Nuevas Funcionalidades

### 📅 Planes de 4 Semanas
- Ahora puedes crear planes nutricionales con **4 semanas** de duración
- Navegación por tabs: Semana 1, 2, 3 y 4
- Cada semana mantiene sus 7 días (Lunes a Domingo) con 5 comidas cada día

### 🗑️ Eliminar Planes
- Botón **"Eliminar"** (rojo) disponible en cada plan guardado
- Confirmación antes de eliminar: "¿Estás seguro de que deseas eliminar este plan?"
- Actualización automática de la lista después de eliminar
- Si el plan eliminado estaba cargado, limpia el formulario automáticamente

### 🖨️ Impresión Mejorada
- Botón actualizado: "Imprimir todas las semanas"
- Imprime las 4 semanas en un solo documento

## Compatibilidad hacia Atrás

✅ **Los planes antiguos de 2 semanas seguirán funcionando correctamente**
- Los campos `semana3` y `semana4` son **opcionales** en el frontend
- El backend verifica si existen antes de procesarlos
- Los planes existentes solo mostrarán contenido en Semana 1 y 2
- Las semanas 3 y 4 aparecerán vacías hasta que se editen y guarden

## Estructura de Datos

### Frontend → Backend (Guardar Plan)
```json
{
  "HistoriaId": "uuid",
  "FechaInicio": "2026-01-18",
  "Semana1": { "lunes": {...}, "martes": {...}, ... },
  "Semana2": { "lunes": {...}, "martes": {...}, ... },
  "Semana3": { "lunes": {...}, "martes": {...}, ... },
  "Semana4": { "lunes": {...}, "martes": {...}, ... }
}
```

### Backend → Frontend (Obtener Plan)
```json
{
  "id": "uuid",
  "historia_id": "uuid",
  "fecha_inicio": "2026-01-18",
  "semana1": {...},
  "semana2": {...},
  "semana3": {...},
  "semana4": {...}
}
```

## Endpoint DELETE Nuevo

### `DELETE /api/planes/{planId}`
**Autenticación**: Requiere token JWT válido

**Respuesta exitosa**:
```json
{
  "success": true,
  "message": "Plan eliminado exitosamente"
}
```

**Errores posibles**:
- `401 Unauthorized`: Token inválido o expirado
- `404 Not Found`: Plan no encontrado
- `500 Internal Server Error`: Error en el servidor

**Transaccionalidad**: Usa transacciones SQL para garantizar que tanto el plan como su alimentación semanal se eliminen correctamente.

## Archivos Modificados

```
database/
  ✨ actualizar_a_4_semanas.sql (NUEVO)
  ✨ actualizar-4-semanas.cmd (NUEVO)

backend/
  ✏️ Program.cs (MODIFICADO)
    - GuardarPlanAlimentacionRequest: +Semana3, +Semana4
    - POST /api/planes: Guarda 4 semanas
    - PUT /api/planes/{id}: Actualiza 4 semanas
    - GET /api/planes/{id}: Devuelve 4 semanas
    - DELETE /api/planes/{id}: NUEVO endpoint

frontend/src/app/
  services/
    ✏️ planes.service.ts (MODIFICADO)
      - PlanAlimentacion: +semana3?, +semana4?
      - eliminarPlan() ya existía
  
  components/planes-alimentacion/
    ✏️ planes-alimentacion.component.ts (MODIFICADO)
      - +semana3, +semana4 variables
      - +tabs Semana 3 y 4
      - +botón eliminar con estilos
      - +eliminarPlan() método
      - Actualizados todos los métodos para 4 semanas
```

## Pruebas Sugeridas

1. ✅ **Crear un plan nuevo con 4 semanas**
   - Llenar datos en las 4 semanas
   - Guardar el plan
   - Verificar que se guardó correctamente

2. ✅ **Cargar un plan existente**
   - Seleccionar una historia clínica
   - Cargar un plan guardado
   - Verificar que las 4 semanas se cargan correctamente

3. ✅ **Eliminar un plan**
   - Ver planes guardados
   - Hacer clic en "Eliminar"
   - Confirmar eliminación
   - Verificar que desaparece de la lista

4. ✅ **Imprimir 4 semanas**
   - Llenar datos en las 4 semanas
   - Hacer clic en "Imprimir todas las semanas"
   - Verificar que aparecen las 4 semanas en la vista previa

5. ✅ **Compatibilidad con planes antiguos**
   - Cargar un plan creado antes de la actualización
   - Verificar que Semana 1 y 2 tienen datos
   - Verificar que Semana 3 y 4 están vacías

## Notas Importantes

⚠️ **Antes de ejecutar en producción**:
- Hacer backup de la base de datos
- Probar en ambiente de desarrollo primero
- Verificar que el script SQL se ejecutó correctamente

✨ **Mejoras futuras posibles**:
- Agregar selector de cantidad de semanas (2, 3 o 4)
- Copiar contenido de una semana a otra
- Plantillas de planes predefinidos
- Exportar planes a PDF con mejor formato

---

**Fecha de actualización**: 18 de enero de 2026  
**Versión**: 2.0 - Sistema de Planes de 4 Semanas con Eliminación
