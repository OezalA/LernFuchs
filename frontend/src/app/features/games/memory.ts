import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { VocabularyService } from '../../core/vocabulary.service';
import { CelebrationService } from '../../core/celebration.service';
import { GameService } from '../../core/game.service';
import { ReadStateService } from '../../core/read-state.service';
import { VocabularyWord } from '../../core/models';
import { shuffle, wordWithArticle, readableWords } from './game-utils';

interface MemoryCard {
  key: number;
  wordId: number;
  kind: 'word' | 'def';
  text: string;
}

@Component({
  selector: 'app-memory-game',
  imports: [RouterLink],
  templateUrl: './memory.html',
  styleUrl: './games.css'
})
export class MemoryGame implements OnInit {
  private vocab = inject(VocabularyService);
  private celebrate = inject(CelebrationService);
  private game = inject(GameService);
  private readState = inject(ReadStateService);

  loading = signal(true);
  cards = signal<MemoryCard[]>([]);
  flipped = signal<number[]>([]);       // keys der aktuell offenen Karten
  matched = signal<Set<number>>(new Set());
  moves = signal(0);
  locked = signal(false);

  finished = computed(() =>
    this.cards().length > 0 && this.matched().size === this.cards().length);

  ngOnInit(): void {
    this.vocab.getAll().subscribe({
      next: words => { this.build(words); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  private build(words: VocabularyWord[]): void {
    const picked = shuffle(readableWords(words, this.readState.ids())).slice(0, 6);
    let key = 0;
    const cards: MemoryCard[] = [];
    for (const w of picked) {
      cards.push({ key: key++, wordId: w.id, kind: 'word', text: wordWithArticle(w) });
      cards.push({ key: key++, wordId: w.id, kind: 'def', text: w.definitionGerman });
    }
    this.cards.set(shuffle(cards));
    this.flipped.set([]);
    this.matched.set(new Set());
    this.moves.set(0);
  }

  isOpen(card: MemoryCard): boolean {
    return this.flipped().includes(card.key) || this.matched().has(card.key);
  }

  flip(card: MemoryCard): void {
    if (this.locked() || this.isOpen(card)) return;
    const open = [...this.flipped(), card.key];
    this.flipped.set(open);
    if (open.length < 2) return;

    this.moves.update(m => m + 1);
    const [aKey, bKey] = open;
    const a = this.cards().find(c => c.key === aKey)!;
    const b = this.cards().find(c => c.key === bKey)!;

    if (a.wordId === b.wordId && a.kind !== b.kind) {
      // Paar gefunden
      const next = new Set(this.matched());
      next.add(aKey); next.add(bKey);
      this.matched.set(next);
      this.flipped.set([]);
      this.celebrate.correct();
      this.vocab.review(a.wordId, true).subscribe(res => this.game.handleActivity(res.game));
      if (this.matched().size === this.cards().length) {
        setTimeout(() => this.celebrate.confettiBig(), 200);
      }
    } else {
      // kein Paar – kurz zeigen, dann zudrehen
      this.locked.set(true);
      this.celebrate.wrong();
      setTimeout(() => {
        this.flipped.set([]);
        this.locked.set(false);
      }, 900);
    }
  }

  restart(): void {
    this.vocab.getAll().subscribe(words => this.build(words));
  }
}
