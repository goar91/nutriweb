using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Configuración de la base de datos
var connectionString = builder.Configuration.GetConnectionString("NutritionDb")
    ?? Environment.GetEnvironmentVariable("NUTRITION_DB")
    ?? "Host=localhost;Port=5432;Database=nutriciondb;Username=postgres;Password=postgres;Pooling=true;Trust Server Certificate=true";

// Configuración de CORS mejorada
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200", "http://localhost:54107")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// Configuración de Auth0
var auth0Domain = builder.Configuration["Auth0:Domain"] ?? "";
var auth0Audience = builder.Configuration["Auth0:Audience"] ?? "";

if (!string.IsNullOrEmpty(auth0Domain) && !string.IsNullOrEmpty(auth0Audience))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = $"https://{auth0Domain}/";
            options.Audience = auth0Audience;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };
        });
}

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/nutrition/status", async () =>
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    return Results.Ok(new { status = "running", database = connection.Database });
});

app.MapPost("/api/nutrition/history", async (JsonElement payload) =>
{
    try
    {
        if (payload.ValueKind == JsonValueKind.Undefined || payload.ValueKind == JsonValueKind.Null)
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
    }
    catch (JsonException)
    {
        return Results.BadRequest(new { error = "JSON inválido" });
    }
    catch (NpgsqlException ex)
    {
        Console.Error.WriteLine($"Error de base de datos: {ex.Message}");
        return Results.Problem("Error al guardar en la base de datos", statusCode: 500);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error inesperado: {ex.Message}");
        return Results.Problem("Error interno del servidor", statusCode: 500);
    }
}); // .RequireAuthorization(); // Descomentar para requerir autenticación

app.Run();

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
