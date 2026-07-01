import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { VocabularyService } from '../../core/vocabulary.service';
import { VocabularyWord, Difficulty } from '../../core/models';

@Component({
  selector: 'app-wortschatz',
  imports: [FormsModule],
  templateUrl: './wortschatz.html',
  styleUrl: './wortschatz.css'
})
export class Wortschatz implements OnInit {
  private vocab = inject(VocabularyService);

  // Datenbestand
  words = signal<VocabularyWord[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  // Formular zum Erzeugen
  topic = signal('');
  difficulty = signal<Difficulty>('Mittel');
  count = signal(8);
  generating = signal(false);

  // Übungsmodus (Karteikarten)
  deck = signal<VocabularyWord[]>([]);
  cardIndex = signal(0);
  flipped = signal(false);
  practicing = signal(false);
  reviewedCount = signal(0);
  correctCount = signal(0);

  currentCard = computed(() => this.deck()[this.cardIndex()] ?? null);
  sessionFinished = computed(() => this.practicing() && this.cardIndex() >= this.deck().length);

  // ngModel-Brücken zu den Signalen
  get topicModel() { return this.topic(); }
  set topicModel(v: string) { this.topic.set(v); }
  get difficultyModel() { return this.difficulty(); }
  set difficultyModel(v: Difficulty) { this.difficulty.set(v); }
  get countModel() { return this.count(); }
  set countModel(v: number) { this.count.set(v); }

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

  generate(): void {
    const topic = this.topic().trim();
    if (!topic) { this.error.set('Bitte gib ein Thema ein.'); return; }
    this.generating.set(true);
    this.error.set(null);
    this.vocab.generate({ topic, difficulty: this.difficulty(), count: this.count() }).subscribe({
      next: created => {
        this.words.update(list => [...created, ...list]);
        this.generating.set(false);
        this.topic.set('');
      },
      error: () => {
        this.error.set('Beim Erstellen ist etwas schiefgelaufen. Versuche es später noch einmal.');
        this.generating.set(false);
      }
    });
  }

  deleteWord(id: number): void {
    this.vocab.delete(id).subscribe({
      next: () => this.words.update(list => list.filter(w => w.id !== id))
    });
  }

  // ----- Karteikarten -----

  startPractice(): void {
    const shuffled = [...this.words()].sort(() => Math.random() - 0.5);
    this.deck.set(shuffled);
    this.cardIndex.set(0);
    this.flipped.set(false);
    this.reviewedCount.set(0);
    this.correctCount.set(0);
    this.practicing.set(true);
  }

  flip(): void {
    this.flipped.update(f => !f);
  }

  answer(correct: boolean): void {
    const card = this.currentCard();
    if (!card) return;
    this.vocab.review(card.id, correct).subscribe();
    this.reviewedCount.update(n => n + 1);
    if (correct) this.correctCount.update(n => n + 1);
    this.cardIndex.update(i => i + 1);
    this.flipped.set(false);
  }

  stopPractice(): void {
    this.practicing.set(false);
    this.load(); // aktualisierte Fortschritte holen
  }

  articleLabel(w: VocabularyWord): string {
    return w.article && w.article !== 'None' ? w.article.toLowerCase() : '';
  }
}
