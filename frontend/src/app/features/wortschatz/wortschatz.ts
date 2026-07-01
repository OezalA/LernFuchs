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

  // Übungsmodus (Karteikarten)
  deck = signal<VocabularyWord[]>([]);
  cardIndex = signal(0);
  flipped = signal(false);
  practicing = signal(false);
  reviewedCount = signal(0);
  correctCount = signal(0);

  currentCard = computed(() => this.deck()[this.cardIndex()] ?? null);
  sessionFinished = computed(() => this.practicing() && this.cardIndex() >= this.deck().length);

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
    this.vocab.review(card.id, correct).subscribe(res => this.game.handleActivity(res.game));
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

  articleLabel(w: VocabularyWord): string {
    return w.article && w.article !== 'None' ? w.article.toLowerCase() : '';
  }
}
