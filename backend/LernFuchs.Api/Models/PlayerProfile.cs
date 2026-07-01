namespace LernFuchs.Api.Models;

/// <summary>
/// Spielstand der Schülerin (Einzelbenutzer): Erfahrungspunkte, Serie (Streak)
/// und Tagesziel. Wird bei der ersten Aktivität angelegt (Id = 1).
/// </summary>
public class PlayerProfile
{
    public int Id { get; set; }

    /// <summary>Gesammelte Erfahrungspunkte (XP).</summary>
    public int Xp { get; set; }

    /// <summary>Aktuelle Serie an aufeinanderfolgenden aktiven Tagen.</summary>
    public int CurrentStreakDays { get; set; }

    /// <summary>Längste je erreichte Serie.</summary>
    public int LongestStreakDays { get; set; }

    /// <summary>Letzter aktiver Tag (für die Serienberechnung).</summary>
    public DateOnly? LastActiveDate { get; set; }

    /// <summary>Heute wiederholte Wörter (für das Tagesziel).</summary>
    public int ReviewsToday { get; set; }

    /// <summary>Tagesziel an Wörtern.</summary>
    public int DailyGoal { get; set; } = 10;
}
