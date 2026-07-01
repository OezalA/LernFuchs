namespace LernFuchs.Api.Dtos;

/// <summary>Ein Abzeichen mit Freischalt-Status für die Anzeige.</summary>
public record AchievementView(string Code, string Title, string Description, string Icon, bool Unlocked);

/// <summary>Der komplette Spielstand für das Frontend.</summary>
public record GameStateDto(
    int Xp,
    int Level,
    int XpIntoLevel,
    int XpForNextLevel,
    int CurrentStreakDays,
    int LongestStreakDays,
    int ReviewsToday,
    int DailyGoal,
    IReadOnlyList<AchievementView> Achievements);

/// <summary>Ergebnis einer Aktivität: erhaltene XP und neu freigeschaltete Abzeichen.</summary>
public record GameActivityResult(
    int XpGained,
    int Xp,
    int Level,
    bool LeveledUp,
    IReadOnlyList<AchievementView> NewAchievements);
