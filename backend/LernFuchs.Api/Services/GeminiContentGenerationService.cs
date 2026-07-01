using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LernFuchs.Api.Models;
using Microsoft.Extensions.Options;

namespace LernFuchs.Api.Services;

/// <summary>
/// Erzeugt Lerninhalte über die Google-Gemini-REST-API (kostenloser Tarif).
/// Antworten werden als JSON angefordert (responseMimeType) und robust geparst.
/// </summary>
public class GeminiContentGenerationService : IContentGenerationService
{
    private readonly HttpClient _http;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiContentGenerationService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GeminiContentGenerationService(
        HttpClient http,
        IOptions<GeminiOptions> options,
        ILogger<GeminiContentGenerationService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<VocabularyWord>> GenerateVocabularyAsync(
        string topic, Difficulty difficulty, int count, CancellationToken ct = default)
    {
        var prompt = $$"""
            Du bist ein Deutschlehrer für eine Schülerin der 5. Klasse Gymnasium.
            Erzeuge genau {{count}} nützliche deutsche Vokabeln zum Thema "{{topic}}" mit Schwierigkeitsgrad "{{difficulty}}".
            Wähle altersgerechte, im Alltag und in der Schule häufige Wörter.

            Alles ist auf DEUTSCH: das Wort, die Erklärung und der Beispielsatz.
            Die Erklärung ("definitionGerman") muss so einfach sein, dass ein Kind der 5. Klasse
            sie ohne Wörterbuch versteht: kurze Sätze, einfache Wörter, gern ein anschauliches Bild.

            Antworte ausschließlich als JSON in genau dieser Struktur:
            {
              "words": [
                {
                  "word": "das deutsche Wort in Grundform",
                  "article": "der | die | das | none",
                  "plural": "Pluralform oder null",
                  "wordType": "Nomen | Verb | Adjektiv | Adverb | Praeposition | Pronomen | Konjunktion | Sonstiges",
                  "definitionGerman": "einfache, kindgerechte deutsche Erklärung der Bedeutung",
                  "exampleSentence": "ein einfacher deutscher Beispielsatz mit dem Wort",
                  "synonyms": ["deutsches Synonym", "..."],
                  "antonyms": ["deutsches Gegenteil", "..."]
                }
              ]
            }
            Regeln: "article" ist nur bei Nomen der/die/das, sonst "none". Bei Nicht-Nomen ist "plural" null.
            "definitionGerman" darf das Wort selbst nicht einfach wiederholen, sondern muss es erklären.
            Gib keine Erklärungen außerhalb des JSON aus.
            """;

        var json = await CallGeminiAsync(prompt, ct);
        var dto = JsonSerializer.Deserialize<VocabularyResponse>(json, JsonOpts)
                  ?? throw new InvalidOperationException("Gemini-Antwort konnte nicht gelesen werden.");

        return dto.Words.Select(w => new VocabularyWord
        {
            Word = w.Word.Trim(),
            Article = ParseEnum(w.Article, Article.None),
            Plural = string.IsNullOrWhiteSpace(w.Plural) ? null : w.Plural.Trim(),
            WordType = ParseEnum(w.WordType, WordType.Sonstiges),
            DefinitionGerman = w.DefinitionGerman?.Trim() ?? string.Empty,
            MeaningTurkish = string.IsNullOrWhiteSpace(w.MeaningTurkish) ? null : w.MeaningTurkish.Trim(),
            ExampleSentence = w.ExampleSentence?.Trim(),
            Synonyms = w.Synonyms ?? new(),
            Antonyms = w.Antonyms ?? new(),
            Difficulty = difficulty,
            Topic = topic
        }).ToList();
    }

    public async Task<ReadingPassage> GenerateReadingPassageAsync(
        string topic, Difficulty difficulty, int questionCount, CancellationToken ct = default)
    {
        var prompt = $$"""
            Du bist ein Deutschlehrer für eine Schülerin der 5. Klasse Gymnasium (Muttersprache Türkisch).
            Schreibe einen altersgerechten deutschen Lesetext zum Thema "{{topic}}" mit Schwierigkeitsgrad "{{difficulty}}".
            Der Text soll etwa 120-200 Wörter haben und interessant sein.
            Formuliere danach genau {{questionCount}} Verständnisfragen zum Text.

            Antworte ausschließlich als JSON in genau dieser Struktur:
            {
              "title": "kurzer Titel",
              "text": "der Lesetext",
              "questions": [
                {
                  "questionText": "die Frage",
                  "questionType": "MultipleChoice | OpenEnded",
                  "options": ["A", "B", "C", "D"],
                  "correctAnswer": "der Text der richtigen Antwort",
                  "explanation": "kurze Begründung"
                }
              ]
            }
            Regeln: Mische MultipleChoice- und OpenEnded-Fragen. Bei OpenEnded ist "options" ein leeres Array
            und "correctAnswer" eine Musterantwort. Bei MultipleChoice muss "correctAnswer" exakt einer Option entsprechen.
            Gib keine Erklärungen außerhalb des JSON aus.
            """;

        var json = await CallGeminiAsync(prompt, ct);
        var dto = JsonSerializer.Deserialize<ReadingResponse>(json, JsonOpts)
                  ?? throw new InvalidOperationException("Gemini-Antwort konnte nicht gelesen werden.");

        var text = dto.Text.Trim();
        return new ReadingPassage
        {
            Title = dto.Title.Trim(),
            Text = text,
            Difficulty = difficulty,
            Topic = topic,
            WordCount = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length,
            Questions = dto.Questions.Select(q => new ComprehensionQuestion
            {
                QuestionText = q.QuestionText.Trim(),
                QuestionType = ParseEnum(q.QuestionType, QuestionType.MultipleChoice),
                Options = q.Options ?? new(),
                CorrectAnswer = q.CorrectAnswer.Trim(),
                Explanation = q.Explanation?.Trim()
            }).ToList()
        };
    }

    /// <summary>Ruft die Gemini-API auf und gibt den reinen JSON-Text der Antwort zurück.</summary>
    private async Task<string> CallGeminiAsync(string prompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException(
                "Kein Gemini-API-Schlüssel konfiguriert. Bitte 'Gemini:ApiKey' in den User Secrets oder appsettings setzen.");

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent";

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new { responseMimeType = "application/json" }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-goog-api-key", _options.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, ct);
        var responseText = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini-Fehler {Status}: {Body}", response.StatusCode, responseText);
            throw new HttpRequestException($"Gemini-API antwortete mit {(int)response.StatusCode}.");
        }

        using var doc = JsonDocument.Parse(responseText);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Gemini lieferte eine leere Antwort.");

        return text;
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        return Enum.TryParse<TEnum>(value.Trim(), ignoreCase: true, out var parsed) ? parsed : fallback;
    }

    // --- interne DTOs zum Parsen der Gemini-JSON-Antwort ---

    private sealed record VocabularyResponse
    {
        [JsonPropertyName("words")] public List<VocabularyDto> Words { get; init; } = new();
    }

    private sealed record VocabularyDto
    {
        public string Word { get; init; } = "";
        public string? Article { get; init; }
        public string? Plural { get; init; }
        public string? WordType { get; init; }
        public string MeaningTurkish { get; init; } = "";
        public string? DefinitionGerman { get; init; }
        public string? ExampleSentence { get; init; }
        public List<string>? Synonyms { get; init; }
        public List<string>? Antonyms { get; init; }
    }

    private sealed record ReadingResponse
    {
        public string Title { get; init; } = "";
        public string Text { get; init; } = "";
        public List<QuestionDto> Questions { get; init; } = new();
    }

    private sealed record QuestionDto
    {
        public string QuestionText { get; init; } = "";
        public string? QuestionType { get; init; }
        public List<string>? Options { get; init; }
        public string CorrectAnswer { get; init; } = "";
        public string? Explanation { get; init; }
    }
}
