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

    // Über die Tage rotierender Themenpool, nach Kategorien verschränkt, damit
    // die 5 Themen eines Tages möglichst unterschiedliche Kategorien abdecken.
    private static readonly string[] TopicPool =
    {
        // Runde 1 – je ein Thema pro Kategorie
        "Haustiere", "Der Wald", "Fußball", "Deutschland", "Berühmte Märchen",
        "Die Ritter", "Berühmte Erfinder", "Die Planeten", "Der menschliche Körper",
        "Freundschaft", "Gesunde Ernährung", "Berufe: Feuerwehr", "Piraten",
        // Runde 2
        "Wilde Tiere Afrikas", "Das Wetter", "Musikinstrumente", "Frankreich", "Fabeln von Äsop",
        "Das alte Rom", "Roboter und Technik", "Sterne und Galaxien", "Unsere fünf Sinne",
        "Meine Familie", "Obst und Gemüse", "Verschiedene Berufe", "Drachen und Abenteuer",
        // Runde 3
        "Insekten", "Vulkane und die Erde", "Schwimmen lernen", "Feste in aller Welt",
        "Sagen und Legenden", "Die Wikinger", "Brücken und Tunnel", "Astronauten im Weltall",
        "Gesund bleiben", "Eine spannende Schatzsuche", "Der Ozean und seine Tiere",
    };

    // Einfache Alltagsthemen für die Fremdsprache Englisch (Anfängerniveau).
    private static readonly string[] EnglishTopicPool =
    {
        "Animals", "My Family", "Food and Drinks", "Colours", "My School",
        "The Body", "Clothes", "The Weather", "Hobbies", "Pets",
        "Fruits and Vegetables", "The House", "Sports", "Toys", "Feelings",
        "Jobs", "Nature", "Holidays", "Days and Months", "My Town",
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

        var germanCount = Math.Clamp(_features.DailyTextCount, 0, 10);
        var englishCount = Math.Clamp(_features.DailyEnglishTextCount, 0, 10);

        // Deutsche (Muttersprache) und englische (Fremdsprache) Inhalte erzeugen.
        var madeDe = await GenerateBatchAsync(db, content, Language.Deutsch, TopicPool, germanCount, today, ct);
        var madeEn = await GenerateBatchAsync(db, content, Language.Englisch, EnglishTopicPool, englishCount, today, ct);

        // Nur als erledigt markieren, wenn wenigstens ein Text erzeugt wurde (sonst später erneut versuchen).
        if (madeDe + madeEn > 0) state.LastDailyContentDate = today;
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Täglicher Inhalt abgeschlossen: {De} DE + {En} EN Texte.", madeDe, madeEn);
    }

    /// <summary>Erzeugt einen Tagesstapel Texte einer Sprache und verknüpft die schwierigen Wörter.</summary>
    private async Task<int> GenerateBatchAsync(
        AppDbContext db, IContentGenerationService content, Language language,
        string[] pool, int count, DateOnly today, CancellationToken ct)
    {
        if (count <= 0) return 0;
        var start = (today.DayNumber * count) % pool.Length;

        // Dubletten je Sprache vermeiden (gleiches Wort in DE und EN ist erlaubt).
        var existingWords = (await db.VocabularyWords
                .Where(w => w.Language == language)
                .Select(w => w.Word).ToListAsync(ct))
            .Select(w => w.ToLowerInvariant())
            .ToHashSet();

        var made = 0;
        for (var i = 0; i < count; i++)
        {
            var topic = pool[(start + i) % pool.Length];
            // Fremdsprache bewusst leicht halten; Muttersprache je nach Thema.
            var difficulty = language == Language.Englisch ? Difficulty.Leicht : TopicDifficulty.For(topic);
            var model = Models[i % Models.Length];
            try
            {
                var generated = await content.GenerateReadingPassageAsync(topic, difficulty, 4, language, ct, model);
                db.ReadingPassages.Add(generated.Passage);
                await db.SaveChangesAsync(ct); // Passage-Id für die Verknüpfung

                foreach (var w in generated.DifficultWords)
                    if (existingWords.Add(w.Word.ToLowerInvariant()))
                    {
                        w.SourcePassageId = generated.Passage.Id;
                        db.VocabularyWords.Add(w);
                    }
                made++;
                _logger.LogInformation("Tagesinhalt erzeugt: {Topic} ({Lang}, {Difficulty})", topic, language, difficulty);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tagesinhalt fehlgeschlagen: {Topic} ({Lang})", topic, language);
            }
            await DelaySafe(TimeSpan.FromSeconds(4), ct);
        }
        return made;
    }

    private static async Task<bool> DelaySafe(TimeSpan delay, CancellationToken ct)
    {
        try { await Task.Delay(delay, ct); return true; }
        catch (TaskCanceledException) { return false; }
    }
}
