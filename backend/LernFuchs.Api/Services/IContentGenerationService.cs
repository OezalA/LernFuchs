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
        string topic, Difficulty difficulty, int count,
        Language language = Language.Deutsch, CancellationToken ct = default);

    /// <summary>
    /// Erzeugt einen Lesetext samt Verständnisfragen und den schwierigen Wörtern
    /// aus dem Text (für den Wortschatz). <paramref name="language"/> wählt die Lernsprache
    /// (Deutsch oder Englisch als Fremdsprache). <paramref name="modelOverride"/> erlaubt es,
    /// ausnahmsweise ein anderes Modell zu verwenden (z. B. beim Erzeugen der Startinhalte).
    /// </summary>
    Task<GeneratedReading> GenerateReadingPassageAsync(
        string topic, Difficulty difficulty, int questionCount,
        Language language = Language.Deutsch,
        CancellationToken ct = default, string? modelOverride = null);
}

/// <summary>Ergebnis der Lesetext-Erzeugung: Text plus die schwierigen Wörter daraus.</summary>
public record GeneratedReading(ReadingPassage Passage, IReadOnlyList<VocabularyWord> DifficultWords);
