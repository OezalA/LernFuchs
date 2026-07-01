namespace LernFuchs.Api.Services;

/// <summary>Konfiguration für die Google-Gemini-Anbindung (aus appsettings.json / Secrets).</summary>
public class GeminiOptions
{
    /// <summary>API-Schlüssel aus Google AI Studio (kostenlos).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Modellname, z. B. "gemini-2.0-flash".</summary>
    public string Model { get; set; } = "gemini-2.0-flash";
}
