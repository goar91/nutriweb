-- Script para insertar 10 pacientes ficticios con datos completos
-- Base de datos: nutriciondb

-- Paciente 1: María González
DO $$
DECLARE
    v_paciente_id UUID := gen_random_uuid();
    v_historia_id UUID := gen_random_uuid();
BEGIN
    -- Insertar paciente
    INSERT INTO pacientes (id, numero_cedula, nombre, edad_cronologica, sexo, lugar_residencia, estado_civil, telefono, ocupacion, email)
    VALUES (v_paciente_id, '1234567890', 'María González', '28', 'Femenino', 'Quito, Ecuador', 'Soltera', '0987654321', 'Ingeniera', 'maria.gonzalez@email.com');
    
    -- Insertar historia clínica
    INSERT INTO historias_clinicas (id, paciente_id, fecha_consulta, motivo_consulta, diagnostico, notas_extras)
    VALUES (v_historia_id, v_paciente_id, CURRENT_DATE - INTERVAL '5 days', 'Control de peso y mejora de hábitos alimenticios', 'Sobrepeso grado I', 'Paciente motivada para cambio de estilo de vida');
    
    -- Insertar antecedentes
    INSERT INTO antecedentes (historia_id, apf, app, alergias, menarquia, p, g, c, a)
    VALUES (v_historia_id, 'Madre con diabetes tipo 2, padre hipertenso', 'Hipotiroidismo controlado', 'Ninguna conocida', '13 años', '0', '0', '0', '0');
    
    -- Insertar hábitos
    INSERT INTO habitos (historia_id, fuma, alcohol, cafe, hidratacion, gaseosas, actividad_fisica, alimentacion)
    VALUES (v_historia_id, 'No', 'Ocasional (fines de semana)', '2 tazas al día', '1.5 litros/día', 'Rara vez', 'Gimnasio 3 veces/semana', 'Dieta rica en carbohidratos, consumo moderado de frutas y verduras');
    
    -- Insertar signos vitales
    INSERT INTO signos_vitales (historia_id, pa, temperatura, fc, fr)
    VALUES (v_historia_id, '120/80', '36.5', '72', '16');
    
    -- Insertar datos antropométricos
    INSERT INTO datos_antropometricos (historia_id, edad, sexo, peso, masa_muscular, gc_porc, gc, talla, gv_porc, imc, cintura, cadera, c_brazo, kcal_basales, actividad_fisica)
    VALUES (v_historia_id, '28', 'Femenino', 72.5, 24.3, 32.5, 23.6, 165.0, 7.2, 26.6, 82.0, 102.0, 28.5, 1450, 'Moderada');
    
    -- Insertar valores bioquímicos
    INSERT INTO valores_bioquimicos (historia_id, glicemia, colesterol_total, trigliceridos, hdl, ldl)
    VALUES (v_historia_id, 95.0, 198.0, 145.0, 48.0, 121.0);
    
    -- Insertar recordatorio 24h
    INSERT INTO recordatorio_24h (historia_id, desayuno, snack1, almuerzo, snack2, cena)
    VALUES (v_historia_id, 'Café con leche, 2 tostadas con mantequilla', 'Manzana', 'Arroz, pollo al horno, ensalada verde', 'Yogur griego', 'Sopa de verduras, pescado a la plancha');
END $$;

-- Paciente 2: Carlos Méndez
DO $$
DECLARE
    v_paciente_id UUID := gen_random_uuid();
    v_historia_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO pacientes (id, numero_cedula, nombre, edad_cronologica, sexo, lugar_residencia, estado_civil, telefono, ocupacion, email)
    VALUES (v_paciente_id, '0987654321', 'Carlos Méndez', '35', 'Masculino', 'Guayaquil, Ecuador', 'Casado', '0991234567', 'Contador', 'carlos.mendez@email.com');
    
    INSERT INTO historias_clinicas (id, paciente_id, fecha_consulta, motivo_consulta, diagnostico, notas_extras)
    VALUES (v_historia_id, v_paciente_id, CURRENT_DATE - INTERVAL '3 days', 'Diabetes tipo 2 recién diagnosticada', 'Diabetes Mellitus tipo 2, Obesidad grado II', 'Requiere educación nutricional intensiva');
    
    INSERT INTO antecedentes (historia_id, apf, app, alergias)
    VALUES (v_historia_id, 'Abuelo con diabetes, tío con enfermedad cardiovascular', 'Diabetes tipo 2 diagnosticada hace 2 meses', 'Alergia a mariscos');
    
    INSERT INTO habitos (historia_id, fuma, alcohol, cafe, hidratacion, gaseosas, actividad_fisica, alimentacion)
    VALUES (v_historia_id, 'No', 'No consume', '3 tazas al día', '2 litros/día', 'Diariamente', 'Sedentario', 'Alto consumo de azúcares y grasas saturadas');
    
    INSERT INTO signos_vitales (historia_id, pa, temperatura, fc, fr)
    VALUES (v_historia_id, '140/90', '36.8', '80', '18');
    
    INSERT INTO datos_antropometricos (historia_id, edad, sexo, peso, masa_muscular, gc_porc, gc, talla, gv_porc, imc, cintura, cadera, c_brazo, kcal_basales)
    VALUES (v_historia_id, '35', 'Masculino', 95.2, 32.5, 35.8, 34.1, 175.0, 12.5, 31.1, 108.0, 105.0, 35.0, 1850);
    
    INSERT INTO valores_bioquimicos (historia_id, glicemia, colesterol_total, trigliceridos, hdl, ldl)
    VALUES (v_historia_id, 178.0, 245.0, 285.0, 38.0, 150.0);
    
    INSERT INTO recordatorio_24h (historia_id, desayuno, snack1, almuerzo, snack2, cena)
    VALUES (v_historia_id, 'Pan con huevo frito, jugo de naranja', 'Galletas', 'Arroz blanco, carne frita, papas fritas', 'Gaseosa y chips', 'Pizza');
END $$;

-- Paciente 3: Ana Rodríguez
DO $$
DECLARE
    v_paciente_id UUID := gen_random_uuid();
    v_historia_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO pacientes (id, numero_cedula, nombre, edad_cronologica, sexo, lugar_residencia, estado_civil, telefono, ocupacion, email)
    VALUES (v_paciente_id, '1122334455', 'Ana Rodríguez', '42', 'Femenino', 'Cuenca, Ecuador', 'Casada', '0998765432', 'Profesora', 'ana.rodriguez@email.com');
    
    INSERT INTO historias_clinicas (id, paciente_id, fecha_consulta, motivo_consulta, diagnostico, notas_extras)
    VALUES (v_historia_id, v_paciente_id, CURRENT_DATE - INTERVAL '7 days', 'Menopausia y aumento de peso', 'Sobrepeso, Síndrome metabólico', 'En tratamiento hormonal');
    
    INSERT INTO antecedentes (historia_id, apf, app, alergias, menarquia, p, g, c, a)
    VALUES (v_historia_id, 'Madre con osteoporosis', 'Hipertensión arterial controlada', 'Penicilina', '12 años', '2', '2', '0', '0');
    
    INSERT INTO habitos (historia_id, fuma, alcohol, cafe, hidratacion, gaseosas, actividad_fisica, alimentacion)
    VALUES (v_historia_id, 'No', 'No consume', '1 taza al día', '1.2 litros/día', 'No consume', 'Caminata 4 veces/semana', 'Dieta balanceada pero con porciones grandes');
    
    INSERT INTO signos_vitales (historia_id, pa, temperatura, fc, fr)
    VALUES (v_historia_id, '135/85', '36.6', '75', '16');
    
    INSERT INTO datos_antropometricos (historia_id, edad, sexo, peso, masa_muscular, gc_porc, gc, talla, gv_porc, imc, cintura, cadera, c_brazo, kcal_basales)
    VALUES (v_historia_id, '42', 'Femenino', 78.0, 22.8, 38.2, 29.8, 160.0, 9.8, 30.5, 95.0, 108.0, 32.0, 1380);
    
    INSERT INTO valores_bioquimicos (historia_id, glicemia, colesterol_total, trigliceridos, hdl, ldl)
    VALUES (v_historia_id, 108.0, 215.0, 168.0, 45.0, 136.0);
    
    INSERT INTO recordatorio_24h (historia_id, desayuno, snack1, almuerzo, snack2, cena)
    VALUES (v_historia_id, 'Avena con frutas', 'Almendras', 'Quinua, pescado, ensalada mixta', 'Té verde con galletas integrales', 'Sopa de lentejas, pollo al horno');
END $$;

-- Paciente 4: Luis Fernández
DO $$
DECLARE
    v_paciente_id UUID := gen_random_uuid();
    v_historia_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO pacientes (id, numero_cedula, nombre, edad_cronologica, sexo, lugar_residencia, estado_civil, telefono, ocupacion, email)
    VALUES (v_paciente_id, '2233445566', 'Luis Fernández', '22', 'Masculino', 'Ambato, Ecuador', 'Soltero', '0987123456', 'Estudiante', 'luis.fernandez@email.com');
    
    INSERT INTO historias_clinicas (id, paciente_id, fecha_consulta, motivo_consulta, diagnostico, notas_extras)
    VALUES (v_historia_id, v_paciente_id, CURRENT_DATE - INTERVAL '1 day', 'Aumento de masa muscular', 'Peso saludable, desea ganar masa muscular', 'Deportista amateur');
    
    INSERT INTO antecedentes (historia_id, apf, app, alergias)
    VALUES (v_historia_id, 'Sin antecedentes relevantes', 'Ninguno', 'Ninguna');
    
    INSERT INTO habitos (historia_id, fuma, alcohol, cafe, hidratacion, gaseosas, actividad_fisica, alimentacion)
    VALUES (v_historia_id, 'No', 'Ocasional', '1 taza al día', '3 litros/día', 'No consume', 'Gimnasio 6 veces/semana, levantamiento de pesas', 'Alta en proteínas');
    
    INSERT INTO signos_vitales (historia_id, pa, temperatura, fc, fr)
    VALUES (v_historia_id, '110/70', '36.4', '65', '14');
    
    INSERT INTO datos_antropometricos (historia_id, edad, sexo, peso, masa_muscular, gc_porc, gc, talla, gv_porc, imc, cintura, cadera, c_brazo, kcal_basales)
    VALUES (v_historia_id, '22', 'Masculino', 68.5, 32.8, 12.5, 8.6, 178.0, 3.2, 21.6, 75.0, 95.0, 32.0, 1750);
    
    INSERT INTO valores_bioquimicos (historia_id, glicemia, colesterol_total, trigliceridos, hdl, ldl)
    VALUES (v_historia_id, 88.0, 165.0, 95.0, 58.0, 88.0);
    
    INSERT INTO recordatorio_24h (historia_id, desayuno, snack1, almuerzo, snack2, cena)
    VALUES (v_historia_id, 'Batido de proteína, avena, huevos', 'Frutos secos', 'Arroz integral, pechuga de pollo, brócoli', 'Atún con galletas', 'Batata, salmón, ensalada');
END $$;

-- Paciente 5: Patricia Salazar
DO $$
DECLARE
    v_paciente_id UUID := gen_random_uuid();
    v_historia_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO pacientes (id, numero_cedula, nombre, edad_cronologica, sexo, lugar_residencia, estado_civil, telefono, ocupacion, email)
    VALUES (v_paciente_id, '3344556677', 'Patricia Salazar', '55', 'Femenino', 'Loja, Ecuador', 'Divorciada', '0995678901', 'Médica', 'patricia.salazar@email.com');
    
    INSERT INTO historias_clinicas (id, paciente_id, fecha_consulta, motivo_consulta, diagnostico, notas_extras)
    VALUES (v_historia_id, v_paciente_id, CURRENT_DATE - INTERVAL '10 days', 'Colesterol alto y prevención cardiovascular', 'Dislipidemia, Riesgo cardiovascular moderado', 'Antecedentes familiares de enfermedad cardiovascular');
    
    INSERT INTO antecedentes (historia_id, apf, app, alergias, menarquia, p, g, c, a)
    VALUES (v_historia_id, 'Padre falleció por infarto, hermana con hipercolesterolemia', 'Colesterol alto hace 5 años', 'Ninguna', '14 años', '3', '3', '1', '0');
    
    INSERT INTO habitos (historia_id, fuma, alcohol, cafe, hidratacion, gaseosas, actividad_fisica, alimentacion)
    VALUES (v_historia_id, 'No', 'Vino ocasionalmente', '2 tazas al día', '2 litros/día', 'No consume', 'Yoga 3 veces/semana, caminata diaria', 'Dieta mediterránea');
    
    INSERT INTO signos_vitales (historia_id, pa, temperatura, fc, fr)
    VALUES (v_historia_id, '128/82', '36.7', '70', '15');
    
    INSERT INTO datos_antropometricos (historia_id, edad, sexo, peso, masa_muscular, gc_porc, gc, talla, gv_porc, imc, cintura, cadera, c_brazo, kcal_basales)
    VALUES (v_historia_id, '55', 'Femenino', 65.0, 21.5, 30.8, 20.0, 162.0, 6.5, 24.8, 78.0, 98.0, 27.0, 1320);
    
    INSERT INTO valores_bioquimicos (historia_id, glicemia, colesterol_total, trigliceridos, hdl, ldl)
    VALUES (v_historia_id, 92.0, 238.0, 155.0, 52.0, 155.0);
    
    INSERT INTO recordatorio_24h (historia_id, desayuno, snack1, almuerzo, snack2, cena)
    VALUES (v_historia_id, 'Pan integral con aceite de oliva, té verde', 'Frutas variadas', 'Ensalada griega, pescado azul', 'Nueces', 'Verduras al vapor, pollo a la plancha');
END $$;

-- Paciente 6: Roberto Castro
DO $$
DECLARE
    v_paciente_id UUID := gen_random_uuid();
    v_historia_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO pacientes (id, numero_cedula, nombre, edad_cronologica, sexo, lugar_residencia, estado_civil, telefono, ocupacion, email)
    VALUES (v_paciente_id, '4455667788', 'Roberto Castro', '48', 'Masculino', 'Manta, Ecuador', 'Casado', '0992345678', 'Empresario', 'roberto.castro@email.com');
    
    INSERT INTO historias_clinicas (id, paciente_id, fecha_consulta, motivo_consulta, diagnostico, notas_extras)
    VALUES (v_historia_id, v_paciente_id, CURRENT_DATE - INTERVAL '2 days', 'Hipertensión y obesidad', 'Obesidad grado I, Hipertensión arterial', 'Alto estrés laboral');
    
    INSERT INTO antecedentes (historia_id, apf, app, alergias)
    VALUES (v_historia_id, 'Padre hipertenso, madre con obesidad', 'Hipertensión diagnosticada hace 3 años', 'Ninguna conocida');
    
    INSERT INTO habitos (historia_id, fuma, alcohol, cafe, hidratacion, gaseosas, actividad_fisica, alimentacion)
    VALUES (v_historia_id, 'Exfumador (dejó hace 2 años)', 'Moderado (3-4 copas/semana)', '4 tazas al día', '1.5 litros/día', 'Ocasionalmente', 'Sedentario por trabajo', 'Comidas rápidas frecuentes, cenas tardías');
    
    INSERT INTO signos_vitales (historia_id, pa, temperatura, fc, fr)
    VALUES (v_historia_id, '145/95', '36.9', '82', '17');
    
    INSERT INTO datos_antropometricos (historia_id, edad, sexo, peso, masa_muscular, gc_porc, gc, talla, gv_porc, imc, cintura, cadera, c_brazo, kcal_basales)
    VALUES (v_historia_id, '48', 'Masculino', 88.0, 30.2, 32.5, 28.6, 172.0, 11.2, 29.7, 102.0, 103.0, 34.0, 1780);
    
    INSERT INTO valores_bioquimicos (historia_id, glicemia, colesterol_total, trigliceridos, hdl, ldl)
    VALUES (v_historia_id, 115.0, 228.0, 198.0, 42.0, 146.0);
    
    INSERT INTO recordatorio_24h (historia_id, desayuno, snack1, almuerzo, snack2, cena)
    VALUES (v_historia_id, 'Café con leche, pan con queso', 'Café solo', 'Menú ejecutivo del restaurante', 'Café y pasteles', 'Comida rápida (hamburguesa o pizza)');
END $$;

-- Paciente 7: Laura Jiménez
DO $$
DECLARE
    v_paciente_id UUID := gen_random_uuid();
    v_historia_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO pacientes (id, numero_cedula, nombre, edad_cronologica, sexo, lugar_residencia, estado_civil, telefono, ocupacion, email)
    VALUES (v_paciente_id, '5566778899', 'Laura Jiménez', '31', 'Femenino', 'Ibarra, Ecuador', 'Soltera', '0993456789', 'Diseñadora Gráfica', 'laura.jimenez@email.com');
    
    INSERT INTO historias_clinicas (id, paciente_id, fecha_consulta, motivo_consulta, diagnostico, notas_extras)
    VALUES (v_historia_id, v_paciente_id, CURRENT_DATE - INTERVAL '4 days', 'Bajo peso y desnutrición', 'Bajo peso, IMC 17.5', 'Antecedentes de trastorno alimenticio');
    
    INSERT INTO antecedentes (historia_id, apf, app, alergias, menarquia, p, g, c, a)
    VALUES (v_historia_id, 'Sin antecedentes relevantes', 'Anemia, trastorno alimenticio en tratamiento psicológico', 'Lactosa', '15 años', '0', '0', '0', '0');
    
    INSERT INTO habitos (historia_id, fuma, alcohol, cafe, hidratacion, gaseosas, actividad_fisica, alimentacion)
    VALUES (v_historia_id, 'No', 'No consume', '2 tazas al día', '1 litro/día', 'No consume', 'Yoga 2 veces/semana', 'Restricción calórica, evita grupos alimenticios');
    
    INSERT INTO signos_vitales (historia_id, pa, temperatura, fc, fr)
    VALUES (v_historia_id, '100/65', '36.3', '68', '14');
    
    INSERT INTO datos_antropometricos (historia_id, edad, sexo, peso, masa_muscular, gc_porc, gc, talla, gv_porc, imc, cintura, cadera, c_brazo, kcal_basales)
    VALUES (v_historia_id, '31', 'Femenino', 48.5, 18.2, 18.5, 9.0, 167.0, 2.1, 17.4, 64.0, 88.0, 22.0, 1180);
    
    INSERT INTO valores_bioquimicos (historia_id, glicemia, colesterol_total, trigliceridos, hdl, ldl)
    VALUES (v_historia_id, 82.0, 158.0, 88.0, 62.0, 78.0);
    
    INSERT INTO recordatorio_24h (historia_id, desayuno, snack1, almuerzo, snack2, cena)
    VALUES (v_historia_id, 'Café negro, tostada', 'Manzana pequeña', 'Ensalada verde pequeña', 'Té de hierbas', 'Sopa de verduras');
END $$;

-- Paciente 8: Diego Morales
DO $$
DECLARE
    v_paciente_id UUID := gen_random_uuid();
    v_historia_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO pacientes (id, numero_cedula, nombre, edad_cronologica, sexo, lugar_residencia, estado_civil, telefono, ocupacion, email)
    VALUES (v_paciente_id, '6677889900', 'Diego Morales', '60', 'Masculino', 'Santo Domingo, Ecuador', 'Casado', '0994567890', 'Jubilado', 'diego.morales@email.com');
    
    INSERT INTO historias_clinicas (id, paciente_id, fecha_consulta, motivo_consulta, diagnostico, notas_extras)
    VALUES (v_historia_id, v_paciente_id, CURRENT_DATE - INTERVAL '6 days', 'Control de diabetes y peso', 'Diabetes tipo 2 de larga data, Obesidad', 'Complicaciones microvasculares iniciales');
    
    INSERT INTO antecedentes (historia_id, apf, app, alergias)
    VALUES (v_historia_id, 'Diabetes tipo 2 familiar (madre y hermanos)', 'Diabetes tipo 2 desde hace 15 años, retinopatía leve', 'Sulfonamidas');
    
    INSERT INTO habitos (historia_id, fuma, alcohol, cafe, hidratacion, gaseosas, actividad_fisica, alimentacion)
    VALUES (v_historia_id, 'Exfumador (30 años sin fumar)', 'No consume', '1 taza al día', '1.8 litros/día', 'No consume', 'Caminata ligera diaria', 'Dieta para diabéticos, a veces no la cumple');
    
    INSERT INTO signos_vitales (historia_id, pa, temperatura, fc, fr)
    VALUES (v_historia_id, '138/88', '36.6', '76', '16');
    
    INSERT INTO datos_antropometricos (historia_id, edad, sexo, peso, masa_muscular, gc_porc, gc, talla, gv_porc, imc, cintura, cadera, c_brazo, kcal_basales)
    VALUES (v_historia_id, '60', 'Masculino', 82.0, 28.5, 30.2, 24.8, 168.0, 10.5, 29.0, 98.0, 100.0, 31.0, 1650);
    
    INSERT INTO valores_bioquimicos (historia_id, glicemia, colesterol_total, trigliceridos, hdl, ldl)
    VALUES (v_historia_id, 165.0, 205.0, 210.0, 40.0, 123.0);
    
    INSERT INTO recordatorio_24h (historia_id, desayuno, snack1, almuerzo, snack2, cena)
    VALUES (v_historia_id, 'Avena sin azúcar, huevo cocido', 'Naranja', 'Arroz integral, pollo sin piel, verduras', 'Gelatina sin azúcar', 'Sopa de vegetales, pescado al horno');
END $$;

-- Paciente 9: Sofía Vargas
DO $$
DECLARE
    v_paciente_id UUID := gen_random_uuid();
    v_historia_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO pacientes (id, numero_cedula, nombre, edad_cronologica, sexo, lugar_residencia, estado_civil, telefono, ocupacion, email)
    VALUES (v_paciente_id, '7788990011', 'Sofía Vargas', '26', 'Femenino', 'Portoviejo, Ecuador', 'Soltera', '0996789012', 'Abogada', 'sofia.vargas@email.com');
    
    INSERT INTO historias_clinicas (id, paciente_id, fecha_consulta, motivo_consulta, diagnostico, notas_extras)
    VALUES (v_historia_id, v_paciente_id, CURRENT_DATE - INTERVAL '8 days', 'SOP y resistencia a la insulina', 'Síndrome de ovario poliquístico, Resistencia a la insulina', 'Irregularidades menstruales');
    
    INSERT INTO antecedentes (historia_id, apf, app, alergias, menarquia, p, g, c, a)
    VALUES (v_historia_id, 'Madre con SOP', 'SOP diagnosticado hace 3 años', 'Ninguna', '16 años (irregular)', '0', '0', '0', '0');
    
    INSERT INTO habitos (historia_id, fuma, alcohol, cafe, hidratacion, gaseosas, actividad_fisica, alimentacion)
    VALUES (v_historia_id, 'No', 'Ocasional (eventos sociales)', '3 tazas al día', '1.8 litros/día', 'Rara vez', 'Spinning 3 veces/semana', 'Dieta baja en carbohidratos refinados');
    
    INSERT INTO signos_vitales (historia_id, pa, temperatura, fc, fr)
    VALUES (v_historia_id, '118/76', '36.5', '74', '15');
    
    INSERT INTO datos_antropometricos (historia_id, edad, sexo, peso, masa_muscular, gc_porc, gc, talla, gv_porc, imc, cintura, cadera, c_brazo, kcal_basales)
    VALUES (v_historia_id, '26', 'Femenino', 69.0, 23.5, 31.2, 21.5, 163.0, 8.0, 26.0, 84.0, 100.0, 29.0, 1420);
    
    INSERT INTO valores_bioquimicos (historia_id, glicemia, colesterol_total, trigliceridos, hdl, ldl)
    VALUES (v_historia_id, 102.0, 188.0, 152.0, 46.0, 112.0);
    
    INSERT INTO recordatorio_24h (historia_id, desayuno, snack1, almuerzo, snack2, cena)
    VALUES (v_historia_id, 'Batido verde (espinaca, manzana, chía)', 'Almendras', 'Ensalada con quinua y pollo', 'Yogur griego natural', 'Verduras salteadas, pescado');
END $$;

-- Paciente 10: Fernando Ríos
DO $$
DECLARE
    v_paciente_id UUID := gen_random_uuid();
    v_historia_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO pacientes (id, numero_cedula, nombre, edad_cronologica, sexo, lugar_residencia, estado_civil, telefono, ocupacion, email)
    VALUES (v_paciente_id, '8899001122', 'Fernando Ríos', '52', 'Masculino', 'Riobamba, Ecuador', 'Viudo', '0997890123', 'Arquitecto', 'fernando.rios@email.com');
    
    INSERT INTO historias_clinicas (id, paciente_id, fecha_consulta, motivo_consulta, diagnostico, notas_extras)
    VALUES (v_historia_id, v_paciente_id, CURRENT_DATE - INTERVAL '1 day', 'Prevención cardiovascular y pérdida de peso', 'Sobrepeso, Pre-hipertensión', 'Motivado por evento cardiovascular de un amigo');
    
    INSERT INTO antecedentes (historia_id, apf, app, alergias)
    VALUES (v_historia_id, 'Padre con enfermedad coronaria', 'Ninguno significativo', 'Ninguna');
    
    INSERT INTO habitos (historia_id, fuma, alcohol, cafe, hidratacion, gaseosas, actividad_fisica, alimentacion)
    VALUES (v_historia_id, 'No', 'Moderado (cerveza fines de semana)', '2 tazas al día', '2.2 litros/día', 'Ocasionalmente', 'Ciclismo fines de semana', 'Dieta variada, porciones grandes');
    
    INSERT INTO signos_vitales (historia_id, pa, temperatura, fc, fr)
    VALUES (v_historia_id, '132/84', '36.7', '72', '16');
    
    INSERT INTO datos_antropometricos (historia_id, edad, sexo, peso, masa_muscular, gc_porc, gc, talla, gv_porc, imc, cintura, cadera, c_brazo, kcal_basales)
    VALUES (v_historia_id, '52', 'Masculino', 83.5, 31.0, 28.5, 23.8, 176.0, 9.2, 26.9, 94.0, 102.0, 32.5, 1720);
    
    INSERT INTO valores_bioquimicos (historia_id, glicemia, colesterol_total, trigliceridos, hdl, ldl)
    VALUES (v_historia_id, 98.0, 195.0, 142.0, 48.0, 118.0);
    
    INSERT INTO recordatorio_24h (historia_id, desayuno, snack1, almuerzo, snack2, cena)
    VALUES (v_historia_id, 'Café con leche, pan integral con aguacate', 'Plátano', 'Arroz, carne asada, ensalada', 'Frutos secos', 'Sopa de pollo, tortilla de verduras');
END $$;

-- Mensaje de confirmación
DO $$
BEGIN
    RAISE NOTICE '✓ Se insertaron exitosamente 10 pacientes ficticios con todos sus datos completos';
END $$;
