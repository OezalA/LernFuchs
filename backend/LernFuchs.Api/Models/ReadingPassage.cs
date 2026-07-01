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

    /// <summary>Themengebiet, z. B. "Tiere", "Abenteuer".</summary>
    [MaxLength(60)]
    public string? Topic { get; set; }

    public int WordCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ComprehensionQuestion> Questions { get; set; } = new();
}
