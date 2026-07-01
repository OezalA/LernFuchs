using LernFuchs.Api.Controllers;
using LernFuchs.Api.Data;
using LernFuchs.Api.Dtos;
using LernFuchs.Api.Models;
using LernFuchs.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LernFuchs.Tests;

public class VocabularyControllerTests
{
    private static VocabularyController CreateController(AppDbContext db, FakeContentGenerationService? content = null)
        => new(db, content ?? new FakeContentGenerationService(), new GameService(db), NullLogger<VocabularyController>.Instance);

    [Fact]
    public async Task Review_CorrectAnswer_MovesWordToNextBoxAndSchedulesFutureReview()
    {
        using var db = TestDb.Create();
        db.VocabularyWords.Add(new VocabularyWord { Word = "Baum", DefinitionGerman = "eine große Pflanze" });
        await db.SaveChangesAsync();
        var id = db.VocabularyWords.Single().Id;

        var controller = CreateController(db);
        var result = await controller.Review(id, new ReviewResult(true));

        Assert.IsType<OkObjectResult>(result);
        var progress = await db.VocabularyProgress.SingleAsync();
        Assert.Equal(1, progress.Box);
        Assert.Equal(1, progress.TimesCorrect);
        Assert.Equal(0, progress.TimesWrong);
        Assert.True(progress.NextReviewAt > DateTime.UtcNow, "Nächste Wiederholung muss in der Zukunft liegen.");
    }

    [Fact]
    public async Task Review_WrongAnswer_ResetsBoxToZero()
    {
        using var db = TestDb.Create();
        var word = new VocabularyWord { Word = "Haus", DefinitionGerman = "ein Gebäude" };
        word.Progress = new VocabularyProgress { Box = 4, TimesCorrect = 5 };
        db.VocabularyWords.Add(word);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.Review(word.Id, new ReviewResult(false));

        Assert.IsType<OkObjectResult>(result);
        var progress = await db.VocabularyProgress.SingleAsync();
        Assert.Equal(0, progress.Box);
        Assert.Equal(1, progress.TimesWrong);
    }

    [Fact]
    public async Task Review_UnknownId_ReturnsNotFound()
    {
        using var db = TestDb.Create();
        var controller = CreateController(db);

        var result = await controller.Review(999, new ReviewResult(true));

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Generate_SavesWordsReturnedByTheContentService()
    {
        using var db = TestDb.Create();
        var fake = new FakeContentGenerationService
        {
            VocabToReturn = new List<VocabularyWord>
            {
                new() { Word = "Katze", DefinitionGerman = "ein Haustier" },
                new() { Word = "Hund", DefinitionGerman = "ein Haustier" }
            }
        };
        var controller = CreateController(db, fake);

        var result = await controller.Generate(new GenerateVocabularyRequest("Tiere"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(2, await db.VocabularyWords.CountAsync());
    }

    [Fact]
    public async Task GetAll_ResultSerializesWithoutObjectCycle()
    {
        using var db = TestDb.Create();
        var word = new VocabularyWord { Word = "Baum", DefinitionGerman = "eine Pflanze" };
        word.Progress = new VocabularyProgress { Box = 2 }; // Fortschritt erzeugt die Rück-Navigation
        db.VocabularyWords.Add(word);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.GetAll(null, null);
        var value = Assert.IsType<OkObjectResult>(result).Value!;

        // Ohne [JsonIgnore] auf der Rück-Navigation würde dies eine Zyklus-Ausnahme werfen.
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        Assert.Contains("Baum", json);
    }

    [Fact]
    public async Task GetDue_ReturnsNewWordsAndSkipsWordsScheduledForTheFuture()
    {
        using var db = TestDb.Create();
        // Neu (kein Fortschritt) -> fällig
        db.VocabularyWords.Add(new VocabularyWord { Word = "neu", DefinitionGerman = "..." });
        // In der Zukunft geplant -> nicht fällig
        db.VocabularyWords.Add(new VocabularyWord
        {
            Word = "spaeter",
            DefinitionGerman = "...",
            Progress = new VocabularyProgress { Box = 3, NextReviewAt = DateTime.UtcNow.AddDays(5) }
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.GetDue(20);

        var due = Assert.IsType<List<VocabularyWord>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Single(due);
        Assert.Equal("neu", due[0].Word);
    }
}
