using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=nutriciondb;Username=postgres;Password=030762;Pooling=true;Trust Server Certificate=true";

var sql = @"
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
    semana INT NOT NULL CHECK (semana IN (1, 2)),
    dia_semana INT NOT NULL CHECK (dia_semana BETWEEN 1 AND 7),
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

-- Índices
CREATE INDEX IF NOT EXISTS idx_planes_historia ON planes_nutricionales(historia_id);
CREATE INDEX IF NOT EXISTS idx_planes_activo ON planes_nutricionales(activo);
CREATE INDEX IF NOT EXISTS idx_alimentacion_plan ON alimentacion_semanal(plan_id);
CREATE INDEX IF NOT EXISTS idx_alimentacion_semana_dia ON alimentacion_semanal(semana, dia_semana);

-- Trigger
CREATE OR REPLACE FUNCTION actualizar_fecha_modificacion_plan()
RETURNS TRIGGER AS $$
BEGIN
    NEW.fecha_modificacion = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trigger_actualizar_plan ON planes_nutricionales;
CREATE TRIGGER trigger_actualizar_plan
    BEFORE UPDATE ON planes_nutricionales
    FOR EACH ROW
    EXECUTE FUNCTION actualizar_fecha_modificacion_plan();
";

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    await using var cmd = new NpgsqlCommand(sql, connection);
    await cmd.ExecuteNonQueryAsync();
    
    Console.WriteLine("✓ Tablas de planes de alimentación creadas exitosamente");
    
    // Verificar
    await using var cmdVerify = new NpgsqlCommand(@"
        SELECT 
            (SELECT COUNT(*) FROM planes_nutricionales) as planes,
            (SELECT COUNT(*) FROM alimentacion_semanal) as alimentacion", connection);
    
    await using var reader = await cmdVerify.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        Console.WriteLine($"Planes nutricionales: {reader.GetInt64(0)} registros");
        Console.WriteLine($"Alimentación semanal: {reader.GetInt64(1)} registros");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Error: {ex.Message}");
    return 1;
}

return 0;
