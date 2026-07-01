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
}
