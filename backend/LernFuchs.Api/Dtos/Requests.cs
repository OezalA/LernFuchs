using LernFuchs.Api.Models;

namespace LernFuchs.Api.Dtos;

/// <summary>Anfrage zum Erzeugen neuer Vokabeln.</summary>
public record GenerateVocabularyRequest(string Topic, Difficulty Difficulty = Difficulty.Mittel, int Count = 10);

/// <summary>Anfrage zum Erzeugen eines Lesetextes mit Fragen.</summary>
public record GenerateReadingRequest(string Topic, Difficulty Difficulty = Difficulty.Mittel, int QuestionCount = 4);

/// <summary>Ergebnis einer Vokabel-Wiederholung (richtig/falsch beantwortet).</summary>
public record ReviewResult(bool Correct);

/// <summary>Eine eingereichte Antwort auf eine Verständnisfrage.</summary>
public record AnswerSubmission(int QuestionId, string Answer);

/// <summary>Rückmeldung zu einer beantworteten Frage.</summary>
public record AnswerFeedback(int QuestionId, bool IsCorrect, string CorrectAnswer, string? Explanation);
