-- Script de creación de base de datos NutriWeb
-- PostgreSQL Database Schema

-- Crear la base de datos (ejecutar como superusuario postgres)
-- CREATE DATABASE nutriciondb;

-- Conectar a la base de datos
-- \c nutriciondb;

-- ============================================
-- Tabla: Pacientes
-- ============================================
CREATE TABLE IF NOT EXISTS pacientes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    numero_cedula VARCHAR(20) UNIQUE,
    nombre VARCHAR(200) NOT NULL,
    edad_cronologica VARCHAR(10),
    sexo VARCHAR(10),
    lugar_residencia VARCHAR(200),
    estado_civil VARCHAR(50),
    telefono VARCHAR(20),
    ocupacion VARCHAR(100),
    email VARCHAR(100),
    fecha_creacion TIMESTAMPTZ DEFAULT NOW(),
    fecha_actualizacion TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- Tabla: Historias Clínicas
-- ============================================
CREATE TABLE IF NOT EXISTS historias_clinicas (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    paciente_id UUID REFERENCES pacientes(id) ON DELETE CASCADE,
    fecha_consulta DATE,
    motivo_consulta TEXT,
    diagnostico TEXT,
    notas_extras TEXT,
    payload JSONB, -- Para almacenar datos adicionales o formato original
    fecha_registro TIMESTAMPTZ DEFAULT NOW(),
    fecha_actualizacion TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- Tabla: Antecedentes
-- ============================================
CREATE TABLE IF NOT EXISTS antecedentes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    historia_id UUID REFERENCES historias_clinicas(id) ON DELETE CASCADE,
    apf TEXT, -- Antecedentes Patológicos Familiares
    app TEXT, -- Antecedentes Patológicos Personales
    apq TEXT, -- Antecedentes Patológicos Quirúrgicos
    ago TEXT, -- Antecedentes Gineco-Obstétricos
    menarquia VARCHAR(50),
    p VARCHAR(10), -- Partos
    g VARCHAR(10), -- Gestaciones
    c VARCHAR(10), -- Cesáreas
    a VARCHAR(10), -- Abortos
    alergias TEXT,
    fecha_registro TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- Tabla: Hábitos
-- ============================================
CREATE TABLE IF NOT EXISTS habitos (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    historia_id UUID REFERENCES historias_clinicas(id) ON DELETE CASCADE,
    fuma VARCHAR(100),
    alcohol VARCHAR(100),
    cafe VARCHAR(100),
    hidratacion VARCHAR(100),
    gaseosas VARCHAR(100),
    actividad_fisica VARCHAR(200),
    te VARCHAR(100),
    edulcorantes VARCHAR(100),
    alimentacion TEXT,
    fecha_registro TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- Tabla: Signos Vitales
-- ============================================
CREATE TABLE IF NOT EXISTS signos_vitales (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    historia_id UUID REFERENCES historias_clinicas(id) ON DELETE CASCADE,
    pa VARCHAR(20), -- Presión Arterial
    temperatura VARCHAR(20),
    fc VARCHAR(20), -- Frecuencia Cardíaca
    fr VARCHAR(20), -- Frecuencia Respiratoria
    fecha_registro TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- Tabla: Datos Antropométricos
-- ============================================
CREATE TABLE IF NOT EXISTS datos_antropometricos (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    historia_id UUID REFERENCES historias_clinicas(id) ON DELETE CASCADE,
    edad VARCHAR(10),
    edad_metabolica VARCHAR(10),
    sexo VARCHAR(10),
    peso DECIMAL(6,2),
    masa_muscular DECIMAL(6,2),
    gc_porc DECIMAL(5,2), -- % Grasa Corporal
    gc DECIMAL(6,2), -- Grasa Corporal
    talla DECIMAL(5,2),
    gv_porc DECIMAL(5,2), -- % Grasa Visceral
    imc DECIMAL(5,2),
    kcal_basales INTEGER,
    actividad_fisica VARCHAR(100),
    cintura DECIMAL(5,2),
    cadera DECIMAL(5,2),
    pantorrilla DECIMAL(5,2),
    c_brazo DECIMAL(5,2),
    c_muslo DECIMAL(5,2),
    peso_ajustado DECIMAL(6,2),
    factor_actividad_fisica DECIMAL(4,2),
    tiempos_comida VARCHAR(100),
    fecha_registro TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- Tabla: Valores Bioquímicos
-- ============================================
CREATE TABLE IF NOT EXISTS valores_bioquimicos (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    historia_id UUID REFERENCES historias_clinicas(id) ON DELETE CASCADE,
    glicemia DECIMAL(6,2),
    colesterol_total DECIMAL(6,2),
    trigliceridos DECIMAL(6,2),
    hdl DECIMAL(6,2),
    ldl DECIMAL(6,2),
    tgo DECIMAL(6,2),
    tgp DECIMAL(6,2),
    urea DECIMAL(6,2),
    creatinina DECIMAL(6,2),
    fecha_registro TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- Tabla: Recordatorio 24 horas
-- ============================================
CREATE TABLE IF NOT EXISTS recordatorio_24h (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    historia_id UUID REFERENCES historias_clinicas(id) ON DELETE CASCADE,
    desayuno TEXT,
    snack1 TEXT,
    almuerzo TEXT,
    snack2 TEXT,
    cena TEXT,
    extras TEXT,
    fecha_registro TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- Tabla: Frecuencia de Consumo de Alimentos
-- ============================================
CREATE TABLE IF NOT EXISTS frecuencia_consumo (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    historia_id UUID REFERENCES historias_clinicas(id) ON DELETE CASCADE,
    categoria VARCHAR(100),
    alimento VARCHAR(100),
    frecuencia VARCHAR(50),
    fecha_registro TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- Tabla: Usuarios (opcional, para gestión de la app)
-- ============================================
CREATE TABLE IF NOT EXISTS usuarios (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth0_id VARCHAR(100) UNIQUE,
    email VARCHAR(100) NOT NULL UNIQUE,
    nombre VARCHAR(200),
    rol VARCHAR(50) DEFAULT 'nutricionista',
    activo BOOLEAN DEFAULT true,
    fecha_creacion TIMESTAMPTZ DEFAULT NOW(),
    fecha_ultimo_acceso TIMESTAMPTZ
);

-- ============================================
-- Tabla: Auditoría (para registro de cambios)
-- ============================================
CREATE TABLE IF NOT EXISTS auditoria (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tabla VARCHAR(100),
    registro_id UUID,
    usuario_id UUID REFERENCES usuarios(id),
    accion VARCHAR(50), -- INSERT, UPDATE, DELETE
    datos_anteriores JSONB,
    datos_nuevos JSONB,
    fecha_accion TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- Índices para mejorar el rendimiento
-- ============================================
CREATE INDEX IF NOT EXISTS idx_pacientes_cedula ON pacientes(numero_cedula);
CREATE INDEX IF NOT EXISTS idx_pacientes_email ON pacientes(email);
CREATE INDEX IF NOT EXISTS idx_historias_paciente ON historias_clinicas(paciente_id);
CREATE INDEX IF NOT EXISTS idx_historias_fecha ON historias_clinicas(fecha_consulta);
CREATE INDEX IF NOT EXISTS idx_antecedentes_historia ON antecedentes(historia_id);
CREATE INDEX IF NOT EXISTS idx_habitos_historia ON habitos(historia_id);
CREATE INDEX IF NOT EXISTS idx_signos_historia ON signos_vitales(historia_id);
CREATE INDEX IF NOT EXISTS idx_antropometricos_historia ON datos_antropometricos(historia_id);
CREATE INDEX IF NOT EXISTS idx_bioquimicos_historia ON valores_bioquimicos(historia_id);
CREATE INDEX IF NOT EXISTS idx_recordatorio_historia ON recordatorio_24h(historia_id);
CREATE INDEX IF NOT EXISTS idx_frecuencia_historia ON frecuencia_consumo(historia_id);
CREATE INDEX IF NOT EXISTS idx_usuarios_auth0 ON usuarios(auth0_id);
CREATE INDEX IF NOT EXISTS idx_auditoria_fecha ON auditoria(fecha_accion);

-- ============================================
-- Vista: Historias Completas
-- ============================================
CREATE OR REPLACE VIEW vista_historias_completas AS
SELECT 
    h.id as historia_id,
    h.fecha_consulta,
    h.motivo_consulta,
    h.diagnostico,
    p.id as paciente_id,
    p.numero_cedula,
    p.nombre,
    p.edad_cronologica,
    p.sexo,
    p.telefono,
    p.email,
    h.fecha_registro as fecha_creacion_historia
FROM historias_clinicas h
INNER JOIN pacientes p ON h.paciente_id = p.id
ORDER BY h.fecha_registro DESC;

-- ============================================
-- Función: Actualizar fecha de modificación
-- ============================================
CREATE OR REPLACE FUNCTION actualizar_fecha_modificacion()
RETURNS TRIGGER AS $$
BEGIN
    NEW.fecha_actualizacion = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- Triggers para actualizar fechas
-- ============================================
CREATE TRIGGER trigger_actualizar_paciente
    BEFORE UPDATE ON pacientes
    FOR EACH ROW
    EXECUTE FUNCTION actualizar_fecha_modificacion();

CREATE TRIGGER trigger_actualizar_historia
    BEFORE UPDATE ON historias_clinicas
    FOR EACH ROW
    EXECUTE FUNCTION actualizar_fecha_modificacion();

-- ============================================
-- Insertar datos de ejemplo (opcional)
-- ============================================
-- Paciente de prueba
INSERT INTO pacientes (numero_cedula, nombre, edad_cronologica, sexo, telefono, email)
VALUES 
    ('1234567890', 'Juan Pérez', '35', 'M', '0991234567', 'juan.perez@example.com'),
    ('0987654321', 'María González', '28', 'F', '0987654321', 'maria.gonzalez@example.com')
ON CONFLICT (numero_cedula) DO NOTHING;

-- ============================================
-- Comentarios en las tablas
-- ============================================
COMMENT ON TABLE pacientes IS 'Información personal de los pacientes';
COMMENT ON TABLE historias_clinicas IS 'Historias clínicas nutricionales de los pacientes';
COMMENT ON TABLE antecedentes IS 'Antecedentes médicos del paciente';
COMMENT ON TABLE habitos IS 'Hábitos de vida del paciente';
COMMENT ON TABLE signos_vitales IS 'Signos vitales registrados en la consulta';
COMMENT ON TABLE datos_antropometricos IS 'Medidas antropométricas del paciente';
COMMENT ON TABLE valores_bioquimicos IS 'Resultados de análisis bioquímicos';
COMMENT ON TABLE recordatorio_24h IS 'Recordatorio de alimentación de 24 horas';
COMMENT ON TABLE frecuencia_consumo IS 'Frecuencia de consumo de diferentes alimentos';
COMMENT ON TABLE usuarios IS 'Usuarios del sistema (nutricionistas)';
COMMENT ON TABLE auditoria IS 'Registro de auditoría de cambios en el sistema';

-- ============================================
-- Mensaje de finalización
-- ============================================
SELECT 'Base de datos NutriWeb creada exitosamente!' as mensaje;
