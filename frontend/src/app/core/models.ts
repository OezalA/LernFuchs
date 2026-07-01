// Typen, die den Backend-Modellen entsprechen.

export type Difficulty = 'Leicht' | 'Mittel' | 'Schwer';
export type Article = 'None' | 'Der' | 'Die' | 'Das';
export type WordType =
  | 'Nomen' | 'Verb' | 'Adjektiv' | 'Adverb'
  | 'Praeposition' | 'Pronomen' | 'Konjunktion' | 'Sonstiges';
export type QuestionType = 'MultipleChoice' | 'OpenEnded';

export interface VocabularyProgress {
  id: number;
  box: number;
  timesCorrect: number;
  timesWrong: number;
  lastReviewedAt: string | null;
  nextReviewAt: string;
  mastered: boolean;
}

export interface VocabularyWord {
  id: number;
  word: string;
  article: Article;
  plural: string | null;
  wordType: WordType;
  definitionGerman: string;
  exampleSentence: string | null;
  synonyms: string[];
  antonyms: string[];
  difficulty: Difficulty;
  topic: string | null;
  sourcePassageId: number | null;
  createdAt: string;
  progress: VocabularyProgress | null;
}

export interface ComprehensionQuestion {
  id: number;
  questionText: string;
  questionType: QuestionType;
  options: string[];
}

export interface ReadingPassage {
  id: number;
  title: string;
  text: string;
  difficulty: Difficulty;
  topic: string | null;
  wordCount: number;
  createdAt: string;
  questions: ComprehensionQuestion[];
}

export interface ReadingPassageSummary {
  id: number;
  title: string;
  difficulty: Difficulty;
  topic: string | null;
  wordCount: number;
  createdAt: string;
  questionCount: number;
}

export interface AnswerFeedback {
  questionId: number;
  isCorrect: boolean;
  correctAnswer: string;
  explanation: string | null;
}

export interface CheckResult {
  total: number;
  score: number;
  feedback: AnswerFeedback[];
  game?: GameActivityResult;
}

export interface DayActivity {
  date: string;
  weekday: string;
  xp: number;
  reviews: number;
}

export interface ProgressData {
  boxes: number[];
  last7Days: DayActivity[];
}

export interface LearningStats {
  totalWords: number;
  masteredWords: number;
  dueWords: number;
  correctReviews: number;
  wrongReviews: number;
  successRate: number;
  totalPassages: number;
}

export interface AchievementView {
  code: string;
  title: string;
  description: string;
  icon: string;
  unlocked: boolean;
}

export interface GameState {
  xp: number;
  level: number;
  xpIntoLevel: number;
  xpForNextLevel: number;
  currentStreakDays: number;
  longestStreakDays: number;
  reviewsToday: number;
  dailyGoal: number;
  achievements: AchievementView[];
}

export interface GameActivityResult {
  xpGained: number;
  xp: number;
  level: number;
  leveledUp: boolean;
  newAchievements: AchievementView[];
}

// Request-Typen
export interface GenerateVocabularyRequest {
  topic: string;
  difficulty: Difficulty;
  count: number;
}

export interface GenerateReadingRequest {
  topic: string;
  difficulty: Difficulty;
  questionCount: number;
}
