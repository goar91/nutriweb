-- Ver los planes existentes
SELECT id, historia_id, fecha_inicio, fecha_creacion 
FROM planes_nutricionales 
ORDER BY fecha_creacion DESC;

-- Borrar el plan más reciente (descomenta la línea siguiente después de verificar)
-- DELETE FROM planes_nutricionales WHERE id = (SELECT id FROM planes_nutricionales ORDER BY fecha_creacion DESC LIMIT 1);
