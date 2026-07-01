namespace LernFuchs.Api.Data;

/// <summary>Struktur der Datei <c>Data/seed-data.json</c> mit den Startinhalten.</summary>
public record SeedRoot(List<SeedPassage> Passages, List<SeedWord> Words);

public record SeedPassage(
    string Title,
    string Text,
    string Difficulty,
    string Topic,
    int WordCount,
    List<SeedQuestion> Questions);

public record SeedQuestion(
    string QuestionText,
    List<string> Options,
    string CorrectAnswer,
    string? Explanation);

public record SeedWord(
    string Word,
    string Article,
    string? Plural,
    string WordType,
    string DefinitionGerman,
    string? ExampleSentence,
    List<string> Synonyms,
    List<string> Antonyms,
    string Difficulty,
    string Topic,
    List<string>? Conjugations = null);
