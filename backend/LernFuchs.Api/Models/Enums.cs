namespace LernFuchs.Api.Models;

/// <summary>Grammatikalisches Geschlecht (Artikel) eines Nomens.</summary>
public enum Article
{
    None,   // Nicht-Nomen (Verb, Adjektiv, ...)
    Der,
    Die,
    Das
}

/// <summary>Wortart.</summary>
public enum WordType
{
    Nomen,
    Verb,
    Adjektiv,
    Adverb,
    Praeposition,
    Pronomen,
    Konjunktion,
    Sonstiges,
    Satz
}

/// <summary>Schwierigkeitsgrad – ausgerichtet auf Gymnasium Klasse 5.</summary>
public enum Difficulty
{
    Leicht,
    Mittel,
    Schwer
}

/// <summary>Fragetyp bei der Leseverständnis-Übung.</summary>
public enum QuestionType
{
    MultipleChoice,
    OpenEnded
}

/// <summary>Lernsprache eines Inhalts: Deutsch (Muttersprache) oder Englisch (Fremdsprache).</summary>
public enum Language
{
    Deutsch,
    Englisch,
    Spanisch,
    Franzoesisch
}
