using LernFuchs.Api.Data;
using LernFuchs.Api.Services;
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

// --- Web API ---
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // Enums als Strings serialisieren (der/die/das statt 0/1/2).
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();

// --- CORS für das Angular-Frontend ---
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(options =>
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

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

app.UseHttpsRedirection();
app.UseCors("frontend");
app.UseAuthorization();
app.MapControllers();

app.Run();
