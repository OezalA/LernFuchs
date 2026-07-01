using LernFuchs.Api.Data;
using LernFuchs.Api.Dtos;
using LernFuchs.Api.Models;
using LernFuchs.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LernFuchs.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReadingController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IContentGenerationService _content;
    private readonly GameService _game;
    private readonly FeatureOptions _features;

    public ReadingController(AppDbContext db, IContentGenerationService content, GameService game,
        IOptions<FeatureOptions> features)
    {
        _db = db;
        _content = content;
        _game = game;
        _features = features.Value;
    }

    /// <summary>Alle Lesetexte (ohne Fragen, als Übersicht).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? topic, [FromQuery] Difficulty? difficulty)
    {
        var query = _db.ReadingPassages.AsQueryable();
        if (!string.IsNullOrWhiteSpace(topic))
            query = query.Where(p => p.Topic == topic);
        if (difficulty is not null)
            query = query.Where(p => p.Difficulty == difficulty);

        var passages = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id, p.Title, p.Difficulty, p.Topic, p.WordCount, p.CreatedAt,
                QuestionCount = p.Questions.Count
            })
            .ToListAsync();
        return Ok(passages);
    }

    /// <summary>Ein Lesetext samt Fragen und den zugehörigen Wörtern (für das Frontend-Spielerlebnis).</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var passage = await _db.ReadingPassages.Include(p => p.Questions)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (passage is null) return NotFound();

        // Wörter, die aus diesem Text stammen (zum Hervorheben im Text und für die Ergebnisseite).
        var words = await _db.VocabularyWords
            .Where(w => w.SourcePassageId == id)
            .Select(w => new { w.Id, w.Word, w.Article, w.Plural, w.WordType, w.DefinitionGerman, w.ExampleSentence })
            .ToListAsync();

        return Ok(new
        {
            passage.Id, passage.Title, passage.Text, passage.Difficulty,
            passage.Topic, passage.WordCount, passage.CreatedAt,
            Questions = passage.Questions.Select(q => new
            {
                q.Id, q.QuestionText, q.QuestionType, q.Options, q.CorrectAnswer, q.Explanation
            }),
            Words = words
        });
    }

    /// <summary>Erzeugt einen neuen Lesetext mit Fragen per KI (Gemini) und speichert ihn.</summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateReadingRequest req, CancellationToken ct)
    {
        if (!_features.UserGenerationEnabled)
            return StatusCode(StatusCodes.Status403Forbidden, "Das Erstellen eigener Inhalte ist derzeit deaktiviert.");
        if (string.IsNullOrWhiteSpace(req.Topic))
            return BadRequest("Bitte ein Thema angeben.");

        var questionCount = Math.Clamp(req.QuestionCount, 1, 10);
        var generated = await _content.GenerateReadingPassageAsync(req.Topic, req.Difficulty, questionCount, ct);

        _db.ReadingPassages.Add(generated.Passage);
        await _db.SaveChangesAsync(ct); // Passage-Id festlegen, um die Wörter zu verknüpfen

        // Schwierige Wörter aus dem Text zum Wortschatz hinzufügen –
        // aber keine Dubletten (Wörter, die es schon gibt).
        var existing = (await _db.VocabularyWords.Select(w => w.Word).ToListAsync(ct))
            .Select(w => w.ToLowerInvariant())
            .ToHashSet();

        var newWords = generated.DifficultWords
            .Where(w => existing.Add(w.Word.ToLowerInvariant()))
            .ToList();

        foreach (var w in newWords)
            w.SourcePassageId = generated.Passage.Id; // Wort mit seinem Text verknüpfen

        if (newWords.Count > 0)
        {
            _db.VocabularyWords.AddRange(newWords);
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { generated.Passage.Id, generated.Passage.Title, addedWords = newWords.Count });
    }

    /// <summary>Prüft eingereichte Antworten und gibt Feedback je Frage.</summary>
    [HttpPost("{id:int}/check")]
    public async Task<IActionResult> Check(int id, [FromBody] List<AnswerSubmission> answers)
    {
        var passage = await _db.ReadingPassages.Include(p => p.Questions)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (passage is null) return NotFound();

        var feedback = new List<AnswerFeedback>();
        foreach (var q in passage.Questions)
        {
            var submitted = answers.FirstOrDefault(a => a.QuestionId == q.Id)?.Answer ?? "";
            var isCorrect = string.Equals(submitted.Trim(), q.CorrectAnswer.Trim(),
                StringComparison.OrdinalIgnoreCase);
            feedback.Add(new AnswerFeedback(q.Id, isCorrect, q.CorrectAnswer, q.Explanation));
        }

        var score = feedback.Count(f => f.IsCorrect);

        // XP je richtiger Antwort; das Lesen zählt als eine Aktivität für die Serie.
        var game = await _game.RegisterActivityAsync(score * 5, wordsReviewed: 0);
        return Ok(new { Total = feedback.Count, Score = score, Feedback = feedback, Game = game });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var passage = await _db.ReadingPassages.FindAsync(id);
        if (passage is null) return NotFound();

        _db.ReadingPassages.Remove(passage);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
