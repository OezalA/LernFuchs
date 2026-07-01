using LernFuchs.Api.Data;
using LernFuchs.Api.Dtos;
using LernFuchs.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LernFuchs.Api.Services;

/// <summary>Verwaltet den Spielstand: XP, Serie (Streak), Tagesziel und Abzeichen.</summary>
public class GameService
{
    private readonly AppDbContext _db;

    public GameService(AppDbContext db) => _db = db;

    /// <summary>Verarbeitet eine Aktivität, vergibt XP, aktualisiert die Serie und prüft Abzeichen.</summary>
    public async Task<GameActivityResult> RegisterActivityAsync(int xpGained, int wordsReviewed, CancellationToken ct = default)
    {
        var profile = await GetOrCreateProfileAsync(ct);
        var levelBefore = GameRules.LevelForXp(profile.Xp);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (profile.LastActiveDate == today)
        {
            profile.ReviewsToday += wordsReviewed;
        }
        else
        {
            // Neuer Tag: Serie fortsetzen (gestern aktiv) oder neu beginnen.
            profile.CurrentStreakDays = profile.LastActiveDate == today.AddDays(-1)
                ? profile.CurrentStreakDays + 1
                : 1;
            profile.ReviewsToday = wordsReviewed;
            profile.LastActiveDate = today;
        }

        profile.Xp += xpGained;
        profile.LongestStreakDays = Math.Max(profile.LongestStreakDays, profile.CurrentStreakDays);

        var newlyUnlocked = await CheckAchievementsAsync(profile, ct);

        await _db.SaveChangesAsync(ct);

        var levelAfter = GameRules.LevelForXp(profile.Xp);
        return new GameActivityResult(
            xpGained, profile.Xp, levelAfter, levelAfter > levelBefore, newlyUnlocked);
    }

    /// <summary>Liefert den aktuellen Spielstand inklusive aller Abzeichen.</summary>
    public async Task<GameStateDto> GetStateAsync(CancellationToken ct = default)
    {
        var profile = await GetOrCreateProfileAsync(ct);
        var unlocked = await _db.Achievements.Select(a => a.Code).ToListAsync(ct);
        var unlockedSet = unlocked.ToHashSet();

        var achievements = GameRules.Definitions
            .Select(d => new AchievementView(d.Code, d.Title, d.Description, d.Icon, unlockedSet.Contains(d.Code)))
            .ToList();

        return new GameStateDto(
            profile.Xp,
            GameRules.LevelForXp(profile.Xp),
            GameRules.XpIntoLevel(profile.Xp),
            GameRules.XpSpanForLevel(profile.Xp),
            profile.CurrentStreakDays,
            profile.LongestStreakDays,
            profile.ReviewsToday,
            profile.DailyGoal,
            achievements);
    }

    private async Task<PlayerProfile> GetOrCreateProfileAsync(CancellationToken ct)
    {
        var profile = await _db.PlayerProfiles.FirstOrDefaultAsync(ct);
        if (profile is null)
        {
            profile = new PlayerProfile();
            _db.PlayerProfiles.Add(profile);
        }
        return profile;
    }

    private async Task<IReadOnlyList<AchievementView>> CheckAchievementsAsync(PlayerProfile profile, CancellationToken ct)
    {
        var metrics = new GameMetrics(
            TotalWords: await _db.VocabularyWords.CountAsync(ct),
            MasteredWords: await _db.VocabularyProgress.CountAsync(p => p.Box >= 5, ct),
            TotalPassages: await _db.ReadingPassages.CountAsync(ct),
            CurrentStreak: profile.CurrentStreakDays,
            Level: GameRules.LevelForXp(profile.Xp),
            Xp: profile.Xp);

        var already = (await _db.Achievements.Select(a => a.Code).ToListAsync(ct)).ToHashSet();
        var newly = new List<AchievementView>();

        foreach (var def in GameRules.Definitions)
        {
            if (already.Contains(def.Code) || !GameRules.IsEarned(def.Code, metrics)) continue;
            _db.Achievements.Add(new Achievement { Code = def.Code });
            newly.Add(new AchievementView(def.Code, def.Title, def.Description, def.Icon, true));
        }

        return newly;
    }
}
