using Npgsql;
using System;

var connectionFile = Path.Combine(Environment.CurrentDirectory, "connection.local");
if (!File.Exists(connectionFile))
{
    Console.WriteLine("Error: No se encontró el archivo connection.local");
    return 1;
}

var connectionString = File.ReadAllText(connectionFile).Trim();
Console.WriteLine("Conectando a la base de datos...\n");

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

Console.WriteLine("Actualizando datos antropométricos faltantes...\n");

// Patricia Salazar
await using var cmd1 = new NpgsqlCommand(@"
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
    )", connection);
var rows1 = await cmd1.ExecuteNonQueryAsync();
Console.WriteLine($"✓ Patricia Salazar: {rows1} registro(s) actualizado(s)");

// Carlos Méndez
await using var cmd2 = new NpgsqlCommand(@"
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
    )", connection);
var rows2 = await cmd2.ExecuteNonQueryAsync();
Console.WriteLine($"✓ Carlos Méndez: {rows2} registro(s) actualizado(s)");

// Ana Rodríguez
await using var cmd3 = new NpgsqlCommand(@"
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
    )", connection);
var rows3 = await cmd3.ExecuteNonQueryAsync();
Console.WriteLine($"✓ Ana Rodríguez: {rows3} registro(s) actualizado(s)");

// Luis Fernández
await using var cmd4 = new NpgsqlCommand(@"
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
    )", connection);
var rows4 = await cmd4.ExecuteNonQueryAsync();
Console.WriteLine($"✓ Luis Fernández: {rows4} registro(s) actualizado(s)");

// María González
await using var cmd5 = new NpgsqlCommand(@"
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
    )", connection);
var rows5 = await cmd5.ExecuteNonQueryAsync();
Console.WriteLine($"✓ María González: {rows5} registro(s) actualizado(s)");

// Roberto Castro
await using var cmd6 = new NpgsqlCommand(@"
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
    )", connection);
var rows6 = await cmd6.ExecuteNonQueryAsync();
Console.WriteLine($"✓ Roberto Castro: {rows6} registro(s) actualizado(s)");

// Laura Jiménez
await using var cmd7 = new NpgsqlCommand(@"
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
    )", connection);
var rows7 = await cmd7.ExecuteNonQueryAsync();
Console.WriteLine($"✓ Laura Jiménez: {rows7} registro(s) actualizado(s)");

// Diego Morales
await using var cmd8 = new NpgsqlCommand(@"
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
    )", connection);
var rows8 = await cmd8.ExecuteNonQueryAsync();
Console.WriteLine($"✓ Diego Morales: {rows8} registro(s) actualizado(s)");

// Sofía Vargas
await using var cmd9 = new NpgsqlCommand(@"
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
    )", connection);
var rows9 = await cmd9.ExecuteNonQueryAsync();
Console.WriteLine($"✓ Sofía Vargas: {rows9} registro(s) actualizado(s)");

// Andrés Rivas
await using var cmd10 = new NpgsqlCommand(@"
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
    )", connection);
var rows10 = await cmd10.ExecuteNonQueryAsync();
Console.WriteLine($"✓ Andrés Rivas: {rows10} registro(s) actualizado(s)");

Console.WriteLine("\n=== VERIFICACIÓN DE DATOS ===\n");

// Verificar los cambios
await using var cmdVerify = new NpgsqlCommand(@"
    SELECT 
        p.nombre,
        da.edad,
        da.edad_metabolica,
        da.peso,
        da.peso_ajustado,
        da.pantorrilla,
        da.c_muslo,
        da.actividad_fisica,
        da.factor_actividad_fisica,
        da.tiempos_comida
    FROM datos_antropometricos da
    JOIN historias_clinicas hc ON da.historia_id = hc.id
    JOIN pacientes p ON hc.paciente_id = p.id
    ORDER BY p.nombre", connection);

await using var readerVerify = await cmdVerify.ExecuteReaderAsync();
while (await readerVerify.ReadAsync())
{
    var nombre = readerVerify.GetString(0);
    var edad = readerVerify.IsDBNull(1) ? "NULL" : readerVerify.GetString(1);
    var edadMet = readerVerify.IsDBNull(2) ? "NULL" : readerVerify.GetString(2);
    var peso = readerVerify.IsDBNull(3) ? "NULL" : readerVerify.GetDecimal(3).ToString();
    var pesoAjust = readerVerify.IsDBNull(4) ? "NULL" : readerVerify.GetDecimal(4).ToString();
    var pantorrilla = readerVerify.IsDBNull(5) ? "NULL" : readerVerify.GetDecimal(5).ToString();
    var muslo = readerVerify.IsDBNull(6) ? "NULL" : readerVerify.GetDecimal(6).ToString();
    var actFis = readerVerify.IsDBNull(7) ? "NULL" : readerVerify.GetString(7);
    var factor = readerVerify.IsDBNull(8) ? "NULL" : readerVerify.GetDecimal(8).ToString();
    var tiempos = readerVerify.IsDBNull(9) ? "NULL" : readerVerify.GetString(9);
    
    Console.WriteLine($"{nombre}:");
    Console.WriteLine($"  Edad: {edad} años | Edad Metab: {edadMet} años");
    Console.WriteLine($"  Peso: {peso} kg | Peso Ajust: {pesoAjust} kg");
    Console.WriteLine($"  C. Pantorrilla: {pantorrilla} cm | C. Muslo: {muslo} cm");
    Console.WriteLine($"  Act. Física: {actFis} | Factor: {factor}");
    Console.WriteLine($"  Tiempos Comida: {tiempos}");
    Console.WriteLine();
}

Console.WriteLine("\n✓ Actualización completada exitosamente!");

return 0;
