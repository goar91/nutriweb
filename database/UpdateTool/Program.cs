using Npgsql;
using System;
using System.IO;
using System.Collections.Generic;

var connectionFile = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.Parent!.FullName, "connection.local");

if (!File.Exists(connectionFile))
{
    Console.WriteLine($"Error: No se encontró el archivo connection.local en {connectionFile}");
    return 1;
}

var connectionString = File.ReadAllText(connectionFile).Trim();
Console.WriteLine("Conectando a la base de datos...\n");

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

Console.WriteLine("Actualizando datos antropométricos faltantes...\n");

// Actualizar TODOS los registros que tengan valores NULL en los campos nuevos
await using var cmdUpdate = new NpgsqlCommand(@"
    UPDATE datos_antropometricos
    SET 
        pantorrilla = CASE 
            WHEN pantorrilla IS NULL THEN ROUND((talla * 0.22)::numeric, 1)
            ELSE pantorrilla 
        END,
        c_muslo = CASE 
            WHEN c_muslo IS NULL THEN ROUND((talla * 0.35)::numeric, 1)
            ELSE c_muslo 
        END,
        peso_ajustado = CASE 
            WHEN peso_ajustado IS NULL THEN ROUND((peso * 0.95)::numeric, 1)
            ELSE peso_ajustado 
        END,
        edad_metabolica = CASE 
            WHEN edad_metabolica IS NULL THEN edad
            ELSE edad_metabolica 
        END,
        actividad_fisica = CASE 
            WHEN actividad_fisica IS NULL THEN 'Moderada'
            ELSE actividad_fisica 
        END,
        factor_actividad_fisica = CASE 
            WHEN factor_actividad_fisica IS NULL THEN 1.55
            ELSE factor_actividad_fisica 
        END,
        tiempos_comida = CASE 
            WHEN tiempos_comida IS NULL THEN '5'
            ELSE tiempos_comida 
        END
    WHERE pantorrilla IS NULL 
       OR c_muslo IS NULL 
       OR peso_ajustado IS NULL 
       OR edad_metabolica IS NULL 
       OR actividad_fisica IS NULL 
       OR factor_actividad_fisica IS NULL 
       OR tiempos_comida IS NULL", connection);

var rowsUpdated = await cmdUpdate.ExecuteNonQueryAsync();
Console.WriteLine($"✓ {rowsUpdated} registro(s) actualizado(s) con valores calculados\n");

Console.WriteLine("\n=== VERIFICACIÓN DE DATOS ===\n");

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
