namespace LernFuchs.Api.Models;

/// <summary>Aggregierte Lernaktivität eines Tages (für die Wochenübersicht).</summary>
public class DailyActivity
{
    public int Id { get; set; }

    /// <summary>Tag der Aktivität (ohne Uhrzeit).</summary>
    public DateOnly Date { get; set; }

    /// <summary>An diesem Tag gesammelte XP.</summary>
    public int XpEarned { get; set; }

    /// <summary>Anzahl der an diesem Tag wiederholten Wörter.</summary>
    public int Reviews { get; set; }
}
