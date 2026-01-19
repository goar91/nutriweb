# Limites de datos (PostgreSQL)

## Resumen general

- VARCHAR(n): hasta n caracteres.
- TEXT / JSONB: hasta ~1 GB por valor.
- INTEGER: -2,147,483,648 a 2,147,483,647.
- DECIMAL(p,s): p digitos totales, s decimales. Ej: DECIMAL(6,2) => max 9999.99.

## Tabla: pacientes

- numero_cedula: VARCHAR(20)
- nombre: VARCHAR(200)
- edad_cronologica: VARCHAR(10)
- sexo: VARCHAR(10)
- lugar_residencia: VARCHAR(200)
- estado_civil: VARCHAR(50)
- telefono: VARCHAR(20)
- ocupacion: VARCHAR(100)
- email: VARCHAR(100)

## Tabla: historias_clinicas

- motivo_consulta: TEXT
- diagnostico: TEXT
- notas_extras: TEXT
- payload: JSONB

## Tabla: antecedentes

- apf/app/apq/ago/alergias: TEXT
- menarquia: VARCHAR(50)
- p/g/c/a: VARCHAR(10)

## Tabla: habitos

- fuma/alcohol/cafe/hidratacion/gaseosas/te/edulcorantes: VARCHAR(100)
- actividad_fisica: VARCHAR(200)
- alimentacion: TEXT

## Tabla: signos_vitales

- pa/temperatura/fc/fr: VARCHAR(20)

## Tabla: datos_antropometricos (numericos)

- peso, masa_muscular, gc, peso_ajustado: DECIMAL(6,2) (max 9999.99)
- gc_porc, gv_porc, imc, cintura, cadera, pantorrilla, c_brazo, c_muslo, talla: DECIMAL(5,2) (max 999.99)
- factor_actividad_fisica: DECIMAL(4,2) (max 99.99)
- kcal_basales: INTEGER
- edad/edad_metabolica/sexo: VARCHAR(10)
- actividad_fisica/tiempos_comida: VARCHAR(100)

## Tabla: valores_bioquimicos

- glicemia/colesterol_total/trigliceridos/hdl/ldl/tgo/tgp/urea/creatinina: DECIMAL(6,2)

## Tabla: recordatorio_24h

- desayuno/snack1/almuerzo/snack2/cena/extras: TEXT

## Tabla: frecuencia_consumo

- categoria: VARCHAR(100)
- alimento: VARCHAR(100)
- frecuencia: VARCHAR(50)

## Tabla: usuarios/sesiones/logs

- usuarios.username: VARCHAR(50)
- usuarios.email: VARCHAR(100)
- usuarios.nombre: VARCHAR(200)
- usuarios.password_hash: VARCHAR(255)
- sesiones.token: VARCHAR(500)
- logs_acceso.accion: VARCHAR(100)
- logs_acceso.username: VARCHAR(100)
- logs_acceso.ip_address: VARCHAR(50)

## Tabla: planes_nutricionales / alimentacion_semanal

- planes_nutricionales.calorias_diarias: DECIMAL(10,2) (max 99999999.99)
- planes_nutricionales.objetivo/observaciones: TEXT
- alimentacion_semanal.*: TEXT
