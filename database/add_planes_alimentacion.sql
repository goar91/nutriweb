-- Tabla para planes nutricionales
CREATE TABLE IF NOT EXISTS planes_nutricionales (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    historia_id UUID NOT NULL REFERENCES historias_clinicas(id) ON DELETE CASCADE,
    fecha_inicio DATE NOT NULL,
    fecha_fin DATE,
    objetivo TEXT,
    calorias_diarias DECIMAL(10,2),
    observaciones TEXT,
    activo BOOLEAN DEFAULT true,
    fecha_creacion TIMESTAMP DEFAULT NOW(),
    fecha_modificacion TIMESTAMP DEFAULT NOW()
);

-- Tabla para alimentación por día de la semana
CREATE TABLE IF NOT EXISTS alimentacion_semanal (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    plan_id UUID NOT NULL REFERENCES planes_nutricionales(id) ON DELETE CASCADE,
    semana INT NOT NULL CHECK (semana IN (1, 2)), -- Semana 1 o 2
    dia_semana INT NOT NULL CHECK (dia_semana BETWEEN 1 AND 7), -- 1=Lunes, 7=Domingo
    desayuno TEXT,
    snack_manana TEXT,
    almuerzo TEXT,
    snack_tarde TEXT,
    cena TEXT,
    snack_noche TEXT,
    observaciones TEXT,
    fecha_creacion TIMESTAMP DEFAULT NOW(),
    UNIQUE(plan_id, semana, dia_semana)
);

-- Índices para mejorar el rendimiento
CREATE INDEX IF NOT EXISTS idx_planes_historia ON planes_nutricionales(historia_id);
CREATE INDEX IF NOT EXISTS idx_planes_activo ON planes_nutricionales(activo);
CREATE INDEX IF NOT EXISTS idx_alimentacion_plan ON alimentacion_semanal(plan_id);
CREATE INDEX IF NOT EXISTS idx_alimentacion_semana_dia ON alimentacion_semanal(semana, dia_semana);

-- Trigger para actualizar fecha_modificacion
CREATE OR REPLACE FUNCTION actualizar_fecha_modificacion_plan()
RETURNS TRIGGER AS $$
BEGIN
    NEW.fecha_modificacion = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_actualizar_plan
    BEFORE UPDATE ON planes_nutricionales
    FOR EACH ROW
    EXECUTE FUNCTION actualizar_fecha_modificacion_plan();

-- Verificar que las tablas se crearon correctamente
SELECT 'planes_nutricionales' as tabla, COUNT(*) as registros FROM planes_nutricionales
UNION ALL
SELECT 'alimentacion_semanal', COUNT(*) FROM alimentacion_semanal;
