using System.Collections.Concurrent;
using System.Text.Json;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("NutritionDb")
    ?? Environment.GetEnvironmentVariable("NUTRITION_DB")
    ?? "Host=localhost;Port=5432;Database=nutriciondb;Username=postgres;Password=postgres;Pooling=true;Trust Server Certificate=true";

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();

// Usuarios de demostración (sustituir por una tabla real cuando esté lista)
var users = new List<User>
{
    new(1, "admin", "Administrador", "admin@nutriweb.local", "admin123"),
    new(2, "nutri", "Nutricionista", "nutri@nutriweb.local", "nutri123")
};

var activeSessions = new ConcurrentDictionary<string, PublicUser>();

app.MapPost("/api/auth/login", (LoginRequest request) =>
{
    var user = users.FirstOrDefault(u =>
        string.Equals(u.Username, request.Username, StringComparison.OrdinalIgnoreCase) &&
        u.Password == request.Password);

    if (user is null)
    {
        return Results.Json(new { success = false, error = "Credenciales inválidas" }, statusCode: StatusCodes.Status401Unauthorized);
    }

    var token = Guid.NewGuid().ToString("N");
    var publicUser = user.WithoutPassword();
    activeSessions[token] = publicUser;

    return Results.Ok(new LoginResponse(true, token, publicUser));
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
    if (TryGetToken(context, out var token) && token is not null && activeSessions.TryGetValue(token, out var user))
    {
        return Results.Ok(new { valid = true, user });
    }

    return Results.Unauthorized();
});

app.MapGet("/api/nutrition/status", async () =>
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    return Results.Ok(new { status = "running", database = connection.Database });
});

app.MapPost("/api/nutrition/history", async (JsonElement payload) =>
{
    if (payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
    {
        return Results.BadRequest(new { error = "Payload inválido" });
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    await EnsureClinicalHistoryTableAsync(connection);

    using var document = JsonDocument.Parse(payload.GetRawText());
    var id = Guid.NewGuid();

    await using var command = new NpgsqlCommand(
        "INSERT INTO clinical_histories (id, payload) VALUES (@id, @payload)",
        connection);

    command.Parameters.AddWithValue("id", id);
    command.Parameters.AddWithValue("payload", document.RootElement.Clone());

    await command.ExecuteNonQueryAsync();

    return Results.Created($"/api/nutrition/history/{id}", new { status = "created", id });
});

app.Run();

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

static async Task EnsureClinicalHistoryTableAsync(NpgsqlConnection connection)
{
    const string script = @"
CREATE TABLE IF NOT EXISTS clinical_histories (
  id uuid PRIMARY KEY,
  payload jsonb NOT NULL,
  recorded_at timestamptz NOT NULL DEFAULT NOW()
);";

    await using var command = new NpgsqlCommand(script, connection);
    await command.ExecuteNonQueryAsync();
}

public record LoginRequest(string Username, string Password);

public record LoginResponse(bool Success, string Token, PublicUser User);

public record User(int Id, string Username, string Nombre, string Email, string Password)
{
    public PublicUser WithoutPassword() => new(Id, Username, Nombre, Email);
}

public record PublicUser(int Id, string Username, string Nombre, string Email);
