using LernFuchs.Api.Data;
using LernFuchs.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LernFuchs.Api.Services;

/// <summary>
/// Erzeugt einmal pro Tag automatisch neue Lesetexte (samt Wortschatz), damit die
/// öffentliche Seite ohne Nutzer-Generierung stetig wächst. Läuft nur, wenn aktiviert.
/// </summary>
public class DailyContentService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly FeatureOptions _features;
    private readonly ILogger<DailyContentService> _logger;

    // Über die Tage rotierender Themenpool (altersgerecht, 5. Klasse).
    private static readonly string[] TopicPool =
    {
        "Der Ozean", "Dinosaurier", "Pflanzen und Bäume", "Das Wetter", "Berühmte Erfinder",
        "Der menschliche Körper", "Vulkane", "Die Wüste", "Musikinstrumente", "Gesunde Ernährung",
        "Fahrzeuge", "Das Mittelalter", "Die Planeten", "Insekten", "Wälder der Welt",
        "Feste und Bräuche", "Roboter und Technik", "Berühmte Bauwerke", "Der Bauernhof", "Recycling",
        "Sterne und Galaxien", "Piraten", "Wüstentiere", "Das Wasser", "Brücken und Tunnel",
    };

    private static readonly string[] Models =
    {
        "gemini-2.5-flash", "gemini-flash-lite-latest", "gemini-flash-latest",
    };

    public DailyContentService(IServiceProvider services, IOptions<FeatureOptions> features,
        ILogger<DailyContentService> logger)
    {
        _services = services;
        _features = features.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_features.DailyAutoGenerationEnabled)
        {
            _logger.LogInformation("Täglicher Inhaltsdienst ist deaktiviert.");
            return;
        }

        // Kurz warten, bis die App vollständig gestartet ist.
        if (!await DelaySafe(TimeSpan.FromSeconds(20), stoppingToken)) return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunIfDueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Täglicher Inhaltsdienst ist fehlgeschlagen.");
            }

            // Mehrmals täglich nachsehen (falls ein Lauf misslang), erzeugt aber nur einmal pro Tag.
            if (!await DelaySafe(TimeSpan.FromHours(6), stoppingToken)) break;
        }
    }

    private async Task RunIfDueAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var content = scope.ServiceProvider.GetRequiredService<IContentGenerationService>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var state = await db.SystemStates.FirstOrDefaultAsync(ct);
        if (state is null)
        {
            state = new SystemState();
            db.SystemStates.Add(state);
        }
        if (state.LastDailyContentDate == today) return; // heute bereits erledigt

        var count = Math.Clamp(_features.DailyTextCount, 1, 10);
        var start = (today.DayNumber * count) % TopicPool.Length;

        var existingWords = (await db.VocabularyWords.Select(w => w.Word).ToListAsync(ct))
            .Select(w => w.ToLowerInvariant())
            .ToHashSet();

        var made = 0;
        for (var i = 0; i < count; i++)
        {
            var topic = TopicPool[(start + i) % TopicPool.Length];
            var difficulty = (Difficulty)(i % 3); // Leicht/Mittel/Schwer im Wechsel
            var model = Models[i % Models.Length];
            try
            {
                var generated = await content.GenerateReadingPassageAsync(topic, difficulty, 4, ct, model);
                db.ReadingPassages.Add(generated.Passage);
                foreach (var w in generated.DifficultWords)
                    if (existingWords.Add(w.Word.ToLowerInvariant()))
                        db.VocabularyWords.Add(w);
                made++;
                _logger.LogInformation("Tagesinhalt erzeugt: {Topic} ({Difficulty})", topic, difficulty);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tagesinhalt fehlgeschlagen: {Topic}", topic);
            }
            await DelaySafe(TimeSpan.FromSeconds(4), ct);
        }

        // Nur als erledigt markieren, wenn wenigstens ein Text erzeugt wurde (sonst später erneut versuchen).
        if (made > 0) state.LastDailyContentDate = today;
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Täglicher Inhalt abgeschlossen: {Made}/{Count} Texte.", made, count);
    }

    private static async Task<bool> DelaySafe(TimeSpan delay, CancellationToken ct)
    {
        try { await Task.Delay(delay, ct); return true; }
        catch (TaskCanceledException) { return false; }
    }
}
