using Npgsql;
using System;

var connectionFile = Path.Combine(Environment.CurrentDirectory, "connection.local");
if (!File.Exists(connectionFile))
{
    Console.WriteLine("Error: No se encontró el archivo connection.local");
    return 1;
}

var connectionString = File.ReadAllText(connectionFile).Trim();
Console.WriteLine($"Conectando a la base de datos...");

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

Console.WriteLine("Conexión exitosa!");
Console.WriteLine("\nPlanes existentes:");
Console.WriteLine("=====================================");

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
    Console.WriteLine($"{planes.Count}. ID: {id}");
    Console.WriteLine($"   Paciente: {paciente}");
    Console.WriteLine($"   Historia ID: {historiaId}");
    Console.WriteLine($"   Fecha Inicio: {fechaInicio:yyyy-MM-dd}");
    Console.WriteLine($"   Fecha Creación: {fechaCreacion:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine();
}
await reader.CloseAsync();

if (planes.Count == 0)
{
    Console.WriteLine("No hay planes para eliminar.");
    return 0;
}

Console.WriteLine($"\nTotal de planes: {planes.Count}");
Console.WriteLine("\n¿Desea eliminar el plan más reciente? (S/N)");
var respuesta = Console.ReadLine()?.ToUpper();

if (respuesta == "S" || respuesta == "SI" || respuesta == "Y" || respuesta == "YES")
{
    var planAEliminar = planes[0];
    
    await using var cmdDelete = new NpgsqlCommand(
        "DELETE FROM planes_nutricionales WHERE id = @id", connection);
    cmdDelete.Parameters.AddWithValue("id", planAEliminar.id);
    
    var rows = await cmdDelete.ExecuteNonQueryAsync();
    
    if (rows > 0)
    {
        Console.WriteLine($"\n✓ Plan eliminado exitosamente: {planAEliminar.id}");
        Console.WriteLine($"  Paciente: {planAEliminar.paciente}");
    }
    else
    {
        Console.WriteLine("\n✗ No se pudo eliminar el plan.");
    }
}
else
{
    Console.WriteLine("\nOperación cancelada.");
}

return 0;
