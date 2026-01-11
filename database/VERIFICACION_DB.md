# ✅ Verificación de Base de Datos NutriWeb

## Estado: **BASE DE DATOS CREADA EXITOSAMENTE**

---

## 📊 Resumen de la Base de Datos

### Información General
- **Nombre**: `nutriciondb`
- **Sistema**: PostgreSQL 18
- **Total de Tablas**: 11 tablas
- **Total de Vistas**: 1 vista
- **Total de Índices**: 27 índices
- **Triggers**: 2 triggers automáticos

---

## 📋 Tablas Creadas (11 tablas)

### ✅ 1. **pacientes**
Almacena la información personal de los pacientes.

**Columnas principales:**
- `id` (UUID, Primary Key)
- `numero_cedula` (VARCHAR, UNIQUE)
- `nombre`, `edad_cronologica`, `sexo`
- `telefono`, `email`, `ocupacion`
- `lugar_residencia`, `estado_civil`
- `fecha_creacion`, `fecha_actualizacion`

**Índices:**
- Primary key en `id`
- Índice único en `numero_cedula`
- Índice en `email`

**Trigger:**
- `trigger_actualizar_paciente` - Actualiza automáticamente la fecha de modificación

**Datos de prueba:** ✅ 2 pacientes insertados
- Juan Pérez (Cédula: 1234567890)
- María González (Cédula: 0987654321)

---

### ✅ 2. **historias_clinicas**
Almacena las historias clínicas nutricionales.

**Columnas principales:**
- `id` (UUID, Primary Key)
- `paciente_id` (UUID, Foreign Key → pacientes)
- `fecha_consulta`, `motivo_consulta`
- `diagnostico`, `notas_extras`
- `payload` (JSONB) - Datos adicionales flexibles
- `fecha_registro`, `fecha_actualizacion`

**Relaciones:**
- Foreign Key: `paciente_id` → `pacientes(id)` ON DELETE CASCADE

**Trigger:**
- `trigger_actualizar_historia` - Actualiza automáticamente la fecha de modificación

---

### ✅ 3. **antecedentes**
Almacena antecedentes médicos del paciente.

**Columnas principales:**
- `id` (UUID, Primary Key)
- `historia_id` (UUID, Foreign Key → historias_clinicas)
- `apf` - Antecedentes Patológicos Familiares
- `app` - Antecedentes Patológicos Personales
- `apq` - Antecedentes Patológicos Quirúrgicos
- `ago` - Antecedentes Gineco-Obstétricos
- `menarquia`, `p`, `g`, `c`, `a`
- `alergias`

---

### ✅ 4. **habitos**
Registra los hábitos de vida del paciente.

**Columnas:**
- `fuma`, `alcohol`, `cafe`
- `hidratacion`, `gaseosas`
- `actividad_fisica`, `te`
- `edulcorantes`, `alimentacion`

---

### ✅ 5. **signos_vitales**
Registra signos vitales de la consulta.

**Columnas:**
- `pa` - Presión Arterial
- `temperatura`
- `fc` - Frecuencia Cardíaca
- `fr` - Frecuencia Respiratoria

---

### ✅ 6. **datos_antropometricos**
Almacena medidas antropométricas.

**Columnas principales:**
- `edad`, `edad_metabolica`, `sexo`
- `peso`, `masa_muscular`
- `gc_porc`, `gc` - Grasa Corporal
- `talla`, `gv_porc` - Grasa Visceral
- `imc`, `kcal_basales`
- `cintura`, `cadera`, `pantorrilla`
- `c_brazo`, `c_muslo`
- `peso_ajustado`, `factor_actividad_fisica`

---

### ✅ 7. **valores_bioquimicos**
Almacena resultados de análisis de laboratorio.

**Columnas:**
- `glicemia`, `colesterol_total`
- `trigliceridos`, `hdl`, `ldl`
- `tgo`, `tgp`
- `urea`, `creatinina`

---

### ✅ 8. **recordatorio_24h**
Recordatorio de alimentación de 24 horas.

**Columnas:**
- `desayuno`, `snack1`
- `almuerzo`, `snack2`
- `cena`, `extras`

---

### ✅ 9. **frecuencia_consumo**
Frecuencia de consumo de diferentes alimentos.

**Columnas:**
- `categoria` - Categoría del alimento
- `alimento` - Nombre del alimento
- `frecuencia` - Frecuencia de consumo

---

### ✅ 10. **usuarios**
Usuarios del sistema (nutricionistas).

**Columnas:**
- `id` (UUID, Primary Key)
- `auth0_id` (VARCHAR, UNIQUE)
- `email` (VARCHAR, UNIQUE, NOT NULL)
- `nombre`, `rol`
- `activo` (BOOLEAN, default: true)
- `fecha_creacion`, `fecha_ultimo_acceso`

**Índice especial:**
- `idx_usuarios_auth0` para búsquedas rápidas por Auth0 ID

---

### ✅ 11. **auditoria**
Registro de auditoría de cambios en el sistema.

**Columnas:**
- `tabla` - Nombre de la tabla afectada
- `registro_id` - ID del registro modificado
- `usuario_id` - Usuario que realizó el cambio
- `accion` - INSERT, UPDATE, DELETE
- `datos_anteriores` (JSONB)
- `datos_nuevos` (JSONB)
- `fecha_accion`

---

## 🔍 Vista Creada

### ✅ **vista_historias_completas**
Combina datos de pacientes con sus historias clínicas.

**Columnas devueltas:**
- `historia_id`, `fecha_consulta`
- `motivo_consulta`, `diagnostico`
- `paciente_id`, `numero_cedula`
- `nombre`, `edad_cronologica`, `sexo`
- `telefono`, `email`
- `fecha_creacion_historia`

**Uso:**
```sql
SELECT * FROM vista_historias_completas 
WHERE nombre LIKE '%Juan%';
```

---

## 📑 Índices Creados (27 índices)

### Primary Keys (11)
Uno por cada tabla principal

### Índices de Rendimiento (13)
- `idx_pacientes_cedula` - Búsqueda por cédula
- `idx_pacientes_email` - Búsqueda por email
- `idx_historias_paciente` - Historias de un paciente
- `idx_historias_fecha` - Búsqueda por fecha
- `idx_antecedentes_historia` - Antecedentes de una historia
- `idx_habitos_historia` - Hábitos de una historia
- `idx_signos_historia` - Signos vitales de una historia
- `idx_antropometricos_historia` - Datos antropométricos
- `idx_bioquimicos_historia` - Valores bioquímicos
- `idx_recordatorio_historia` - Recordatorio 24h
- `idx_frecuencia_historia` - Frecuencia de consumo
- `idx_usuarios_auth0` - Búsqueda por Auth0 ID
- `idx_auditoria_fecha` - Auditoría por fecha

### Unique Constraints (3)
- `pacientes_numero_cedula_key`
- `usuarios_auth0_id_key`
- `usuarios_email_key`

---

## ⚙️ Funciones y Triggers

### Función: `actualizar_fecha_modificacion()`
Actualiza automáticamente el campo `fecha_actualizacion` cuando se modifica un registro.

### Triggers Activos:
1. **trigger_actualizar_paciente** → Tabla `pacientes`
2. **trigger_actualizar_historia** → Tabla `historias_clinicas`

---

## 🔗 Relaciones Entre Tablas

```
pacientes (1)
    ↓
historias_clinicas (N)
    ↓
    ├── antecedentes (1)
    ├── habitos (1)
    ├── signos_vitales (1)
    ├── datos_antropometricos (1)
    ├── valores_bioquimicos (1)
    ├── recordatorio_24h (1)
    └── frecuencia_consumo (N)

usuarios (1)
    ↓
auditoria (N)
```

**Integridad Referencial:**
- Todas las relaciones usan `ON DELETE CASCADE`
- Cuando se elimina un paciente, se eliminan todas sus historias
- Cuando se elimina una historia, se eliminan todos sus datos relacionados

---

## 📝 Datos de Ejemplo

### ✅ Pacientes Insertados (2)

1. **Juan Pérez**
   - Cédula: 1234567890
   - Edad: 35 años
   - Sexo: M
   - Teléfono: 0991234567
   - Email: juan.perez@example.com

2. **María González**
   - Cédula: 0987654321
   - Edad: 28 años
   - Sexo: F
   - Teléfono: 0987654321
   - Email: maria.gonzalez@example.com

---

## 🎯 Consultas de Ejemplo

### 1. Listar todos los pacientes
```sql
SELECT numero_cedula, nombre, email, telefono 
FROM pacientes 
ORDER BY nombre;
```

### 2. Crear una nueva historia clínica
```sql
INSERT INTO historias_clinicas (paciente_id, fecha_consulta, motivo_consulta, diagnostico, payload)
VALUES (
    (SELECT id FROM pacientes WHERE numero_cedula = '1234567890'),
    CURRENT_DATE,
    'Control nutricional',
    'Sobrepeso grado I',
    '{"source": "web_app"}'::jsonb
);
```

### 3. Ver historias completas
```sql
SELECT * FROM vista_historias_completas;
```

### 4. Buscar paciente y sus historias
```sql
SELECT 
    p.nombre,
    p.email,
    COUNT(h.id) as total_historias
FROM pacientes p
LEFT JOIN historias_clinicas h ON p.id = h.paciente_id
GROUP BY p.id, p.nombre, p.email;
```

---

## ✅ Checklist de Verificación

- [x] Base de datos `nutriciondb` creada
- [x] 11 tablas creadas correctamente
- [x] 27 índices creados para rendimiento
- [x] 1 vista creada (vista_historias_completas)
- [x] 2 triggers funcionando
- [x] 1 función personalizada creada
- [x] Relaciones y Foreign Keys configuradas
- [x] Datos de ejemplo insertados
- [x] Comentarios en tablas agregados
- [x] Integridad referencial (CASCADE) configurada

---

## 🚀 Próximos Pasos

1. **Conectar el Backend**: El backend ya está configurado para conectarse a `nutriciondb`
2. **Probar la conexión**: Ejecutar `dotnet run` en el backend
3. **Insertar datos reales**: Usar el formulario web para crear historias
4. **Crear índices adicionales**: Si se necesita optimizar consultas específicas
5. **Backups**: Configurar backups automáticos de la base de datos

---

## 📌 Comandos Útiles

### Conectarse a la base de datos
```bash
psql -U postgres -d nutriciondb
```

### Ver todas las tablas
```sql
\dt
```

### Ver estructura de una tabla
```sql
\d+ pacientes
```

### Ver todas las vistas
```sql
\dv
```

### Exportar datos
```bash
pg_dump -U postgres nutriciondb > backup.sql
```

### Restaurar datos
```bash
psql -U postgres -d nutriciondb < backup.sql
```

---

## ✨ Resumen

La base de datos **nutriciondb** ha sido creada exitosamente con:
- ✅ Estructura completa y optimizada
- ✅ Índices para búsquedas rápidas
- ✅ Triggers para automatización
- ✅ Vistas para consultas complejas
- ✅ Datos de prueba para testing
- ✅ Integridad referencial garantizada

**Estado final: LISTO PARA PRODUCCIÓN** 🎉
