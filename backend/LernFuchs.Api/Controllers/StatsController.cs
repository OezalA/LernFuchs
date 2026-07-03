using LernFuchs.Api.Data;
using LernFuchs.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LernFuchs.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly AppDbContext _db;

    public StatsController(AppDbContext db) => _db = db;

    /// <summary>Liefert eine Übersicht über den Lernfortschritt.</summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Language? language = null)
    {
        var now = DateTime.UtcNow;

        // Inhaltsbezogene Zahlen nach Sprache filtern (Wörter, Texte, Fortschritt).
        var words = _db.VocabularyWords.AsQueryable();
        var progress = _db.VocabularyProgress.AsQueryable();
        var passages = _db.ReadingPassages.AsQueryable();
        if (language is not null)
        {
            words = words.Where(w => w.Language == language);
            progress = progress.Where(p => p.VocabularyWord!.Language == language);
            passages = passages.Where(p => p.Language == language);
        }

        var totalWords = await words.CountAsync();
        var masteredWords = await progress.CountAsync(p => p.Box >= 5);
        var dueWords = await words
            .CountAsync(w => w.Progress == null || w.Progress.NextReviewAt <= now);
        var correctReviews = await progress.SumAsync(p => (int?)p.TimesCorrect) ?? 0;
        var wrongReviews = await progress.SumAsync(p => (int?)p.TimesWrong) ?? 0;
        var totalPassages = await passages.CountAsync();

        var totalReviews = correctReviews + wrongReviews;
        var successRate = totalReviews > 0 ? (int)Math.Round(100.0 * correctReviews / totalReviews) : 0;

        return Ok(new
        {
            totalWords,
            masteredWords,
            dueWords,
            correctReviews,
            wrongReviews,
            successRate,
            totalPassages
        });
    }

    /// <summary>Daten für die Diagramme: Leitner-Boxverteilung und die letzten 7 Tage.</summary>
    [HttpGet("progress")]
    public async Task<IActionResult> Progress([FromQuery] Language? language, CancellationToken ct)
    {
        // Verteilung über die Leitner-Boxen (0..5), nach Sprache gefiltert. Neue Wörter zählen als Box 0.
        var progressQuery = _db.VocabularyProgress.AsQueryable();
        var newWordsQuery = _db.VocabularyWords.Where(w => w.Progress == null);
        if (language is not null)
        {
            progressQuery = progressQuery.Where(p => p.VocabularyWord!.Language == language);
            newWordsQuery = newWordsQuery.Where(w => w.Language == language);
        }

        var boxes = new int[6];
        var grouped = await progressQuery
            .GroupBy(p => p.Box)
            .Select(g => new { Box = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        foreach (var g in grouped)
            if (g.Box is >= 0 and <= 5) boxes[g.Box] += g.Count;
        boxes[0] += await newWordsQuery.CountAsync(ct);

        // Aktivität der letzten 7 Tage (inkl. heute), lückenlos aufgefüllt.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-6);
        var activities = await _db.DailyActivities
            .Where(a => a.Date >= from)
            .ToListAsync(ct);
        var map = activities.ToDictionary(a => a.Date);

        string[] germanWeekdays = { "So", "Mo", "Di", "Mi", "Do", "Fr", "Sa" };
        var last7Days = Enumerable.Range(0, 7).Select(i =>
        {
            var d = from.AddDays(i);
            map.TryGetValue(d, out var a);
            return new
            {
                date = d.ToString("yyyy-MM-dd"),
                weekday = germanWeekdays[(int)d.DayOfWeek],
                xp = a?.XpEarned ?? 0,
                reviews = a?.Reviews ?? 0
            };
        }).ToList();

        return Ok(new { boxes, last7Days });
    }
}
