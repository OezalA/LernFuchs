using LernFuchs.Api.Data;
using LernFuchs.Api.Models;
using LernFuchs.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LernFuchs.Tests;

/// <summary>Erzeugt eine frische In-Memory-SQLite-Datenbank pro Test.</summary>
public static class TestDb
{
    public static AppDbContext Create()
    {
        // In-Memory-SQLite: schnell und relational wie die echte DB.
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}

/// <summary>Ersetzt im Test die echte Gemini-Anbindung durch feste Rückgaben.</summary>
public sealed class FakeContentGenerationService : IContentGenerationService
{
    public IReadOnlyList<VocabularyWord> VocabToReturn { get; set; } = new List<VocabularyWord>();
    public GeneratedReading? ReadingToReturn { get; set; }

    public Task<IReadOnlyList<VocabularyWord>> GenerateVocabularyAsync(
        string topic, Difficulty difficulty, int count, CancellationToken ct = default)
        => Task.FromResult(VocabToReturn);

    public Task<GeneratedReading> GenerateReadingPassageAsync(
        string topic, Difficulty difficulty, int questionCount, CancellationToken ct = default)
        => Task.FromResult(ReadingToReturn
            ?? throw new InvalidOperationException("ReadingToReturn wurde im Test nicht gesetzt."));
}
