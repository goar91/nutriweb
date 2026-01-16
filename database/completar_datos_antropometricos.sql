-- Actualizar datos antropométricos faltantes para Patricia Salazar
-- Agrega los campos que faltan y que se muestran como N/A en el dashboard

UPDATE datos_antropometricos da
SET 
    pantorrilla = 34.0,
    c_muslo = 52.0,
    peso_ajustado = 63.5,
    edad_metabolica = '52',
    actividad_fisica = 'Moderada (Yoga + caminata)',
    factor_actividad_fisica = 1.55,
    tiempos_comida = '5'
WHERE da.historia_id IN (
    SELECT hc.id 
    FROM historias_clinicas hc
    JOIN pacientes p ON hc.paciente_id = p.id
    WHERE p.nombre LIKE '%Patricia%'
);

-- Actualizar datos antropométricos faltantes para Carlos Méndez
UPDATE datos_antropometricos da
SET 
    pantorrilla = 38.0,
    c_muslo = 58.0,
    peso_ajustado = 98.0,
    edad_metabolica = '52',
    actividad_fisica = 'Sedentaria',
    factor_actividad_fisica = 1.2,
    tiempos_comida = '6'
WHERE da.historia_id IN (
    SELECT hc.id 
    FROM historias_clinicas hc
    JOIN pacientes p ON hc.paciente_id = p.id
    WHERE p.nombre LIKE '%Carlos Méndez%'
);

-- Actualizar datos antropométricos faltantes para Ana Rodríguez
UPDATE datos_antropometricos da
SET 
    pantorrilla = 32.0,
    c_muslo = 50.0,
    peso_ajustado = 62.0,
    edad_metabolica = '35',
    actividad_fisica = 'Alta (Crossfit)',
    factor_actividad_fisica = 1.75,
    tiempos_comida = '6'
WHERE da.historia_id IN (
    SELECT hc.id 
    FROM historias_clinicas hc
    JOIN pacientes p ON hc.paciente_id = p.id
    WHERE p.nombre LIKE '%Ana Rodr%'
);

-- Actualizar datos antropométricos faltantes para Luis Fernández
UPDATE datos_antropometricos da
SET 
    pantorrilla = 40.0,
    c_muslo = 62.0,
    peso_ajustado = 88.0,
    edad_metabolica = '26',
    actividad_fisica = 'Muy Alta (Gym diario)',
    factor_actividad_fisica = 1.9,
    tiempos_comida = '7'
WHERE da.historia_id IN (
    SELECT hc.id 
    FROM historias_clinicas hc
    JOIN pacientes p ON hc.paciente_id = p.id
    WHERE p.nombre LIKE '%Luis Fern%'
);

-- Actualizar datos antropométricos faltantes para María González
UPDATE datos_antropometricos da
SET 
    pantorrilla = 35.0,
    c_muslo = 54.0,
    peso_ajustado = 68.0,
    edad_metabolica = '61',
    actividad_fisica = 'Ligera (Caminata)',
    factor_actividad_fisica = 1.375,
    tiempos_comida = '4'
WHERE da.historia_id IN (
    SELECT hc.id 
    FROM historias_clinicas hc
    JOIN pacientes p ON hc.paciente_id = p.id
    WHERE p.nombre LIKE '%María González%'
);

-- Actualizar datos antropométricos faltantes para Roberto Castro
UPDATE datos_antropometricos da
SET 
    pantorrilla = 37.0,
    c_muslo = 56.0,
    peso_ajustado = 92.0,
    edad_metabolica = '56',
    actividad_fisica = 'Baja (Trabajo sedentario)',
    factor_actividad_fisica = 1.3,
    tiempos_comida = '3'
WHERE da.historia_id IN (
    SELECT hc.id 
    FROM historias_clinicas hc
    JOIN pacientes p ON hc.paciente_id = p.id
    WHERE p.nombre LIKE '%Roberto Castro%'
);

-- Actualizar datos antropométricos faltantes para Laura Jiménez
UPDATE datos_antropometricos da
SET 
    pantorrilla = 30.0,
    c_muslo = 46.0,
    peso_ajustado = 48.0,
    edad_metabolica = '42',
    actividad_fisica = 'Moderada',
    factor_actividad_fisica = 1.5,
    tiempos_comida = '3'
WHERE da.historia_id IN (
    SELECT hc.id 
    FROM historias_clinicas hc
    JOIN pacientes p ON hc.paciente_id = p.id
    WHERE p.nombre LIKE '%Laura Jim%'
);

-- Actualizar datos antropométricos faltantes para Diego Morales
UPDATE datos_antropometricos da
SET 
    pantorrilla = 36.0,
    c_muslo = 54.0,
    peso_ajustado = 80.0,
    edad_metabolica = '45',
    actividad_fisica = 'Moderada (Caminata)',
    factor_actividad_fisica = 1.55,
    tiempos_comida = '6'
WHERE da.historia_id IN (
    SELECT hc.id 
    FROM historias_clinicas hc
    JOIN pacientes p ON hc.paciente_id = p.id
    WHERE p.nombre LIKE '%Diego Morales%'
);

-- Actualizar datos antropométricos faltantes para Sofía Vargas
UPDATE datos_antropometricos da
SET 
    pantorrilla = 31.0,
    c_muslo = 48.0,
    peso_ajustado = 59.0,
    edad_metabolica = '24',
    actividad_fisica = 'Alta (Gimnasio)',
    factor_actividad_fisica = 1.7,
    tiempos_comida = '5'
WHERE da.historia_id IN (
    SELECT hc.id 
    FROM historias_clinicas hc
    JOIN pacientes p ON hc.paciente_id = p.id
    WHERE p.nombre LIKE '%Sofía Vargas%'
);

-- Actualizar datos antropométricos faltantes para Andrés Rivas
UPDATE datos_antropometricos da
SET 
    pantorrilla = 39.0,
    c_muslo = 60.0,
    peso_ajustado = 74.0,
    edad_metabolica = '34',
    actividad_fisica = 'Muy Alta (Deportista)',
    factor_actividad_fisica = 1.9,
    tiempos_comida = '6'
WHERE da.historia_id IN (
    SELECT hc.id 
    FROM historias_clinicas hc
    JOIN pacientes p ON hc.paciente_id = p.id
    WHERE p.nombre LIKE '%Andrés Rivas%'
);

-- Verificar los cambios
SELECT 
    p.nombre,
    da.edad,
    da.edad_metabolica,
    da.peso,
    da.peso_ajustado,
    da.pantorrilla as c_pantorrilla,
    da.c_muslo,
    da.actividad_fisica,
    da.factor_actividad_fisica,
    da.tiempos_comida
FROM datos_antropometricos da
JOIN historias_clinicas hc ON da.historia_id = hc.id
JOIN pacientes p ON hc.paciente_id = p.id
ORDER BY p.nombre;
