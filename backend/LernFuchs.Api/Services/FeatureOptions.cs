namespace LernFuchs.Api.Services;

/// <summary>Schaltet Funktionen ein/aus (aus dem Abschnitt "Features" der Konfiguration).</summary>
public class FeatureOptions
{
    /// <summary>
    /// Dürfen Nutzer selbst Inhalte per KI erzeugen? In der öffentlichen Version aus
    /// (Kosten/Kontingent); täglich erzeugt stattdessen ein Hintergrunddienst neue Inhalte.
    /// </summary>
    public bool UserGenerationEnabled { get; set; } = true;
}
