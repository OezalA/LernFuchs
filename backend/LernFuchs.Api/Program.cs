using LernFuchs.Api.Data;
using LernFuchs.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- Datenbank (SQLite via EF Core) ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// --- Gemini-Content-Service ---
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.AddHttpClient<IContentGenerationService, GeminiContentGenerationService>();

// --- Spiel-/Fortschrittslogik ---
builder.Services.AddScoped<GameService>();

// --- Feature-Flags ---
builder.Services.Configure<FeatureOptions>(builder.Configuration.GetSection("Features"));

// --- Täglicher Inhaltsdienst (erzeugt automatisch neue Texte) ---
builder.Services.AddHostedService<DailyContentService>();

// --- Web API ---
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // Enums als Strings serialisieren (der/die/das statt 0/1/2).
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();

// --- CORS (nur für lokale Entwicklung nötig; in Produktion wird alles von hier ausgeliefert) ---
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(options =>
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Verzeichnis der SQLite-Datei sicherstellen (falls ein persistenter Pfad gesetzt ist).
var dbPath = new SqliteConnectionStringBuilder(
    builder.Configuration.GetConnectionString("Default")).DataSource;
var dbDir = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrEmpty(dbDir)) Directory.CreateDirectory(dbDir);

// Datenbank bei Start automatisch anlegen/migrieren und ggf. mit Startinhalten füllen.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DatabaseSeeder.SeedAsync(db, app.Environment.ContentRootPath, logger);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // UI unter /scalar/v1
}

// Angular-Frontend (statische Dateien aus wwwroot) ausliefern.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("frontend");
app.UseAuthorization();
app.MapControllers();

// SPA-Fallback: unbekannte Pfade (z. B. /wortschatz) liefern die Angular-Startseite.
app.MapFallbackToFile("index.html");

app.Run();
