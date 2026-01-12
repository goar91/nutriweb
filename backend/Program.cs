using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

var connectionString = ResolveConnectionString(builder.Configuration);

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
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    return Results.Ok(new { status = "running", database = connection.Database });
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
    await InsertSignosAsync(connection, tx, historiaId, request.Signos);
    await InsertAntropometricosAsync(connection, tx, historiaId, request.Antropometricos);
    await InsertBioquimicosAsync(connection, tx, historiaId, request.ValoresBioquimicos);
    await InsertRecordatorioAsync(connection, tx, historiaId, request.Recordatorio24h);
    await InsertFrecuenciaAsync(connection, tx, historiaId, request.Frequency);

    await tx.CommitAsync();

    return Results.Created($"/api/nutrition/history/{historiaId}", new { status = "created", id = historiaId });
});

// ============================================
// API de Pacientes
// ============================================

app.MapGet("/api/pacientes", async (HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    await using var cmd = new NpgsqlCommand(
        @"SELECT id, numero_cedula, nombre, edad_cronologica, sexo, telefono, email, 
                 fecha_creacion, fecha_actualizacion
          FROM pacientes
          ORDER BY fecha_actualizacion DESC",
        connection);

    var pacientes = new List<object>();
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        pacientes.Add(new
        {
            id = reader.GetGuid(0),
            numeroCedula = reader.IsDBNull(1) ? null : reader.GetString(1),
            nombre = reader.IsDBNull(2) ? null : reader.GetString(2),
            edadCronologica = reader.IsDBNull(3) ? null : reader.GetString(3),
            sexo = reader.IsDBNull(4) ? null : reader.GetString(4),
            telefono = reader.IsDBNull(5) ? null : reader.GetString(5),
            email = reader.IsDBNull(6) ? null : reader.GetString(6),
            fechaCreacion = reader.GetDateTime(7),
            fechaActualizacion = reader.GetDateTime(8)
        });
    }

    return Results.Ok(pacientes);
});

app.MapGet("/api/pacientes/{id:guid}", async (Guid id, HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    await using var cmd = new NpgsqlCommand(
        @"SELECT id, numero_cedula, nombre, edad_cronologica, sexo, lugar_residencia, 
                 estado_civil, telefono, ocupacion, email, fecha_creacion, fecha_actualizacion
          FROM pacientes
          WHERE id = @id
          LIMIT 1",
        connection);
    cmd.Parameters.AddWithValue("id", id);

    await using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
    {
        return Results.NotFound(new { error = "Paciente no encontrado" });
    }

    var paciente = new
    {
        id = reader.GetGuid(0),
        numeroCedula = reader.IsDBNull(1) ? null : reader.GetString(1),
        nombre = reader.IsDBNull(2) ? null : reader.GetString(2),
        edadCronologica = reader.IsDBNull(3) ? null : reader.GetString(3),
        sexo = reader.IsDBNull(4) ? null : reader.GetString(4),
        lugarResidencia = reader.IsDBNull(5) ? null : reader.GetString(5),
        estadoCivil = reader.IsDBNull(6) ? null : reader.GetString(6),
        telefono = reader.IsDBNull(7) ? null : reader.GetString(7),
        ocupacion = reader.IsDBNull(8) ? null : reader.GetString(8),
        email = reader.IsDBNull(9) ? null : reader.GetString(9),
        fechaCreacion = reader.GetDateTime(10),
        fechaActualizacion = reader.GetDateTime(11)
    };

    return Results.Ok(paciente);
});

app.MapGet("/api/pacientes/{id:guid}/historias", async (Guid id, HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    await using var cmd = new NpgsqlCommand(
        @"SELECT h.id, h.fecha_consulta, h.motivo_consulta, h.diagnostico, h.fecha_registro,
                 a.imc, a.peso, a.talla
          FROM historias_clinicas h
          LEFT JOIN datos_antropometricos a ON a.historia_id = h.id
          WHERE h.paciente_id = @pacienteId
          ORDER BY h.fecha_consulta DESC",
        connection);
    cmd.Parameters.AddWithValue("pacienteId", id);

    var historias = new List<object>();
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        historias.Add(new
        {
            id = reader.GetGuid(0),
            fechaConsulta = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1),
            motivoConsulta = reader.IsDBNull(2) ? null : reader.GetString(2),
            diagnostico = reader.IsDBNull(3) ? null : reader.GetString(3),
            fechaRegistro = reader.GetDateTime(4),
            imc = reader.IsDBNull(5) ? (decimal?)null : reader.GetDecimal(5),
            peso = reader.IsDBNull(6) ? (decimal?)null : reader.GetDecimal(6),
            talla = reader.IsDBNull(7) ? (decimal?)null : reader.GetDecimal(7)
        });
    }

    return Results.Ok(historias);
});

app.MapDelete("/api/pacientes/{id:guid}", async (Guid id, HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    await using var cmd = new NpgsqlCommand(
        "DELETE FROM pacientes WHERE id = @id",
        connection);
    cmd.Parameters.AddWithValue("id", id);

    var affected = await cmd.ExecuteNonQueryAsync();
    if (affected == 0)
    {
        return Results.NotFound(new { error = "Paciente no encontrado" });
    }

    return Results.Ok(new { success = true, message = "Paciente eliminado" });
});

// ============================================
// API de Reportes
// ============================================

app.MapGet("/api/reportes/estadisticas", async (HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    await using var cmd = new NpgsqlCommand(
        @"SELECT 
            (SELECT COUNT(*) FROM pacientes) as total_pacientes,
            (SELECT COUNT(*) FROM historias_clinicas) as total_historias,
            (SELECT COUNT(*) FROM pacientes WHERE DATE_PART('day', NOW() - fecha_creacion) <= 30) as pacientes_mes,
            (SELECT COUNT(*) FROM historias_clinicas WHERE DATE_PART('day', NOW() - fecha_registro) <= 30) as historias_mes,
            (SELECT COUNT(*) FROM pacientes WHERE sexo = 'F' OR sexo = 'f') as pacientes_femenino,
            (SELECT COUNT(*) FROM pacientes WHERE sexo = 'M' OR sexo = 'm') as pacientes_masculino",
        connection);

    await using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
    {
        return Results.Ok(new
        {
            totalPacientes = 0,
            totalHistorias = 0,
            pacientesMes = 0,
            historiasMes = 0,
            pacientesFemenino = 0,
            pacientesMasculino = 0
        });
    }

    var stats = new
    {
        totalPacientes = reader.GetInt64(0),
        totalHistorias = reader.GetInt64(1),
        pacientesMes = reader.GetInt64(2),
        historiasMes = reader.GetInt64(3),
        pacientesFemenino = reader.GetInt64(4),
        pacientesMasculino = reader.GetInt64(5)
    };

    return Results.Ok(stats);
});

app.MapGet("/api/reportes/pacientes", async (string? fechaDesde, string? fechaHasta, HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    var sql = @"SELECT 
            p.id,
            p.numero_cedula,
            p.nombre,
            p.edad_cronologica,
            p.sexo,
            p.telefono,
            p.email,
            p.fecha_creacion,
            COUNT(h.id) as total_historias,
            MAX(h.fecha_consulta) as ultima_consulta
        FROM pacientes p
        LEFT JOIN historias_clinicas h ON h.paciente_id = p.id";

    var whereClauses = new List<string>();
    if (!string.IsNullOrWhiteSpace(fechaDesde))
    {
        whereClauses.Add("p.fecha_creacion >= @fechaDesde");
    }
    if (!string.IsNullOrWhiteSpace(fechaHasta))
    {
        whereClauses.Add("p.fecha_creacion <= @fechaHasta");
    }

    if (whereClauses.Count > 0)
    {
        sql += " WHERE " + string.Join(" AND ", whereClauses);
    }

    sql += @" GROUP BY p.id, p.numero_cedula, p.nombre, p.edad_cronologica, p.sexo, p.telefono, p.email, p.fecha_creacion
              ORDER BY p.fecha_creacion DESC";

    await using var cmd = new NpgsqlCommand(sql, connection);
    if (!string.IsNullOrWhiteSpace(fechaDesde))
    {
        cmd.Parameters.AddWithValue("fechaDesde", DateTime.Parse(fechaDesde));
    }
    if (!string.IsNullOrWhiteSpace(fechaHasta))
    {
        cmd.Parameters.AddWithValue("fechaHasta", DateTime.Parse(fechaHasta));
    }

    var pacientes = new List<object>();
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        pacientes.Add(new
        {
            id = reader.GetGuid(0),
            numeroCedula = reader.IsDBNull(1) ? null : reader.GetString(1),
            nombre = reader.IsDBNull(2) ? null : reader.GetString(2),
            edadCronologica = reader.IsDBNull(3) ? null : reader.GetString(3),
            sexo = reader.IsDBNull(4) ? null : reader.GetString(4),
            telefono = reader.IsDBNull(5) ? null : reader.GetString(5),
            email = reader.IsDBNull(6) ? null : reader.GetString(6),
            fechaCreacion = reader.GetDateTime(7),
            totalHistorias = reader.GetInt64(8),
            ultimaConsulta = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9)
        });
    }

    return Results.Ok(pacientes);
});

app.MapGet("/api/reportes/historias", async (string? fechaDesde, string? fechaHasta, HttpContext httpContext) =>
{
    if (!TryGetToken(httpContext, out var token) || token is null || !IsTokenValid(token, activeSessions, out var user))
    {
        return Results.Unauthorized();
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    var sql = @"SELECT 
            h.id as historia_id,
            h.fecha_consulta,
            h.motivo_consulta,
            h.diagnostico,
            h.fecha_registro,
            p.id as paciente_id,
            p.numero_cedula,
            p.nombre,
            p.edad_cronologica,
            p.sexo,
            a.imc,
            a.peso,
            a.talla
        FROM historias_clinicas h
        INNER JOIN pacientes p ON h.paciente_id = p.id
        LEFT JOIN datos_antropometricos a ON a.historia_id = h.id";

    var whereClauses = new List<string>();
    if (!string.IsNullOrWhiteSpace(fechaDesde))
    {
        whereClauses.Add("h.fecha_consulta >= @fechaDesde");
    }
    if (!string.IsNullOrWhiteSpace(fechaHasta))
    {
        whereClauses.Add("h.fecha_consulta <= @fechaHasta");
    }

    if (whereClauses.Count > 0)
    {
        sql += " WHERE " + string.Join(" AND ", whereClauses);
    }

    sql += " ORDER BY h.fecha_registro DESC LIMIT 500";

    await using var cmd = new NpgsqlCommand(sql, connection);
    if (!string.IsNullOrWhiteSpace(fechaDesde))
    {
        cmd.Parameters.AddWithValue("fechaDesde", DateTime.Parse(fechaDesde));
    }
    if (!string.IsNullOrWhiteSpace(fechaHasta))
    {
        cmd.Parameters.AddWithValue("fechaHasta", DateTime.Parse(fechaHasta));
    }

    var historias = new List<object>();
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        historias.Add(new
        {
            historiaId = reader.GetGuid(0),
            fechaConsulta = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1),
            motivoConsulta = reader.IsDBNull(2) ? null : reader.GetString(2),
            diagnostico = reader.IsDBNull(3) ? null : reader.GetString(3),
            fechaRegistro = reader.GetDateTime(4),
            pacienteId = reader.GetGuid(5),
            numeroCedula = reader.IsDBNull(6) ? null : reader.GetString(6),
            nombrePaciente = reader.IsDBNull(7) ? null : reader.GetString(7),
            edad = reader.IsDBNull(8) ? null : reader.GetString(8),
            sexo = reader.IsDBNull(9) ? null : reader.GetString(9),
            imc = reader.IsDBNull(10) ? (decimal?)null : reader.GetDecimal(10),
            peso = reader.IsDBNull(11) ? (decimal?)null : reader.GetDecimal(11),
            talla = reader.IsDBNull(12) ? (decimal?)null : reader.GetDecimal(12)
        });
    }

    return Results.Ok(historias);
});

app.Run();

static string ResolveConnectionString(IConfiguration configuration)
{
    var fromEnv = Environment.GetEnvironmentVariable("NUTRITION_DB");
    if (!string.IsNullOrWhiteSpace(fromEnv))
    {
        return fromEnv;
    }

    var current = Directory.GetCurrentDirectory();
    var connectionFile = Path.GetFullPath(Path.Combine(current, "..", "database", "connection.local"));
    if (File.Exists(connectionFile))
    {
        var text = File.ReadAllText(connectionFile).Trim();
        if (!string.IsNullOrWhiteSpace(text))
        {
            return text;
        }
    }

    var fromConfig = configuration.GetConnectionString("NutritionDb");
    if (!string.IsNullOrWhiteSpace(fromConfig))
    {
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
    cmd.Parameters.AddWithValue("apf", DbString(antecedentes.Apf));
    cmd.Parameters.AddWithValue("app", DbString(antecedentes.App));
    cmd.Parameters.AddWithValue("apq", DbString(antecedentes.Apq));
    cmd.Parameters.AddWithValue("ago", DbString(antecedentes.Ago));
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
    cmd.Parameters.AddWithValue("pa", DbString(signos.Pa));
    cmd.Parameters.AddWithValue("temp", DbString(signos.Temperatura));
    cmd.Parameters.AddWithValue("fc", DbString(signos.Fc));
    cmd.Parameters.AddWithValue("fr", DbString(signos.Fr));

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
    cmd.Parameters.AddWithValue("gcPorc", DbDecimal(ParseDecimal(datos.GcPorc)));
    cmd.Parameters.AddWithValue("gc", DbDecimal(ParseDecimal(datos.Gc)));
    cmd.Parameters.AddWithValue("talla", DbDecimal(ParseDecimal(datos.Talla)));
    cmd.Parameters.AddWithValue("gvPorc", DbDecimal(ParseDecimal(datos.GvPorc)));
    cmd.Parameters.AddWithValue("imc", DbDecimal(ParseDecimal(datos.Imc)));
    cmd.Parameters.AddWithValue("kcal", DbInt(ParseInt(datos.KcalBasales)));
    cmd.Parameters.AddWithValue("actividad", DbString(datos.ActividadFisica));
    cmd.Parameters.AddWithValue("cintura", DbDecimal(ParseDecimal(datos.Cintura)));
    cmd.Parameters.AddWithValue("cadera", DbDecimal(ParseDecimal(datos.Cadera)));
    cmd.Parameters.AddWithValue("pantorrilla", DbDecimal(ParseDecimal(datos.Pantorrilla)));
    cmd.Parameters.AddWithValue("cBrazo", DbDecimal(ParseDecimal(datos.CBrazo)));
    cmd.Parameters.AddWithValue("cMuslo", DbDecimal(ParseDecimal(datos.CMuslo)));
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
    cmd.Parameters.AddWithValue("hdl", DbDecimal(ParseDecimal(datos.Hdl)));
    cmd.Parameters.AddWithValue("ldl", DbDecimal(ParseDecimal(datos.Ldl)));
    cmd.Parameters.AddWithValue("tgo", DbDecimal(ParseDecimal(datos.Tgo)));
    cmd.Parameters.AddWithValue("tgp", DbDecimal(ParseDecimal(datos.Tgp)));
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
    public SignosData? Signos { get; set; }
    public AntropometricosData? Antropometricos { get; set; }
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
    public string? Apf { get; set; }
    public string? App { get; set; }
    public string? Apq { get; set; }
    public string? Ago { get; set; }
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
    public string? Pa { get; set; }
    public string? Temperatura { get; set; }
    public string? Fc { get; set; }
    public string? Fr { get; set; }
}

public class AntropometricosData
{
    public string? Edad { get; set; }
    public string? EdadMetabolica { get; set; }
    public string? Sexo { get; set; }
    public string? Peso { get; set; }
    public string? MasaMuscular { get; set; }
    public string? GcPorc { get; set; }
    public string? Gc { get; set; }
    public string? Talla { get; set; }
    public string? GvPorc { get; set; }
    public string? Imc { get; set; }
    public string? KcalBasales { get; set; }
    public string? ActividadFisica { get; set; }
    public string? Cintura { get; set; }
    public string? Cadera { get; set; }
    public string? Pantorrilla { get; set; }
    public string? CBrazo { get; set; }
    public string? CMuslo { get; set; }
    public string? PesoAjustado { get; set; }
    public string? FactorActividadFisica { get; set; }
    public string? TiemposComida { get; set; }
}

public class BioquimicosData
{
    public string? Glicemia { get; set; }
    public string? ColesterolTotal { get; set; }
    public string? Trigliceridos { get; set; }
    public string? Hdl { get; set; }
    public string? Ldl { get; set; }
    public string? Tgo { get; set; }
    public string? Tgp { get; set; }
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
