using Npgsql;
using System;

var connectionFile = Path.Combine(Environment.CurrentDirectory, "connection.local");
if (!File.Exists(connectionFile))
{
    Console.WriteLine("Error: No se encontró el archivo connection.local");
    return 1;
}

var connectionString = File.ReadAllText(connectionFile).Trim();
Console.WriteLine($"Conectando a la base de datos...\n");

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

// Buscar Patricia Salazar
await using var cmdPaciente = new NpgsqlCommand(@"
    SELECT id, nombre, numero_cedula 
    FROM pacientes 
    WHERE nombre LIKE '%Patricia%'
    LIMIT 1", connection);

await using var readerPaciente = await cmdPaciente.ExecuteReaderAsync();
if (!await readerPaciente.ReadAsync())
{
    Console.WriteLine("No se encontró el paciente Patricia");
    return 1;
}

var pacienteId = readerPaciente.GetGuid(0);
var nombre = readerPaciente.GetString(1);
var cedula = readerPaciente.GetString(2);
await readerPaciente.CloseAsync();

Console.WriteLine($"Paciente encontrado:");
Console.WriteLine($"  ID: {pacienteId}");
Console.WriteLine($"  Nombre: {nombre}");
Console.WriteLine($"  Cédula: {cedula}\n");

// Buscar la historia clínica más reciente
await using var cmdHistoria = new NpgsqlCommand(@"
    SELECT id, fecha_consulta
    FROM historias_clinicas
    WHERE paciente_id = @pacienteId
    ORDER BY fecha_consulta DESC
    LIMIT 1", connection);
cmdHistoria.Parameters.AddWithValue("pacienteId", pacienteId);

await using var readerHistoria = await cmdHistoria.ExecuteReaderAsync();
if (!await readerHistoria.ReadAsync())
{
    Console.WriteLine("No se encontró historia clínica");
    return 1;
}

var historiaId = readerHistoria.GetGuid(0);
var fechaConsulta = readerHistoria.GetDateTime(1);
await readerHistoria.CloseAsync();

Console.WriteLine($"Historia clínica más reciente:");
Console.WriteLine($"  ID: {historiaId}");
Console.WriteLine($"  Fecha: {fechaConsulta:yyyy-MM-dd}\n");

// Ver datos antropométricos
Console.WriteLine("=== DATOS ANTROPOMÉTRICOS ===");
await using var cmdAntro = new NpgsqlCommand(@"
    SELECT 
        peso, talla, imc, cintura, cadera, c_brazo,
        masa_muscular, gc_porc, gc, gv_porc, edad, sexo,
        edad_metabolica, kcal_basales, actividad_fisica,
        pantorrilla, c_muslo, peso_ajustado, 
        factor_actividad_fisica, tiempos_comida
    FROM datos_antropometricos
    WHERE historia_id = @historiaId", connection);
cmdAntro.Parameters.AddWithValue("historiaId", historiaId);

await using var readerAntro = await cmdAntro.ExecuteReaderAsync();
if (await readerAntro.ReadAsync())
{
    Console.WriteLine($"Peso: {(readerAntro.IsDBNull(0) ? "NULL" : readerAntro.GetDecimal(0).ToString())} kg");
    Console.WriteLine($"Talla: {(readerAntro.IsDBNull(1) ? "NULL" : readerAntro.GetDecimal(1).ToString())} cm");
    Console.WriteLine($"IMC: {(readerAntro.IsDBNull(2) ? "NULL" : readerAntro.GetDecimal(2).ToString())}");
    Console.WriteLine($"C. Cintura: {(readerAntro.IsDBNull(3) ? "NULL" : readerAntro.GetDecimal(3).ToString())} cm");
    Console.WriteLine($"C. Cadera: {(readerAntro.IsDBNull(4) ? "NULL" : readerAntro.GetDecimal(4).ToString())} cm");
    Console.WriteLine($"C. Brazo: {(readerAntro.IsDBNull(5) ? "NULL" : readerAntro.GetDecimal(5).ToString())} cm");
    Console.WriteLine($"C. Muslo: {(readerAntro.IsDBNull(16) ? "NULL" : readerAntro.GetDecimal(16).ToString())} cm");
    Console.WriteLine($"C. Pantorrilla: {(readerAntro.IsDBNull(15) ? "NULL" : readerAntro.GetDecimal(15).ToString())} cm");
    Console.WriteLine($"Masa Muscular: {(readerAntro.IsDBNull(6) ? "NULL" : readerAntro.GetDecimal(6).ToString())} kg");
    Console.WriteLine($"Grasa Corporal %: {(readerAntro.IsDBNull(7) ? "NULL" : readerAntro.GetDecimal(7).ToString())}%");
    Console.WriteLine($"Grasa Corporal: {(readerAntro.IsDBNull(8) ? "NULL" : readerAntro.GetDecimal(8).ToString())} kg");
    Console.WriteLine($"Grasa Visceral %: {(readerAntro.IsDBNull(9) ? "NULL" : readerAntro.GetDecimal(9).ToString())}%");
    Console.WriteLine($"Edad: {(readerAntro.IsDBNull(10) ? "NULL" : readerAntro.GetString(10))} años");
    Console.WriteLine($"Sexo: {(readerAntro.IsDBNull(11) ? "NULL" : readerAntro.GetString(11))}");
    Console.WriteLine($"Edad Metabólica: {(readerAntro.IsDBNull(12) ? "NULL" : readerAntro.GetString(12))} años");
    Console.WriteLine($"Kcal Basales: {(readerAntro.IsDBNull(13) ? "NULL" : readerAntro.GetInt32(13).ToString())} kcal");
    Console.WriteLine($"Peso Ajustado: {(readerAntro.IsDBNull(17) ? "NULL" : readerAntro.GetDecimal(17).ToString())} kg");
    Console.WriteLine($"Actividad Física: {(readerAntro.IsDBNull(14) ? "NULL" : readerAntro.GetString(14))}");
    Console.WriteLine($"Factor Act. Física: {(readerAntro.IsDBNull(18) ? "NULL" : readerAntro.GetDecimal(18).ToString())}");
    Console.WriteLine($"Tiempos de Comida: {(readerAntro.IsDBNull(19) ? "NULL" : readerAntro.GetString(19))}");
}
else
{
    Console.WriteLine("No hay datos antropométricos para esta historia");
}
await readerAntro.CloseAsync();

return 0;
