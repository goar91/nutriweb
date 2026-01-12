-- ============================================
-- Extensión del esquema para Reportes
-- ============================================

-- Tabla para almacenar reportes generados
CREATE TABLE IF NOT EXISTS reportes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id UUID REFERENCES usuarios(id),
    tipo_reporte VARCHAR(50) NOT NULL, -- 'pacientes', 'historias', 'estadisticas'
    titulo VARCHAR(200),
    descripcion TEXT,
    parametros JSONB, -- Parámetros usados para generar el reporte
    datos JSONB, -- Datos del reporte en formato JSON
    fecha_generacion TIMESTAMPTZ DEFAULT NOW(),
    fecha_desde DATE,
    fecha_hasta DATE
);

-- Índices para reportes
CREATE INDEX IF NOT EXISTS idx_reportes_tipo ON reportes(tipo_reporte);
CREATE INDEX IF NOT EXISTS idx_reportes_fecha ON reportes(fecha_generacion);
CREATE INDEX IF NOT EXISTS idx_reportes_usuario ON reportes(usuario_id);

-- Vista: Resumen de pacientes activos
CREATE OR REPLACE VIEW vista_resumen_pacientes AS
SELECT 
    p.id,
    p.numero_cedula,
    p.nombre,
    p.edad_cronologica,
    p.sexo,
    p.telefono,
    p.email,
    p.fecha_creacion,
    p.fecha_actualizacion,
    COUNT(h.id) as total_historias,
    MAX(h.fecha_consulta) as ultima_consulta,
    MIN(h.fecha_consulta) as primera_consulta
FROM pacientes p
LEFT JOIN historias_clinicas h ON h.paciente_id = p.id
GROUP BY p.id, p.numero_cedula, p.nombre, p.edad_cronologica, p.sexo, p.telefono, p.email, p.fecha_creacion, p.fecha_actualizacion
ORDER BY p.fecha_actualizacion DESC;

-- Vista: Estadísticas generales
CREATE OR REPLACE VIEW vista_estadisticas_generales AS
SELECT 
    (SELECT COUNT(*) FROM pacientes) as total_pacientes,
    (SELECT COUNT(*) FROM historias_clinicas) as total_historias,
    (SELECT COUNT(*) FROM pacientes WHERE DATE_PART('day', NOW() - fecha_creacion) <= 30) as pacientes_mes,
    (SELECT COUNT(*) FROM historias_clinicas WHERE DATE_PART('day', NOW() - fecha_registro) <= 30) as historias_mes,
    (SELECT COUNT(*) FROM pacientes WHERE sexo = 'F') as pacientes_femenino,
    (SELECT COUNT(*) FROM pacientes WHERE sexo = 'M') as pacientes_masculino,
    (SELECT AVG(CAST(edad_cronologica AS INTEGER)) FROM pacientes WHERE edad_cronologica ~ '^[0-9]+$') as edad_promedio;

-- Vista: Historias recientes con datos completos
CREATE OR REPLACE VIEW vista_historias_recientes AS
SELECT 
    h.id as historia_id,
    h.fecha_consulta,
    h.motivo_consulta,
    h.diagnostico,
    h.fecha_registro,
    p.id as paciente_id,
    p.numero_cedula,
    p.nombre,
    p.edad_cronologica,
    p.sexo,
    p.telefono,
    p.email,
    a.imc as imc_actual,
    a.peso as peso_actual,
    a.talla as talla_actual
FROM historias_clinicas h
INNER JOIN pacientes p ON h.paciente_id = p.id
LEFT JOIN datos_antropometricos a ON a.historia_id = h.id
ORDER BY h.fecha_registro DESC
LIMIT 100;

-- Comentarios
COMMENT ON TABLE reportes IS 'Almacena reportes generados por los usuarios';
COMMENT ON VIEW vista_resumen_pacientes IS 'Vista con resumen de todos los pacientes y sus historias';
COMMENT ON VIEW vista_estadisticas_generales IS 'Vista con estadísticas generales del sistema';
COMMENT ON VIEW vista_historias_recientes IS 'Vista con las 100 historias clínicas más recientes';

SELECT 'Esquema de reportes creado exitosamente!' as mensaje;
