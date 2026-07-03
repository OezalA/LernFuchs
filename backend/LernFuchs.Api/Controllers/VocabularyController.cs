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
public class VocabularyController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IContentGenerationService _content;
    private readonly GameService _game;
    private readonly FeatureOptions _features;
    private readonly ILogger<VocabularyController> _logger;

    /// <summary>Leitner-Intervalle in Tagen je Box (Index 0..5).</summary>
    private static readonly int[] BoxIntervalsDays = { 0, 1, 3, 7, 16, 35 };

    public VocabularyController(AppDbContext db, IContentGenerationService content, GameService game,
        IOptions<FeatureOptions> features, ILogger<VocabularyController> logger)
    {
        _db = db;
        _content = content;
        _game = game;
        _features = features.Value;
        _logger = logger;
    }

    /// <summary>Alle Vokabeln, optional gefiltert nach Thema/Schwierigkeit.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? topic, [FromQuery] Difficulty? difficulty, [FromQuery] Language? language = null)
    {
        var query = _db.VocabularyWords.Include(w => w.Progress).AsQueryable();
        if (!string.IsNullOrWhiteSpace(topic))
            query = query.Where(w => w.Topic == topic);
        if (difficulty is not null)
            query = query.Where(w => w.Difficulty == difficulty);
        if (language is not null)
            query = query.Where(w => w.Language == language);

        var words = await query.OrderBy(w => w.Word).ToListAsync();
        return Ok(words);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var word = await _db.VocabularyWords.Include(w => w.Progress)
            .FirstOrDefaultAsync(w => w.Id == id);
        return word is null ? NotFound() : Ok(word);
    }

    /// <summary>Vokabeln, die heute zur Wiederholung fällig sind (Leitner-System).</summary>
    [HttpGet("due")]
    public async Task<IActionResult> GetDue([FromQuery] int limit = 20, [FromQuery] Language? language = null)
    {
        var now = DateTime.UtcNow;
        var query = _db.VocabularyWords.Include(w => w.Progress).AsQueryable();
        if (language is not null)
            query = query.Where(w => w.Language == language);
        // Neue Wörter (ohne Fortschritt) zuerst, dann die am längsten fälligen.
        var due = await query
            .Where(w => w.Progress == null || w.Progress.NextReviewAt <= now)
            .OrderBy(w => w.Progress == null ? DateTime.MinValue : w.Progress.NextReviewAt)
            .Take(limit)
            .ToListAsync();
        return Ok(due);
    }

    /// <summary>Erzeugt neue Vokabeln per KI (Gemini) und speichert sie.</summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateVocabularyRequest req, CancellationToken ct)
    {
        if (!_features.UserGenerationEnabled)
            return StatusCode(StatusCodes.Status403Forbidden, "Das Erstellen eigener Inhalte ist derzeit deaktiviert.");
        if (string.IsNullOrWhiteSpace(req.Topic))
            return BadRequest("Bitte ein Thema angeben.");

        var count = Math.Clamp(req.Count, 1, 30);
        var words = await _content.GenerateVocabularyAsync(req.Topic, req.Difficulty, count, req.Language, ct);

        _db.VocabularyWords.AddRange(words);
        await _db.SaveChangesAsync(ct);
        return Ok(words);
    }

    /// <summary>Verarbeitet eine Wiederholung und aktualisiert den Leitner-Fortschritt.</summary>
    [HttpPost("{id:int}/review")]
    public async Task<IActionResult> Review(int id, [FromBody] ReviewResult result)
    {
        var word = await _db.VocabularyWords.Include(w => w.Progress)
            .FirstOrDefaultAsync(w => w.Id == id);
        if (word is null) return NotFound();

        var p = word.Progress ??= new VocabularyProgress { VocabularyWordId = id };

        if (result.Correct)
        {
            p.TimesCorrect++;
            p.Box = Math.Min(p.Box + 1, 5);
        }
        else
        {
            p.TimesWrong++;
            p.Box = 0; // zurück in die erste Box
        }

        p.LastReviewedAt = DateTime.UtcNow;
        p.NextReviewAt = DateTime.UtcNow.AddDays(BoxIntervalsDays[p.Box]);

        await _db.SaveChangesAsync();

        // XP vergeben: richtig gibt mehr, aber auch Üben zählt.
        var game = await _game.RegisterActivityAsync(result.Correct ? 10 : 2, wordsReviewed: 1);
        return Ok(new { progress = p, game });
    }

    /// <summary>Markiert ein Wort direkt als "gelernt" (höchste Box). Für "Gewusst" und den 4er-Test.</summary>
    [HttpPost("{id:int}/learned")]
    public async Task<IActionResult> MarkLearned(int id)
    {
        var word = await _db.VocabularyWords.Include(w => w.Progress)
            .FirstOrDefaultAsync(w => w.Id == id);
        if (word is null) return NotFound();

        var p = word.Progress ??= new VocabularyProgress { VocabularyWordId = id };
        var wasLearned = p.Box >= 5;

        p.TimesCorrect++;
        p.Box = 5; // höchste Leitner-Box = gelernt
        p.LastReviewedAt = DateTime.UtcNow;
        p.NextReviewAt = DateTime.UtcNow.AddDays(BoxIntervalsDays[5]);

        await _db.SaveChangesAsync();

        // XP nur beim erstmaligen Lernen vergeben.
        var game = await _game.RegisterActivityAsync(wasLearned ? 0 : 15, wordsReviewed: 1);
        return Ok(new { progress = p, game });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var word = await _db.VocabularyWords.FindAsync(id);
        if (word is null) return NotFound();

        _db.VocabularyWords.Remove(word);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
