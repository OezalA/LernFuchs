using LernFuchs.Api.Models;
using LernFuchs.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace LernFuchs.Tests;

public class GameRulesTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(300, 3)]
    [InlineData(600, 4)]
    public void LevelForXp_MatchesTriangularThresholds(int xp, int expectedLevel)
    {
        Assert.Equal(expectedLevel, GameRules.LevelForXp(xp));
    }

    [Fact]
    public void XpIntoLevel_IsZeroExactlyAtLevelBoundary()
    {
        Assert.Equal(0, GameRules.XpIntoLevel(100)); // Start von Level 2
        Assert.Equal(50, GameRules.XpIntoLevel(150));
    }
}

public class GameServiceTests
{
    [Fact]
    public async Task RegisterActivity_AwardsXp_AndStartsStreakAtOne()
    {
        using var db = TestDb.Create();
        var service = new GameService(db);

        var result = await service.RegisterActivityAsync(10, wordsReviewed: 1);

        Assert.Equal(10, result.Xp);
        var state = await service.GetStateAsync();
        Assert.Equal(1, state.CurrentStreakDays);
        Assert.Equal(1, state.ReviewsToday);
    }

    [Fact]
    public async Task RegisterActivity_TwiceSameDay_DoesNotIncreaseStreak_ButAccumulatesXp()
    {
        using var db = TestDb.Create();
        var service = new GameService(db);

        await service.RegisterActivityAsync(10, 1);
        await service.RegisterActivityAsync(10, 1);

        var state = await service.GetStateAsync();
        Assert.Equal(20, state.Xp);
        Assert.Equal(1, state.CurrentStreakDays);
        Assert.Equal(2, state.ReviewsToday);
    }

    [Fact]
    public async Task RegisterActivity_UnlocksFirstWordAchievement_WhenAWordExists()
    {
        using var db = TestDb.Create();
        db.VocabularyWords.Add(new VocabularyWord { Word = "Baum", DefinitionGerman = "Pflanze" });
        await db.SaveChangesAsync();
        var service = new GameService(db);

        var result = await service.RegisterActivityAsync(10, 1);

        Assert.Contains(result.NewAchievements, a => a.Code == "first_word");
        Assert.Equal(1, await db.Achievements.CountAsync(a => a.Code == "first_word"));
    }
}
