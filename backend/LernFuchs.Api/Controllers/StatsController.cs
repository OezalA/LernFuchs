using LernFuchs.Api.Data;
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
    public async Task<IActionResult> Get()
    {
        var now = DateTime.UtcNow;

        var totalWords = await _db.VocabularyWords.CountAsync();
        var masteredWords = await _db.VocabularyProgress.CountAsync(p => p.Box >= 5);
        var dueWords = await _db.VocabularyWords
            .CountAsync(w => w.Progress == null || w.Progress.NextReviewAt <= now);
        var correctReviews = await _db.VocabularyProgress.SumAsync(p => (int?)p.TimesCorrect) ?? 0;
        var wrongReviews = await _db.VocabularyProgress.SumAsync(p => (int?)p.TimesWrong) ?? 0;
        var totalPassages = await _db.ReadingPassages.CountAsync();

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
    public async Task<IActionResult> Progress(CancellationToken ct)
    {
        // Verteilung über die Leitner-Boxen (0..5). Neue Wörter zählen als Box 0.
        var boxes = new int[6];
        var grouped = await _db.VocabularyProgress
            .GroupBy(p => p.Box)
            .Select(g => new { Box = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        foreach (var g in grouped)
            if (g.Box is >= 0 and <= 5) boxes[g.Box] += g.Count;
        boxes[0] += await _db.VocabularyWords.CountAsync(w => w.Progress == null, ct);

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
