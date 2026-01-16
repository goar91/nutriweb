using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

var connectionString = ResolveConnectionString(builder.Configuration);
var wwwRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
var browserPath = Path.Combine(wwwRootPath, "browser");
var staticRoot = Directory.Exists(browserPath) ? browserPath : wwwRootPath;
var hasStaticFiles = Directory.Exists(staticRoot);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200", "http://localhost:4300", "http://localhost:4301")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.Urls.Clear();
app.Urls.Add("http://localhost:5000");

app.UseCors();

// Agregar middleware de logging
app.Use(async (context, next) =>
{
    Console.WriteLine($"[REQUEST] {context.Request.Method} {context.Request.Path}");
    try
    {
        await next();
        Console.WriteLine($"[RESPONSE] Status: {context.Response.StatusCode}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[ERROR] Unhandled exception: {ex.Message}");
        Console.Error.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
        throw;
    }
});

if (hasStaticFiles)
{
    Console.WriteLine($"[STATIC] Sirviendo frontend desde {staticRoot}");
    var fileProvider = new PhysicalFileProvider(staticRoot);
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = fileProvider,
        RequestPath = ""
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = fileProvider,
        RequestPath = ""
    });
    app.MapFallback(async context =>
    {
        var indexPath = Path.Combine(staticRoot, "index.html");
        if (File.Exists(indexPath))
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(indexPath);
        }
    });
}
else
{
    Console.WriteLine("[STATIC] No se encontro la carpeta wwwroot; modo solo API");
}

// Sesiones en memoria (local)
var activeSessions = new System.Collections.Concurrent.ConcurrentDictionary<string, PublicUser>();

app.MapPost("/api/auth/login", async (LoginRequest request, HttpContext context) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.Json(new { success = false, error = "Username y password requeridos" }, statusCode: StatusCodes.Status400BadRequest);
    }

    var ipAddress = GetClientIp(context);
    var userAgent = GetUserAgent(context);

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await EnsureAuthSchemaAsync(connection);

    await using var cmd = new NpgsqlCommand(
        @"SELECT id, username, nombre, email, password_hash
          FROM usuarios
          WHERE username = @username AND activo = true
          LIMIT 1",
        connection);
    cmd.Parameters.AddWithValue("username", request.Username);

    await using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
    {
        await reader.CloseAsync();
        await LogAccessAttemptAsync(connection, null, request.Username, "login", ipAddress, userAgent, false, "Usuario no encontrado");
        return Results.Json(new { success = false, error = "Credenciales inválidas" }, statusCode: StatusCodes.Status401Unauthorized);
    }

    var user = new PublicUser(
        reader.GetGuid(0),
        reader.IsDBNull(1) ? request.Username : reader.GetString(1),
        reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
        reader.IsDBNull(3) ? string.Empty : reader.GetString(3));
    var storedHash = reader.IsDBNull(4) ? null : reader.GetString(4);
    await reader.CloseAsync();

    if (string.IsNullOrWhiteSpace(storedHash))
    {
        return Results.Json(new { success = false, error = "Credenciales inválidas" }, statusCode: StatusCodes.Status401Unauthorized);
    }

    var shouldUpgrade = false;
    var isValid = storedHash.StartsWith("PBKDF2$", StringComparison.Ordinal)
        ? VerifyPassword(request.Password, storedHash)
        : string.Equals(storedHash, request.Password, StringComparison.Ordinal);

    if (isValid && !storedHash.StartsWith("PBKDF2$", StringComparison.Ordinal))
    {
        shouldUpgrade = true;
    }

    if (!isValid)
    {
        await LogAccessAttemptAsync(connection, user.Id, user.Username, "login", ipAddress, userAgent, false, "Contraseña incorrecta");
        return Results.Json(new { success = false, error = "Credenciales inválidas" }, statusCode: StatusCodes.Status401Unauthorized);
    }

    if (shouldUpgrade)
    {
        var newHash = HashPassword(request.Password);
        await using var upgradeCmd = new NpgsqlCommand(
            "UPDATE usuarios SET password_hash = @hash WHERE id = @id",
            connection);
        upgradeCmd.Parameters.AddWithValue("hash", newHash);
        upgradeCmd.Parameters.AddWithValue("id", user.Id);
        await upgradeCmd.ExecuteNonQueryAsync();
    }

    await using var accessCmd = new NpgsqlCommand(
        "UPDATE usuarios SET fecha_ultimo_acceso = NOW() WHERE id = @id",
        connection);
    accessCmd.Parameters.AddWithValue("id", user.Id);
    await accessCmd.ExecuteNonQueryAsync();

    await LogAccessAttemptAsync(connection, user.Id, user.Username, "login", ipAddress, userAgent, true, "Login exitoso");

    var token = Guid.NewGuid().ToString("N");
    activeSessions[token] = user;

    return Results.Ok(new LoginResponse(true, token, user));
});

app.MapPost("/api/auth/register", async (RegisterRequest request, HttpContext context) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.Json(new { success = false, error = "Username y password requeridos" }, statusCode: StatusCodes.Status400BadRequest);
    }

    var ipAddress = GetClientIp(context);
    var userAgent = GetUserAgent(context);
    var email = string.IsNullOrWhiteSpace(request.Email)
        ? $"{request.Username}@nutriweb.local"
        : request.Email!.Trim();
    var nombre = string.IsNullOrWhiteSpace(request.Nombre) ? request.Username : request.Nombre!.Trim();

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await EnsureAuthSchemaAsync(connection);

    var userId = Guid.NewGuid();
    var hash = HashPassword(request.Password);

    try
    {
        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO usuarios (id, username, email, nombre, password_hash, rol, activo, fecha_creacion)
              VALUES (@id, @username, @email, @nombre, @hash, @rol, true, NOW())",
            connection);
        cmd.Parameters.AddWithValue("id", userId);
        cmd.Parameters.AddWithValue("username", request.Username);
        cmd.Parameters.AddWithValue("email", email);
        cmd.Parameters.AddWithValue("nombre", nombre);
        cmd.Parameters.AddWithValue("hash", hash);
        cmd.Parameters.AddWithValue("rol", "nutricionista");

        await cmd.ExecuteNonQueryAsync();
        await LogAccessAttemptAsync(connection, userId, request.Username, "register", ipAddress, userAgent, true, "Usuario registrado exitosamente");
    }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
    {
        await LogAccessAttemptAsync(connection, null, request.Username, "register", ipAddress, userAgent, false, "Usuario o email ya existe");
        return Results.Conflict(new { success = false, error = "El usuario o email ya existe" });
    }

    var user = new PublicUser(userId, request.Username, nombre, email);
    var token = Guid.NewGuid().ToString("N");
    activeSessions[token] = user;

    return Results.Ok(new LoginResponse(true, token, user));
});

app.MapPost("/api/auth/logout", (HttpContext context) =>
{
    if (TryGetToken(context, out var token) && token is not null)
    {
        activeSessions.TryRemove(token, out _);
    }

    return Results.Ok(new { success = true });
});

app.MapGet("/api/auth/verify", (HttpContext context) =>
{
    if (TryGetToken(context, out var token) && token is not null)
    {
        if (activeSessions.TryGetValue(token, out var user))
        {
            return Results.Ok(new { valid = true, user });
        }
    }

    return Results.Unauthorized();
});

app.MapGet("/api/nutrition/status", async () =>
{
    try
    {
        Console.WriteLine("[STATUS] Endpoint called");
        Console.WriteLine($"[STATUS] Connection string: {connectionString}");
        await using var connection = new NpgsqlConnection(connectionString);
        Console.WriteLine("[STATUS] Connection object created");
        await connection.OpenAsync();
        Console.WriteLine("[STATUS] Connection opened successfully");
        var dbName = connection.Database;
        Console.WriteLine($"[STATUS] Database name: {dbName}");
        return Results.Ok(new { status = "running", database = dbName });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[STATUS ERROR] Error connecting to database: {ex.Message}");
        Console.Error.WriteLine($"[STATUS ERROR] Stack trace: {ex.StackTrace}");
        return Results.Problem($"Database connection failed: {ex.Message}");
    }
});

app.MapPost("/api/nutrition/history", async (HistoryRequest request, HttpContext httpContext) =>
{
    if (request is null)
    {
        return Results.BadRequest(new { error = "Payload requerido" });
    }

    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    await using var tx = await connection.BeginTransactionAsync();

    var pacienteId = await UpsertPacienteAsync(connection, tx, request.PersonalData);
    var historiaId = await InsertHistoriaClinicaAsync(connection, tx, pacienteId, request);

    await InsertAntecedentesAsync(connection, tx, historiaId, request.Antecedentes);
    await InsertHabitosAsync(connection, tx, historiaId, request.Habitos);
    await InsertSignosAsync(connection, tx, historiaId, request.SignosVitales);
    await InsertAntropometricosAsync(connection, tx, historiaId, request.DatosAntropometricos);
    await InsertBioquimicosAsync(connection, tx, historiaId, request.ValoresBioquimicos);
    await InsertRecordatorioAsync(connection, tx, historiaId, request.Recordatorio24h);
    await InsertFrecuenciaAsync(connection, tx, historiaId, request.Frequency);

    await tx.CommitAsync();

    return Results.Created($"/api/nutrition/history/{historiaId}", new { status = "created", id = historiaId });
});

app.MapGet("/api/nutrition/pacientes", async (HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    await using var cmd = new NpgsqlCommand(
        @"SELECT id, numero_cedula, nombre, edad_cronologica, sexo, email, telefono, 
                 lugar_residencia, estado_civil, ocupacion, fecha_creacion
          FROM pacientes 
          ORDER BY fecha_creacion DESC",
        connection);

    var pacientes = new List<object>();
    await using var reader = await cmd.ExecuteReaderAsync();
    
    var pacientesData = new List<dynamic>();
    while (await reader.ReadAsync())
    {
        pacientesData.Add(new
        {
            id = reader.GetGuid(0).ToString(),
            numero_cedula = reader.IsDBNull(1) ? null : reader.GetString(1),
            nombre = reader.IsDBNull(2) ? null : reader.GetString(2),
            edad_cronologica = reader.IsDBNull(3) ? null : reader.GetString(3),
            sexo = reader.IsDBNull(4) ? null : reader.GetString(4),
            email = reader.IsDBNull(5) ? null : reader.GetString(5),
            telefono = reader.IsDBNull(6) ? null : reader.GetString(6),
            lugar_residencia = reader.IsDBNull(7) ? null : reader.GetString(7),
            estado_civil = reader.IsDBNull(8) ? null : reader.GetString(8),
            ocupacion = reader.IsDBNull(9) ? null : reader.GetString(9),
            fecha_creacion = reader.GetDateTime(10)
        });
    }
    await reader.CloseAsync();

    // Para cada paciente, obtener sus historias clínicas
    foreach (var paciente in pacientesData)
    {
        await using var cmdHistorias = new NpgsqlCommand(
            @"SELECT id, fecha_consulta 
              FROM historias_clinicas 
              WHERE paciente_id = @pacienteId 
              ORDER BY fecha_consulta DESC",
            connection);
        cmdHistorias.Parameters.AddWithValue("pacienteId", Guid.Parse(paciente.id));

        var historias = new List<object>();
        await using var readerHistorias = await cmdHistorias.ExecuteReaderAsync();
        while (await readerHistorias.ReadAsync())
        {
            historias.Add(new
            {
                id = readerHistorias.GetGuid(0).ToString(),
                fecha_consulta = readerHistorias.IsDBNull(1) ? null : readerHistorias.GetDateTime(1).ToString("yyyy-MM-dd")
            });
        }
        await readerHistorias.CloseAsync();

        pacientes.Add(new
        {
            id = paciente.id,
            numero_cedula = paciente.numero_cedula,
            nombre = paciente.nombre,
            edad_cronologica = paciente.edad_cronologica,
            sexo = paciente.sexo,
            email = paciente.email,
            telefono = paciente.telefono,
            lugar_residencia = paciente.lugar_residencia,
            estado_civil = paciente.estado_civil,
            ocupacion = paciente.ocupacion,
            fecha_creacion = paciente.fecha_creacion,
            historias_clinicas = historias
        });
    }

    return Results.Ok(pacientes);
});

app.MapGet("/api/nutrition/reportes", async (HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    // Total de pacientes
    await using var cmdPacientes = new NpgsqlCommand("SELECT COUNT(*) FROM pacientes", connection);
    var totalPacientes = Convert.ToInt32(await cmdPacientes.ExecuteScalarAsync());

    // Total de historias
    await using var cmdHistorias = new NpgsqlCommand("SELECT COUNT(*) FROM historias_clinicas", connection);
    var totalHistorias = Convert.ToInt32(await cmdHistorias.ExecuteScalarAsync());

    // Historias del último mes
    await using var cmdHistoriasMes = new NpgsqlCommand(
        "SELECT COUNT(*) FROM historias_clinicas WHERE fecha_registro >= NOW() - INTERVAL '30 days'", 
        connection);
    var historiasUltimoMes = Convert.ToInt32(await cmdHistoriasMes.ExecuteScalarAsync());

    // Pacientes por género
    await using var cmdGenero = new NpgsqlCommand(
        @"SELECT 
            LOWER(COALESCE(sexo, 'otro')) as sexo, 
            COUNT(*) as cantidad 
          FROM pacientes 
          GROUP BY LOWER(COALESCE(sexo, 'otro'))",
        connection);

    var generos = new Dictionary<string, int>
    {
        ["masculino"] = 0,
        ["femenino"] = 0
    };

    await using var readerGenero = await cmdGenero.ExecuteReaderAsync();
    while (await readerGenero.ReadAsync())
    {
        var sexo = readerGenero.GetString(0);
        var cantidad = readerGenero.GetInt32(1);
        
        if (sexo.Contains("f") || sexo.Contains("mujer") || sexo.Contains("femenino"))
            generos["femenino"] += cantidad;
        else if (sexo.Contains("m") || sexo.Contains("h") || sexo.Contains("masculino") || sexo.Contains("varon"))
            generos["masculino"] += cantidad;
    }
    await readerGenero.CloseAsync();

    // Total de planes activos
    int totalPlanes = 0;
    try
    {
        await using var cmdPlanes = new NpgsqlCommand(
            "SELECT COUNT(*) FROM planes_nutricionales WHERE activo = true", 
            connection);
        totalPlanes = Convert.ToInt32(await cmdPlanes.ExecuteScalarAsync());
    }
    catch
    {
        // Si la tabla no existe, devolver 0
        totalPlanes = 0;
    }

    var reporte = new
    {
        totalPacientes,
        totalHistorias,
        historiasUltimoMes,
        pacientesPorGenero = generos,
        totalPlanes
    };

    return Results.Ok(reporte);
});

app.MapDelete("/api/nutrition/pacientes/deleteall", async (HttpContext httpContext) =>
{
    // Endpoint temporal - eliminar después del uso
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    var deletedCounts = new Dictionary<string, int>();

    // Eliminar en orden correcto respetando claves foráneas
    await using var cmd1 = new NpgsqlCommand("DELETE FROM frecuencia_consumo", connection);
    deletedCounts["frecuencia_consumo"] = await cmd1.ExecuteNonQueryAsync();

    await using var cmd2 = new NpgsqlCommand("DELETE FROM recordatorio_24h", connection);
    deletedCounts["recordatorio_24h"] = await cmd2.ExecuteNonQueryAsync();

    await using var cmd3 = new NpgsqlCommand("DELETE FROM valores_bioquimicos", connection);
    deletedCounts["valores_bioquimicos"] = await cmd3.ExecuteNonQueryAsync();

    await using var cmd4 = new NpgsqlCommand("DELETE FROM habitos", connection);
    deletedCounts["habitos"] = await cmd4.ExecuteNonQueryAsync();

    await using var cmd5 = new NpgsqlCommand("DELETE FROM antecedentes", connection);
    deletedCounts["antecedentes"] = await cmd5.ExecuteNonQueryAsync();

    await using var cmd6 = new NpgsqlCommand("DELETE FROM signos_vitales", connection);
    deletedCounts["signos_vitales"] = await cmd6.ExecuteNonQueryAsync();

    await using var cmd7 = new NpgsqlCommand("DELETE FROM datos_antropometricos", connection);
    deletedCounts["datos_antropometricos"] = await cmd7.ExecuteNonQueryAsync();

    await using var cmd8 = new NpgsqlCommand("DELETE FROM historias_clinicas", connection);
    deletedCounts["historias_clinicas"] = await cmd8.ExecuteNonQueryAsync();

    await using var cmd9 = new NpgsqlCommand("DELETE FROM pacientes", connection);
    deletedCounts["pacientes"] = await cmd9.ExecuteNonQueryAsync();

    Console.WriteLine("[DB] Todos los pacientes y datos relacionados han sido eliminados");

    return Results.Ok(new { message = "Todos los datos han sido eliminados exitosamente", deletedCounts });
});

app.MapPost("/api/nutrition/ejecutar-sql", async (HttpContext httpContext) =>
{
    // Endpoint temporal - solo para desarrollo
    try
    {
        var request = await httpContext.Request.ReadFromJsonAsync<JsonDocument>();
        if (request == null)
        {
            return Results.BadRequest("No se recibió el SQL");
        }

        var sqlCommand = request.RootElement.GetProperty("sql").GetString();
        if (string.IsNullOrWhiteSpace(sqlCommand))
        {
            return Results.BadRequest("SQL vacío");
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand(sqlCommand, connection);
        var result = await cmd.ExecuteNonQueryAsync();

        Console.WriteLine($"[SQL] Ejecutado: {sqlCommand.Substring(0, Math.Min(100, sqlCommand.Length))}...");
        return Results.Ok(new { message = "SQL ejecutado correctamente", rowsAffected = result });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[SQL ERROR] {ex.Message}");
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/nutrition/pacientes/detallados", async (HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    var reportesDetallados = new List<object>();

    // Obtener todas las historias clínicas con datos del paciente
    await using var cmdHistorias = new NpgsqlCommand(
        @"SELECT 
            hc.id as historia_id,
            hc.fecha_consulta,
            hc.motivo_consulta,
            hc.diagnostico,
            hc.notas_extras,
            hc.fecha_registro,
            p.id as paciente_id,
            p.numero_cedula,
            p.nombre,
            p.edad_cronologica,
            p.sexo,
            p.email,
            p.telefono,
            p.lugar_residencia,
            p.estado_civil,
            p.ocupacion,
            p.fecha_creacion
          FROM historias_clinicas hc
          JOIN pacientes p ON hc.paciente_id = p.id
          ORDER BY p.nombre, hc.fecha_consulta DESC",
        connection);

    await using var readerHistorias = await cmdHistorias.ExecuteReaderAsync();
    var historiasConPaciente = new List<dynamic>();
    
    while (await readerHistorias.ReadAsync())
    {
        historiasConPaciente.Add(new
        {
            historia_id = readerHistorias.GetGuid(0).ToString(),
            fecha_consulta = readerHistorias.IsDBNull(1) ? null : readerHistorias.GetDateTime(1).ToString("yyyy-MM-dd"),
            motivo_consulta = readerHistorias.IsDBNull(2) ? null : readerHistorias.GetString(2),
            diagnostico = readerHistorias.IsDBNull(3) ? null : readerHistorias.GetString(3),
            notas_extras = readerHistorias.IsDBNull(4) ? null : readerHistorias.GetString(4),
            fecha_registro = readerHistorias.GetDateTime(5),
            paciente_id = readerHistorias.GetGuid(6).ToString(),
            numero_cedula = readerHistorias.IsDBNull(7) ? null : readerHistorias.GetString(7),
            nombre = readerHistorias.IsDBNull(8) ? null : readerHistorias.GetString(8),
            edad_cronologica = readerHistorias.IsDBNull(9) ? null : readerHistorias.GetString(9),
            sexo = readerHistorias.IsDBNull(10) ? null : readerHistorias.GetString(10),
            email = readerHistorias.IsDBNull(11) ? null : readerHistorias.GetString(11),
            telefono = readerHistorias.IsDBNull(12) ? null : readerHistorias.GetString(12),
            lugar_residencia = readerHistorias.IsDBNull(13) ? null : readerHistorias.GetString(13),
            estado_civil = readerHistorias.IsDBNull(14) ? null : readerHistorias.GetString(14),
            ocupacion = readerHistorias.IsDBNull(15) ? null : readerHistorias.GetString(15),
            fecha_creacion = readerHistorias.GetDateTime(16)
        });
    }
    await readerHistorias.CloseAsync();

    // Para cada historia clínica, obtener sus datos relacionados
    foreach (var historia in historiasConPaciente)
    {
        var historiaId = historia.historia_id;

        // Datos antropométricos
        object? datosAntropometricos = null;
        await using var cmdAntro = new NpgsqlCommand(
            @"SELECT peso, talla, imc, cintura, cadera, c_brazo, 
                     masa_muscular, gc_porc, gc, gv_porc, edad, sexo,
                     edad_metabolica, kcal_basales, actividad_fisica,
                     pantorrilla, c_muslo, peso_ajustado, factor_actividad_fisica,
                     tiempos_comida
              FROM datos_antropometricos
              WHERE historia_id = @historiaId",
            connection);
        cmdAntro.Parameters.AddWithValue("historiaId", Guid.Parse(historiaId));
        
        await using var readerAntro = await cmdAntro.ExecuteReaderAsync();
        if (await readerAntro.ReadAsync())
        {
            datosAntropometricos = new
            {
                peso = readerAntro.IsDBNull(0) ? null : readerAntro.GetDecimal(0).ToString(),
                talla = readerAntro.IsDBNull(1) ? null : readerAntro.GetDecimal(1).ToString(),
                imc = readerAntro.IsDBNull(2) ? null : readerAntro.GetDecimal(2).ToString(),
                circunferencia_cintura = readerAntro.IsDBNull(3) ? null : readerAntro.GetDecimal(3).ToString(),
                circunferencia_cadera = readerAntro.IsDBNull(4) ? null : readerAntro.GetDecimal(4).ToString(),
                circunferencia_brazo = readerAntro.IsDBNull(5) ? null : readerAntro.GetDecimal(5).ToString(),
                masa_muscular = readerAntro.IsDBNull(6) ? null : readerAntro.GetDecimal(6).ToString(),
                grasa_corporal_porcentaje = readerAntro.IsDBNull(7) ? null : readerAntro.GetDecimal(7).ToString(),
                grasa_corporal = readerAntro.IsDBNull(8) ? null : readerAntro.GetDecimal(8).ToString(),
                grasa_visceral_porcentaje = readerAntro.IsDBNull(9) ? null : readerAntro.GetDecimal(9).ToString(),
                edad = readerAntro.IsDBNull(10) ? null : readerAntro.GetString(10),
                sexo = readerAntro.IsDBNull(11) ? null : readerAntro.GetString(11),
                edad_metabolica = readerAntro.IsDBNull(12) ? null : readerAntro.GetString(12),
                kcal_basales = readerAntro.IsDBNull(13) ? null : readerAntro.GetInt32(13).ToString(),
                actividad_fisica = readerAntro.IsDBNull(14) ? null : readerAntro.GetString(14),
                circunferencia_pantorrilla = readerAntro.IsDBNull(15) ? null : readerAntro.GetDecimal(15).ToString(),
                circunferencia_muslo = readerAntro.IsDBNull(16) ? null : readerAntro.GetDecimal(16).ToString(),
                peso_ajustado = readerAntro.IsDBNull(17) ? null : readerAntro.GetDecimal(17).ToString(),
                factor_actividad_fisica = readerAntro.IsDBNull(18) ? null : readerAntro.GetDecimal(18).ToString(),
                tiempos_comida = readerAntro.IsDBNull(19) ? null : readerAntro.GetString(19)
            };
        }
        await readerAntro.CloseAsync();

        // Signos vitales
        object? signosVitales = null;
        await using var cmdSignos = new NpgsqlCommand(
            @"SELECT pa, fc, fr, temperatura
              FROM signos_vitales
              WHERE historia_id = @historiaId",
            connection);
        cmdSignos.Parameters.AddWithValue("historiaId", Guid.Parse(historiaId));
        
        await using var readerSignos = await cmdSignos.ExecuteReaderAsync();
        if (await readerSignos.ReadAsync())
        {
            signosVitales = new
            {
                presion_arterial = readerSignos.IsDBNull(0) ? null : readerSignos.GetString(0),
                frecuencia_cardiaca = readerSignos.IsDBNull(1) ? null : readerSignos.GetString(1),
                frecuencia_respiratoria = readerSignos.IsDBNull(2) ? null : readerSignos.GetString(2),
                temperatura = readerSignos.IsDBNull(3) ? null : readerSignos.GetString(3)
            };
        }
        await readerSignos.CloseAsync();

        // Antecedentes
        object? antecedentes = null;
        await using var cmdAntecedentes = new NpgsqlCommand(
            @"SELECT apf, app, apq, ago, menarquia, p, g, c, a, alergias
              FROM antecedentes
              WHERE historia_id = @historiaId",
            connection);
        cmdAntecedentes.Parameters.AddWithValue("historiaId", Guid.Parse(historiaId));
        
        await using var readerAntecedentes = await cmdAntecedentes.ExecuteReaderAsync();
        if (await readerAntecedentes.ReadAsync())
        {
            antecedentes = new
            {
                apf = readerAntecedentes.IsDBNull(0) ? null : readerAntecedentes.GetString(0),
                app = readerAntecedentes.IsDBNull(1) ? null : readerAntecedentes.GetString(1),
                apq = readerAntecedentes.IsDBNull(2) ? null : readerAntecedentes.GetString(2),
                ago = readerAntecedentes.IsDBNull(3) ? null : readerAntecedentes.GetString(3),
                menarquia = readerAntecedentes.IsDBNull(4) ? null : readerAntecedentes.GetString(4),
                p = readerAntecedentes.IsDBNull(5) ? null : readerAntecedentes.GetString(5),
                g = readerAntecedentes.IsDBNull(6) ? null : readerAntecedentes.GetString(6),
                c = readerAntecedentes.IsDBNull(7) ? null : readerAntecedentes.GetString(7),
                a = readerAntecedentes.IsDBNull(8) ? null : readerAntecedentes.GetString(8),
                alergias = readerAntecedentes.IsDBNull(9) ? null : readerAntecedentes.GetString(9)
            };
        }
        await readerAntecedentes.CloseAsync();

        // Hábitos
        object? habitos = null;
        await using var cmdHabitos = new NpgsqlCommand(
            @"SELECT fuma, alcohol, cafe, hidratacion, gaseosas, actividad_fisica, 
                     te, edulcorantes, alimentacion
              FROM habitos
              WHERE historia_id = @historiaId",
            connection);
        cmdHabitos.Parameters.AddWithValue("historiaId", Guid.Parse(historiaId));
        
        await using var readerHabitos = await cmdHabitos.ExecuteReaderAsync();
        if (await readerHabitos.ReadAsync())
        {
            habitos = new
            {
                fuma = readerHabitos.IsDBNull(0) ? null : readerHabitos.GetString(0),
                alcohol = readerHabitos.IsDBNull(1) ? null : readerHabitos.GetString(1),
                cafe = readerHabitos.IsDBNull(2) ? null : readerHabitos.GetString(2),
                hidratacion = readerHabitos.IsDBNull(3) ? null : readerHabitos.GetString(3),
                gaseosas = readerHabitos.IsDBNull(4) ? null : readerHabitos.GetString(4),
                actividad_fisica = readerHabitos.IsDBNull(5) ? null : readerHabitos.GetString(5),
                te = readerHabitos.IsDBNull(6) ? null : readerHabitos.GetString(6),
                edulcorantes = readerHabitos.IsDBNull(7) ? null : readerHabitos.GetString(7),
                alimentacion = readerHabitos.IsDBNull(8) ? null : readerHabitos.GetString(8)
            };
        }
        await readerHabitos.CloseAsync();

        // Valores Bioquímicos
        object? valoresBioquimicos = null;
        await using var cmdBioquimicos = new NpgsqlCommand(
            @"SELECT glicemia, colesterol_total, trigliceridos, hdl, ldl, 
                     tgo, tgp, urea, creatinina
              FROM valores_bioquimicos
              WHERE historia_id = @historiaId",
            connection);
        cmdBioquimicos.Parameters.AddWithValue("historiaId", Guid.Parse(historiaId));
        
        await using var readerBioquimicos = await cmdBioquimicos.ExecuteReaderAsync();
        if (await readerBioquimicos.ReadAsync())
        {
            valoresBioquimicos = new
            {
                glicemia = readerBioquimicos.IsDBNull(0) ? null : readerBioquimicos.GetDecimal(0).ToString(),
                colesterol_total = readerBioquimicos.IsDBNull(1) ? null : readerBioquimicos.GetDecimal(1).ToString(),
                trigliceridos = readerBioquimicos.IsDBNull(2) ? null : readerBioquimicos.GetDecimal(2).ToString(),
                hdl = readerBioquimicos.IsDBNull(3) ? null : readerBioquimicos.GetDecimal(3).ToString(),
                ldl = readerBioquimicos.IsDBNull(4) ? null : readerBioquimicos.GetDecimal(4).ToString(),
                tgo = readerBioquimicos.IsDBNull(5) ? null : readerBioquimicos.GetDecimal(5).ToString(),
                tgp = readerBioquimicos.IsDBNull(6) ? null : readerBioquimicos.GetDecimal(6).ToString(),
                urea = readerBioquimicos.IsDBNull(7) ? null : readerBioquimicos.GetDecimal(7).ToString(),
                creatinina = readerBioquimicos.IsDBNull(8) ? null : readerBioquimicos.GetDecimal(8).ToString()
            };
        }
        await readerBioquimicos.CloseAsync();

        // Recordatorio 24h
        object? recordatorio24h = null;
        await using var cmdRecordatorio = new NpgsqlCommand(
            @"SELECT desayuno, snack1, almuerzo, snack2, cena, extras
              FROM recordatorio_24h
              WHERE historia_id = @historiaId",
            connection);
        cmdRecordatorio.Parameters.AddWithValue("historiaId", Guid.Parse(historiaId));
        
        await using var readerRecordatorio = await cmdRecordatorio.ExecuteReaderAsync();
        if (await readerRecordatorio.ReadAsync())
        {
            recordatorio24h = new
            {
                desayuno = readerRecordatorio.IsDBNull(0) ? null : readerRecordatorio.GetString(0),
                snack1 = readerRecordatorio.IsDBNull(1) ? null : readerRecordatorio.GetString(1),
                almuerzo = readerRecordatorio.IsDBNull(2) ? null : readerRecordatorio.GetString(2),
                snack2 = readerRecordatorio.IsDBNull(3) ? null : readerRecordatorio.GetString(3),
                cena = readerRecordatorio.IsDBNull(4) ? null : readerRecordatorio.GetString(4),
                extras = readerRecordatorio.IsDBNull(5) ? null : readerRecordatorio.GetString(5)
            };
        }
        await readerRecordatorio.CloseAsync();

        // Frecuencia de consumo
        var frecuenciaConsumo = new List<object>();
        await using var cmdFrecuencia = new NpgsqlCommand(
            @"SELECT categoria, alimento, frecuencia
              FROM frecuencia_consumo
              WHERE historia_id = @historiaId
              ORDER BY categoria, alimento",
            connection);
        cmdFrecuencia.Parameters.AddWithValue("historiaId", Guid.Parse(historiaId));
        
        await using var readerFrecuencia = await cmdFrecuencia.ExecuteReaderAsync();
        while (await readerFrecuencia.ReadAsync())
        {
            frecuenciaConsumo.Add(new
            {
                categoria = readerFrecuencia.IsDBNull(0) ? null : readerFrecuencia.GetString(0),
                alimento = readerFrecuencia.IsDBNull(1) ? null : readerFrecuencia.GetString(1),
                frecuencia = readerFrecuencia.IsDBNull(2) ? null : readerFrecuencia.GetString(2)
            });
        }
        await readerFrecuencia.CloseAsync();

        // Crear un reporte por cada historia clínica (paciente + fecha)
        reportesDetallados.Add(new
        {
            // Datos del paciente
            paciente_id = historia.paciente_id,
            numero_cedula = historia.numero_cedula,
            nombre = historia.nombre,
            edad_cronologica = historia.edad_cronologica,
            sexo = historia.sexo,
            email = historia.email,
            telefono = historia.telefono,
            lugar_residencia = historia.lugar_residencia,
            estado_civil = historia.estado_civil,
            ocupacion = historia.ocupacion,
            fecha_creacion = historia.fecha_creacion,
            // Datos de la historia clínica
            historia_id = historia.historia_id,
            fecha_consulta = historia.fecha_consulta,
            motivo_consulta = historia.motivo_consulta,
            diagnostico = historia.diagnostico,
            notas_extras = historia.notas_extras,
            fecha_registro = historia.fecha_registro,
            // Datos relacionados con esta historia específica
            datos_antropometricos = datosAntropometricos,
            signos_vitales = signosVitales,
            antecedentes = antecedentes,
            habitos = habitos,
            valores_bioquimicos = valoresBioquimicos,
            recordatorio_24h = recordatorio24h,
            frecuencia_consumo = frecuenciaConsumo
        });
    }

    return Results.Ok(reportesDetallados);
});

// Obtener datos completos de un paciente específico para edición
app.MapGet("/api/nutrition/pacientes/{id}", async (string id, HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    if (!Guid.TryParse(id, out var pacienteGuid))
    {
        return Results.BadRequest(new { error = "ID de paciente inválido" });
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    // Obtener datos del paciente
    await using var cmdPaciente = new NpgsqlCommand(
        @"SELECT numero_cedula, nombre, edad_cronologica, sexo, email, telefono, 
                 lugar_residencia, estado_civil, ocupacion
          FROM pacientes 
          WHERE id = @id",
        connection);
    cmdPaciente.Parameters.AddWithValue("id", pacienteGuid);

    await using var reader = await cmdPaciente.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
    {
        return Results.NotFound(new { error = "Paciente no encontrado" });
    }

    var paciente = new
    {
        id = id,
        numero_cedula = reader.IsDBNull(0) ? null : reader.GetString(0),
        nombre = reader.IsDBNull(1) ? null : reader.GetString(1),
        edad_cronologica = reader.IsDBNull(2) ? null : reader.GetString(2),
        sexo = reader.IsDBNull(3) ? null : reader.GetString(3),
        email = reader.IsDBNull(4) ? null : reader.GetString(4),
        telefono = reader.IsDBNull(5) ? null : reader.GetString(5),
        lugar_residencia = reader.IsDBNull(6) ? null : reader.GetString(6),
        estado_civil = reader.IsDBNull(7) ? null : reader.GetString(7),
        ocupacion = reader.IsDBNull(8) ? null : reader.GetString(8)
    };
    await reader.CloseAsync();

    return Results.Ok(paciente);
});

// Buscar paciente por número de cédula
app.MapGet("/api/nutrition/pacientes/buscar/cedula/{cedula}", async (string cedula, HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(cedula))
    {
        return Results.BadRequest(new { error = "Número de cédula requerido" });
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    await using var cmd = new NpgsqlCommand(
        @"SELECT id, numero_cedula, nombre, edad_cronologica, sexo, email, telefono, 
                 lugar_residencia, estado_civil, ocupacion
          FROM pacientes 
          WHERE numero_cedula = @cedula
          LIMIT 1",
        connection);
    cmd.Parameters.AddWithValue("cedula", cedula.Trim());

    await using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
    {
        return Results.NotFound(new { error = "Paciente no encontrado" });
    }

    var paciente = new
    {
        id = reader.GetGuid(0).ToString(),
        numero_cedula = reader.IsDBNull(1) ? null : reader.GetString(1),
        nombre = reader.IsDBNull(2) ? null : reader.GetString(2),
        edad_cronologica = reader.IsDBNull(3) ? null : reader.GetString(3),
        sexo = reader.IsDBNull(4) ? null : reader.GetString(4),
        email = reader.IsDBNull(5) ? null : reader.GetString(5),
        telefono = reader.IsDBNull(6) ? null : reader.GetString(6),
        lugar_residencia = reader.IsDBNull(7) ? null : reader.GetString(7),
        estado_civil = reader.IsDBNull(8) ? null : reader.GetString(8),
        ocupacion = reader.IsDBNull(9) ? null : reader.GetString(9)
    };

    return Results.Ok(paciente);
});

// Buscar paciente por cédula con su última historia clínica completa
app.MapGet("/api/nutrition/pacientes/buscar/cedula/{cedula}/ultima-historia", async (string cedula, HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(cedula))
    {
        return Results.BadRequest(new { error = "Número de cédula requerido" });
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    // Buscar paciente
    await using var cmdPaciente = new NpgsqlCommand(
        @"SELECT id, numero_cedula, nombre, edad_cronologica, sexo, email, telefono, 
                 lugar_residencia, estado_civil, ocupacion
          FROM pacientes 
          WHERE numero_cedula = @cedula
          LIMIT 1",
        connection);
    cmdPaciente.Parameters.AddWithValue("cedula", cedula.Trim());

    await using var readerPaciente = await cmdPaciente.ExecuteReaderAsync();
    if (!await readerPaciente.ReadAsync())
    {
        return Results.NotFound(new { error = "Paciente no encontrado" });
    }

    var pacienteId = readerPaciente.GetGuid(0);
    var pacienteData = new
    {
        id = pacienteId.ToString(),
        numero_cedula = readerPaciente.IsDBNull(1) ? null : readerPaciente.GetString(1),
        nombre = readerPaciente.IsDBNull(2) ? null : readerPaciente.GetString(2),
        edad_cronologica = readerPaciente.IsDBNull(3) ? null : readerPaciente.GetString(3),
        sexo = readerPaciente.IsDBNull(4) ? null : readerPaciente.GetString(4),
        email = readerPaciente.IsDBNull(5) ? null : readerPaciente.GetString(5),
        telefono = readerPaciente.IsDBNull(6) ? null : readerPaciente.GetString(6),
        lugar_residencia = readerPaciente.IsDBNull(7) ? null : readerPaciente.GetString(7),
        estado_civil = readerPaciente.IsDBNull(8) ? null : readerPaciente.GetString(8),
        ocupacion = readerPaciente.IsDBNull(9) ? null : readerPaciente.GetString(9)
    };
    await readerPaciente.CloseAsync();

    // Buscar la última historia clínica
    await using var cmdHistoria = new NpgsqlCommand(
        @"SELECT id FROM historias_clinicas 
          WHERE paciente_id = @pacienteId 
          ORDER BY fecha_consulta DESC, fecha_registro DESC 
          LIMIT 1",
        connection);
    cmdHistoria.Parameters.AddWithValue("pacienteId", pacienteId);

    var historiaId = await cmdHistoria.ExecuteScalarAsync() as Guid?;
    
    if (!historiaId.HasValue)
    {
        // No hay historial previo, solo devolver datos del paciente
        return Results.Ok(new { paciente = pacienteData, ultima_historia = (object?)null });
    }

    // Obtener datos de la última historia
    object? antecedentes = null;
    await using (var cmdAnt = new NpgsqlCommand(
        "SELECT apf, app, apq, ago, menarquia, p, g, c, a, alergias FROM antecedentes WHERE historia_id = @historiaId",
        connection))
    {
        cmdAnt.Parameters.AddWithValue("historiaId", historiaId.Value);
        await using var reader = await cmdAnt.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            antecedentes = new
            {
                apf = reader.IsDBNull(0) ? null : reader.GetString(0),
                app = reader.IsDBNull(1) ? null : reader.GetString(1),
                apq = reader.IsDBNull(2) ? null : reader.GetString(2),
                ago = reader.IsDBNull(3) ? null : reader.GetString(3),
                menarquia = reader.IsDBNull(4) ? null : reader.GetString(4),
                p = reader.IsDBNull(5) ? null : reader.GetString(5),
                g = reader.IsDBNull(6) ? null : reader.GetString(6),
                c = reader.IsDBNull(7) ? null : reader.GetString(7),
                a = reader.IsDBNull(8) ? null : reader.GetString(8),
                alergias = reader.IsDBNull(9) ? null : reader.GetString(9)
            };
        }
    }

    object? habitos = null;
    await using (var cmdHab = new NpgsqlCommand(
        "SELECT fuma, alcohol, cafe, hidratacion, gaseosas, actividad_fisica, te, edulcorantes, alimentacion FROM habitos WHERE historia_id = @historiaId",
        connection))
    {
        cmdHab.Parameters.AddWithValue("historiaId", historiaId.Value);
        await using var reader = await cmdHab.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            habitos = new
            {
                fuma = reader.IsDBNull(0) ? null : reader.GetString(0),
                alcohol = reader.IsDBNull(1) ? null : reader.GetString(1),
                cafe = reader.IsDBNull(2) ? null : reader.GetString(2),
                hidratacion = reader.IsDBNull(3) ? null : reader.GetString(3),
                gaseosas = reader.IsDBNull(4) ? null : reader.GetString(4),
                actividad_fisica = reader.IsDBNull(5) ? null : reader.GetString(5),
                te = reader.IsDBNull(6) ? null : reader.GetString(6),
                edulcorantes = reader.IsDBNull(7) ? null : reader.GetString(7),
                alimentacion = reader.IsDBNull(8) ? null : reader.GetString(8)
            };
        }
    }

    object? valoresBioquimicos = null;
    await using (var cmdBio = new NpgsqlCommand(
        "SELECT glicemia, colesterol_total, trigliceridos, hdl, ldl, tgo, tgp, urea, creatinina FROM valores_bioquimicos WHERE historia_id = @historiaId",
        connection))
    {
        cmdBio.Parameters.AddWithValue("historiaId", historiaId.Value);
        await using var reader = await cmdBio.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            valoresBioquimicos = new
            {
                glicemia = reader.IsDBNull(0) ? null : reader.GetDecimal(0).ToString(),
                colesterol_total = reader.IsDBNull(1) ? null : reader.GetDecimal(1).ToString(),
                trigliceridos = reader.IsDBNull(2) ? null : reader.GetDecimal(2).ToString(),
                hdl = reader.IsDBNull(3) ? null : reader.GetDecimal(3).ToString(),
                ldl = reader.IsDBNull(4) ? null : reader.GetDecimal(4).ToString(),
                tgo = reader.IsDBNull(5) ? null : reader.GetDecimal(5).ToString(),
                tgp = reader.IsDBNull(6) ? null : reader.GetDecimal(6).ToString(),
                urea = reader.IsDBNull(7) ? null : reader.GetDecimal(7).ToString(),
                creatinina = reader.IsDBNull(8) ? null : reader.GetDecimal(8).ToString()
            };
        }
    }

    object? recordatorio24h = null;
    await using (var cmdRec = new NpgsqlCommand(
        "SELECT desayuno, snack1, almuerzo, snack2, cena, extras FROM recordatorio_24h WHERE historia_id = @historiaId",
        connection))
    {
        cmdRec.Parameters.AddWithValue("historiaId", historiaId.Value);
        await using var reader = await cmdRec.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            recordatorio24h = new
            {
                desayuno = reader.IsDBNull(0) ? null : reader.GetString(0),
                snack1 = reader.IsDBNull(1) ? null : reader.GetString(1),
                almuerzo = reader.IsDBNull(2) ? null : reader.GetString(2),
                snack2 = reader.IsDBNull(3) ? null : reader.GetString(3),
                cena = reader.IsDBNull(4) ? null : reader.GetString(4),
                extras = reader.IsDBNull(5) ? null : reader.GetString(5)
            };
        }
    }

    return Results.Ok(new
    {
        paciente = pacienteData,
        ultima_historia = new
        {
            antecedentes,
            habitos,
            valores_bioquimicos = valoresBioquimicos,
            recordatorio_24h = recordatorio24h
        }
    });
});

// Obtener datos completos de una historia clínica específica para edición
app.MapGet("/api/nutrition/historias/{id}", async (string id, HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    if (!Guid.TryParse(id, out var historiaGuid))
    {
        return Results.BadRequest(new { error = "ID de historia inválido" });
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    // Obtener datos básicos de la historia
    await using var cmdHistoria = new NpgsqlCommand(
        @"SELECT hc.paciente_id, hc.fecha_consulta, hc.motivo_consulta, hc.diagnostico, hc.notas_extras,
                 p.numero_cedula, p.nombre, p.edad_cronologica, p.sexo, p.email, p.telefono,
                 p.lugar_residencia, p.estado_civil, p.ocupacion
          FROM historias_clinicas hc
          JOIN pacientes p ON hc.paciente_id = p.id
          WHERE hc.id = @id",
        connection);
    cmdHistoria.Parameters.AddWithValue("id", historiaGuid);

    await using var reader = await cmdHistoria.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
    {
        return Results.NotFound(new { error = "Historia clínica no encontrada" });
    }

    var historiaData = new Dictionary<string, object?>
    {
        ["id"] = id,
        ["paciente_id"] = reader.GetGuid(0).ToString(),
        ["fecha_consulta"] = reader.IsDBNull(1) ? null : reader.GetDateTime(1).ToString("yyyy-MM-dd"),
        ["motivo_consulta"] = reader.IsDBNull(2) ? null : reader.GetString(2),
        ["diagnostico"] = reader.IsDBNull(3) ? null : reader.GetString(3),
        ["notas_extras"] = reader.IsDBNull(4) ? null : reader.GetString(4),
        ["personal_data"] = new
        {
            numero_cedula = reader.IsDBNull(5) ? null : reader.GetString(5),
            nombre = reader.IsDBNull(6) ? null : reader.GetString(6),
            edad_cronologica = reader.IsDBNull(7) ? null : reader.GetString(7),
            sexo = reader.IsDBNull(8) ? null : reader.GetString(8),
            email = reader.IsDBNull(9) ? null : reader.GetString(9),
            telefono = reader.IsDBNull(10) ? null : reader.GetString(10),
            lugar_residencia = reader.IsDBNull(11) ? null : reader.GetString(11),
            estado_civil = reader.IsDBNull(12) ? null : reader.GetString(12),
            ocupacion = reader.IsDBNull(13) ? null : reader.GetString(13)
        }
    };
    await reader.CloseAsync();

    // Obtener datos antropométricos
    await using var cmdAntro = new NpgsqlCommand(
        @"SELECT peso, talla, imc, cintura, cadera, c_brazo, masa_muscular, gc_porc, gc, gv_porc, 
                 edad, sexo, edad_metabolica, kcal_basales, actividad_fisica, pantorrilla, c_muslo, 
                 peso_ajustado, factor_actividad_fisica, tiempos_comida 
          FROM datos_antropometricos WHERE historia_id = @id",
        connection);
    cmdAntro.Parameters.AddWithValue("id", historiaGuid);
    await using var readerAntro = await cmdAntro.ExecuteReaderAsync();
    if (await readerAntro.ReadAsync())
    {
        historiaData["datos_antropometricos"] = new
        {
            peso = readerAntro.IsDBNull(0) ? null : readerAntro.GetDecimal(0).ToString(),
            talla = readerAntro.IsDBNull(1) ? null : readerAntro.GetDecimal(1).ToString(),
            imc = readerAntro.IsDBNull(2) ? null : readerAntro.GetDecimal(2).ToString(),
            circunferencia_cintura = readerAntro.IsDBNull(3) ? null : readerAntro.GetDecimal(3).ToString(),
            circunferencia_cadera = readerAntro.IsDBNull(4) ? null : readerAntro.GetDecimal(4).ToString(),
            circunferencia_brazo = readerAntro.IsDBNull(5) ? null : readerAntro.GetDecimal(5).ToString(),
            masa_muscular = readerAntro.IsDBNull(6) ? null : readerAntro.GetDecimal(6).ToString(),
            grasa_corporal_porcentaje = readerAntro.IsDBNull(7) ? null : readerAntro.GetDecimal(7).ToString(),
            grasa_corporal = readerAntro.IsDBNull(8) ? null : readerAntro.GetDecimal(8).ToString(),
            grasa_visceral_porcentaje = readerAntro.IsDBNull(9) ? null : readerAntro.GetDecimal(9).ToString(),
            edad = readerAntro.IsDBNull(10) ? null : readerAntro.GetString(10),
            sexo = readerAntro.IsDBNull(11) ? null : readerAntro.GetString(11),
            edad_metabolica = readerAntro.IsDBNull(12) ? null : readerAntro.GetString(12),
            kcal_basales = readerAntro.IsDBNull(13) ? null : readerAntro.GetInt32(13).ToString(),
            actividad_fisica = readerAntro.IsDBNull(14) ? null : readerAntro.GetString(14),
            circunferencia_pantorrilla = readerAntro.IsDBNull(15) ? null : readerAntro.GetDecimal(15).ToString(),
            circunferencia_muslo = readerAntro.IsDBNull(16) ? null : readerAntro.GetDecimal(16).ToString(),
            peso_ajustado = readerAntro.IsDBNull(17) ? null : readerAntro.GetDecimal(17).ToString(),
            factor_actividad_fisica = readerAntro.IsDBNull(18) ? null : readerAntro.GetDecimal(18).ToString(),
            tiempos_comida = readerAntro.IsDBNull(19) ? null : readerAntro.GetString(19)
        };
    }
    await readerAntro.CloseAsync();

    // Obtener signos vitales
    await using var cmdSignos = new NpgsqlCommand(
        "SELECT pa, fc, fr, temperatura FROM signos_vitales WHERE historia_id = @id",
        connection);
    cmdSignos.Parameters.AddWithValue("id", historiaGuid);
    await using var readerSignos = await cmdSignos.ExecuteReaderAsync();
    if (await readerSignos.ReadAsync())
    {
        historiaData["signos_vitales"] = new
        {
            presion_arterial = readerSignos.IsDBNull(0) ? null : readerSignos.GetString(0),
            frecuencia_cardiaca = readerSignos.IsDBNull(1) ? null : readerSignos.GetString(1),
            frecuencia_respiratoria = readerSignos.IsDBNull(2) ? null : readerSignos.GetString(2),
            temperatura = readerSignos.IsDBNull(3) ? null : readerSignos.GetString(3)
        };
    }
    await readerSignos.CloseAsync();

    // Obtener antecedentes
    await using var cmdAnt = new NpgsqlCommand(
        "SELECT apf, app, apq, ago, menarquia, p, g, c, a, alergias FROM antecedentes WHERE historia_id = @id",
        connection);
    cmdAnt.Parameters.AddWithValue("id", historiaGuid);
    await using var readerAnt = await cmdAnt.ExecuteReaderAsync();
    if (await readerAnt.ReadAsync())
    {
        historiaData["antecedentes"] = new
        {
            apf = readerAnt.IsDBNull(0) ? null : readerAnt.GetString(0),
            app = readerAnt.IsDBNull(1) ? null : readerAnt.GetString(1),
            apq = readerAnt.IsDBNull(2) ? null : readerAnt.GetString(2),
            ago = readerAnt.IsDBNull(3) ? null : readerAnt.GetString(3),
            menarquia = readerAnt.IsDBNull(4) ? null : readerAnt.GetString(4),
            p = readerAnt.IsDBNull(5) ? null : readerAnt.GetString(5),
            g = readerAnt.IsDBNull(6) ? null : readerAnt.GetString(6),
            c = readerAnt.IsDBNull(7) ? null : readerAnt.GetString(7),
            a = readerAnt.IsDBNull(8) ? null : readerAnt.GetString(8),
            alergias = readerAnt.IsDBNull(9) ? null : readerAnt.GetString(9)
        };
    }
    await readerAnt.CloseAsync();

    // Obtener hábitos
    await using var cmdHabitos = new NpgsqlCommand(
        "SELECT fuma, alcohol, cafe, hidratacion, gaseosas, actividad_fisica, te, edulcorantes, alimentacion FROM habitos WHERE historia_id = @id",
        connection);
    cmdHabitos.Parameters.AddWithValue("id", historiaGuid);
    await using var readerHabitos = await cmdHabitos.ExecuteReaderAsync();
    if (await readerHabitos.ReadAsync())
    {
        historiaData["habitos"] = new
        {
            fuma = readerHabitos.IsDBNull(0) ? null : readerHabitos.GetString(0),
            alcohol = readerHabitos.IsDBNull(1) ? null : readerHabitos.GetString(1),
            cafe = readerHabitos.IsDBNull(2) ? null : readerHabitos.GetString(2),
            hidratacion = readerHabitos.IsDBNull(3) ? null : readerHabitos.GetString(3),
            gaseosas = readerHabitos.IsDBNull(4) ? null : readerHabitos.GetString(4),
            actividad_fisica = readerHabitos.IsDBNull(5) ? null : readerHabitos.GetString(5),
            te = readerHabitos.IsDBNull(6) ? null : readerHabitos.GetString(6),
            edulcorantes = readerHabitos.IsDBNull(7) ? null : readerHabitos.GetString(7),
            alimentacion = readerHabitos.IsDBNull(8) ? null : readerHabitos.GetString(8)
        };
    }
    await readerHabitos.CloseAsync();

    // Obtener valores bioquímicos
    await using var cmdBio = new NpgsqlCommand(
        "SELECT glicemia, colesterol_total, trigliceridos, hdl, ldl, tgo, tgp, urea, creatinina FROM valores_bioquimicos WHERE historia_id = @id",
        connection);
    cmdBio.Parameters.AddWithValue("id", historiaGuid);
    await using var readerBio = await cmdBio.ExecuteReaderAsync();
    if (await readerBio.ReadAsync())
    {
        historiaData["valores_bioquimicos"] = new
        {
            glicemia = readerBio.IsDBNull(0) ? null : readerBio.GetDecimal(0).ToString(),
            colesterol_total = readerBio.IsDBNull(1) ? null : readerBio.GetDecimal(1).ToString(),
            trigliceridos = readerBio.IsDBNull(2) ? null : readerBio.GetDecimal(2).ToString(),
            hdl = readerBio.IsDBNull(3) ? null : readerBio.GetDecimal(3).ToString(),
            ldl = readerBio.IsDBNull(4) ? null : readerBio.GetDecimal(4).ToString(),
            tgo = readerBio.IsDBNull(5) ? null : readerBio.GetDecimal(5).ToString(),
            tgp = readerBio.IsDBNull(6) ? null : readerBio.GetDecimal(6).ToString(),
            urea = readerBio.IsDBNull(7) ? null : readerBio.GetDecimal(7).ToString(),
            creatinina = readerBio.IsDBNull(8) ? null : readerBio.GetDecimal(8).ToString()
        };
    }
    await readerBio.CloseAsync();

    // Obtener recordatorio 24h
    await using var cmdRec = new NpgsqlCommand(
        "SELECT desayuno, snack1, almuerzo, snack2, cena, extras FROM recordatorio_24h WHERE historia_id = @id",
        connection);
    cmdRec.Parameters.AddWithValue("id", historiaGuid);
    await using var readerRec = await cmdRec.ExecuteReaderAsync();
    if (await readerRec.ReadAsync())
    {
        historiaData["recordatorio_24h"] = new
        {
            desayuno = readerRec.IsDBNull(0) ? null : readerRec.GetString(0),
            snack1 = readerRec.IsDBNull(1) ? null : readerRec.GetString(1),
            almuerzo = readerRec.IsDBNull(2) ? null : readerRec.GetString(2),
            snack2 = readerRec.IsDBNull(3) ? null : readerRec.GetString(3),
            cena = readerRec.IsDBNull(4) ? null : readerRec.GetString(4),
            extras = readerRec.IsDBNull(5) ? null : readerRec.GetString(5)
        };
    }
    await readerRec.CloseAsync();

    // Obtener frecuencia de consumo
    var frecuencias = new List<object>();
    await using var cmdFreq = new NpgsqlCommand(
        "SELECT categoria, alimento, frecuencia FROM frecuencia_consumo WHERE historia_id = @id ORDER BY categoria, alimento",
        connection);
    cmdFreq.Parameters.AddWithValue("id", historiaGuid);
    await using var readerFreq = await cmdFreq.ExecuteReaderAsync();
    while (await readerFreq.ReadAsync())
    {
        frecuencias.Add(new
        {
            categoria = readerFreq.GetString(0),
            alimento = readerFreq.GetString(1),
            frecuencia = readerFreq.GetString(2)
        });
    }
    await readerFreq.CloseAsync();
    historiaData["frecuencia_consumo"] = frecuencias;

    return Results.Ok(historiaData);
});

// Actualizar datos del paciente
app.MapPut("/api/nutrition/pacientes/{id}", async (string id, UpdatePacienteRequest data, HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    if (!Guid.TryParse(id, out var pacienteGuid))
    {
        return Results.BadRequest(new { error = "ID de paciente inválido" });
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    await using var cmd = new NpgsqlCommand(
        @"UPDATE pacientes 
          SET numero_cedula = @numeroCedula,
              nombre = @nombre,
              edad_cronologica = @edadCronologica,
              sexo = @sexo,
              email = @email,
              telefono = @telefono,
              lugar_residencia = @lugarResidencia,
              estado_civil = @estadoCivil,
              ocupacion = @ocupacion
          WHERE id = @id",
        connection);

    cmd.Parameters.AddWithValue("id", pacienteGuid);
    cmd.Parameters.AddWithValue("numeroCedula", data.NumeroCedula ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("nombre", data.Nombre ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("edadCronologica", data.EdadCronologica ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("sexo", data.Sexo ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("email", data.Email ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("telefono", data.Telefono ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("lugarResidencia", data.LugarResidencia ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("estadoCivil", data.EstadoCivil ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("ocupacion", data.Ocupacion ?? (object)DBNull.Value);

    var rowsAffected = await cmd.ExecuteNonQueryAsync();

    if (rowsAffected == 0)
    {
        return Results.NotFound(new { error = "Paciente no encontrado" });
    }

    return Results.Ok(new { success = true, message = "Paciente actualizado correctamente" });
});

// Actualizar historia clínica completa
app.MapPut("/api/nutrition/historias/{id}", async (string id, HistoryRequest request, HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    if (!Guid.TryParse(id, out var historiaGuid))
    {
        return Results.BadRequest(new { error = "ID de historia inválido" });
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var transaction = await connection.BeginTransactionAsync();

    try
    {
        // Primero, obtener el paciente_id de la historia clínica
        Guid pacienteId;
        await using (var cmdGetPaciente = new NpgsqlCommand(
            "SELECT paciente_id FROM historias_clinicas WHERE id = @id",
            connection, transaction))
        {
            cmdGetPaciente.Parameters.AddWithValue("id", historiaGuid);
            var result = await cmdGetPaciente.ExecuteScalarAsync();
            if (result == null)
            {
                await transaction.RollbackAsync();
                return Results.NotFound(new { error = "Historia clínica no encontrada" });
            }
            pacienteId = (Guid)result;
        }

        // Actualizar datos del paciente si están presentes
        if (request.PersonalData != null)
        {
            await using var cmdPaciente = new NpgsqlCommand(
                @"UPDATE pacientes 
                  SET nombre = @nombre,
                      edad_cronologica = @edadCronologica,
                      sexo = @sexo,
                      lugar_residencia = @lugarResidencia,
                      estado_civil = @estadoCivil,
                      telefono = @telefono,
                      ocupacion = @ocupacion,
                      email = @email,
                      fecha_actualizacion = NOW()
                  WHERE id = @id",
                connection, transaction);

            cmdPaciente.Parameters.AddWithValue("id", pacienteId);
            cmdPaciente.Parameters.AddWithValue("nombre", request.PersonalData.Nombre ?? (object)DBNull.Value);
            cmdPaciente.Parameters.AddWithValue("edadCronologica", request.PersonalData.EdadCronologica ?? (object)DBNull.Value);
            cmdPaciente.Parameters.AddWithValue("sexo", request.PersonalData.Sexo ?? (object)DBNull.Value);
            cmdPaciente.Parameters.AddWithValue("lugarResidencia", request.PersonalData.LugarResidencia ?? (object)DBNull.Value);
            cmdPaciente.Parameters.AddWithValue("estadoCivil", request.PersonalData.EstadoCivil ?? (object)DBNull.Value);
            cmdPaciente.Parameters.AddWithValue("telefono", request.PersonalData.Telefono ?? (object)DBNull.Value);
            cmdPaciente.Parameters.AddWithValue("ocupacion", request.PersonalData.Ocupacion ?? (object)DBNull.Value);
            cmdPaciente.Parameters.AddWithValue("email", request.PersonalData.Email ?? (object)DBNull.Value);

            await cmdPaciente.ExecuteNonQueryAsync();
        }

        // Actualizar historia clínica básica
        await using var cmdHistoria = new NpgsqlCommand(
            @"UPDATE historias_clinicas 
              SET fecha_consulta = @fechaConsulta,
                  motivo_consulta = @motivoConsulta,
                  diagnostico = @diagnostico,
                  notas_extras = @notasExtras
              WHERE id = @id",
            connection, transaction);

        cmdHistoria.Parameters.AddWithValue("id", historiaGuid);
        cmdHistoria.Parameters.AddWithValue("fechaConsulta", NpgsqlDbType.Date, 
            string.IsNullOrWhiteSpace(request.PersonalData?.FechaConsulta) ? DBNull.Value : DateTime.Parse(request.PersonalData.FechaConsulta));
        cmdHistoria.Parameters.AddWithValue("motivoConsulta", request.MotivoConsulta ?? (object)DBNull.Value);
        cmdHistoria.Parameters.AddWithValue("diagnostico", request.Diagnostico ?? (object)DBNull.Value);
        cmdHistoria.Parameters.AddWithValue("notasExtras", request.NotasExtras ?? (object)DBNull.Value);

        await cmdHistoria.ExecuteNonQueryAsync();

        // Actualizar o insertar datos antropométricos
        var datoAntro = request.DatosAntropometricos;
        if (datoAntro != null)
        {
            await using var cmdCheckAntro = new NpgsqlCommand(
                "SELECT COUNT(*) FROM datos_antropometricos WHERE historia_id = @historiaId",
                connection, transaction);
            cmdCheckAntro.Parameters.AddWithValue("historiaId", historiaGuid);
            var existeAntro = Convert.ToInt32(await cmdCheckAntro.ExecuteScalarAsync()) > 0;

            if (existeAntro)
            {
                await using var cmdUpdateAntro = new NpgsqlCommand(
                    @"UPDATE datos_antropometricos 
                      SET peso = @peso, talla = @talla, imc = @imc, cintura = @cintura, cadera = @cadera,
                          c_brazo = @cBrazo, masa_muscular = @masaMuscular, gc_porc = @gcPorc, gc = @gc, gv_porc = @gvPorc,
                          edad = @edad, sexo = @sexo, edad_metabolica = @edadMet, kcal_basales = @kcal,
                          actividad_fisica = @actividad, pantorrilla = @pantorrilla, c_muslo = @cMuslo,
                          peso_ajustado = @pesoAjustado, factor_actividad_fisica = @factor, tiempos_comida = @tiempos
                      WHERE historia_id = @historiaId",
                    connection, transaction);
                
                cmdUpdateAntro.Parameters.AddWithValue("historiaId", historiaGuid);
                cmdUpdateAntro.Parameters.AddWithValue("peso", ParseDecimalOrNull(datoAntro.Peso) ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("talla", ParseDecimalOrNull(datoAntro.Talla) ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("imc", ParseDecimalOrNull(datoAntro.IMC) ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("cintura", ParseDecimalOrNull(datoAntro.CircunferenciaCintura) ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("cadera", ParseDecimalOrNull(datoAntro.CircunferenciaCadera) ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("cBrazo", ParseDecimalOrNull(datoAntro.CircunferenciaBrazo) ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("masaMuscular", ParseDecimalOrNull(datoAntro.MasaMuscular) ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("gcPorc", ParseDecimalOrNull(datoAntro.GrasaCorporalPorcentaje) ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("gc", ParseDecimalOrNull(datoAntro.GrasaCorporal) ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("gvPorc", ParseDecimalOrNull(datoAntro.GrasaVisceralPorcentaje) ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("edad", datoAntro.Edad ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("sexo", datoAntro.Sexo ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("edadMet", datoAntro.EdadMetabolica ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("kcal", ParseIntOrNull(datoAntro.KcalBasales) ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("actividad", datoAntro.ActividadFisica ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("pantorrilla", ParseDecimalOrNull(datoAntro.Pantorrilla) ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("cMuslo", ParseDecimalOrNull(datoAntro.CircunferenciaMuslo) ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("pesoAjustado", ParseDecimalOrNull(datoAntro.PesoAjustado) ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("factor", ParseDecimalOrNull(datoAntro.FactorActividadFisica) ?? (object)DBNull.Value);
                cmdUpdateAntro.Parameters.AddWithValue("tiempos", datoAntro.TiemposComida ?? (object)DBNull.Value);

                await cmdUpdateAntro.ExecuteNonQueryAsync();
            }
            else
            {
                // Insertar nuevos datos antropométricos
                await InsertAntropometricosAsync(connection, transaction, historiaGuid, datoAntro);
            }
        }

        // Actualizar o insertar signos vitales
        var signosVitales = request.SignosVitales;
        if (signosVitales != null)
        {
            await using var cmdCheckSignos = new NpgsqlCommand(
                "SELECT COUNT(*) FROM signos_vitales WHERE historia_id = @historiaId",
                connection, transaction);
            cmdCheckSignos.Parameters.AddWithValue("historiaId", historiaGuid);
            var existeSignos = Convert.ToInt32(await cmdCheckSignos.ExecuteScalarAsync()) > 0;

            if (existeSignos)
            {
                await using var cmdUpdateSignos = new NpgsqlCommand(
                    @"UPDATE signos_vitales 
                      SET pa = @pa, fc = @fc, fr = @fr, temperatura = @temperatura
                      WHERE historia_id = @historiaId",
                    connection, transaction);
                
                cmdUpdateSignos.Parameters.AddWithValue("historiaId", historiaGuid);
                cmdUpdateSignos.Parameters.AddWithValue("pa", signosVitales.PresionArterial ?? (object)DBNull.Value);
                cmdUpdateSignos.Parameters.AddWithValue("fc", signosVitales.FrecuenciaCardiaca ?? (object)DBNull.Value);
                cmdUpdateSignos.Parameters.AddWithValue("fr", signosVitales.FrecuenciaRespiratoria ?? (object)DBNull.Value);
                cmdUpdateSignos.Parameters.AddWithValue("temperatura", signosVitales.Temperatura ?? (object)DBNull.Value);

                await cmdUpdateSignos.ExecuteNonQueryAsync();
            }
            else
            {
                // Insertar nuevos signos vitales
                await InsertSignosAsync(connection, transaction, historiaGuid, signosVitales);
            }
        }

        // Actualizar o insertar antecedentes
        var antecedentes = request.Antecedentes;
        if (antecedentes != null)
        {
            await using var cmdCheckAnt = new NpgsqlCommand(
                "SELECT COUNT(*) FROM antecedentes WHERE historia_id = @historiaId",
                connection, transaction);
            cmdCheckAnt.Parameters.AddWithValue("historiaId", historiaGuid);
            var existeAnt = Convert.ToInt32(await cmdCheckAnt.ExecuteScalarAsync()) > 0;

            if (existeAnt)
            {
                await using var cmdUpdateAnt = new NpgsqlCommand(
                    @"UPDATE antecedentes 
                      SET apf = @apf, app = @app, apq = @apq, ago = @ago, menarquia = @menarquia,
                          p = @p, g = @g, c = @c, a = @a, alergias = @alergias
                      WHERE historia_id = @historiaId",
                    connection, transaction);
                
                cmdUpdateAnt.Parameters.AddWithValue("historiaId", historiaGuid);
                cmdUpdateAnt.Parameters.AddWithValue("apf", antecedentes.APF ?? (object)DBNull.Value);
                cmdUpdateAnt.Parameters.AddWithValue("app", antecedentes.APP ?? (object)DBNull.Value);
                cmdUpdateAnt.Parameters.AddWithValue("apq", antecedentes.APQ ?? (object)DBNull.Value);
                cmdUpdateAnt.Parameters.AddWithValue("ago", antecedentes.AGO ?? (object)DBNull.Value);
                cmdUpdateAnt.Parameters.AddWithValue("menarquia", antecedentes.Menarquia ?? (object)DBNull.Value);
                cmdUpdateAnt.Parameters.AddWithValue("p", antecedentes.P ?? (object)DBNull.Value);
                cmdUpdateAnt.Parameters.AddWithValue("g", antecedentes.G ?? (object)DBNull.Value);
                cmdUpdateAnt.Parameters.AddWithValue("c", antecedentes.C ?? (object)DBNull.Value);
                cmdUpdateAnt.Parameters.AddWithValue("a", antecedentes.A ?? (object)DBNull.Value);
                cmdUpdateAnt.Parameters.AddWithValue("alergias", antecedentes.Alergias ?? (object)DBNull.Value);

                await cmdUpdateAnt.ExecuteNonQueryAsync();
            }
            else
            {
                // Insertar nuevos antecedentes
                await InsertAntecedentesAsync(connection, transaction, historiaGuid, antecedentes);
            }
        }

        // Actualizar o insertar hábitos
        var habitos = request.Habitos;
        if (habitos != null)
        {
            await using var cmdCheckHab = new NpgsqlCommand(
                "SELECT COUNT(*) FROM habitos WHERE historia_id = @historiaId",
                connection, transaction);
            cmdCheckHab.Parameters.AddWithValue("historiaId", historiaGuid);
            var existeHab = Convert.ToInt32(await cmdCheckHab.ExecuteScalarAsync()) > 0;

            if (existeHab)
            {
                await using var cmdUpdateHab = new NpgsqlCommand(
                    @"UPDATE habitos 
                      SET fuma = @fuma, alcohol = @alcohol, cafe = @cafe, hidratacion = @hidratacion,
                          gaseosas = @gaseosas, actividad_fisica = @actividadFisica, te = @te,
                          edulcorantes = @edulcorantes, alimentacion = @alimentacion
                      WHERE historia_id = @historiaId",
                    connection, transaction);
                
                cmdUpdateHab.Parameters.AddWithValue("historiaId", historiaGuid);
                cmdUpdateHab.Parameters.AddWithValue("fuma", habitos.Fuma ?? (object)DBNull.Value);
                cmdUpdateHab.Parameters.AddWithValue("alcohol", habitos.Alcohol ?? (object)DBNull.Value);
                cmdUpdateHab.Parameters.AddWithValue("cafe", habitos.Cafe ?? (object)DBNull.Value);
                cmdUpdateHab.Parameters.AddWithValue("hidratacion", habitos.Hidratacion ?? (object)DBNull.Value);
                cmdUpdateHab.Parameters.AddWithValue("gaseosas", habitos.Gaseosas ?? (object)DBNull.Value);
                cmdUpdateHab.Parameters.AddWithValue("actividadFisica", habitos.ActividadFisica ?? (object)DBNull.Value);
                cmdUpdateHab.Parameters.AddWithValue("te", habitos.Te ?? (object)DBNull.Value);
                cmdUpdateHab.Parameters.AddWithValue("edulcorantes", habitos.Edulcorantes ?? (object)DBNull.Value);
                cmdUpdateHab.Parameters.AddWithValue("alimentacion", habitos.Alimentacion ?? (object)DBNull.Value);

                await cmdUpdateHab.ExecuteNonQueryAsync();
            }
            else
            {
                // Insertar nuevos hábitos
                await InsertHabitosAsync(connection, transaction, historiaGuid, habitos);
            }
        }

        // Actualizar o insertar valores bioquímicos
        var valoresBio = request.ValoresBioquimicos;
        if (valoresBio != null)
        {
            await using var cmdCheckBio = new NpgsqlCommand(
                "SELECT COUNT(*) FROM valores_bioquimicos WHERE historia_id = @historiaId",
                connection, transaction);
            cmdCheckBio.Parameters.AddWithValue("historiaId", historiaGuid);
            var existeBio = Convert.ToInt32(await cmdCheckBio.ExecuteScalarAsync()) > 0;

            if (existeBio)
            {
                await using var cmdUpdateBio = new NpgsqlCommand(
                    @"UPDATE valores_bioquimicos 
                      SET glicemia = @glicemia, colesterol_total = @colesterolTotal, trigliceridos = @trigliceridos,
                          hdl = @hdl, ldl = @ldl, tgo = @tgo, tgp = @tgp, urea = @urea, creatinina = @creatinina
                      WHERE historia_id = @historiaId",
                    connection, transaction);
                
                cmdUpdateBio.Parameters.AddWithValue("historiaId", historiaGuid);
                cmdUpdateBio.Parameters.AddWithValue("glicemia", ParseDecimalOrNull(valoresBio.Glicemia) ?? (object)DBNull.Value);
                cmdUpdateBio.Parameters.AddWithValue("colesterolTotal", ParseDecimalOrNull(valoresBio.ColesterolTotal) ?? (object)DBNull.Value);
                cmdUpdateBio.Parameters.AddWithValue("trigliceridos", ParseDecimalOrNull(valoresBio.Trigliceridos) ?? (object)DBNull.Value);
                cmdUpdateBio.Parameters.AddWithValue("hdl", ParseDecimalOrNull(valoresBio.HDL) ?? (object)DBNull.Value);
                cmdUpdateBio.Parameters.AddWithValue("ldl", ParseDecimalOrNull(valoresBio.LDL) ?? (object)DBNull.Value);
                cmdUpdateBio.Parameters.AddWithValue("tgo", ParseDecimalOrNull(valoresBio.TGO) ?? (object)DBNull.Value);
                cmdUpdateBio.Parameters.AddWithValue("tgp", ParseDecimalOrNull(valoresBio.TGP) ?? (object)DBNull.Value);
                cmdUpdateBio.Parameters.AddWithValue("urea", ParseDecimalOrNull(valoresBio.Urea) ?? (object)DBNull.Value);
                cmdUpdateBio.Parameters.AddWithValue("creatinina", ParseDecimalOrNull(valoresBio.Creatinina) ?? (object)DBNull.Value);

                await cmdUpdateBio.ExecuteNonQueryAsync();
            }
            else
            {
                // Insertar nuevos valores bioquímicos
                await InsertBioquimicosAsync(connection, transaction, historiaGuid, valoresBio);
            }
        }

        // Actualizar o insertar recordatorio 24h
        var recordatorio = request.Recordatorio24h;
        if (recordatorio != null)
        {
            await using var cmdCheckRec = new NpgsqlCommand(
                "SELECT COUNT(*) FROM recordatorio_24h WHERE historia_id = @historiaId",
                connection, transaction);
            cmdCheckRec.Parameters.AddWithValue("historiaId", historiaGuid);
            var existeRec = Convert.ToInt32(await cmdCheckRec.ExecuteScalarAsync()) > 0;

            if (existeRec)
            {
                await using var cmdUpdateRec = new NpgsqlCommand(
                    @"UPDATE recordatorio_24h 
                      SET desayuno = @desayuno, snack1 = @snack1, almuerzo = @almuerzo,
                          snack2 = @snack2, cena = @cena, extras = @extras
                      WHERE historia_id = @historiaId",
                    connection, transaction);
                
                cmdUpdateRec.Parameters.AddWithValue("historiaId", historiaGuid);
                cmdUpdateRec.Parameters.AddWithValue("desayuno", recordatorio.Desayuno ?? (object)DBNull.Value);
                cmdUpdateRec.Parameters.AddWithValue("snack1", recordatorio.Snack1 ?? (object)DBNull.Value);
                cmdUpdateRec.Parameters.AddWithValue("almuerzo", recordatorio.Almuerzo ?? (object)DBNull.Value);
                cmdUpdateRec.Parameters.AddWithValue("snack2", recordatorio.Snack2 ?? (object)DBNull.Value);
                cmdUpdateRec.Parameters.AddWithValue("cena", recordatorio.Cena ?? (object)DBNull.Value);
                cmdUpdateRec.Parameters.AddWithValue("extras", recordatorio.Extras ?? (object)DBNull.Value);

                await cmdUpdateRec.ExecuteNonQueryAsync();
            }
            else
            {
                // Insertar nuevo recordatorio 24h
                await InsertRecordatorioAsync(connection, transaction, historiaGuid, recordatorio);
            }
        }

        // Actualizar frecuencia de consumo (eliminar y reinsertar)
        if (request.Frequency != null && request.Frequency.Count > 0)
        {
            await using var cmdDeleteFreq = new NpgsqlCommand(
                "DELETE FROM frecuencia_consumo WHERE historia_id = @historiaId",
                connection, transaction);
            cmdDeleteFreq.Parameters.AddWithValue("historiaId", historiaGuid);
            await cmdDeleteFreq.ExecuteNonQueryAsync();

            foreach (var kvp in request.Frequency)
            {
                var parts = kvp.Key.Split("::");
                if (parts.Length != 2) continue;

                await using var cmdInsertFreq = new NpgsqlCommand(
                    @"INSERT INTO frecuencia_consumo (historia_id, categoria, alimento, frecuencia)
                      VALUES (@historiaId, @categoria, @alimento, @frecuencia)",
                    connection, transaction);
                
                cmdInsertFreq.Parameters.AddWithValue("historiaId", historiaGuid);
                cmdInsertFreq.Parameters.AddWithValue("categoria", parts[0]);
                cmdInsertFreq.Parameters.AddWithValue("alimento", parts[1]);
                cmdInsertFreq.Parameters.AddWithValue("frecuencia", kvp.Value);

                await cmdInsertFreq.ExecuteNonQueryAsync();
            }
        }

        await transaction.CommitAsync();
        return Results.Ok(new { success = true, message = "Historia clínica actualizada correctamente" });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        Console.Error.WriteLine($"Error actualizando historia: {ex.Message}");
        return Results.Json(new { error = "Error al actualizar la historia clínica", details = ex.Message }, statusCode: 500);
    }
});

// ====== ENDPOINTS PLANES NUTRICIONALES ======

// GET - Obtener todos los planes de una historia clínica
app.MapGet("/api/nutrition/planes/{historiaId}", async (HttpContext httpContext, string historiaId) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    var planes = new List<object>();

    await using var cmd = new NpgsqlCommand(@"
        SELECT id, historia_id, fecha_inicio, fecha_fin, objetivo, calorias_diarias, 
               observaciones, activo, fecha_creacion, fecha_modificacion
        FROM planes_nutricionales
        WHERE historia_id = @historiaId
        ORDER BY fecha_creacion DESC", connection);
    
    cmd.Parameters.AddWithValue("historiaId", Guid.Parse(historiaId));

    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        planes.Add(new
        {
            id = reader.GetGuid(0).ToString(),
            historia_id = reader.GetGuid(1).ToString(),
            fecha_inicio = reader.IsDBNull(2) ? null : reader.GetDateTime(2).ToString("yyyy-MM-dd"),
            fecha_fin = reader.IsDBNull(3) ? null : reader.GetDateTime(3).ToString("yyyy-MM-dd"),
            objetivo = reader.IsDBNull(4) ? null : reader.GetString(4),
            calorias_diarias = reader.IsDBNull(5) ? (decimal?)null : reader.GetDecimal(5),
            observaciones = reader.IsDBNull(6) ? null : reader.GetString(6),
            activo = reader.GetBoolean(7),
            fecha_creacion = reader.GetDateTime(8).ToString("yyyy-MM-dd HH:mm:ss"),
            fecha_modificacion = reader.GetDateTime(9).ToString("yyyy-MM-dd HH:mm:ss")
        });
    }

    return Results.Ok(planes);
});

// POST - Crear un nuevo plan nutricional con su alimentación semanal
app.MapPost("/api/nutrition/planes", async (HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    var request = await httpContext.Request.ReadFromJsonAsync<CrearPlanRequest>();
    if (request == null)
    {
        return Results.BadRequest("Datos inválidos");
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var transaction = await connection.BeginTransactionAsync();

    try
    {
        // Insertar el plan nutricional
        var planId = Guid.NewGuid();
        await using var cmdPlan = new NpgsqlCommand(@"
            INSERT INTO planes_nutricionales 
            (id, historia_id, fecha_inicio, fecha_fin, objetivo, calorias_diarias, observaciones, activo)
            VALUES (@id, @historiaId, @fechaInicio, @fechaFin, @objetivo, @calorias, @observaciones, @activo)", connection, transaction);

        cmdPlan.Parameters.AddWithValue("id", planId);
        cmdPlan.Parameters.AddWithValue("historiaId", Guid.Parse(request.HistoriaId));
        cmdPlan.Parameters.AddWithValue("fechaInicio", DateTime.Parse(request.FechaInicio));
        cmdPlan.Parameters.AddWithValue("fechaFin", request.FechaFin != null ? DateTime.Parse(request.FechaFin) : DBNull.Value);
        cmdPlan.Parameters.AddWithValue("objetivo", request.Objetivo ?? (object)DBNull.Value);
        cmdPlan.Parameters.AddWithValue("calorias", request.CaloriasDiarias ?? (object)DBNull.Value);
        cmdPlan.Parameters.AddWithValue("observaciones", request.Observaciones ?? (object)DBNull.Value);
        cmdPlan.Parameters.AddWithValue("activo", request.Activo);

        await cmdPlan.ExecuteNonQueryAsync();

        // Insertar alimentación semanal si existe
        if (request.AlimentacionSemanal != null && request.AlimentacionSemanal.Count > 0)
        {
            foreach (var alimentacion in request.AlimentacionSemanal)
            {
                await using var cmdAlimentacion = new NpgsqlCommand(@"
                    INSERT INTO alimentacion_semanal 
                    (plan_id, semana, dia_semana, desayuno, snack_manana, almuerzo, snack_tarde, cena, snack_noche, observaciones)
                    VALUES (@planId, @semana, @diaSemana, @desayuno, @snackManana, @almuerzo, @snackTarde, @cena, @snackNoche, @observaciones)", 
                    connection, transaction);

                cmdAlimentacion.Parameters.AddWithValue("planId", planId);
                cmdAlimentacion.Parameters.AddWithValue("semana", alimentacion.Semana);
                cmdAlimentacion.Parameters.AddWithValue("diaSemana", alimentacion.DiaSemana);
                cmdAlimentacion.Parameters.AddWithValue("desayuno", alimentacion.Desayuno ?? (object)DBNull.Value);
                cmdAlimentacion.Parameters.AddWithValue("snackManana", alimentacion.SnackManana ?? (object)DBNull.Value);
                cmdAlimentacion.Parameters.AddWithValue("almuerzo", alimentacion.Almuerzo ?? (object)DBNull.Value);
                cmdAlimentacion.Parameters.AddWithValue("snackTarde", alimentacion.SnackTarde ?? (object)DBNull.Value);
                cmdAlimentacion.Parameters.AddWithValue("cena", alimentacion.Cena ?? (object)DBNull.Value);
                cmdAlimentacion.Parameters.AddWithValue("snackNoche", alimentacion.SnackNoche ?? (object)DBNull.Value);
                cmdAlimentacion.Parameters.AddWithValue("observaciones", alimentacion.Observaciones ?? (object)DBNull.Value);

                await cmdAlimentacion.ExecuteNonQueryAsync();
            }
        }

        await transaction.CommitAsync();
        return Results.Ok(new { success = true, planId = planId.ToString() });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        Console.Error.WriteLine($"Error creando plan: {ex.Message}");
        return Results.Json(new { error = "Error al crear el plan nutricional", details = ex.Message }, statusCode: 500);
    }
});

// GET - Obtener la alimentación semanal de un plan
app.MapGet("/api/nutrition/planes/{planId}/alimentacion", async (HttpContext httpContext, string planId) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    var alimentacion = new List<object>();

    await using var cmd = new NpgsqlCommand(@"
        SELECT semana, dia_semana, desayuno, snack_manana, almuerzo, snack_tarde, cena, snack_noche, observaciones
        FROM alimentacion_semanal
        WHERE plan_id = @planId
        ORDER BY semana, dia_semana", connection);
    
    cmd.Parameters.AddWithValue("planId", Guid.Parse(planId));

    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        alimentacion.Add(new
        {
            semana = reader.GetInt32(0),
            dia_semana = reader.GetInt32(1),
            desayuno = reader.IsDBNull(2) ? null : reader.GetString(2),
            snack_manana = reader.IsDBNull(3) ? null : reader.GetString(3),
            almuerzo = reader.IsDBNull(4) ? null : reader.GetString(4),
            snack_tarde = reader.IsDBNull(5) ? null : reader.GetString(5),
            cena = reader.IsDBNull(6) ? null : reader.GetString(6),
            snack_noche = reader.IsDBNull(7) ? null : reader.GetString(7),
            observaciones = reader.IsDBNull(8) ? null : reader.GetString(8)
        });
    }

    return Results.Ok(alimentacion);
});

// PUT - Actualizar un plan nutricional
app.MapPut("/api/nutrition/planes/{planId}", async (HttpContext httpContext, string planId) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    var request = await httpContext.Request.ReadFromJsonAsync<ActualizarPlanRequest>();
    if (request == null)
    {
        return Results.BadRequest("Datos inválidos");
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    await using var cmd = new NpgsqlCommand(@"
        UPDATE planes_nutricionales 
        SET fecha_fin = @fechaFin, objetivo = @objetivo, calorias_diarias = @calorias, 
            observaciones = @observaciones, activo = @activo, fecha_modificacion = NOW()
        WHERE id = @id", connection);

    cmd.Parameters.AddWithValue("id", Guid.Parse(planId));
    cmd.Parameters.AddWithValue("fechaFin", request.FechaFin != null ? DateTime.Parse(request.FechaFin) : DBNull.Value);
    cmd.Parameters.AddWithValue("objetivo", request.Objetivo ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("calorias", request.CaloriasDiarias ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("observaciones", request.Observaciones ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("activo", request.Activo);

    await cmd.ExecuteNonQueryAsync();

    return Results.Ok(new { success = true });
});

// DELETE - Eliminar un plan nutricional
app.MapDelete("/api/nutrition/planes/{planId}", async (HttpContext httpContext, string planId) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    await using var cmd = new NpgsqlCommand("DELETE FROM planes_nutricionales WHERE id = @id", connection);
    cmd.Parameters.AddWithValue("id", Guid.Parse(planId));

    await cmd.ExecuteNonQueryAsync();

    return Results.Ok(new { success = true });
});

// =============================================
// ENDPOINTS PARA PLANES DE ALIMENTACIÓN
// =============================================

// GET - Listar historias clínicas disponibles
app.MapGet("/api/historias/list", async (HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    var historias = new List<object>();

    await using var cmd = new NpgsqlCommand(@"
        SELECT h.id, h.fecha_consulta, p.nombre, p.numero_cedula
        FROM historias_clinicas h
        INNER JOIN pacientes p ON h.paciente_id = p.id
        ORDER BY h.fecha_consulta DESC", connection);

    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        historias.Add(new
        {
            id = reader.GetGuid(0).ToString(),
            fecha_consulta = reader.IsDBNull(1) ? null : reader.GetDateTime(1).ToString("yyyy-MM-dd"),
            paciente_nombre = reader.IsDBNull(2) ? "Sin nombre" : reader.GetString(2),
            paciente_cedula = reader.IsDBNull(3) ? "" : reader.GetString(3)
        });
    }

    return Results.Ok(historias);
});

// POST - Guardar plan de alimentación completo
app.MapPost("/api/planes", async (HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    var request = await httpContext.Request.ReadFromJsonAsync<GuardarPlanAlimentacionRequest>();
    if (request == null)
    {
        return Results.BadRequest(new { error = "Datos inválidos" });
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var transaction = await connection.BeginTransactionAsync();

    try
    {
        // Crear el plan nutricional
        var planId = Guid.NewGuid();
        await using var cmdPlan = new NpgsqlCommand(@"
            INSERT INTO planes_nutricionales 
            (id, historia_id, fecha_inicio, fecha_fin, objetivo, observaciones, activo)
            VALUES (@id, @historiaId, @fechaInicio, @fechaFin, @objetivo, @observaciones, true)", connection, transaction);

        cmdPlan.Parameters.AddWithValue("id", planId);
        cmdPlan.Parameters.AddWithValue("historiaId", Guid.Parse(request.HistoriaId));
        cmdPlan.Parameters.AddWithValue("fechaInicio", DateTime.Parse(request.FechaInicio));
        cmdPlan.Parameters.AddWithValue("fechaFin", string.IsNullOrEmpty(request.FechaFin) ? DBNull.Value : DateTime.Parse(request.FechaFin));
        cmdPlan.Parameters.AddWithValue("objetivo", request.Objetivo ?? (object)DBNull.Value);
        cmdPlan.Parameters.AddWithValue("observaciones", request.Observaciones ?? (object)DBNull.Value);

        await cmdPlan.ExecuteNonQueryAsync();

        // Mapeo de días
        var diasMap = new Dictionary<string, int>
        {
            { "lunes", 1 }, { "martes", 2 }, { "miercoles", 3 }, { "jueves", 4 },
            { "viernes", 5 }, { "sabado", 6 }, { "domingo", 7 }
        };

        // Guardar semana 1
        if (request.Semana1 != null)
        {
            foreach (var dia in request.Semana1)
            {
                if (diasMap.TryGetValue(dia.Key.ToLower(), out var diaSemana))
                {
                    await InsertarAlimentacionDia(connection, transaction, planId, 1, diaSemana, dia.Value);
                }
            }
        }

        // Guardar semana 2
        if (request.Semana2 != null)
        {
            foreach (var dia in request.Semana2)
            {
                if (diasMap.TryGetValue(dia.Key.ToLower(), out var diaSemana))
                {
                    await InsertarAlimentacionDia(connection, transaction, planId, 2, diaSemana, dia.Value);
                }
            }
        }

        await transaction.CommitAsync();
        return Results.Ok(new { success = true, planId = planId.ToString(), message = "Plan guardado exitosamente" });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        Console.Error.WriteLine($"[ERROR] Error guardando plan: {ex.Message}");
        Console.Error.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
        return Results.Json(new { error = "Error al guardar el plan", details = ex.Message }, statusCode: 500);
    }
});

// PUT - Actualizar plan de alimentación existente
app.MapPut("/api/planes/{planId}", async (HttpContext httpContext, string planId) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    var request = await httpContext.Request.ReadFromJsonAsync<GuardarPlanAlimentacionRequest>();
    if (request == null)
    {
        return Results.BadRequest(new { error = "Datos inválidos" });
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var transaction = await connection.BeginTransactionAsync();

    try
    {
        var planGuid = Guid.Parse(planId);

        // Actualizar el plan nutricional
        await using var cmdPlan = new NpgsqlCommand(@"
            UPDATE planes_nutricionales 
            SET fecha_inicio = @fechaInicio, 
                fecha_fin = @fechaFin, 
                objetivo = @objetivo, 
                observaciones = @observaciones,
                fecha_modificacion = NOW()
            WHERE id = @id", connection, transaction);

        cmdPlan.Parameters.AddWithValue("id", planGuid);
        cmdPlan.Parameters.AddWithValue("fechaInicio", DateTime.Parse(request.FechaInicio));
        cmdPlan.Parameters.AddWithValue("fechaFin", string.IsNullOrEmpty(request.FechaFin) ? DBNull.Value : DateTime.Parse(request.FechaFin));
        cmdPlan.Parameters.AddWithValue("objetivo", request.Objetivo ?? (object)DBNull.Value);
        cmdPlan.Parameters.AddWithValue("observaciones", request.Observaciones ?? (object)DBNull.Value);

        await cmdPlan.ExecuteNonQueryAsync();

        // Eliminar alimentación existente
        await using var cmdDelete = new NpgsqlCommand("DELETE FROM alimentacion_semanal WHERE plan_id = @planId", connection, transaction);
        cmdDelete.Parameters.AddWithValue("planId", planGuid);
        await cmdDelete.ExecuteNonQueryAsync();

        // Mapeo de días
        var diasMap = new Dictionary<string, int>
        {
            { "lunes", 1 }, { "martes", 2 }, { "miercoles", 3 }, { "jueves", 4 },
            { "viernes", 5 }, { "sabado", 6 }, { "domingo", 7 }
        };

        // Guardar semana 1
        if (request.Semana1 != null)
        {
            foreach (var dia in request.Semana1)
            {
                if (diasMap.TryGetValue(dia.Key.ToLower(), out var diaSemana))
                {
                    await InsertarAlimentacionDia(connection, transaction, planGuid, 1, diaSemana, dia.Value);
                }
            }
        }

        // Guardar semana 2
        if (request.Semana2 != null)
        {
            foreach (var dia in request.Semana2)
            {
                if (diasMap.TryGetValue(dia.Key.ToLower(), out var diaSemana))
                {
                    await InsertarAlimentacionDia(connection, transaction, planGuid, 2, diaSemana, dia.Value);
                }
            }
        }

        await transaction.CommitAsync();
        return Results.Ok(new { success = true, planId = planId, message = "Plan actualizado exitosamente" });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        Console.Error.WriteLine($"[ERROR] Error actualizando plan: {ex.Message}");
        Console.Error.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
        return Results.Json(new { error = "Error al actualizar el plan", details = ex.Message }, statusCode: 500);
    }
});

// GET - Obtener plan por ID con alimentación
app.MapGet("/api/planes/{planId}", async (HttpContext httpContext, string planId) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    // Obtener plan
    await using var cmdPlan = new NpgsqlCommand(@"
        SELECT id, historia_id, fecha_inicio, fecha_fin, objetivo, observaciones
        FROM planes_nutricionales WHERE id = @id", connection);
    cmdPlan.Parameters.AddWithValue("id", Guid.Parse(planId));

    await using var readerPlan = await cmdPlan.ExecuteReaderAsync();
    if (!await readerPlan.ReadAsync())
    {
        return Results.NotFound(new { error = "Plan no encontrado" });
    }

    var planInfo = new
    {
        id = readerPlan.GetGuid(0).ToString(),
        historia_id = readerPlan.GetGuid(1).ToString(),
        fecha_inicio = readerPlan.GetDateTime(2).ToString("yyyy-MM-dd"),
        fecha_fin = readerPlan.IsDBNull(3) ? null : readerPlan.GetDateTime(3).ToString("yyyy-MM-dd"),
        objetivo = readerPlan.IsDBNull(4) ? null : readerPlan.GetString(4),
        observaciones = readerPlan.IsDBNull(5) ? null : readerPlan.GetString(5)
    };
    await readerPlan.CloseAsync();

    // Obtener alimentación
    var alimentacionRaw = new Dictionary<int, Dictionary<int, object>>();
    await using var cmdAlim = new NpgsqlCommand(@"
        SELECT semana, dia_semana, desayuno, snack_manana, almuerzo, snack_tarde, cena
        FROM alimentacion_semanal WHERE plan_id = @planId
        ORDER BY semana, dia_semana", connection);
    cmdAlim.Parameters.AddWithValue("planId", Guid.Parse(planId));

    await using var readerAlim = await cmdAlim.ExecuteReaderAsync();
    while (await readerAlim.ReadAsync())
    {
        var semana = readerAlim.GetInt32(0);
        var dia = readerAlim.GetInt32(1);

        if (!alimentacionRaw.ContainsKey(semana))
            alimentacionRaw[semana] = new Dictionary<int, object>();

        alimentacionRaw[semana][dia] = new
        {
            desayuno = readerAlim.IsDBNull(2) ? "" : readerAlim.GetString(2),
            snack1 = readerAlim.IsDBNull(3) ? "" : readerAlim.GetString(3),
            almuerzo = readerAlim.IsDBNull(4) ? "" : readerAlim.GetString(4),
            snack2 = readerAlim.IsDBNull(5) ? "" : readerAlim.GetString(5),
            merienda = readerAlim.IsDBNull(6) ? "" : readerAlim.GetString(6)
        };
    }

    // Mapear días numéricos a nombres
    var diasMap = new Dictionary<int, string>
    {
        { 1, "lunes" }, { 2, "martes" }, { 3, "miercoles" }, { 4, "jueves" },
        { 5, "viernes" }, { 6, "sabado" }, { 7, "domingo" }
    };

    // Crear estructura semana1 y semana2
    var semanaVacia = new Dictionary<string, object>
    {
        { "lunes", new { desayuno = "", snack1 = "", almuerzo = "", snack2 = "", merienda = "" } },
        { "martes", new { desayuno = "", snack1 = "", almuerzo = "", snack2 = "", merienda = "" } },
        { "miercoles", new { desayuno = "", snack1 = "", almuerzo = "", snack2 = "", merienda = "" } },
        { "jueves", new { desayuno = "", snack1 = "", almuerzo = "", snack2 = "", merienda = "" } },
        { "viernes", new { desayuno = "", snack1 = "", almuerzo = "", snack2 = "", merienda = "" } },
        { "sabado", new { desayuno = "", snack1 = "", almuerzo = "", snack2 = "", merienda = "" } },
        { "domingo", new { desayuno = "", snack1 = "", almuerzo = "", snack2 = "", merienda = "" } }
    };

    var semana1 = new Dictionary<string, object>(semanaVacia);
    var semana2 = new Dictionary<string, object>(semanaVacia);

    // Llenar semana1
    if (alimentacionRaw.ContainsKey(1))
    {
        foreach (var dia in alimentacionRaw[1])
        {
            if (diasMap.TryGetValue(dia.Key, out var nombreDia))
            {
                semana1[nombreDia] = dia.Value;
            }
        }
    }

    // Llenar semana2
    if (alimentacionRaw.ContainsKey(2))
    {
        foreach (var dia in alimentacionRaw[2])
        {
            if (diasMap.TryGetValue(dia.Key, out var nombreDia))
            {
                semana2[nombreDia] = dia.Value;
            }
        }
    }

    return Results.Ok(new 
    { 
        id = planInfo.id,
        historia_id = planInfo.historia_id,
        fecha_inicio = planInfo.fecha_inicio,
        fecha_fin = planInfo.fecha_fin,
        objetivo = planInfo.objetivo,
        observaciones = planInfo.observaciones,
        semana1, 
        semana2 
    });
});

// GET - Obtener planes por historia
app.MapGet("/api/planes/historia/{historiaId}", async (HttpContext httpContext, string historiaId) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    var planes = new List<object>();

    await using var cmd = new NpgsqlCommand(@"
        SELECT id, fecha_inicio, fecha_fin, objetivo, observaciones, activo
        FROM planes_nutricionales 
        WHERE historia_id = @historiaId
        ORDER BY fecha_creacion DESC", connection);
    
    cmd.Parameters.AddWithValue("historiaId", Guid.Parse(historiaId));

    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        planes.Add(new
        {
            id = reader.GetGuid(0).ToString(),
            fecha_inicio = reader.GetDateTime(1).ToString("yyyy-MM-dd"),
            fecha_fin = reader.IsDBNull(2) ? null : reader.GetDateTime(2).ToString("yyyy-MM-dd"),
            objetivo = reader.IsDBNull(3) ? null : reader.GetString(3),
            observaciones = reader.IsDBNull(4) ? null : reader.GetString(4),
            activo = reader.GetBoolean(5)
        });
    }

    return Results.Ok(planes);
});

app.Run();

static async Task InsertarAlimentacionDia(NpgsqlConnection connection, NpgsqlTransaction transaction, 
    Guid planId, int semana, int diaSemana, Dictionary<string, object>? comidas)
{
    if (comidas == null) return;

    var desayuno = comidas.ContainsKey("desayuno") ? comidas["desayuno"]?.ToString() : null;
    var snack1 = comidas.ContainsKey("snack1") ? comidas["snack1"]?.ToString() : null;
    var almuerzo = comidas.ContainsKey("almuerzo") ? comidas["almuerzo"]?.ToString() : null;
    var snack2 = comidas.ContainsKey("snack2") ? comidas["snack2"]?.ToString() : null;
    var merienda = comidas.ContainsKey("merienda") ? comidas["merienda"]?.ToString() : null;

    // Solo insertar si hay al menos una comida con contenido
    if (string.IsNullOrWhiteSpace(desayuno) && string.IsNullOrWhiteSpace(snack1) && 
        string.IsNullOrWhiteSpace(almuerzo) && string.IsNullOrWhiteSpace(snack2) && 
        string.IsNullOrWhiteSpace(merienda))
    {
        return;
    }

    await using var cmd = new NpgsqlCommand(@"
        INSERT INTO alimentacion_semanal 
        (plan_id, semana, dia_semana, desayuno, snack_manana, almuerzo, snack_tarde, cena)
        VALUES (@planId, @semana, @diaSemana, @desayuno, @snack1, @almuerzo, @snack2, @merienda)
        ON CONFLICT (plan_id, semana, dia_semana) 
        DO UPDATE SET 
            desayuno = EXCLUDED.desayuno,
            snack_manana = EXCLUDED.snack_manana,
            almuerzo = EXCLUDED.almuerzo,
            snack_tarde = EXCLUDED.snack_tarde,
            cena = EXCLUDED.cena", connection, transaction);

    cmd.Parameters.AddWithValue("planId", planId);
    cmd.Parameters.AddWithValue("semana", semana);
    cmd.Parameters.AddWithValue("diaSemana", diaSemana);
    cmd.Parameters.AddWithValue("desayuno", desayuno ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("snack1", snack1 ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("almuerzo", almuerzo ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("snack2", snack2 ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("merienda", merienda ?? (object)DBNull.Value);

    await cmd.ExecuteNonQueryAsync();
}


static decimal? ParseDecimalOrNull(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return null;
    }
    
    if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
    {
        return result;
    }
    
    return null;
}

static int? ParseIntOrNull(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return null;
    }
    
    if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
    {
        return result;
    }
    
    return null;
}

static string ResolveConnectionString(IConfiguration configuration)
{
    var fromEnv = Environment.GetEnvironmentVariable("NUTRITION_DB");
    if (!string.IsNullOrWhiteSpace(fromEnv))
    {
        Console.WriteLine($"[DB] Using connection from NUTRITION_DB environment variable");
        return fromEnv;
    }

    var current = Directory.GetCurrentDirectory();
    var connectionFile = Path.GetFullPath(Path.Combine(current, "..", "database", "connection.local"));
    Console.WriteLine($"[DB] Looking for connection file at: {connectionFile}");
    if (File.Exists(connectionFile))
    {
        var text = File.ReadAllText(connectionFile).Trim();
        if (!string.IsNullOrWhiteSpace(text))
        {
            Console.WriteLine($"[DB] Using connection from file: {connectionFile}");
            Console.WriteLine($"[DB] Connection string: {text}");
            return text;
        }
    }

    var packagedConnectionFile = Path.Combine(AppContext.BaseDirectory, "database", "connection.local");
    if (File.Exists(packagedConnectionFile))
    {
        var text = File.ReadAllText(packagedConnectionFile).Trim();
        if (!string.IsNullOrWhiteSpace(text))
        {
            Console.WriteLine($"[DB] Using connection from file: {packagedConnectionFile}");
            return text;
        }
    }

    var fromConfig = configuration.GetConnectionString("NutritionDb");
    if (!string.IsNullOrWhiteSpace(fromConfig))
    {
        Console.WriteLine($"[DB] Using connection from appsettings.json");
        return fromConfig;
    }

    throw new InvalidOperationException("No se encontró la cadena de conexión. Defina NUTRITION_DB o cree database/connection.local con la cadena completa.");
}

static bool TryGetToken(HttpContext context, out string? token)
{
    token = null;
    if (!context.Request.Headers.TryGetValue("Authorization", out var header))
    {
        return false;
    }

    var value = header.ToString();
    if (!value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    token = value["Bearer ".Length..].Trim();
    return !string.IsNullOrWhiteSpace(token);
}

static bool IsTokenValid(string token, System.Collections.Concurrent.ConcurrentDictionary<string, PublicUser> sessions, out PublicUser? user)
{
    if (sessions.TryGetValue(token, out var found))
    {
        user = found;
        return true;
    }

    user = null;
    return false;
}

static async Task EnsureAuthSchemaAsync(NpgsqlConnection connection)
{
    const string script = @"
CREATE TABLE IF NOT EXISTS usuarios (
    id uuid PRIMARY KEY,
    email varchar(100) NOT NULL UNIQUE,
    nombre varchar(200),
    rol varchar(50) DEFAULT 'nutricionista',
    activo boolean DEFAULT true,
    fecha_creacion timestamptz DEFAULT NOW(),
    fecha_ultimo_acceso timestamptz
);
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS username varchar(50);
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS password_hash varchar(255);
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS ultimo_login timestamptz;
CREATE UNIQUE INDEX IF NOT EXISTS idx_usuarios_username ON usuarios(username);
";

    await using var cmd = new NpgsqlCommand(script, connection);
    await cmd.ExecuteNonQueryAsync();
}

static async Task LogAccessAttemptAsync(NpgsqlConnection connection, Guid? userId, string username, string action, string ipAddress, string userAgent, bool success, string message)
{
    try
    {
        var sql = userId.HasValue
            ? @"INSERT INTO logs_acceso (usuario_id, username, accion, ip_address, user_agent, exitoso, mensaje)
                VALUES (@uid, @username, @action, @ip, @ua, @success, @msg)"
            : @"INSERT INTO logs_acceso (username, accion, ip_address, user_agent, exitoso, mensaje)
                VALUES (@username, @action, @ip, @ua, @success, @msg)";

        await using var cmd = new NpgsqlCommand(sql, connection);
        if (userId.HasValue)
        {
            cmd.Parameters.AddWithValue("uid", userId.Value);
        }
        cmd.Parameters.AddWithValue("username", username);
        cmd.Parameters.AddWithValue("action", action);
        cmd.Parameters.AddWithValue("ip", ipAddress);
        cmd.Parameters.AddWithValue("ua", userAgent);
        cmd.Parameters.AddWithValue("success", success);
        cmd.Parameters.AddWithValue("msg", message);
        await cmd.ExecuteNonQueryAsync();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error al registrar acceso: {ex.Message}");
    }
}

static string GetClientIp(HttpContext context)
{
    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

static string GetUserAgent(HttpContext context)
{
    return context.Request.Headers.UserAgent.ToString();
}

static string HashPassword(string password)
{
    var salt = RandomNumberGenerator.GetBytes(16);
    const int iterations = 100_000;
    var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
    return $"PBKDF2${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
}

static bool VerifyPassword(string password, string storedHash)
{
    var parts = storedHash.Split('$');
    if (parts.Length != 4 || !string.Equals(parts[0], "PBKDF2", StringComparison.Ordinal))
    {
        return false;
    }

    if (!int.TryParse(parts[1], out var iterations))
    {
        return false;
    }

    var salt = Convert.FromBase64String(parts[2]);
    var expected = Convert.FromBase64String(parts[3]);
    var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

    return CryptographicOperations.FixedTimeEquals(actual, expected);
}

static async Task<Guid> UpsertPacienteAsync(NpgsqlConnection connection, NpgsqlTransaction tx, PersonalData? data)
{
    if (data is null)
    {
        throw new ArgumentNullException(nameof(data));
    }

    Guid? existingId = null;
    if (!string.IsNullOrWhiteSpace(data.NumeroCedula))
    {
        await using var checkCmd = new NpgsqlCommand(
            "SELECT id FROM pacientes WHERE numero_cedula = @cedula LIMIT 1",
            connection, tx);
        checkCmd.Parameters.AddWithValue("cedula", data.NumeroCedula);
        var result = await checkCmd.ExecuteScalarAsync();
        if (result is Guid g)
        {
            existingId = g;
        }
    }

    if (existingId.HasValue)
    {
        await using var updateCmd = new NpgsqlCommand(
            @"UPDATE pacientes SET
                nombre = @nombre,
                edad_cronologica = @edad,
                sexo = @sexo,
                lugar_residencia = @residencia,
                estado_civil = @estadoCivil,
                telefono = @telefono,
                ocupacion = @ocupacion,
                email = @email,
                fecha_actualizacion = NOW()
              WHERE id = @id",
            connection, tx);

        updateCmd.Parameters.AddWithValue("nombre", DbString(data.Nombre));
        updateCmd.Parameters.AddWithValue("edad", DbString(data.EdadCronologica));
        updateCmd.Parameters.AddWithValue("sexo", DbString(data.Sexo));
        updateCmd.Parameters.AddWithValue("residencia", DbString(data.LugarResidencia));
        updateCmd.Parameters.AddWithValue("estadoCivil", DbString(data.EstadoCivil));
        updateCmd.Parameters.AddWithValue("telefono", DbString(data.Telefono));
        updateCmd.Parameters.AddWithValue("ocupacion", DbString(data.Ocupacion));
        updateCmd.Parameters.AddWithValue("email", DbString(data.Email));
        updateCmd.Parameters.AddWithValue("id", existingId.Value);

        await updateCmd.ExecuteNonQueryAsync();
        return existingId.Value;
    }

    await using var insertCmd = new NpgsqlCommand(
        @"INSERT INTO pacientes (numero_cedula, nombre, edad_cronologica, sexo, lugar_residencia, estado_civil, telefono, ocupacion, email)
          VALUES (@cedula, @nombre, @edad, @sexo, @residencia, @estadoCivil, @telefono, @ocupacion, @email)
          RETURNING id",
        connection, tx);

    insertCmd.Parameters.AddWithValue("cedula", DbString(data.NumeroCedula));
    insertCmd.Parameters.AddWithValue("nombre", DbString(data.Nombre));
    insertCmd.Parameters.AddWithValue("edad", DbString(data.EdadCronologica));
    insertCmd.Parameters.AddWithValue("sexo", DbString(data.Sexo));
    insertCmd.Parameters.AddWithValue("residencia", DbString(data.LugarResidencia));
    insertCmd.Parameters.AddWithValue("estadoCivil", DbString(data.EstadoCivil));
    insertCmd.Parameters.AddWithValue("telefono", DbString(data.Telefono));
    insertCmd.Parameters.AddWithValue("ocupacion", DbString(data.Ocupacion));
    insertCmd.Parameters.AddWithValue("email", DbString(data.Email));

    var id = (Guid)(await insertCmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("No se pudo insertar el paciente"));
    return id;
}

static async Task<Guid> InsertHistoriaClinicaAsync(NpgsqlConnection connection, NpgsqlTransaction tx, Guid pacienteId, HistoryRequest request)
{
    await using var cmd = new NpgsqlCommand(
        @"INSERT INTO historias_clinicas (paciente_id, fecha_consulta, motivo_consulta, diagnostico, notas_extras, payload)
          VALUES (@pacienteId, @fechaConsulta, @motivo, @diagnostico, @notas, @payload)
          RETURNING id",
        connection, tx);

    cmd.Parameters.AddWithValue("pacienteId", pacienteId);
    cmd.Parameters.AddWithValue("fechaConsulta", DbDate(ParseDate(request.PersonalData?.FechaConsulta)));
    cmd.Parameters.AddWithValue("motivo", DbString(request.MotivoConsulta));
    cmd.Parameters.AddWithValue("diagnostico", DbString(request.Diagnostico));
    cmd.Parameters.AddWithValue("notas", DbString(request.NotasExtras));
    cmd.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(request));

    var id = (Guid)(await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("No se pudo insertar la historia clínica"));
    return id;
}

static async Task InsertAntecedentesAsync(NpgsqlConnection connection, NpgsqlTransaction tx, Guid historiaId, AntecedentesData? antecedentes)
{
    if (antecedentes is null) return;

    await using var cmd = new NpgsqlCommand(
        @"INSERT INTO antecedentes (historia_id, apf, app, apq, ago, menarquia, p, g, c, a, alergias)
          VALUES (@historiaId, @apf, @app, @apq, @ago, @menarquia, @p, @g, @c, @a, @alergias)",
        connection, tx);

    cmd.Parameters.AddWithValue("historiaId", historiaId);
    cmd.Parameters.AddWithValue("apf", DbString(antecedentes.APF));
    cmd.Parameters.AddWithValue("app", DbString(antecedentes.APP));
    cmd.Parameters.AddWithValue("apq", DbString(antecedentes.APQ));
    cmd.Parameters.AddWithValue("ago", DbString(antecedentes.AGO));
    cmd.Parameters.AddWithValue("menarquia", DbString(antecedentes.Menarquia));
    cmd.Parameters.AddWithValue("p", DbString(antecedentes.P));
    cmd.Parameters.AddWithValue("g", DbString(antecedentes.G));
    cmd.Parameters.AddWithValue("c", DbString(antecedentes.C));
    cmd.Parameters.AddWithValue("a", DbString(antecedentes.A));
    cmd.Parameters.AddWithValue("alergias", DbString(antecedentes.Alergias));

    await cmd.ExecuteNonQueryAsync();
}

static async Task InsertHabitosAsync(NpgsqlConnection connection, NpgsqlTransaction tx, Guid historiaId, HabitosData? habitos)
{
    if (habitos is null) return;

    await using var cmd = new NpgsqlCommand(
        @"INSERT INTO habitos (historia_id, fuma, alcohol, cafe, hidratacion, gaseosas, actividad_fisica, te, edulcorantes, alimentacion)
          VALUES (@historiaId, @fuma, @alcohol, @cafe, @hidratacion, @gaseosas, @actividad, @te, @edulcorantes, @alimentacion)",
        connection, tx);

    cmd.Parameters.AddWithValue("historiaId", historiaId);
    cmd.Parameters.AddWithValue("fuma", DbString(habitos.Fuma));
    cmd.Parameters.AddWithValue("alcohol", DbString(habitos.Alcohol));
    cmd.Parameters.AddWithValue("cafe", DbString(habitos.Cafe));
    cmd.Parameters.AddWithValue("hidratacion", DbString(habitos.Hidratacion));
    cmd.Parameters.AddWithValue("gaseosas", DbString(habitos.Gaseosas));
    cmd.Parameters.AddWithValue("actividad", DbString(habitos.ActividadFisica));
    cmd.Parameters.AddWithValue("te", DbString(habitos.Te));
    cmd.Parameters.AddWithValue("edulcorantes", DbString(habitos.Edulcorantes));
    cmd.Parameters.AddWithValue("alimentacion", DbString(habitos.Alimentacion));

    await cmd.ExecuteNonQueryAsync();
}

static async Task InsertSignosAsync(NpgsqlConnection connection, NpgsqlTransaction tx, Guid historiaId, SignosData? signos)
{
    if (signos is null) return;

    await using var cmd = new NpgsqlCommand(
        @"INSERT INTO signos_vitales (historia_id, pa, temperatura, fc, fr)
          VALUES (@historiaId, @pa, @temp, @fc, @fr)",
        connection, tx);

    cmd.Parameters.AddWithValue("historiaId", historiaId);
    cmd.Parameters.AddWithValue("pa", DbString(signos.PresionArterial));
    cmd.Parameters.AddWithValue("temp", DbString(signos.Temperatura));
    cmd.Parameters.AddWithValue("fc", DbString(signos.FrecuenciaCardiaca));
    cmd.Parameters.AddWithValue("fr", DbString(signos.FrecuenciaRespiratoria));

    await cmd.ExecuteNonQueryAsync();
}

static async Task InsertAntropometricosAsync(NpgsqlConnection connection, NpgsqlTransaction tx, Guid historiaId, AntropometricosData? datos)
{
    if (datos is null) return;

    await using var cmd = new NpgsqlCommand(
        @"INSERT INTO datos_antropometricos (
            historia_id, edad, edad_metabolica, sexo, peso, masa_muscular, gc_porc, gc, talla, gv_porc, imc, kcal_basales,
            actividad_fisica, cintura, cadera, pantorrilla, c_brazo, c_muslo, peso_ajustado, factor_actividad_fisica, tiempos_comida)
          VALUES (
            @historiaId, @edad, @edadMet, @sexo, @peso, @masaMuscular, @gcPorc, @gc, @talla, @gvPorc, @imc, @kcal,
            @actividad, @cintura, @cadera, @pantorrilla, @cBrazo, @cMuslo, @pesoAjustado, @factor, @tiempos)",
        connection, tx);

    cmd.Parameters.AddWithValue("historiaId", historiaId);
    cmd.Parameters.AddWithValue("edad", DbString(datos.Edad));
    cmd.Parameters.AddWithValue("edadMet", DbString(datos.EdadMetabolica));
    cmd.Parameters.AddWithValue("sexo", DbString(datos.Sexo));
    cmd.Parameters.AddWithValue("peso", DbDecimal(ParseDecimal(datos.Peso)));
    cmd.Parameters.AddWithValue("masaMuscular", DbDecimal(ParseDecimal(datos.MasaMuscular)));
    cmd.Parameters.AddWithValue("gcPorc", DbDecimal(ParseDecimal(datos.GrasaCorporalPorcentaje)));
    cmd.Parameters.AddWithValue("gc", DbDecimal(ParseDecimal(datos.GrasaCorporal)));
    cmd.Parameters.AddWithValue("talla", DbDecimal(ParseDecimal(datos.Talla)));
    cmd.Parameters.AddWithValue("gvPorc", DbDecimal(ParseDecimal(datos.GrasaVisceralPorcentaje)));
    cmd.Parameters.AddWithValue("imc", DbDecimal(ParseDecimal(datos.IMC)));
    cmd.Parameters.AddWithValue("kcal", DbInt(ParseInt(datos.KcalBasales)));
    cmd.Parameters.AddWithValue("actividad", DbString(datos.ActividadFisica));
    cmd.Parameters.AddWithValue("cintura", DbDecimal(ParseDecimal(datos.CircunferenciaCintura)));
    cmd.Parameters.AddWithValue("cadera", DbDecimal(ParseDecimal(datos.CircunferenciaCadera)));
    cmd.Parameters.AddWithValue("pantorrilla", DbDecimal(ParseDecimal(datos.Pantorrilla)));
    cmd.Parameters.AddWithValue("cBrazo", DbDecimal(ParseDecimal(datos.CircunferenciaBrazo)));
    cmd.Parameters.AddWithValue("cMuslo", DbDecimal(ParseDecimal(datos.CircunferenciaMuslo)));
    cmd.Parameters.AddWithValue("pesoAjustado", DbDecimal(ParseDecimal(datos.PesoAjustado)));
    cmd.Parameters.AddWithValue("factor", DbDecimal(ParseDecimal(datos.FactorActividadFisica)));
    cmd.Parameters.AddWithValue("tiempos", DbString(datos.TiemposComida));

    await cmd.ExecuteNonQueryAsync();
}

static async Task InsertBioquimicosAsync(NpgsqlConnection connection, NpgsqlTransaction tx, Guid historiaId, BioquimicosData? datos)
{
    if (datos is null) return;

    await using var cmd = new NpgsqlCommand(
        @"INSERT INTO valores_bioquimicos (historia_id, glicemia, colesterol_total, trigliceridos, hdl, ldl, tgo, tgp, urea, creatinina)
          VALUES (@historiaId, @glicemia, @colesterol, @trigliceridos, @hdl, @ldl, @tgo, @tgp, @urea, @creatinina)",
        connection, tx);

    cmd.Parameters.AddWithValue("historiaId", historiaId);
    cmd.Parameters.AddWithValue("glicemia", DbDecimal(ParseDecimal(datos.Glicemia)));
    cmd.Parameters.AddWithValue("colesterol", DbDecimal(ParseDecimal(datos.ColesterolTotal)));
    cmd.Parameters.AddWithValue("trigliceridos", DbDecimal(ParseDecimal(datos.Trigliceridos)));
    cmd.Parameters.AddWithValue("hdl", DbDecimal(ParseDecimal(datos.HDL)));
    cmd.Parameters.AddWithValue("ldl", DbDecimal(ParseDecimal(datos.LDL)));
    cmd.Parameters.AddWithValue("tgo", DbDecimal(ParseDecimal(datos.TGO)));
    cmd.Parameters.AddWithValue("tgp", DbDecimal(ParseDecimal(datos.TGP)));
    cmd.Parameters.AddWithValue("urea", DbDecimal(ParseDecimal(datos.Urea)));
    cmd.Parameters.AddWithValue("creatinina", DbDecimal(ParseDecimal(datos.Creatinina)));

    await cmd.ExecuteNonQueryAsync();
}

static async Task InsertRecordatorioAsync(NpgsqlConnection connection, NpgsqlTransaction tx, Guid historiaId, Recordatorio24hData? recordatorio)
{
    if (recordatorio is null) return;

    await using var cmd = new NpgsqlCommand(
        @"INSERT INTO recordatorio_24h (historia_id, desayuno, snack1, almuerzo, snack2, cena, extras)
          VALUES (@historiaId, @desayuno, @snack1, @almuerzo, @snack2, @cena, @extras)",
        connection, tx);

    cmd.Parameters.AddWithValue("historiaId", historiaId);
    cmd.Parameters.AddWithValue("desayuno", DbString(recordatorio.Desayuno));
    cmd.Parameters.AddWithValue("snack1", DbString(recordatorio.Snack1));
    cmd.Parameters.AddWithValue("almuerzo", DbString(recordatorio.Almuerzo));
    cmd.Parameters.AddWithValue("snack2", DbString(recordatorio.Snack2));
    cmd.Parameters.AddWithValue("cena", DbString(recordatorio.Cena));
    cmd.Parameters.AddWithValue("extras", DbString(recordatorio.Extras));

    await cmd.ExecuteNonQueryAsync();
}

static async Task InsertFrecuenciaAsync(NpgsqlConnection connection, NpgsqlTransaction tx, Guid historiaId, IDictionary<string, string>? frequency)
{
    if (frequency is null || frequency.Count == 0) return;

    foreach (var entry in frequency)
    {
        var parts = entry.Key.Split("::", 2);
        var categoria = parts.Length > 1 ? parts[0] : "General";
        var alimento = parts.Length > 1 ? parts[1] : parts[0];

        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO frecuencia_consumo (historia_id, categoria, alimento, frecuencia)
              VALUES (@historiaId, @categoria, @alimento, @frecuencia)",
            connection, tx);

        cmd.Parameters.AddWithValue("historiaId", historiaId);
        cmd.Parameters.AddWithValue("categoria", categoria);
        cmd.Parameters.AddWithValue("alimento", alimento);
        cmd.Parameters.AddWithValue("frecuencia", entry.Value);

        await cmd.ExecuteNonQueryAsync();
    }
}

static object DbString(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
static object DbDecimal(decimal? value) => value.HasValue ? value.Value : DBNull.Value;
static object DbInt(int? value) => value.HasValue ? value.Value : DBNull.Value;
static object DbDate(DateTime? value) => value.HasValue ? value.Value : DBNull.Value;

static decimal? ParseDecimal(string? input)
{
    if (string.IsNullOrWhiteSpace(input)) return null;
    if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
    {
        return result;
    }

    if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
    {
        return result;
    }

    return null;
}

static int? ParseInt(string? input)
{
    if (string.IsNullOrWhiteSpace(input)) return null;
    if (int.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
    {
        return result;
    }

    if (int.TryParse(input, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
    {
        return result;
    }

    return null;
}

static DateTime? ParseDate(string? input)
{
    if (string.IsNullOrWhiteSpace(input)) return null;
    if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result))
    {
        return result.Date;
    }

    if (DateTime.TryParse(input, out result))
    {
        return result.Date;
    }

    return null;
}

public record LoginRequest(string Username, string Password);

public record RegisterRequest(string Username, string Password, string? Email, string? Nombre);

public record LoginResponse(bool Success, string Token, PublicUser User);

public record PublicUser(Guid Id, string Username, string Nombre, string Email);

public class HistoryRequest
{
    public PersonalData? PersonalData { get; set; }
    public string? MotivoConsulta { get; set; }
    public AntecedentesData? Antecedentes { get; set; }
    public string? Diagnostico { get; set; }
    public HabitosData? Habitos { get; set; }
    public SignosData? SignosVitales { get; set; }
    public AntropometricosData? DatosAntropometricos { get; set; }
    public BioquimicosData? ValoresBioquimicos { get; set; }
    public Recordatorio24hData? Recordatorio24h { get; set; }
    public IDictionary<string, string>? Frequency { get; set; }
    public string? NotasExtras { get; set; }
}

public class PersonalData
{
    public string? Nombre { get; set; }
    public string? EdadCronologica { get; set; }
    public string? Sexo { get; set; }
    public string? LugarResidencia { get; set; }
    public string? EstadoCivil { get; set; }
    public string? NumeroCedula { get; set; }
    public string? Telefono { get; set; }
    public string? Ocupacion { get; set; }
    public string? Email { get; set; }
    public string? FechaConsulta { get; set; }
}

public class AntecedentesData
{
    public string? APF { get; set; }
    public string? APP { get; set; }
    public string? APQ { get; set; }
    public string? AGO { get; set; }
    public string? Menarquia { get; set; }
    public string? P { get; set; }
    public string? G { get; set; }
    public string? C { get; set; }
    public string? A { get; set; }
    public string? Alergias { get; set; }
}

public class HabitosData
{
    public string? Fuma { get; set; }
    public string? Alcohol { get; set; }
    public string? Cafe { get; set; }
    public string? Hidratacion { get; set; }
    public string? Gaseosas { get; set; }
    public string? ActividadFisica { get; set; }
    public string? Te { get; set; }
    public string? Edulcorantes { get; set; }
    public string? Alimentacion { get; set; }
}

public class SignosData
{
    public string? PresionArterial { get; set; }
    public string? Temperatura { get; set; }
    public string? FrecuenciaCardiaca { get; set; }
    public string? FrecuenciaRespiratoria { get; set; }
}

public class AntropometricosData
{
    public string? Edad { get; set; }
    public string? EdadMetabolica { get; set; }
    public string? Sexo { get; set; }
    public string? Peso { get; set; }
    public string? MasaMuscular { get; set; }
    public string? GrasaCorporalPorcentaje { get; set; }
    public string? GrasaCorporal { get; set; }
    public string? Talla { get; set; }
    public string? GrasaVisceralPorcentaje { get; set; }
    public string? IMC { get; set; }
    public string? KcalBasales { get; set; }
    public string? ActividadFisica { get; set; }
    public string? CircunferenciaCintura { get; set; }
    public string? CircunferenciaCadera { get; set; }
    public string? CircunferenciaBrazo { get; set; }
    public string? Pantorrilla { get; set; }
    public string? CircunferenciaMuslo { get; set; }
    public string? PesoAjustado { get; set; }
    public string? FactorActividadFisica { get; set; }
    public string? TiemposComida { get; set; }
}

public class BioquimicosData
{
    public string? Glicemia { get; set; }
    public string? ColesterolTotal { get; set; }
    public string? Trigliceridos { get; set; }
    public string? HDL { get; set; }
    public string? LDL { get; set; }
    public string? TGO { get; set; }
    public string? TGP { get; set; }
    public string? Urea { get; set; }
    public string? Creatinina { get; set; }
}

public class Recordatorio24hData
{
    public string? Desayuno { get; set; }
    public string? Snack1 { get; set; }
    public string? Almuerzo { get; set; }
    public string? Snack2 { get; set; }
    public string? Cena { get; set; }
    public string? Extras { get; set; }
}

public class UpdatePacienteRequest
{
    public string? NumeroCedula { get; set; }
    public string? Nombre { get; set; }
    public string? EdadCronologica { get; set; }
    public string? Sexo { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? LugarResidencia { get; set; }
    public string? EstadoCivil { get; set; }
    public string? Ocupacion { get; set; }
}

public class CrearPlanRequest
{
    public string HistoriaId { get; set; } = string.Empty;
    public string FechaInicio { get; set; } = string.Empty;
    public string? FechaFin { get; set; }
    public string? Objetivo { get; set; }
    public decimal? CaloriasDiarias { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;
    public List<AlimentacionSemanalItem>? AlimentacionSemanal { get; set; }
}

public class AlimentacionSemanalItem
{
    public int Semana { get; set; }
    public int DiaSemana { get; set; }
    public string? Desayuno { get; set; }
    public string? SnackManana { get; set; }
    public string? Almuerzo { get; set; }
    public string? SnackTarde { get; set; }
    public string? Cena { get; set; }
    public string? SnackNoche { get; set; }
    public string? Observaciones { get; set; }
}

public class ActualizarPlanRequest
{
    public string? FechaFin { get; set; }
    public string? Objetivo { get; set; }
    public decimal? CaloriasDiarias { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;
}

public class GuardarPlanAlimentacionRequest
{
    public string HistoriaId { get; set; } = string.Empty;
    public string FechaInicio { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    public string? FechaFin { get; set; }
    public string? Objetivo { get; set; }
    public string? Observaciones { get; set; }
    public Dictionary<string, Dictionary<string, object>>? Semana1 { get; set; }
    public Dictionary<string, Dictionary<string, object>>? Semana2 { get; set; }
}





