using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LernFuchs.Api.Models;

/// <summary>Eine Frage zum Leseverständnis eines <see cref="ReadingPassage"/>.</summary>
public class ComprehensionQuestion
{
    public int Id { get; set; }

    public int ReadingPassageId { get; set; }

    // Rück-Navigation nicht serialisieren (Zyklus Passage -> Questions -> Passage).
    [JsonIgnore]
    public ReadingPassage? ReadingPassage { get; set; }

    [Required, MaxLength(400)]
    public string QuestionText { get; set; } = string.Empty;

    public QuestionType QuestionType { get; set; } = QuestionType.MultipleChoice;

    /// <summary>Antwortmöglichkeiten bei MultipleChoice (als JSON gespeichert).</summary>
    public List<string> Options { get; set; } = new();

    /// <summary>Die richtige Antwort (bei MC der korrekte Options-Text).</summary>
    [Required, MaxLength(400)]
    public string CorrectAnswer { get; set; } = string.Empty;

    /// <summary>Kurze Erklärung, warum die Antwort richtig ist.</summary>
    [MaxLength(500)]
    public string? Explanation { get; set; }
}
