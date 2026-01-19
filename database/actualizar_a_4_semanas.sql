-- Script para actualizar el sistema de planes de 2 a 4 semanas
-- Ejecutar este script en la base de datos nutriciondb

-- Modificar el constraint para permitir semanas 1, 2, 3, 4
ALTER TABLE alimentacion_semanal 
DROP CONSTRAINT IF EXISTS alimentacion_semanal_semana_check;

ALTER TABLE alimentacion_semanal 
ADD CONSTRAINT alimentacion_semanal_semana_check 
CHECK (semana IN (1, 2, 3, 4));

-- Verificar que la modificación fue exitosa
SELECT 
    table_name,
    constraint_name,
    check_clause
FROM information_schema.check_constraints
WHERE constraint_name = 'alimentacion_semanal_semana_check';
