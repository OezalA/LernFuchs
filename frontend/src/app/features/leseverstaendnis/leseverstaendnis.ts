import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { LowerCasePipe } from '@angular/common';
import { ReadingService } from '../../core/reading.service';
import { VocabularyService } from '../../core/vocabulary.service';
import { SpeechService } from '../../core/speech.service';
import { CelebrationService } from '../../core/celebration.service';
import { GameService } from '../../core/game.service';
import { ReadStateService } from '../../core/read-state.service';
import { LanguageService } from '../../core/language.service';
import { categoryFor } from '../../core/category';
import {
  ReadingPassage, ReadingPassageSummary, PassageWord, PassageSentence, ComprehensionQuestion, CheckResult
} from '../../core/models';

// In der Fremdsprache lernt das Kind vor dem Lesen erst die Wörter ('vocab').
type View = 'list' | 'vocab' | 'reading' | 'quiz' | 'result';
interface TextToken { text: string; word?: PassageWord; }
interface VocabItem { word: PassageWord; options: string[]; answer: string; }

@Component({
  selector: 'app-leseverstaendnis',
  imports: [LowerCasePipe],
  templateUrl: './leseverstaendnis.html',
  styleUrl: './leseverstaendnis.css'
})
export class Leseverstaendnis implements OnInit {
  private reading = inject(ReadingService);
  private vocab = inject(VocabularyService);
  private speech = inject(SpeechService);
  private celebrate = inject(CelebrationService);
  private game = inject(GameService);
  private readState = inject(ReadStateService);
  private lang = inject(LanguageService);

  canSpeak = this.speech.supported;
  isForeign = this.lang.current() !== 'Deutsch';

  // Wörter-Lernphase vor dem Lesen (Fremdsprachen): Karteikarten der schwierigen Wörter.
  vIndex = signal(0);
  vFlipped = signal(false);
  vocabDone = signal(false); // wurde die (optionale) Wörter-Lernphase abgeschlossen?
  currentCard = computed<PassageWord | null>(() => this.vocabWords()[this.vIndex()] ?? null);

  // --- Multiple-Choice-Abfrage: aktuell ungenutzt, wird in einer späteren Aufgabe wiederverwendet ---
  vocabDeck = signal<VocabItem[]>([]);
  vChosen = signal<string | null>(null);
  currentVocabItem = computed<VocabItem | null>(() => this.vocabDeck()[this.vIndex()] ?? null);
  vocabCorrect = computed(() => {
    const item = this.currentVocabItem();
    return !!item && this.vChosen() === item.answer;
  });

  view = signal<View>('list');
  passages = signal<ReadingPassageSummary[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  // Geöffneter Text
  current = signal<ReadingPassage | null>(null);
  loadingText = signal(false);
  activeWord = signal<PassageWord | null>(null); // angetipptes schwieriges Wort

  // "Satz für Satz" (Fremdsprache): jeder Satz mit deutscher Übersetzung.
  sentences = computed<PassageSentence[]>(() => this.current()?.sentences ?? []);
  activeSentence = signal<number | null>(null); // angetippter Satz (Index)

  // Quiz
  quizActive = signal(false); // läuft ein Quiz? (für "Zurück zu den Fragen")
  qIndex = signal(0);
  chosen = signal<string | null>(null);
  answers = signal<{ questionId: number; answer: string }[]>([]);
  checkResult = signal<CheckResult | null>(null);

  currentQuestion = computed<ComprehensionQuestion | null>(
    () => this.current()?.questions[this.qIndex()] ?? null);

  readCount = computed(() => this.passages().filter(p => this.readState.isRead(p.id)).length);

  // Bibliotheks-Reiter: noch zu lesen oder bereits gelesen.
  libraryTab = signal<'unread' | 'read'>('unread');
  // Ausgewählte Kategorie (null = alle anzeigen).
  selectedCategory = signal<string | null>(null);

  unreadCount = computed(() => this.passages().filter(p => !this.readState.isRead(p.id)).length);

  // Texte des aktuellen Reiters.
  tabPassages = computed(() => {
    const wantRead = this.libraryTab() === 'read';
    return this.passages().filter(p => this.readState.isRead(p.id) === wantRead);
  });

  // Nach Kategorie gruppierte Kacheln (des aktuellen Reiters)
  groupedPassages = computed(() => {
    const groups = new Map<string, { icon: string; items: ReadingPassageSummary[] }>();
    for (const p of this.tabPassages()) {
      const cat = categoryFor(p.topic);
      const g = groups.get(cat.name);
      if (g) g.items.push(p); else groups.set(cat.name, { icon: cat.icon, items: [p] });
    }
    return Array.from(groups, ([name, g]) => ({ name, icon: g.icon, items: g.items }))
      .sort((a, b) => a.name.localeCompare(b.name));
  });

  setTab(tab: 'unread' | 'read'): void {
    this.libraryTab.set(tab);
    this.selectedCategory.set(null);
  }

  // Kategorie-Buttons (Name, Icon, Anzahl).
  categories = computed(() =>
    this.groupedPassages().map(g => ({ name: g.name, icon: g.icon, count: g.items.length })));

  // Angezeigte Gruppen je nach ausgewählter Kategorie.
  displayedGroups = computed(() => {
    const sel = this.selectedCategory();
    return sel ? this.groupedPassages().filter(g => g.name === sel) : this.groupedPassages();
  });

  selectCategory(name: string | null): void {
    this.selectedCategory.set(name);
  }

  // Text in Tokens zerlegt; schwierige Wörter sind markiert (Muttersprache).
  textTokens = computed<TextToken[]>(() => {
    const p = this.current();
    return p ? this.tokenize(p.text, p.words) : [];
  });

  // Fremdsprache: Text satzweise – jeder Satz ist unterstrichen und antippbar
  // (hören + deutsche Übersetzung); die schwierigen Wörter darin bleiben einzeln antippbar.
  sentenceRows = computed<{ german: string; tokens: TextToken[] }[]>(() => {
    const p = this.current();
    if (!p) return [];
    // Anfänger-Sprachen (Glossar vorhanden): keine Wörter unterstreichen, nur die Sätze.
    const words = p.glossary.length ? [] : p.words;
    return this.sentences().map(s => ({ german: s.german, tokens: this.tokenize(s.text, words) }));
  });

  // Zerlegt einen Text in Tokens; Vergleich über den längsten gemeinsamen Wortanfang,
  // damit auch gebeugte Formen erkannt werden (z. B. "stürmischen" -> "stürmisch").
  private tokenize(text: string, words: PassageWord[]): TextToken[] {
    return text.split(/(\s+)/).map(tok => {
      const core = tok.replace(/^[^A-Za-zÀ-ÿ]+|[^A-Za-zÀ-ÿ]+$/g, '');
      const word = core ? this.matchWord(core.toLowerCase(), words) : undefined;
      return { text: tok, word };
    });
  }

  /** Findet das schwierige Wort, dessen Wortstamm am besten zum Token passt. */
  private matchWord(token: string, words: PassageWord[]): PassageWord | undefined {
    // Exakte Treffer zuerst – so werden auch kurze Wörter wie "sky" oder "run" erkannt.
    for (const w of words) if (token === w.word.toLowerCase()) return w;

    let best: PassageWord | undefined;
    let bestLcp = 0;
    for (const w of words) {
      const base = w.word.toLowerCase();
      const lcp = this.commonPrefix(token, base);
      // Gebeugte Formen zulassen; auch kürzere Wörter (mind. 3 gemeinsame Zeichen).
      const threshold = Math.max(3, base.length - 3);
      if (lcp >= threshold && lcp > bestLcp) { best = w; bestLcp = lcp; }
    }
    return best;
  }

  private commonPrefix(a: string, b: string): number {
    const n = Math.min(a.length, b.length);
    let i = 0;
    while (i < n && a[i] === b[i]) i++;
    return i;
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.reading.getAll().subscribe({
      next: p => { this.passages.set(p); this.loading.set(false); },
      error: () => { this.error.set('Die Texte konnten nicht geladen werden.'); this.loading.set(false); }
    });
  }

  // ---- Navigation ----
  open(id: number): void {
    this.loadingText.set(true);
    this.activeWord.set(null);
    this.activeSentence.set(null);
    this.reading.getById(id).subscribe({
      next: p => {
        this.current.set(p);
        this.loadingText.set(false);
        this.vocabDone.set(false);
        // Direkt zum Text; in der Fremdsprache wird das Wörterlernen dort optional angeboten.
        this.view.set('reading');
      },
      error: () => { this.error.set('Der Text konnte nicht geöffnet werden.'); this.loadingText.set(false); }
    });
  }

  // Anfänger-Fremdsprachen (Spanisch/Französisch) liefern ein vollständiges Glossar (ALLE Wörter);
  // dann werden keine Wörter im Text unterstrichen, nur ganze Sätze.
  hasGlossary = computed(() => (this.current()?.glossary.length ?? 0) > 0);

  // Lernkarten: bei Anfänger-Sprachen ALLE Wörter (Glossar), sonst die schwierigen Wörter.
  vocabWords = computed<PassageWord[]>(() => {
    const p = this.current();
    if (!p) return [];
    return p.glossary.length ? p.glossary : p.words;
  });

  /** Startet die optionale Wörter-Lernphase (Karteikarten) vom Hinweis im Lesetext aus. */
  learnWords(): void {
    if (!this.vocabWords().length) return;
    this.vIndex.set(0);
    this.vFlipped.set(false);
    this.view.set('vocab');
    this.speakCard();
  }

  /** Dreht die Karteikarte um (Wort <-> Bedeutung). */
  flipCard(): void {
    this.vFlipped.update(f => !f);
  }

  /** Liest das aktuelle Wort auf der Fremdsprache vor. */
  speakCard(): void {
    const w = this.currentCard();
    if (w) this.speech.speak(w.word);
  }

  /** "Gewusst": Wort direkt als gelernt markieren und zur nächsten Karte. */
  markCardLearned(): void {
    const w = this.currentCard();
    if (w) this.vocab.markLearned(w.id).subscribe(res => this.game.handleActivity(res.game));
    this.celebrate.correct();
    this.nextCard();
  }

  /** Nächste Karte; nach der letzten geht es zum Text. */
  nextCard(): void {
    if (this.vIndex() + 1 < this.vocabWords().length) {
      this.vIndex.update(i => i + 1);
      this.vFlipped.set(false);
      this.speakCard();
    } else {
      this.speech.stop();
      this.vocabDone.set(true);
      this.view.set('reading');
    }
  }

  // --- Multiple-Choice-Abfrage: aktuell ungenutzt, für eine spätere Aufgabe aufbewahrt ---

  /** Baut je Wort eine Multiple-Choice-Frage (deutsche Bedeutung + 3 Ablenker). */
  private buildVocabDeck(words: PassageWord[]): VocabItem[] {
    const allDefs = [...new Set(words.map(w => w.definitionGerman).filter(d => !!d))];
    return words.map(w => {
      const answer = w.definitionGerman;
      const distractors = this.sample(allDefs.filter(d => d !== answer), 3);
      const options = this.shuffle([answer, ...distractors]);
      return { word: w, options, answer };
    });
  }

  speakCurrentVocab(): void {
    const item = this.currentVocabItem();
    if (item) this.speech.speak(item.word.word);
  }

  chooseVocab(option: string): void {
    if (this.vChosen()) return;
    this.vChosen.set(option);
    if (option === this.currentVocabItem()?.answer) this.celebrate.correct();
    else this.celebrate.wrong();
  }

  vocabOptionState(option: string): 'correct' | 'wrong' | '' {
    if (!this.vChosen()) return '';
    const item = this.currentVocabItem();
    if (option === item?.answer) return 'correct';
    if (option === this.vChosen()) return 'wrong';
    return '';
  }

  nextVocab(): void {
    if (this.vIndex() + 1 < this.vocabDeck().length) {
      this.vIndex.update(i => i + 1);
      this.vChosen.set(null);
      this.speakCurrentVocab();
    } else {
      // Wörter gelernt – zurück zum Text.
      this.speech.stop();
      this.vocabDone.set(true);
      this.view.set('reading');
    }
  }

  private sample<T>(arr: T[], n: number): T[] {
    return this.shuffle([...arr]).slice(0, n);
  }
  private shuffle<T>(arr: T[]): T[] {
    for (let i = arr.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [arr[i], arr[j]] = [arr[j], arr[i]];
    }
    return arr;
  }

  startQuiz(): void {
    this.speech.stop();
    this.qIndex.set(0);
    this.chosen.set(null);
    this.answers.set([]);
    this.checkResult.set(null);
    this.quizActive.set(true);
    this.view.set('quiz');
  }

  /** Während des Quiz kurz zum Text zurück (Fortschritt bleibt erhalten). */
  backToText(): void {
    this.speech.stop();
    this.activeWord.set(null);
    this.view.set('reading');
  }

  /** Vom Text zurück ins laufende Quiz. */
  resumeQuiz(): void {
    this.speech.stop();
    this.view.set('quiz');
  }

  backToList(): void {
    this.speech.stop();
    this.current.set(null);
    this.activeWord.set(null);
    this.quizActive.set(false);
    this.view.set('list');
    this.load();
  }

  // ---- Schwierige Wörter ----
  tapWord(word: PassageWord): void {
    this.activeWord.set(word);
    const article = word.article && word.article !== 'None' ? word.article.toLowerCase() + ' ' : '';
    this.speech.speak(article + word.word);
  }

  speakText(): void {
    const p = this.current();
    if (p) this.speech.speak(p.text);
  }
  stopSpeaking(): void { this.speech.stop(); }

  // ---- Satz für Satz (Fremdsprache) ----
  /** Liest den Satz vor und zeigt/versteckt seine deutsche Übersetzung. */
  tapSentence(index: number): void {
    const s = this.sentences()[index];
    if (!s) return;
    this.activeSentence.update(cur => (cur === index ? null : index));
    this.speech.speak(s.text);
  }

  // ---- Quiz ----
  choose(option: string): void {
    if (this.chosen()) return;
    const q = this.currentQuestion();
    if (!q) return;
    this.chosen.set(option);
    const correct = option === q.correctAnswer;
    if (correct) this.celebrate.correct(); else this.celebrate.wrong();
    this.answers.update(a => [...a, { questionId: q.id, answer: option }]);
  }

  isCorrectChoice(): boolean {
    const q = this.currentQuestion();
    return !!q && this.chosen() === q.correctAnswer;
  }

  optionState(option: string): 'correct' | 'wrong' | '' {
    if (!this.chosen()) return '';
    const q = this.currentQuestion();
    if (option === q?.correctAnswer) return 'correct';
    if (option === this.chosen()) return 'wrong';
    return '';
  }

  next(): void {
    const p = this.current();
    if (!p) return;
    if (this.qIndex() + 1 < p.questions.length) {
      this.qIndex.update(i => i + 1);
      this.chosen.set(null);
    } else {
      this.finish();
    }
  }

  private finish(): void {
    const p = this.current();
    if (!p) return;
    this.reading.check(p.id, this.answers()).subscribe({
      next: r => {
        this.checkResult.set(r);
        this.readState.markRead(p.id);
        this.game.handleActivity(r.game);
        this.quizActive.set(false);
        this.view.set('result');
        if (r.score === r.total) this.celebrate.fanfare();
        else if (r.score > 0) this.celebrate.confettiSmall();
      },
      error: () => this.error.set('Die Antworten konnten nicht geprüft werden.')
    });
  }

  // ---- Anzeige-Helfer ----
  difficultyStars(d: string): number {
    return d === 'Leicht' ? 1 : d === 'Mittel' ? 2 : 3;
  }

  resultStars(): number {
    const r = this.checkResult();
    if (!r || r.total === 0) return 0;
    const ratio = r.score / r.total;
    return ratio >= 1 ? 3 : ratio >= 0.6 ? 2 : ratio > 0 ? 1 : 0;
  }

  isRead(id: number): boolean {
    return this.readState.isRead(id);
  }
}
