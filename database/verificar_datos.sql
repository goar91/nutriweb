-- Buscar Patricia Salazar y sus datos antropométricos
SELECT 
    p.nombre,
    p.numero_cedula,
    hc.fecha_consulta,
    da.peso,
    da.talla,
    da.imc,
    da.cintura,
    da.cadera,
    da.c_brazo,
    da.c_muslo,
    da.pantorrilla,
    da.masa_muscular,
    da.gc_porc,
    da.gc,
    da.gv_porc,
    da.edad,
    da.edad_metabolica,
    da.kcal_basales,
    da.peso_ajustado,
    da.actividad_fisica,
    da.factor_actividad_fisica,
    da.tiempos_comida
FROM pacientes p
LEFT JOIN historias_clinicas hc ON p.id = hc.paciente_id
LEFT JOIN datos_antropometricos da ON hc.id = da.historia_id
WHERE p.nombre LIKE '%Patricia%'
ORDER BY hc.fecha_consulta DESC
LIMIT 1;
