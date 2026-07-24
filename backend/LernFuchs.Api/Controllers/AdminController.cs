using LernFuchs.Api.Data;
using LernFuchs.Api.Models;
using LernFuchs.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LernFuchs.Api.Controllers;

/// <summary>
/// Geschützter Admin-Bereich: Inhalte auflisten, löschen und (unabhängig vom
/// UserGenerationEnabled-Flag) neu erzeugen. Erfordert die Entra-App-Rolle "Admin".
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IContentGenerationService _content;

    public AdminController(AppDbContext db, IContentGenerationService content)
    {
        _db = db;
        _content = content;
    }

    /// <summary>Anzahl Texte und Wörter je Sprache (Überblick fürs Dashboard).</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var passages = await _db.ReadingPassages
            .GroupBy(p => p.Language)
            .Select(g => new { Language = g.Key, Count = g.Count() })
            .ToListAsync();
        var words = await _db.VocabularyWords
            .GroupBy(w => w.Language)
            .Select(g => new { Language = g.Key, Count = g.Count() })
            .ToListAsync();
        return Ok(new { user = User.Identity?.Name, passages, words });
    }

    /// <summary>Texte (optional nach Sprache gefiltert).</summary>
    [HttpGet("passages")]
    public async Task<IActionResult> Passages([FromQuery] Language? language)
    {
        var query = _db.ReadingPassages.AsQueryable();
        if (language is not null) query = query.Where(p => p.Language == language);
        var list = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id, p.Title, p.Text, p.Language, p.Difficulty, p.Topic, p.WordCount, p.CreatedAt,
                QuestionCount = p.Questions.Count
            })
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>Wörter (optional nach Sprache gefiltert).</summary>
    [HttpGet("words")]
    public async Task<IActionResult> Words([FromQuery] Language? language)
    {
        var query = _db.VocabularyWords.AsQueryable();
        if (language is not null) query = query.Where(w => w.Language == language);
        var list = await query
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new
            {
                w.Id, w.Word, w.DefinitionGerman, w.ExampleSentence, w.WordType, w.Language, w.Difficulty,
                w.Topic, w.SourcePassageId
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpDelete("passages/{id:int}")]
    public async Task<IActionResult> DeletePassage(int id)
    {
        var p = await _db.ReadingPassages.FindAsync(id);
        if (p is null) return NotFound();
        _db.ReadingPassages.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("words/{id:int}")]
    public async Task<IActionResult> DeleteWord(int id)
    {
        var w = await _db.VocabularyWords.FindAsync(id);
        if (w is null) return NotFound();
        _db.VocabularyWords.Remove(w);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Löscht ALLE Texte und Wörter einer Sprache (Rundum-Reset).</summary>
    [HttpDelete("language/{language}")]
    public async Task<IActionResult> DeleteLanguage(Language language)
    {
        var passages = await _db.ReadingPassages.Where(p => p.Language == language).ToListAsync();
        var words = await _db.VocabularyWords.Where(w => w.Language == language).ToListAsync();
        _db.ReadingPassages.RemoveRange(passages);
        _db.VocabularyWords.RemoveRange(words);
        await _db.SaveChangesAsync();
        return Ok(new { deletedPassages = passages.Count, deletedWords = words.Count });
    }

    /// <summary>
    /// Erzeugt einen neuen Text (+ Wörter). Umgeht bewusst das UserGenerationEnabled-Flag –
    /// erlaubt ist das nur Admins, daher hier sicher.
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] AdminGenerateRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Topic)) return BadRequest("Bitte ein Thema angeben.");

        var questionCount = Math.Clamp(req.QuestionCount ?? 4, 1, 10);
        var generated = await _content.GenerateReadingPassageAsync(
            req.Topic, req.Difficulty ?? Difficulty.Leicht, questionCount, req.Language, ct);

        _db.ReadingPassages.Add(generated.Passage);
        await _db.SaveChangesAsync(ct); // Passage-Id für die Verknüpfung

        var lang = generated.Passage.Language;
        var existing = (await _db.VocabularyWords.Where(w => w.Language == lang)
                .Select(w => w.Word).ToListAsync(ct))
            .Select(w => w.ToLowerInvariant())
            .ToHashSet();

        var newWords = generated.DifficultWords
            .Where(w => existing.Add(w.Word.ToLowerInvariant()))
            .ToList();
        foreach (var w in newWords) w.SourcePassageId = generated.Passage.Id;
        if (newWords.Count > 0)
        {
            _db.VocabularyWords.AddRange(newWords);
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { generated.Passage.Id, generated.Passage.Title, addedWords = newWords.Count });
    }

    /// <summary>Bearbeitet einen Text (Titel/Text/Schwierigkeit).</summary>
    [HttpPut("passages/{id:int}")]
    public async Task<IActionResult> UpdatePassage(int id, [FromBody] UpdatePassageRequest req)
    {
        var p = await _db.ReadingPassages.FindAsync(id);
        if (p is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(req.Title)) p.Title = req.Title.Trim();
        if (!string.IsNullOrWhiteSpace(req.Text))
        {
            p.Text = req.Text.Trim();
            p.WordCount = p.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        }
        if (req.Difficulty is not null) p.Difficulty = req.Difficulty.Value;
        await _db.SaveChangesAsync();
        return Ok(new { p.Id, p.Title, p.WordCount, p.Difficulty });
    }

    /// <summary>Bearbeitet ein Wort (Wort/deutsche Bedeutung/Beispielsatz).</summary>
    [HttpPut("words/{id:int}")]
    public async Task<IActionResult> UpdateWord(int id, [FromBody] UpdateWordRequest req)
    {
        var w = await _db.VocabularyWords.FindAsync(id);
        if (w is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(req.Word)) w.Word = req.Word.Trim();
        if (req.DefinitionGerman is not null) w.DefinitionGerman = req.DefinitionGerman.Trim();
        if (req.ExampleSentence is not null)
            w.ExampleSentence = string.IsNullOrWhiteSpace(req.ExampleSentence) ? null : req.ExampleSentence.Trim();
        await _db.SaveChangesAsync();
        return Ok(new { w.Id, w.Word, w.DefinitionGerman, w.ExampleSentence });
    }
}

public record AdminGenerateRequest(string Topic, Language Language, Difficulty? Difficulty, int? QuestionCount);
public record UpdatePassageRequest(string? Title, string? Text, Difficulty? Difficulty);
public record UpdateWordRequest(string? Word, string? DefinitionGerman, string? ExampleSentence);
