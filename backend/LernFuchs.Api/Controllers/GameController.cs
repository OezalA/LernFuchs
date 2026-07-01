using LernFuchs.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LernFuchs.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly GameService _game;

    public GameController(GameService game) => _game = game;

    /// <summary>Aktueller Spielstand: XP, Level, Serie, Tagesziel und Abzeichen.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await _game.GetStateAsync(ct));
}
