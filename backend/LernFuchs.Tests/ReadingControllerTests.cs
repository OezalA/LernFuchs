using LernFuchs.Api.Controllers;
using LernFuchs.Api.Data;
using LernFuchs.Api.Dtos;
using LernFuchs.Api.Models;
using LernFuchs.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LernFuchs.Tests;

public class ReadingControllerTests
{
    private static object? Prop(object source, string name)
        => source.GetType().GetProperty(name)!.GetValue(source);

    [Fact]
    public async Task Check_GradesAnswersCaseInsensitively_AndCountsScore()
    {
        using var db = TestDb.Create();
        var passage = new ReadingPassage
        {
            Title = "Test",
            Text = "…",
            Questions = new List<ComprehensionQuestion>
            {
                new() { QuestionText = "Hauptstadt?", CorrectAnswer = "Berlin" },
                new() { QuestionText = "Farbe?", CorrectAnswer = "Blau" }
            }
        };
        db.ReadingPassages.Add(passage);
        await db.SaveChangesAsync();

        var q = passage.Questions.ToList();
        var controller = new ReadingController(db, new FakeContentGenerationService(), new GameService(db), Options.Create(new FeatureOptions()));

        var submissions = new List<AnswerSubmission>
        {
            new(q[0].Id, "berlin"),   // richtig, andere Groß-/Kleinschreibung
            new(q[1].Id, "Rot")       // falsch
        };
        var result = await controller.Check(passage.Id, submissions);

        var value = Assert.IsType<OkObjectResult>(result).Value!;
        Assert.Equal(2, Prop(value, "Total"));
        Assert.Equal(1, Prop(value, "Score"));

        var feedback = Assert.IsType<List<AnswerFeedback>>(Prop(value, "Feedback"));
        Assert.True(feedback.Single(f => f.QuestionId == q[0].Id).IsCorrect);
        Assert.False(feedback.Single(f => f.QuestionId == q[1].Id).IsCorrect);
    }

    [Fact]
    public async Task Generate_AddsOnlyNewDifficultWords_AndSkipsExistingOnes()
    {
        using var db = TestDb.Create();
        db.VocabularyWords.Add(new VocabularyWord { Word = "Baum", DefinitionGerman = "Pflanze" });
        await db.SaveChangesAsync();

        var passage = new ReadingPassage { Title = "Wald", Text = "Ein Baum im Wald." };
        var difficult = new List<VocabularyWord>
        {
            new() { Word = "Baum", DefinitionGerman = "Pflanze" }, // existiert schon -> überspringen
            new() { Word = "Wald", DefinitionGerman = "viele Bäume" } // neu
        };
        var fake = new FakeContentGenerationService
        {
            ReadingToReturn = new GeneratedReading(passage, difficult)
        };
        var controller = new ReadingController(db, fake, new GameService(db), Options.Create(new FeatureOptions()));

        var result = await controller.Generate(new GenerateReadingRequest("Wald"), CancellationToken.None);

        var value = Assert.IsType<OkObjectResult>(result).Value!;
        Assert.Equal(1, Prop(value, "addedWords"));

        var words = await db.VocabularyWords.Select(w => w.Word).ToListAsync();
        Assert.Equal(2, words.Count);
        Assert.Contains("Wald", words);
        Assert.Equal(1, await db.ReadingPassages.CountAsync());
    }

    [Fact]
    public async Task GetById_ReturnsQuestionsAndTheSourceWordsOfThePassage()
    {
        using var db = TestDb.Create();
        var passage = new ReadingPassage
        {
            Title = "Test",
            Text = "…",
            Questions = new List<ComprehensionQuestion>
            {
                new() { QuestionText = "Frage?", CorrectAnswer = "A", Options = new() { "A", "B" } }
            }
        };
        db.ReadingPassages.Add(passage);
        await db.SaveChangesAsync();
        db.VocabularyWords.Add(new VocabularyWord
        {
            Word = "Baum", DefinitionGerman = "Pflanze", SourcePassageId = passage.Id
        });
        await db.SaveChangesAsync();

        var controller = new ReadingController(db, new FakeContentGenerationService(), new GameService(db), Options.Create(new FeatureOptions()));
        var result = await controller.GetById(passage.Id);

        var value = Assert.IsType<OkObjectResult>(result).Value!;
        var questions = ((System.Collections.IEnumerable)Prop(value, "Questions")!).Cast<object>().ToList();
        Assert.Single(questions);

        var words = ((System.Collections.IEnumerable)Prop(value, "Words")!).Cast<object>().ToList();
        Assert.Single(words);
        Assert.Equal("Baum", Prop(words[0], "Word"));
    }
}
