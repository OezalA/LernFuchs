using LernFuchs.Api.Models;

namespace LernFuchs.Api.Services;

/// <summary>
/// Erzeugt Lerninhalte (Vokabeln, Lesetexte) mit Hilfe eines KI-Modells.
/// Die Implementierung ist austauschbar (aktuell: Google Gemini).
/// </summary>
public interface IContentGenerationService
{
    /// <summary>Erzeugt eine Liste neuer Vokabeln zu einem Thema.</summary>
    Task<IReadOnlyList<VocabularyWord>> GenerateVocabularyAsync(
        string topic, Difficulty difficulty, int count, CancellationToken ct = default);

    /// <summary>Erzeugt einen Lesetext samt Verständnisfragen zu einem Thema.</summary>
    Task<ReadingPassage> GenerateReadingPassageAsync(
        string topic, Difficulty difficulty, int questionCount, CancellationToken ct = default);
}
