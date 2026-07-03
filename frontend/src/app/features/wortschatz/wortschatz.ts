import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { VocabularyService } from '../../core/vocabulary.service';
import { SpeechService } from '../../core/speech.service';
import { CelebrationService } from '../../core/celebration.service';
import { GameService } from '../../core/game.service';
import { ReadStateService } from '../../core/read-state.service';
import { categoryFor } from '../../core/category';
import { VocabularyWord } from '../../core/models';

@Component({
  selector: 'app-wortschatz',
  imports: [RouterLink],
  templateUrl: './wortschatz.html',
  styleUrl: './wortschatz.css'
})
export class Wortschatz implements OnInit {
  private vocab = inject(VocabularyService);
  private speech = inject(SpeechService);
  private celebrate = inject(CelebrationService);
  private game = inject(GameService);
  private readState = inject(ReadStateService);

  words = signal<VocabularyWord[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  canSpeak = this.speech.supported;

  // Ausgewählte Kategorie (null = alle).
  selectedCategory = signal<string | null>(null);

  // Übungsmodus (Karteikarten – Lernen)
  deck = signal<VocabularyWord[]>([]);
  cardIndex = signal(0);
  flipped = signal(false);
  practicing = signal(false);
  reviewedCount = signal(0);
  correctCount = signal(0);

  currentCard = computed(() => this.deck()[this.cardIndex()] ?? null);
  sessionFinished = computed(() => this.practicing() && this.cardIndex() >= this.deck().length);

  // Test-Modus (Multiple Choice; ein Wort 4x hintereinander richtig = gelernt)
  readonly testTarget = 4;
  readonly dots4 = [0, 1, 2, 3];
  testing = signal(false);
  testPool = signal<VocabularyWord[]>([]);       // noch nicht "gelernte" Wörter dieser Runde
  testCurrent = signal<VocabularyWord | null>(null);
  testOptions = signal<string[]>([]);
  testChosen = signal<string | null>(null);
  testLearned = signal(0);                        // in dieser Runde gelernte Wörter
  testJustLearned = signal(false);
  private testStreak = new Map<number, number>(); // richtige in Folge je Wort-Id

  testFinished = computed(() => this.testing() && this.testPool().length === 0);
  testCorrect = computed(() => {
    const c = this.testCurrent();
    return !!c && this.testChosen() === c.definitionGerman;
  });

  // Nur Wörter aus gelesenen Texten (oder eigenständige Wörter ohne Quelltext).
  availableWords = computed(() => {
    const read = this.readState.ids();
    return this.words().filter(w => w.sourcePassageId == null || read.has(w.sourcePassageId));
  });

  // Nach Kategorie gruppiert
  groupedWords = computed(() => {
    const groups = new Map<string, { icon: string; words: VocabularyWord[] }>();
    for (const w of this.availableWords()) {
      const cat = categoryFor(w.topic);
      const g = groups.get(cat.name);
      if (g) g.words.push(w); else groups.set(cat.name, { icon: cat.icon, words: [w] });
    }
    return Array.from(groups, ([name, g]) => ({ name, icon: g.icon, words: g.words }))
      .sort((a, b) => a.name.localeCompare(b.name));
  });

  categories = computed(() =>
    this.groupedWords().map(g => ({ name: g.name, icon: g.icon, count: g.words.length })));

  displayedGroups = computed(() => {
    const sel = this.selectedCategory();
    return sel ? this.groupedWords().filter(g => g.name === sel) : this.groupedWords();
  });

  // Wörter des aktuellen Filters (für die Übung).
  shownWords = computed(() => {
    const sel = this.selectedCategory();
    return sel
      ? this.availableWords().filter(w => categoryFor(w.topic).name === sel)
      : this.availableWords();
  });

  selectCategory(name: string | null): void {
    this.selectedCategory.set(name);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.vocab.getAll().subscribe({
      next: w => { this.words.set(w); this.loading.set(false); },
      error: () => { this.error.set('Die Wörter konnten nicht geladen werden.'); this.loading.set(false); }
    });
  }

  // ----- Karteikarten -----

  /** Übt die aktuell angezeigten Wörter (alle oder die gewählte Kategorie). */
  startPractice(): void {
    this.beginPractice([...this.shownWords()]);
  }

  /** Übt nur die heute fälligen Wörter (Leitner-System) aus gelesenen Texten. */
  startDuePractice(): void {
    this.vocab.getDue(50).subscribe({
      next: due => {
        const read = this.readState.ids();
        const scoped = due.filter(w => w.sourcePassageId == null || read.has(w.sourcePassageId));
        if (!scoped.length) { this.error.set('Heute ist nichts fällig – super, du bist auf dem neuesten Stand! 🎉'); return; }
        this.beginPractice(scoped);
      }
    });
  }

  private beginPractice(cards: VocabularyWord[]): void {
    const shuffled = cards.sort(() => Math.random() - 0.5);
    this.deck.set(shuffled);
    this.cardIndex.set(0);
    this.flipped.set(false);
    this.reviewedCount.set(0);
    this.correctCount.set(0);
    this.practicing.set(true);
    this.error.set(null);
  }

  /** Liest ein Wort (mit Artikel) laut vor. */
  speak(w: VocabularyWord, event?: Event): void {
    event?.stopPropagation();
    const article = this.articleLabel(w);
    this.speech.speak(article ? `${article} ${w.word}` : w.word);
  }

  flip(): void {
    this.flipped.update(f => !f);
  }

  answer(correct: boolean): void {
    const card = this.currentCard();
    if (!card) return;
    // "Gewusst" markiert das Wort direkt als gelernt; "Nochmal" bleibt zum Üben.
    const req = correct ? this.vocab.markLearned(card.id) : this.vocab.review(card.id, false);
    req.subscribe(res => this.game.handleActivity(res.game));
    this.reviewedCount.update(n => n + 1);
    if (correct) {
      this.correctCount.update(n => n + 1);
      this.celebrate.correct();
    } else {
      this.celebrate.wrong();
    }
    this.cardIndex.update(i => i + 1);
    this.flipped.set(false);
    if (this.cardIndex() >= this.deck().length) this.celebrate.confettiBig();
  }

  stopPractice(): void {
    this.practicing.set(false);
    this.load();
  }

  // ----- Test (Multiple Choice) -----

  /** Startet den Test mit den angezeigten, noch nicht gelernten Wörtern. */
  startTest(): void {
    const pool = this.shownWords().filter(w => !w.progress?.mastered);
    if (!pool.length) { this.error.set('Alle Wörter sind schon gelernt – super! 🎉'); return; }
    this.testStreak.clear();
    this.testPool.set([...pool]);
    this.testLearned.set(0);
    this.testing.set(true);
    this.error.set(null);
    this.nextTestQuestion();
  }

  private nextTestQuestion(): void {
    const pool = this.testPool();
    this.testJustLearned.set(false);
    this.testChosen.set(null);
    if (!pool.length) { this.testCurrent.set(null); return; }

    // Zufälliges Wort, möglichst nicht dasselbe wie zuletzt.
    const prev = this.testCurrent();
    let word = pool[Math.floor(Math.random() * pool.length)];
    for (let g = 0; pool.length > 1 && prev && word.id === prev.id && g < 5; g++) {
      word = pool[Math.floor(Math.random() * pool.length)];
    }

    // Optionen: richtige Bedeutung + 3 Ablenker aus allen sichtbaren Wörtern.
    const allDefs = [...new Set(this.shownWords().map(w => w.definitionGerman).filter(d => !!d))];
    const distractors = this.sample(allDefs.filter(d => d !== word.definitionGerman), 3);
    this.testOptions.set(this.shuffle([word.definitionGerman, ...distractors]));
    this.testCurrent.set(word);
    this.speak(word);
  }

  chooseTest(option: string): void {
    if (this.testChosen()) return;
    const word = this.testCurrent();
    if (!word) return;
    this.testChosen.set(option);

    if (option === word.definitionGerman) {
      const streak = (this.testStreak.get(word.id) ?? 0) + 1;
      this.testStreak.set(word.id, streak);
      if (streak >= this.testTarget) {
        // 4x hintereinander richtig -> gelernt.
        this.vocab.markLearned(word.id).subscribe(res => this.game.handleActivity(res.game));
        this.testPool.update(p => p.filter(w => w.id !== word.id));
        this.testLearned.update(n => n + 1);
        this.testJustLearned.set(true);
        this.celebrate.confettiSmall();
      } else {
        this.celebrate.correct();
      }
    } else {
      this.testStreak.set(word.id, 0); // Serie zurückgesetzt
      this.celebrate.wrong();
    }
  }

  testOptionState(option: string): 'correct' | 'wrong' | '' {
    if (!this.testChosen()) return '';
    const word = this.testCurrent();
    if (option === word?.definitionGerman) return 'correct';
    if (option === this.testChosen()) return 'wrong';
    return '';
  }

  /** Richtige in Folge für das aktuelle Wort (0–4, für die Punkte-Anzeige). */
  testProgress(): number {
    const w = this.testCurrent();
    return w ? Math.min(this.testTarget, this.testStreak.get(w.id) ?? 0) : 0;
  }

  nextTest(): void {
    this.nextTestQuestion();
  }

  stopTest(): void {
    this.testing.set(false);
    this.load();
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

  articleLabel(w: VocabularyWord): string {
    return w.article && w.article !== 'None' ? w.article.toLowerCase() : '';
  }
}
