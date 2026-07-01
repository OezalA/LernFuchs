namespace LernFuchs.Api.Services;

/// <summary>Kennzahlen, die zum Prüfen der Abzeichen gebraucht werden.</summary>
public readonly record struct GameMetrics(
    int TotalWords, int MasteredWords, int TotalPassages, int CurrentStreak, int Level, int Xp);

/// <summary>Definition eines Abzeichens (für die Anzeige im Frontend).</summary>
public record AchievementDef(string Code, string Title, string Description, string Icon);

/// <summary>Reine Spielregeln: Level-Berechnung und Abzeichen-Bedingungen.</summary>
public static class GameRules
{
    /// <summary>Gesamt-XP, die für das Erreichen eines Levels nötig sind (Level 1 = 0).</summary>
    public static int TotalXpForLevel(int level) => 100 * (level - 1) * level / 2;

    public static int LevelForXp(int xp)
    {
        var level = 1;
        while (xp >= TotalXpForLevel(level + 1)) level++;
        return level;
    }

    /// <summary>XP innerhalb des aktuellen Levels.</summary>
    public static int XpIntoLevel(int xp) => xp - TotalXpForLevel(LevelForXp(xp));

    /// <summary>XP-Spanne des aktuellen Levels (bis zum nächsten Level).</summary>
    public static int XpSpanForLevel(int xp)
    {
        var l = LevelForXp(xp);
        return TotalXpForLevel(l + 1) - TotalXpForLevel(l);
    }

    /// <summary>Alle verfügbaren Abzeichen-Definitionen.</summary>
    public static readonly IReadOnlyList<AchievementDef> Definitions = new List<AchievementDef>
    {
        new("first_word",  "Erster Schritt",     "Dein erstes Wort gesammelt",        "🌱"),
        new("words_25",    "Sammler",            "25 Wörter gesammelt",               "📚"),
        new("words_100",   "Wörter-Meister",     "100 Wörter gesammelt",              "🏆"),
        new("mastered_10", "Gedächtnis-Held",    "10 Wörter gelernt (höchste Box)",   "⭐"),
        new("streak_3",    "Dranbleiber",        "3 Tage in Folge geübt",             "🔥"),
        new("streak_7",    "Wochen-Champion",    "7 Tage in Folge geübt",             "🚀"),
        new("level_5",     "Aufsteiger",         "Level 5 erreicht",                  "🎖️"),
        new("reader_1",    "Erste Geschichte",   "Deinen ersten Text gelesen",        "📖"),
        new("reader_5",    "Leseratte",          "5 Texte gelesen",                   "🦉"),
    };

    /// <summary>Ist ein Abzeichen anhand der Kennzahlen verdient?</summary>
    public static bool IsEarned(string code, GameMetrics m) => code switch
    {
        "first_word"  => m.TotalWords >= 1,
        "words_25"    => m.TotalWords >= 25,
        "words_100"   => m.TotalWords >= 100,
        "mastered_10" => m.MasteredWords >= 10,
        "streak_3"    => m.CurrentStreak >= 3,
        "streak_7"    => m.CurrentStreak >= 7,
        "level_5"     => m.Level >= 5,
        "reader_1"    => m.TotalPassages >= 1,
        "reader_5"    => m.TotalPassages >= 5,
        _ => false
    };
}
