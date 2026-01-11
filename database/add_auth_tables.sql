-- Script para agregar tabla de sesiones y logging
-- PostgreSQL

-- ============================================
-- Tabla: Sesiones de usuario
-- ============================================
CREATE TABLE IF NOT EXISTS sesiones (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id UUID REFERENCES usuarios(id) ON DELETE CASCADE,
    token VARCHAR(500) UNIQUE,
    ip_address VARCHAR(50),
    user_agent TEXT,
    fecha_inicio TIMESTAMPTZ DEFAULT NOW(),
    fecha_expiracion TIMESTAMPTZ,
    activa BOOLEAN DEFAULT true
);

-- ============================================
-- Tabla: Logging de accesos
-- ============================================
CREATE TABLE IF NOT EXISTS logs_acceso (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id UUID REFERENCES usuarios(id),
    accion VARCHAR(100), -- LOGIN, LOGOUT, LOGIN_FAILED, etc.
    username VARCHAR(100),
    ip_address VARCHAR(50),
    user_agent TEXT,
    exitoso BOOLEAN,
    mensaje TEXT,
    fecha_hora TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- Índices para rendimiento
-- ============================================
CREATE INDEX IF NOT EXISTS idx_sesiones_token ON sesiones(token);
CREATE INDEX IF NOT EXISTS idx_sesiones_usuario ON sesiones(usuario_id);
CREATE INDEX IF NOT EXISTS idx_sesiones_activa ON sesiones(activa);
CREATE INDEX IF NOT EXISTS idx_logs_usuario ON logs_acceso(usuario_id);
CREATE INDEX IF NOT EXISTS idx_logs_fecha ON logs_acceso(fecha_hora);
CREATE INDEX IF NOT EXISTS idx_logs_accion ON logs_acceso(accion);

-- ============================================
-- Modificar tabla usuarios para eliminar auth0_id
-- ============================================
ALTER TABLE usuarios DROP COLUMN IF EXISTS auth0_id;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS username VARCHAR(50) UNIQUE;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS password_hash VARCHAR(255);
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS ultimo_login TIMESTAMPTZ;

-- ============================================
-- Insertar usuario admin por defecto
-- ============================================
-- Contraseña: admin (en producción usar hash bcrypt)
INSERT INTO usuarios (username, email, nombre, password_hash, rol, activo)
VALUES ('admin', 'admin@nutriweb.com', 'Administrador', 'admin', 'admin', true)
ON CONFLICT (username) DO UPDATE 
SET password_hash = 'admin', email = 'admin@nutriweb.com';

-- ============================================
-- Función: Limpiar sesiones expiradas
-- ============================================
CREATE OR REPLACE FUNCTION limpiar_sesiones_expiradas()
RETURNS void AS $$
BEGIN
    UPDATE sesiones 
    SET activa = false 
    WHERE fecha_expiracion < NOW() AND activa = true;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- Comentarios
-- ============================================
COMMENT ON TABLE sesiones IS 'Sesiones activas de usuarios';
COMMENT ON TABLE logs_acceso IS 'Registro de todos los intentos de acceso al sistema';

SELECT 'Tablas de sesiones y logging creadas exitosamente!' as mensaje;
