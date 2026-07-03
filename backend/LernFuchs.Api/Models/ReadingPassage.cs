using System.ComponentModel.DataAnnotations;

namespace LernFuchs.Api.Models;

/// <summary>Ein Lesetext (Leseverständnis) mit dazugehörigen Fragen.</summary>
public class ReadingPassage
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Der eigentliche Lesetext.</summary>
    [Required]
    public string Text { get; set; } = string.Empty;

    public Difficulty Difficulty { get; set; } = Difficulty.Mittel;

    /// <summary>Lernsprache des Textes (Deutsch oder Englisch als Fremdsprache).</summary>
    public Language Language { get; set; } = Language.Deutsch;

    /// <summary>Themengebiet, z. B. "Tiere", "Abenteuer".</summary>
    [MaxLength(60)]
    public string? Topic { get; set; }

    public int WordCount { get; set; }

    /// <summary>
    /// Vollständiges Wörterverzeichnis des Textes als JSON [{ "word", "meaning" }, …].
    /// Wird für die Fremdsprache (Englisch) genutzt, damit die Wörter-Lernphase fast
    /// jedes Wort des Textes abfragt – unabhängig von der (entdoppelten) Wortschatz-Tabelle.
    /// Null bei Muttersprache/Altdaten (dann werden die verknüpften Wörter genutzt).
    /// </summary>
    public string? GlossaryJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ComprehensionQuestion> Questions { get; set; } = new();
}
