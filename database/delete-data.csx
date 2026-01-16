using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=nutriciondb;Username=postgres;Password=030762;Pooling=true;Trust Server Certificate=true";

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

Console.WriteLine("Eliminando datos...\n");

// Eliminar frecuencia de consumo
await using var cmd1 = new NpgsqlCommand("DELETE FROM frecuencia_consumo", connection);
var count1 = await cmd1.ExecuteNonQueryAsync();
Console.WriteLine($"Eliminados {count1} registros de frecuencia_consumo");

// Eliminar recordatorio 24h
await using var cmd2 = new NpgsqlCommand("DELETE FROM recordatorio_24h", connection);
var count2 = await cmd2.ExecuteNonQueryAsync();
Console.WriteLine($"Eliminados {count2} registros de recordatorio_24h");

// Eliminar valores bioquímicos
await using var cmd3 = new NpgsqlCommand("DELETE FROM valores_bioquimicos", connection);
var count3 = await cmd3.ExecuteNonQueryAsync();
Console.WriteLine($"Eliminados {count3} registros de valores_bioquimicos");

// Eliminar hábitos
await using var cmd4 = new NpgsqlCommand("DELETE FROM habitos", connection);
var count4 = await cmd4.ExecuteNonQueryAsync();
Console.WriteLine($"Eliminados {count4} registros de habitos");

// Eliminar antecedentes
await using var cmd5 = new NpgsqlCommand("DELETE FROM antecedentes", connection);
var count5 = await cmd5.ExecuteNonQueryAsync();
Console.WriteLine($"Eliminados {count5} registros de antecedentes");

// Eliminar signos vitales
await using var cmd6 = new NpgsqlCommand("DELETE FROM signos_vitales", connection);
var count6 = await cmd6.ExecuteNonQueryAsync();
Console.WriteLine($"Eliminados {count6} registros de signos_vitales");

// Eliminar datos antropométricos
await using var cmd7 = new NpgsqlCommand("DELETE FROM datos_antropometricos", connection);
var count7 = await cmd7.ExecuteNonQueryAsync();
Console.WriteLine($"Eliminados {count7} registros de datos_antropometricos");

// Eliminar historias clínicas
await using var cmd8 = new NpgsqlCommand("DELETE FROM historias_clinicas", connection);
var count8 = await cmd8.ExecuteNonQueryAsync();
Console.WriteLine($"Eliminados {count8} registros de historias_clinicas");

// Eliminar pacientes
await using var cmd9 = new NpgsqlCommand("DELETE FROM pacientes", connection);
var count9 = await cmd9.ExecuteNonQueryAsync();
Console.WriteLine($"Eliminados {count9} registros de pacientes");

Console.WriteLine("\n¡Todos los datos han sido eliminados exitosamente!");
