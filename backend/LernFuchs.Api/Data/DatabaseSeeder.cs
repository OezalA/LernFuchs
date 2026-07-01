using System.Text.Json;
using LernFuchs.Api.Models;
using LernFuchs.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace LernFuchs.Api.Data;

/// <summary>
/// Füllt eine frische Datenbank beim Start mit Startinhalten aus <c>Data/seed-data.json</c>
/// (10 Lesetexte samt Fragen und den zugehörigen Vokabeln). Läuft nur, wenn noch keine
/// Texte vorhanden sind – vorhandene Nutzerinhalte werden nie überschrieben.
/// </summary>
public static class DatabaseSeeder
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static async Task SeedAsync(AppDbContext db, string contentRoot, ILogger logger, CancellationToken ct = default)
    {
        if (await db.ReadingPassages.AnyAsync(ct))
            return; // Es gibt bereits Inhalte – nichts tun.

        var path = Path.Combine(contentRoot, "Data", "seed-data.json");
        if (!File.Exists(path))
        {
            logger.LogWarning("Seed-Datei nicht gefunden: {Path}", path);
            return;
        }

        var json = await File.ReadAllTextAsync(path, ct);
        var seed = JsonSerializer.Deserialize<SeedRoot>(json, JsonOpts);
        if (seed is null) return;

        foreach (var p in seed.Passages)
        {
            db.ReadingPassages.Add(new ReadingPassage
            {
                Title = p.Title,
                Text = p.Text,
                Difficulty = ParseEnum(p.Difficulty, Difficulty.Mittel),
                Topic = p.Topic,
                WordCount = p.WordCount,
                Questions = p.Questions.Select(q => new ComprehensionQuestion
                {
                    QuestionText = q.QuestionText,
                    QuestionType = QuestionType.MultipleChoice,
                    Options = q.Options,
                    CorrectAnswer = q.CorrectAnswer,
                    Explanation = q.Explanation
                }).ToList()
            });
        }

        var seen = new HashSet<string>();
        foreach (var w in seed.Words)
        {
            if (!seen.Add(w.Word.Trim().ToLowerInvariant())) continue;
            db.VocabularyWords.Add(new VocabularyWord
            {
                Word = w.Word,
                Article = ParseEnum(w.Article, Article.None),
                Plural = w.Plural,
                WordType = ParseEnum(w.WordType, WordType.Sonstiges),
                DefinitionGerman = w.DefinitionGerman,
                ExampleSentence = w.ExampleSentence,
                Synonyms = w.Synonyms,
                Antonyms = w.Antonyms,
                Difficulty = ParseEnum(w.Difficulty, Difficulty.Mittel),
                Topic = w.Topic
            });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Startinhalte geladen: {Passages} Texte, {Words} Wörter.",
            seed.Passages.Count, seed.Words.Count);
    }

    /// <summary>Baut aus generierten Inhalten die Seed-Struktur (für das einmalige Erzeugen der Datei).</summary>
    public static SeedRoot Build(IEnumerable<GeneratedReading> generated)
    {
        var passages = new List<SeedPassage>();
        var words = new List<SeedWord>();
        var seen = new HashSet<string>();

        foreach (var g in generated)
        {
            passages.Add(new SeedPassage(
                g.Passage.Title,
                g.Passage.Text,
                g.Passage.Difficulty.ToString(),
                g.Passage.Topic ?? "",
                g.Passage.WordCount,
                g.Passage.Questions.Select(q => new SeedQuestion(
                    q.QuestionText, q.Options, q.CorrectAnswer, q.Explanation)).ToList()));

            foreach (var w in g.DifficultWords)
            {
                if (!seen.Add(w.Word.Trim().ToLowerInvariant())) continue;
                words.Add(new SeedWord(
                    w.Word, w.Article.ToString(), w.Plural, w.WordType.ToString(),
                    w.DefinitionGerman, w.ExampleSentence, w.Synonyms, w.Antonyms,
                    w.Difficulty.ToString(), w.Topic ?? ""));
            }
        }

        return new SeedRoot(passages, words);
    }

    public static string Serialize(SeedRoot seed) => JsonSerializer.Serialize(seed, JsonOpts);

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : fallback;
}
