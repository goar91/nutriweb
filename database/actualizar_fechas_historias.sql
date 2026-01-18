-- Script para actualizar las fechas de las historias clínicas
-- Esto asegura que cada paciente tenga historias con fechas diferentes

-- Ver las fechas actuales
SELECT 
    p.nombre,
    hc.fecha_consulta,
    hc.id
FROM pacientes p
JOIN historias_clinicas hc ON p.id = hc.paciente_id
ORDER BY p.nombre, hc.fecha_consulta;

-- Actualizar fechas para crear variedad
-- Esto creará fechas diferentes para cada historia clínica

DO $$
DECLARE
    rec RECORD;
    contador INT := 0;
BEGIN
    FOR rec IN 
        SELECT id, paciente_id 
        FROM historias_clinicas 
        ORDER BY paciente_id, fecha_registro
    LOOP
        contador := contador + 1;
        -- Asignar fechas diferentes: cada historia tendrá una fecha con diferente intervalo
        UPDATE historias_clinicas 
        SET fecha_consulta = CURRENT_DATE - (contador * 7 || ' days')::INTERVAL
        WHERE id = rec.id;
    END LOOP;
END $$;

-- Verificar las nuevas fechas
SELECT 
    p.nombre,
    hc.fecha_consulta,
    hc.motivo_consulta
FROM pacientes p
JOIN historias_clinicas hc ON p.id = hc.paciente_id
ORDER BY p.nombre, hc.fecha_consulta DESC;
