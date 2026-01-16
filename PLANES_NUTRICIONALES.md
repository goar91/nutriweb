# Funcionalidad de Planes Nutricionales

## Descripción
Sistema completo para gestión de planes nutricionales con alimentación programada para 2 semanas.

## Base de Datos

### Tablas creadas:
1. **planes_nutricionales**
   - `id`: UUID (clave primaria)
   - `historia_id`: UUID (referencia a historias_clinicas)
   - `fecha_inicio`: DATE
   - `fecha_fin`: DATE (opcional)
   - `objetivo`: TEXT
   - `calorias_diarias`: DECIMAL(10,2)
   - `observaciones`: TEXT
   - `activo`: BOOLEAN
   - `fecha_creacion`: TIMESTAMP
   - `fecha_modificacion`: TIMESTAMP

2. **alimentacion_semanal**
   - `id`: UUID (clave primaria)
   - `plan_id`: UUID (referencia a planes_nutricionales)
   - `semana`: INT (1 o 2)
   - `dia_semana`: INT (1-7, siendo 1=Lunes)
   - `desayuno`: TEXT
   - `snack_manana`: TEXT
   - `almuerzo`: TEXT
   - `snack_tarde`: TEXT
   - `cena`: TEXT
   - `snack_noche`: TEXT
   - `observaciones`: TEXT
   - `fecha_creacion`: TIMESTAMP

## Backend - API Endpoints

Todos los endpoints requieren autenticación (Bearer Token).

### GET /api/nutrition/planes/{historiaId}
Obtiene todos los planes de una historia clínica.

**Respuesta:**
```json
[
  {
    "id": "uuid",
    "historia_id": "uuid",
    "fecha_inicio": "2024-01-15",
    "fecha_fin": "2024-02-15",
    "objetivo": "Pérdida de peso",
    "calorias_diarias": 1800.00,
    "observaciones": "Plan inicial",
    "activo": true,
    "fecha_creacion": "2024-01-15 10:00:00",
    "fecha_modificacion": "2024-01-15 10:00:00"
  }
]
```

### POST /api/nutrition/planes
Crea un nuevo plan nutricional con su alimentación semanal.

**Body:**
```json
{
  "historiaId": "uuid",
  "fechaInicio": "2024-01-15",
  "fechaFin": "2024-02-15",
  "objetivo": "Pérdida de peso",
  "caloriasDiarias": 1800.00,
  "observaciones": "Plan inicial",
  "activo": true,
  "alimentacionSemanal": [
    {
      "semana": 1,
      "diaSemana": 1,
      "desayuno": "Avena con frutas",
      "snackManana": "Yogur natural",
      "almuerzo": "Pollo a la plancha con vegetales",
      "snackTarde": "Frutas",
      "cena": "Ensalada de atún",
      "snackNoche": "Infusión",
      "observaciones": ""
    }
  ]
}
```

### GET /api/nutrition/planes/{planId}/alimentacion
Obtiene la alimentación semanal de un plan específico.

### PUT /api/nutrition/planes/{planId}
Actualiza un plan nutricional existente.

**Body:**
```json
{
  "fechaFin": "2024-03-15",
  "objetivo": "Mantenimiento",
  "caloriasDiarias": 2000.00,
  "observaciones": "Plan actualizado",
  "activo": true
}
```

### DELETE /api/nutrition/planes/{planId}
Elimina un plan nutricional (elimina automáticamente la alimentación semanal por CASCADE).

## Frontend

### Componente: PlanNutricionalComponent
Ubicación: `frontend/src/app/components/plan-nutricional/plan-nutricional.component.ts`

**Características:**
- Formulario para crear planes nutricionales
- Interfaz de pestañas para gestionar 2 semanas
- 7 días por semana (Lunes a Domingo)
- 6 tiempos de comida por día:
  - Desayuno
  - Snack Mañana
  - Almuerzo
  - Snack Tarde
  - Cena
  - Snack Noche
- Validación y mensajes de éxito/error
- Auto-limpieza del formulario después de guardar

### Integración en Dashboard
El componente se integra en el dashboard dentro de cada tarjeta de historia clínica:
- Botón "Plan Nutricional" en el header de la historia clínica
- Se despliega debajo de los datos de la historia
- Color verde para diferenciarlo del botón de editar
- Toggle para abrir/cerrar el formulario

## Uso

1. **Crear un plan:**
   - En el dashboard, expandir un paciente
   - Hacer clic en "Plan Nutricional" en la historia clínica
   - Completar la información del plan
   - Agregar alimentación para cada día de las 2 semanas
   - Hacer clic en "Guardar Plan"

2. **Editar un plan:**
   - Funcionalidad pendiente de implementar

3. **Ver planes existentes:**
   - Funcionalidad pendiente de implementar (lista de planes)

## Scripts SQL

### Crear tablas:
```bash
c:\NutriWeb\nutriweb-1\database\add_planes_alimentacion.sql
```

### Script simplificado (una línea):
```bash
c:\NutriWeb\nutriweb-1\database\planes_simple.sql
```

## Archivos Modificados

### Backend:
- `backend/Program.cs`: 5 nuevos endpoints agregados

### Frontend:
- `frontend/src/app/components/plan-nutricional/plan-nutricional.component.ts`: Nuevo componente
- `frontend/src/app/components/dashboard/dashboard.component.ts`: 
  - Import del nuevo componente
  - Signal para controlar visibilidad
  - Funciones toggle
  - Estilos CSS para botones

### Database:
- `database/add_planes_alimentacion.sql`: Script completo con triggers
- `database/planes_simple.sql`: Script simplificado

## Próximas Mejoras Sugeridas

1. **Listar planes existentes** en la vista del dashboard
2. **Editar planes** existentes
3. **Copiar plan** de una semana a otra
4. **Plantillas de alimentación** predefinidas
5. **Exportar plan** a PDF
6. **Historial de planes** por paciente
7. **Estadísticas** de planes activos en dashboard
8. **Notificaciones** al paciente sobre su plan
9. **Calcular macronutrientes** automáticamente
10. **Validación de calorías** por tiempo de comida

## Notas Técnicas

- Las tablas tienen restricciones de integridad referencial (CASCADE DELETE)
- Los índices mejoran el rendimiento de consultas frecuentes
- El trigger actualiza automáticamente `fecha_modificacion`
- La constraint UNIQUE en alimentacion_semanal previene duplicados (plan_id, semana, dia_semana)
- El componente usa Signals API de Angular 21
- Formulario reactivo con two-way binding [(ngModel)]
