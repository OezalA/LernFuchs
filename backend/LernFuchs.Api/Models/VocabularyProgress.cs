using System.Text.Json.Serialization;

namespace LernFuchs.Api.Models;

/// <summary>
/// Lernfortschritt zu einer Vokabel nach dem Leitner-System (Karteikästen).
/// Box 0 = neu/unsicher, höhere Box = längeres Wiederholungsintervall.
/// </summary>
public class VocabularyProgress
{
    public int Id { get; set; }

    public int VocabularyWordId { get; set; }

    // Rück-Navigation nicht serialisieren – sonst entsteht ein Objektzyklus
    // (Word -> Progress -> Word -> ...).
    [JsonIgnore]
    public VocabularyWord? VocabularyWord { get; set; }

    /// <summary>Leitner-Box (0–5).</summary>
    public int Box { get; set; } = 0;

    public int TimesCorrect { get; set; }
    public int TimesWrong { get; set; }

    public DateTime? LastReviewedAt { get; set; }

    /// <summary>Nächster fälliger Wiederholungstermin.</summary>
    public DateTime NextReviewAt { get; set; } = DateTime.UtcNow;

    /// <summary>Als "gelernt" markiert, sobald die höchste Box erreicht ist.</summary>
    public bool Mastered => Box >= 5;
}
