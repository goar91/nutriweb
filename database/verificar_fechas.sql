-- Verificar las fechas de las historias clínicas
SELECT 
    p.nombre,
    p.numero_cedula,
    hc.fecha_consulta,
    hc.motivo_consulta,
    hc.id as historia_id
FROM pacientes p
JOIN historias_clinicas hc ON p.id = hc.paciente_id
ORDER BY p.nombre, hc.fecha_consulta DESC;
