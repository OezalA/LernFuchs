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
        string topic, Difficulty difficulty, int count,
        Language language = Language.Deutsch, CancellationToken ct = default)
    {
                var task = language switch
        {
            Language.Englisch => $$"""
                Du bist ein Englischlehrer für eine deutschsprachige Schülerin der 5. Klasse,
                die Englisch als FREMDSPRACHE lernt (Anfängerniveau, ca. A1-A2).
                Erzeuge genau {{count}} nützliche ENGLISCHE Vokabeln zum Thema "{{topic}}".
                Wähle sehr einfache, häufige Alltagswörter (leichter als im Deutschen!).
                Das Wort ("word") ist ENGLISCH. Die Erklärung ("definitionGerman") ist die deutsche
                Bedeutung/Übersetzung, kindgerecht und kurz. Der Beispielsatz ("exampleSentence")
                ist ein sehr einfacher ENGLISCHER Satz mit dem Wort.
                "article" ist immer "none" (Englisch hat keine der/die/das-Artikel),
                "plural" ist die englische Pluralform bei Nomen (sonst null),
                "conjugations" bleibt immer ein leeres Array [].
                """,
            Language.Spanisch => $$"""
                Du bist ein Spanischlehrer für eine deutschsprachige Schülerin der 5. Klasse,
                die zum ALLERERSTEN MAL Spanisch lernt (absolute Anfängerin, ganz von vorne, vor-A1).
                Sie hat noch KEINE Vorkenntnisse in Spanisch.
                Erzeuge genau {{count}} sehr einfache, grundlegende SPANISCHE Vokabeln zum Thema "{{topic}}".
                Wähle die allereinfachsten, häufigsten Grundwörter (noch leichter als im Englischen!).
                Das Wort ("word") ist SPANISCH. Die Erklärung ("definitionGerman") ist die deutsche
                Bedeutung/Übersetzung, kindgerecht und kurz. Der Beispielsatz ("exampleSentence")
                ist ein sehr einfacher SPANISCHER Satz mit dem Wort.
                "article" ist immer "none" (die spanischen Artikel el/la behandeln wir hier noch nicht),
                "plural" ist die spanische Pluralform bei Nomen (sonst null),
                "conjugations" bleibt immer ein leeres Array [].
                """,
            Language.Franzoesisch => $$"""
                Du bist ein Französischlehrer für eine deutschsprachige Schülerin der 5. Klasse,
                die zum ALLERERSTEN MAL Französisch lernt (absolute Anfängerin, ganz von vorne, vor-A1).
                Sie hat noch KEINE Vorkenntnisse in Französisch.
                Erzeuge genau {{count}} sehr einfache, grundlegende FRANZÖSISCHE Vokabeln zum Thema "{{topic}}".
                Wähle die allereinfachsten, häufigsten Grundwörter (noch leichter als im Englischen!).
                Das Wort ("word") ist FRANZÖSISCH. Die Erklärung ("definitionGerman") ist die deutsche
                Bedeutung/Übersetzung, kindgerecht und kurz. Der Beispielsatz ("exampleSentence")
                ist ein sehr einfacher FRANZÖSISCHER Satz mit dem Wort.
                "article" ist immer "none" (die französischen Artikel le/la behandeln wir hier noch nicht),
                "plural" ist die französische Pluralform bei Nomen (sonst null),
                "conjugations" bleibt immer ein leeres Array [].
                """,
            _ => $$"""
                Du bist ein Deutschlehrer für eine Schülerin der 5. Klasse Gymnasium.
                Erzeuge genau {{count}} nützliche deutsche Vokabeln zum Thema "{{topic}}" mit Schwierigkeitsgrad "{{difficulty}}".
                Wähle altersgerechte, im Alltag und in der Schule häufige Wörter.

                Alles ist auf DEUTSCH: das Wort, die Erklärung und der Beispielsatz.
                Die Erklärung ("definitionGerman") muss so einfach sein, dass ein Kind der 5. Klasse
                sie ohne Wörterbuch versteht: kurze Sätze, einfache Wörter, gern ein anschauliches Bild.
                """
        };


        var prompt = $$"""
            {{task}}

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
            Article = language == Language.Deutsch ? ParseEnum(w.Article, Article.None) : Article.None,
            Plural = string.IsNullOrWhiteSpace(w.Plural) ? null : w.Plural.Trim(),
            WordType = ParseEnum(w.WordType, WordType.Sonstiges),
            DefinitionGerman = w.DefinitionGerman?.Trim() ?? string.Empty,
            ExampleSentence = w.ExampleSentence?.Trim(),
            Synonyms = w.Synonyms ?? new(),
            Antonyms = w.Antonyms ?? new(),
            Conjugations = w.Conjugations ?? new(),
            Difficulty = difficulty,
            Language = language,
            Topic = topic
        }).ToList();
    }

    public async Task<GeneratedReading> GenerateReadingPassageAsync(
        string topic, Difficulty difficulty, int questionCount,
        Language language = Language.Deutsch,
        CancellationToken ct = default, string? modelOverride = null)
    {
                var task = language switch
        {
            Language.Englisch => $$"""
                Du bist ein Englischlehrer für eine deutschsprachige Schülerin der 5. Klasse,
                die Englisch als FREMDSPRACHE lernt (Anfängerniveau, ca. A1-A2).
                Schreibe einen SEHR EINFACHEN, kurzen ENGLISCHEN Lesetext zum Thema "{{topic}}":
                nur etwa 60-110 Wörter, kurze Sätze, häufige Alltagswörter, meistens Präsens.
                Er soll deutlich leichter sein als ein deutscher Text für dieses Alter.
                Formuliere danach genau {{questionCount}} einfache Verständnisfragen AUF ENGLISCH zum Text.

                Da Englisch eine Fremdsprache ist, kennt die Anfängerin viele Wörter noch NICHT –
                auch einfache Alltagswörter (z. B. sky, run, happy, blue). Nimm deshalb etwa
                10-16 nützliche englische Wörter aus DEINEM Text als "difficultWords" auf
                (Nomen, Verben, Adjektive), die eine deutsche Anfängerin lernen sollte.
                Erkläre jedes: "definitionGerman" ist die DEUTSCHE Übersetzung/Bedeutung
                (kindgerecht), "exampleSentence" ist ein einfacher ENGLISCHER Beispielsatz.
                "word" steht in der Grundform (z. B. "run" statt "running", "sky" statt "skies").
                "article" ist immer "none", "conjugations" bleibt ein leeres Array [].

                Titel, Text, Fragen und Antwortmöglichkeiten sind auf ENGLISCH;
                nur "definitionGerman" der schwierigen Wörter ist auf Deutsch.
                """,
            Language.Spanisch => $$"""
                Du bist ein Spanischlehrer für eine deutschsprachige Schülerin der 5. Klasse,
                die zum ALLERERSTEN MAL Spanisch lernt (absolute Anfängerin, vor-A1, keine Vorkenntnisse).
                Schreibe einen EXTREM EINFACHEN, sehr kurzen SPANISCHEN Lesetext zum Thema "{{topic}}":
                nur etwa 40-80 Wörter, sehr kurze Sätze, nur die häufigsten Grundwörter, nur Präsens.
                Er soll noch deutlich leichter sein als ein englischer Anfängertext.
                Formuliere danach genau {{questionCount}} sehr einfache Verständnisfragen AUF DEUTSCH zum Text
                (die Anfängerin kann noch kein Spanisch lesen).

                Da Spanisch völlig neu ist, kennt die Anfängerin so gut wie KEIN Wort.
                Nimm deshalb etwa 10-16 nützliche spanische Wörter aus DEINEM Text als "difficultWords" auf
                (Nomen, Verben, Adjektive), die eine deutsche Anfängerin lernen sollte.
                Erkläre jedes: "definitionGerman" ist die DEUTSCHE Übersetzung/Bedeutung
                (kindgerecht), "exampleSentence" ist ein einfacher SPANISCHER Beispielsatz.
                "word" steht in der Grundform.
                "article" ist immer "none", "conjugations" bleibt ein leeres Array [].

                Titel und Text sind auf SPANISCH (das ist der Lesetext).
                Die Verständnisfragen und ihre Antwortmöglichkeiten sind auf DEUTSCH,
                ebenso "definitionGerman" der schwierigen Wörter.
                """,
            Language.Franzoesisch => $$"""
                Du bist ein Französischlehrer für eine deutschsprachige Schülerin der 5. Klasse,
                die zum ALLERERSTEN MAL Französisch lernt (absolute Anfängerin, vor-A1, keine Vorkenntnisse).
                Schreibe einen EXTREM EINFACHEN, sehr kurzen FRANZÖSISCHEN Lesetext zum Thema "{{topic}}":
                nur etwa 40-80 Wörter, sehr kurze Sätze, nur die häufigsten Grundwörter, nur Präsens.
                Er soll noch deutlich leichter sein als ein englischer Anfängertext.
                Formuliere danach genau {{questionCount}} sehr einfache Verständnisfragen AUF DEUTSCH zum Text
                (die Anfängerin kann noch kein Französisch lesen).

                Da Französisch völlig neu ist, kennt die Anfängerin so gut wie KEIN Wort.
                Nimm deshalb etwa 10-16 nützliche französische Wörter aus DEINEM Text als "difficultWords" auf
                (Nomen, Verben, Adjektive), die eine deutsche Anfängerin lernen sollte.
                Erkläre jedes: "definitionGerman" ist die DEUTSCHE Übersetzung/Bedeutung
                (kindgerecht), "exampleSentence" ist ein einfacher FRANZÖSISCHER Beispielsatz.
                "word" steht in der Grundform.
                "article" ist immer "none", "conjugations" bleibt ein leeres Array [].

                Titel und Text sind auf FRANZÖSISCH (das ist der Lesetext).
                Die Verständnisfragen und ihre Antwortmöglichkeiten sind auf DEUTSCH,
                ebenso "definitionGerman" der schwierigen Wörter.
                """,
            _ => $$"""
                Du bist ein Deutschlehrer für eine Schülerin der 5. Klasse Gymnasium.
                Schreibe einen altersgerechten deutschen Lesetext zum Thema "{{topic}}" mit Schwierigkeitsgrad "{{difficulty}}".
                Der Text soll etwa 120-200 Wörter haben und interessant sein.
                Formuliere danach genau {{questionCount}} Verständnisfragen zum Text.

                Suche außerdem die 3-6 schwierigsten Wörter aus DEINEM Text heraus (Wörter, die ein Kind
                der 5. Klasse vielleicht noch nicht kennt) und erkläre sie einfach – alles auf Deutsch.
                """
        };


        var prompt = $$"""
            {{task}}

            Alle Fragen sind Multiple-Choice mit genau 4 Antwortmöglichkeiten.

            Antworte ausschließlich als JSON in genau dieser Struktur:
            {
              "title": "kurzer Titel",
              "text": "der Lesetext",
              "questions": [
                {
                  "questionText": "die Frage",
                  "questionType": "MultipleChoice",
                  "options": ["A", "B", "C", "D"],
                  "correctAnswer": "der Text der richtigen Antwort",
                  "explanation": "kurze Begründung"
                }
              ],
              "difficultWords": [
                {
                  "word": "schwieriges Wort aus dem Text in Grundform",
                  "article": "der | die | das | none",
                  "plural": "Pluralform oder null",
                  "wordType": "Nomen | Verb | Adjektiv | Adverb | Praeposition | Pronomen | Konjunktion | Sonstiges",
                  "definitionGerman": "einfache, kindgerechte deutsche Erklärung",
                  "exampleSentence": "einfacher deutscher Beispielsatz",
                  "synonyms": ["deutsches Synonym"],
                  "antonyms": ["deutsches Gegenteil"],
                  "conjugations": ["ich-Form", "du-Form", "er/sie/es-Form", "wir-Form", "ihr-Form", "sie/Sie-Form"]
                }
              ]
            }
            Regeln: Jede Frage hat genau 4 Optionen, und "correctAnswer" muss exakt einer der Optionen entsprechen.
            Formuliere klare Fragen mit nur einer eindeutig richtigen Antwort.
            Die Wörter in "difficultWords" müssen wirklich im Text vorkommen.
            "conjugations" NUR bei Verben ausfüllen (Präsens: ich, du, er/sie/es, wir, ihr, sie/Sie),
            bei allen anderen Wortarten ein leeres Array [].
            Gib keine Erklärungen außerhalb des JSON aus.
            """;

        var json = await CallGeminiAsync(prompt, ct, modelOverride);
        var dto = JsonSerializer.Deserialize<ReadingResponse>(json, JsonOpts)
                  ?? throw new InvalidOperationException("Gemini-Antwort konnte nicht gelesen werden.");

        var text = dto.Text.Trim();
        var passage = new ReadingPassage
        {
            Title = dto.Title.Trim(),
            Text = text,
            Difficulty = difficulty,
            Language = language,
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

        var difficultWords = (dto.DifficultWords ?? new())
            .Where(w => !string.IsNullOrWhiteSpace(w.Word))
            .Select(w => new VocabularyWord
            {
                Word = w.Word.Trim(),
                Article = language == Language.Deutsch ? ParseEnum(w.Article, Article.None) : Article.None,
                Plural = string.IsNullOrWhiteSpace(w.Plural) ? null : w.Plural.Trim(),
                WordType = ParseEnum(w.WordType, WordType.Sonstiges),
                DefinitionGerman = w.DefinitionGerman?.Trim() ?? string.Empty,
                ExampleSentence = w.ExampleSentence?.Trim(),
                Synonyms = w.Synonyms ?? new(),
                Antonyms = w.Antonyms ?? new(),
                Difficulty = difficulty,
                Language = language,
                Topic = topic
            })
            .ToList();

        return new GeneratedReading(passage, difficultWords);
    }

    /// <summary>Ruft die Gemini-API auf und gibt den reinen JSON-Text der Antwort zurück.</summary>
    private async Task<string> CallGeminiAsync(string prompt, CancellationToken ct, string? modelOverride = null)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException(
                "Kein Gemini-API-Schlüssel konfiguriert. Bitte 'Gemini:ApiKey' in den User Secrets oder appsettings setzen.");

        var model = string.IsNullOrWhiteSpace(modelOverride) ? _options.Model : modelOverride;
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new { responseMimeType = "application/json" }
        };
        var bodyJson = JsonSerializer.Serialize(requestBody);

        // Bei vorübergehender Überlastung (503) mehrmals mit wachsender Wartezeit erneut versuchen.
        const int maxAttempts = 4;
        string responseText = "";
        System.Net.HttpStatusCode status = default;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", _options.ApiKey);
            request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

            using var response = await _http.SendAsync(request, ct);
            status = response.StatusCode;
            responseText = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode) break;

            var transient = status is System.Net.HttpStatusCode.ServiceUnavailable
                                   or System.Net.HttpStatusCode.InternalServerError;
            if (transient && attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(2 * attempt);
                _logger.LogWarning("Gemini {Status}, Versuch {Attempt}/{Max} – neuer Versuch in {Delay}s.",
                    (int)status, attempt, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay, ct);
                continue;
            }

            _logger.LogError("Gemini-Fehler {Status}: {Body}", status, responseText);
            throw new HttpRequestException($"Gemini-API antwortete mit {(int)status}.");
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
        public string? DefinitionGerman { get; init; }
        public string? ExampleSentence { get; init; }
        public List<string>? Synonyms { get; init; }
        public List<string>? Antonyms { get; init; }
        public List<string>? Conjugations { get; init; }
    }

    private sealed record ReadingResponse
    {
        public string Title { get; init; } = "";
        public string Text { get; init; } = "";
        public List<QuestionDto> Questions { get; init; } = new();
        public List<VocabularyDto>? DifficultWords { get; init; }
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
