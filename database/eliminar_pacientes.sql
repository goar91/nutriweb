-- Script para eliminar todos los pacientes y sus datos relacionados
-- ADVERTENCIA: Esto eliminará TODOS los datos de pacientes e historias clínicas

-- Eliminar en orden correcto respetando las claves foráneas

-- 1. Eliminar frecuencia de consumo
DELETE FROM frecuencia_consumo;

-- 2. Eliminar recordatorio 24h
DELETE FROM recordatorio_24h;

-- 3. Eliminar valores bioquímicos
DELETE FROM valores_bioquimicos;

-- 4. Eliminar hábitos
DELETE FROM habitos;

-- 5. Eliminar antecedentes
DELETE FROM antecedentes;

-- 6. Eliminar signos vitales
DELETE FROM signos_vitales;

-- 7. Eliminar datos antropométricos
DELETE FROM datos_antropometricos;

-- 8. Eliminar historias clínicas
DELETE FROM historias_clinicas;

-- 9. Finalmente, eliminar pacientes
DELETE FROM pacientes;

-- Verificar que las tablas están vacías
SELECT 'pacientes' as tabla, COUNT(*) as registros FROM pacientes
UNION ALL
SELECT 'historias_clinicas', COUNT(*) FROM historias_clinicas
UNION ALL
SELECT 'datos_antropometricos', COUNT(*) FROM datos_antropometricos
UNION ALL
SELECT 'signos_vitales', COUNT(*) FROM signos_vitales
UNION ALL
SELECT 'antecedentes', COUNT(*) FROM antecedentes
UNION ALL
SELECT 'habitos', COUNT(*) FROM habitos
UNION ALL
SELECT 'valores_bioquimicos', COUNT(*) FROM valores_bioquimicos
UNION ALL
SELECT 'recordatorio_24h', COUNT(*) FROM recordatorio_24h
UNION ALL
SELECT 'frecuencia_consumo', COUNT(*) FROM frecuencia_consumo;
