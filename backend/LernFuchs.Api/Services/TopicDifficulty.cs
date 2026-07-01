using LernFuchs.Api.Models;

namespace LernFuchs.Api.Services;

/// <summary>
/// Empfiehlt einen Schwierigkeitsgrad je nach Thema: Bei ohnehin komplexen Themen
/// (z. B. Wissenschaft, Weltall) wird der Lesetext leichter gehalten, bei
/// alltagsnahen Themen darf er anspruchsvoller sein.
/// </summary>
public static class TopicDifficulty
{
    private static readonly (string[] Keywords, Difficulty Difficulty)[] Rules =
    {
        // Komplexe Sachthemen -> leichter Text
        (new[] { "wissenschaft", "technik", "erfind", "roboter", "experiment", "maschine", "strom", "brücke", "tunnel" }, Difficulty.Leicht),
        (new[] { "weltall", "weltraum", "planet", "stern", "galax", "astronaut", "rakete", "mond" }, Difficulty.Leicht),
        (new[] { "körper", "mensch", "gesund", "sinne", "muskel", "herz", "skelett" }, Difficulty.Leicht),
        (new[] { "ritter", "burg", "ägypt", "römer", "rom", "mittelalter", "wikinger", "steinzeit", "pharao" }, Difficulty.Leicht),

        // Alltagsnahe/erzählende Themen -> anspruchsvollerer Text
        (new[] { "freund", "familie", "gefühl" }, Difficulty.Schwer),
        (new[] { "abenteuer", "pirat", "drache", "fantasie", "schatz" }, Difficulty.Schwer),
        (new[] { "hobby", "sport", "fußball", "musik", "spiel", "schwimmen" }, Difficulty.Schwer),
    };

    /// <summary>Empfohlener Schwierigkeitsgrad für ein Thema (Standard: Mittel).</summary>
    public static Difficulty For(string topic)
    {
        var t = topic.ToLowerInvariant();
        foreach (var (keywords, difficulty) in Rules)
            if (keywords.Any(k => t.Contains(k)))
                return difficulty;
        return Difficulty.Mittel;
    }
}
