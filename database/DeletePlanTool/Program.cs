using Npgsql;

var connectionFile = Path.Combine(Environment.CurrentDirectory, "..", "connection.local");
if (!File.Exists(connectionFile))
{
    Console.WriteLine($"Error: No se encontró el archivo connection.local");
    Console.WriteLine($"Buscado en: {connectionFile}");
    Console.WriteLine($"Directorio actual: {Environment.CurrentDirectory}");
    return;
}

var connectionString = File.ReadAllText(connectionFile).Trim();
Console.WriteLine("Conectando a la base de datos...\n");

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

Console.WriteLine("✓ Conexión exitosa!\n");
Console.WriteLine("Planes existentes:");
Console.WriteLine("==========================================");

// Listar planes
await using var cmdList = new NpgsqlCommand(@"
    SELECT pn.id, pn.historia_id, pn.fecha_inicio, pn.fecha_creacion, p.nombre
    FROM planes_nutricionales pn
    LEFT JOIN historias_clinicas h ON pn.historia_id = h.id
    LEFT JOIN pacientes p ON h.paciente_id = p.id
    ORDER BY pn.fecha_creacion DESC", connection);

var planes = new List<(Guid id, Guid historiaId, DateTime fechaInicio, DateTime fechaCreacion, string paciente)>();

await using var reader = await cmdList.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    var id = reader.GetGuid(0);
    var historiaId = reader.GetGuid(1);
    var fechaInicio = reader.GetDateTime(2);
    var fechaCreacion = reader.GetDateTime(3);
    var paciente = reader.IsDBNull(4) ? "Sin nombre" : reader.GetString(4);
    
    planes.Add((id, historiaId, fechaInicio, fechaCreacion, paciente));
    Console.WriteLine($"{planes.Count}. Paciente: {paciente}");
    Console.WriteLine($"   ID Plan: {id}");
    Console.WriteLine($"   Fecha Creación: {fechaCreacion:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine();
}
await reader.CloseAsync();

if (planes.Count == 0)
{
    Console.WriteLine("No hay planes para eliminar.");
    return;
}

Console.WriteLine($"Total de planes: {planes.Count}");

// Eliminar el plan más reciente automáticamente
var planAEliminar = planes[0];

Console.WriteLine($"\nEliminando el plan más reciente...");
Console.WriteLine($"  Paciente: {planAEliminar.paciente}");

await using var cmdDelete = new NpgsqlCommand(
    "DELETE FROM planes_nutricionales WHERE id = @id", connection);
cmdDelete.Parameters.AddWithValue("id", planAEliminar.id);

var rows = await cmdDelete.ExecuteNonQueryAsync();

if (rows > 0)
{
    Console.WriteLine($"\n✓ Plan eliminado exitosamente!");
    Console.WriteLine($"\nPlanes restantes: {planes.Count - 1}");
}
else
{
    Console.WriteLine("\n✗ No se pudo eliminar el plan.");
}
