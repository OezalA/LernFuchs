using System.ComponentModel.DataAnnotations;

namespace LernFuchs.Api.Models;

/// <summary>Ein Vokabeleintrag (Wortschatz) für das Deutschlernen.</summary>
public class VocabularyWord
{
    public int Id { get; set; }

    /// <summary>Das deutsche Wort (Grundform, z. B. "Baum", "gehen").</summary>
    [Required, MaxLength(100)]
    public string Word { get; set; } = string.Empty;

    /// <summary>Artikel bei Nomen (der/die/das), sonst None.</summary>
    public Article Article { get; set; } = Article.None;

    /// <summary>Pluralform bei Nomen, z. B. "Bäume".</summary>
    [MaxLength(100)]
    public string? Plural { get; set; }

    public WordType WordType { get; set; } = WordType.Sonstiges;

    /// <summary>
    /// Kindgerechte deutsche Erklärung der Bedeutung – so einfach formuliert,
    /// dass ein Kind der 5. Klasse sie versteht. Dies ist die zentrale Bedeutung.
    /// </summary>
    [Required, MaxLength(500)]
    public string DefinitionGerman { get; set; } = string.Empty;

    /// <summary>Optionale türkische Bedeutung als kleine Hilfe (kann leer bleiben).</summary>
    [MaxLength(200)]
    public string? MeaningTurkish { get; set; }

    /// <summary>Beispielsatz mit dem Wort.</summary>
    [MaxLength(300)]
    public string? ExampleSentence { get; set; }

    /// <summary>Synonyme (Liste, als JSON gespeichert).</summary>
    public List<string> Synonyms { get; set; } = new();

    /// <summary>Antonyme (Liste, als JSON gespeichert).</summary>
    public List<string> Antonyms { get; set; } = new();

    public Difficulty Difficulty { get; set; } = Difficulty.Mittel;

    /// <summary>Themengebiet, z. B. "Schule", "Natur", "Familie".</summary>
    [MaxLength(60)]
    public string? Topic { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public VocabularyProgress? Progress { get; set; }
}
