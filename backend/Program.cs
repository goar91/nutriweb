using System.Text.Json;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("NutritionDb")
    ?? Environment.GetEnvironmentVariable("NUTRITION_DB")
    ?? "Host=localhost;Port=5432;Database=nutriciondb;Username=postgres;Password=postgres;Pooling=true;Trust Server Certificate=true";

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();

app.MapGet("/api/nutrition/status", async () =>
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    return Results.Ok(new { status = "running", database = connection.Database });
});

app.MapPost("/api/nutrition/history", async (JsonElement payload) =>
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    await EnsureClinicalHistoryTableAsync(connection);

    using var document = JsonDocument.Parse(payload.GetRawText());
    await using var command = new NpgsqlCommand(
        "INSERT INTO clinical_histories (id, payload) VALUES (@id, @payload)",
        connection);

    command.Parameters.AddWithValue("id", Guid.NewGuid());
    command.Parameters.AddWithValue("payload", document.RootElement.Clone());

    await command.ExecuteNonQueryAsync();

    return Results.Created("/api/nutrition/history", new { status = "created" });
});

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
