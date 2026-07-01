namespace LernFuchs.Api.Models;

/// <summary>Interner Systemzustand (eine Zeile), z. B. wann zuletzt Tagesinhalte erzeugt wurden.</summary>
public class SystemState
{
    public int Id { get; set; }

    /// <summary>Tag, an dem zuletzt automatische Tagesinhalte erzeugt wurden.</summary>
    public DateOnly? LastDailyContentDate { get; set; }
}
