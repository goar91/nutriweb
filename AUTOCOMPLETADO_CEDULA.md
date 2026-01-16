# Autocompletado de Datos Personales por Cédula

## Descripción

Se ha implementado una funcionalidad de autocompletado en el formulario de creación de historias clínicas. Cuando el usuario ingresa un número de cédula que ya existe en el sistema, los datos personales del paciente se cargan automáticamente.

## Funcionamiento

### 1. Detección de Cédula
- El sistema escucha los cambios en el campo "Número de cédula"
- Espera 500ms después de que el usuario deja de escribir (debounce)
- Solo busca si la cédula tiene al menos 6 caracteres
- No se activa en modo edición (solo al crear nuevas historias)

### 2. Búsqueda Automática
- Cuando se detecta una cédula válida, se realiza una búsqueda en el backend
- Si encuentra un paciente existente con esa cédula, carga automáticamente:
  - Nombre completo
  - Edad cronológica
  - Sexo
  - Lugar de residencia
  - Estado civil
  - Teléfono
  - Ocupación
  - Email

### 3. Campos NO Autocompletados
- **Número de cédula**: Se mantiene tal como fue ingresado
- **Fecha de consulta**: Debe ser ingresada manualmente para cada historia

### 4. Retroalimentación al Usuario
- Muestra un mensaje temporal: "✓ Datos del paciente cargados automáticamente"
- El mensaje se oculta automáticamente después de 3 segundos
- Si la cédula no existe, no se muestra ningún mensaje (paciente nuevo)

## Implementación Técnica

### Backend
**Endpoint:** `GET /api/nutrition/pacientes/buscar/cedula/{cedula}`

Busca un paciente por su número de cédula y devuelve todos sus datos personales.

### Frontend

#### Servicio (nutrition.service.ts)
```typescript
buscarPacientePorCedula(cedula: string)
```

#### Componente (app.ts)
- `setupCedulaAutocomplete()`: Configura el listener del campo cédula
- `buscarYAutocompletarPorCedula(cedula)`: Realiza la búsqueda y autocompleta

## Ventajas

1. **Evita duplicados**: Reutiliza datos existentes del paciente
2. **Ahorra tiempo**: El usuario no necesita reingresar información ya existente
3. **Reduce errores**: Mantiene la consistencia de los datos del paciente
4. **No intrusivo**: Si la cédula no existe, simplemente permite crear un nuevo paciente

## Casos de Uso

### Caso 1: Paciente Nuevo
1. Usuario ingresa cédula "1234567890"
2. Sistema busca pero no encuentra coincidencias
3. Usuario completa manualmente todos los campos
4. Se crea un nuevo paciente

### Caso 2: Paciente Existente - Nueva Historia
1. Usuario ingresa cédula "0987654321" (existente)
2. Sistema encuentra al paciente "María González"
3. Campos se autocompletar automáticamente
4. Usuario solo necesita ingresar fecha de consulta
5. Usuario puede modificar cualquier dato si es necesario
6. Se crea una nueva historia clínica para el paciente existente

### Caso 3: Editando Historia Existente
1. Usuario está editando una historia clínica
2. El autocompletado NO se activa
3. Los cambios en la cédula no disparan búsquedas automáticas

## Consideraciones

- La búsqueda se realiza con un delay de 500ms para evitar múltiples llamadas mientras el usuario escribe
- Requiere autenticación (token válido)
- Solo funciona al crear nuevas historias, no al editar
- Los datos autocompletados pueden ser modificados por el usuario antes de guardar
