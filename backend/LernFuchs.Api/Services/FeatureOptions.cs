namespace LernFuchs.Api.Services;

/// <summary>Schaltet Funktionen ein/aus (aus dem Abschnitt "Features" der Konfiguration).</summary>
public class FeatureOptions
{
    /// <summary>
    /// Dürfen Nutzer selbst Inhalte per KI erzeugen? In der öffentlichen Version aus
    /// (Kosten/Kontingent); täglich erzeugt stattdessen ein Hintergrunddienst neue Inhalte.
    /// </summary>
    public bool UserGenerationEnabled { get; set; } = true;

    /// <summary>
    /// Erzeugt ein Hintergrunddienst täglich automatisch neue Inhalte? In der Produktion an,
    /// in der Entwicklung standardmäßig aus (spart Kontingent beim lokalen Arbeiten).
    /// </summary>
    public bool DailyAutoGenerationEnabled { get; set; }

    /// <summary>Anzahl der täglich automatisch erzeugten deutschen Texte.</summary>
    public int DailyTextCount { get; set; } = 1;

    /// <summary>Anzahl der täglich automatisch erzeugten englischen Texte (Fremdsprache).</summary>
    public int DailyEnglishTextCount { get; set; } = 1;
    public int DailySpanishTextCount { get; set; } = 1;

    /// <summary>Anzahl der täglich automatisch erzeugten französischen Texte (Fremdsprache, Anfänger).</summary>
    public int DailyFrenchTextCount { get; set; } = 1;
}
