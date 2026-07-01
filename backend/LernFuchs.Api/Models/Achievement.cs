namespace LernFuchs.Api.Models;

/// <summary>Ein freigeschaltetes Abzeichen (Achievement). Die Definitionen liegen im Code.</summary>
public class Achievement
{
    public int Id { get; set; }

    /// <summary>Eindeutiger Code der Definition, z. B. "streak_7".</summary>
    public string Code { get; set; } = string.Empty;

    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
}
