import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { VocabularyService } from '../../core/vocabulary.service';
import { CelebrationService } from '../../core/celebration.service';
import { GameService } from '../../core/game.service';
import { VocabularyWord } from '../../core/models';
import { shuffle } from './game-utils';

@Component({
  selector: 'app-artikel-game',
  imports: [RouterLink],
  templateUrl: './artikel.html',
  styleUrl: './games.css'
})
export class ArtikelGame implements OnInit {
  private vocab = inject(VocabularyService);
  private celebrate = inject(CelebrationService);
  private game = inject(GameService);

  readonly articles = ['der', 'die', 'das'];

  loading = signal(true);
  deck = signal<VocabularyWord[]>([]);
  index = signal(0);
  score = signal(0);
  chosen = signal<string | null>(null);
  finished = signal(false);

  current = computed(() => this.deck()[this.index()] ?? null);

  ngOnInit(): void {
    this.vocab.getAll().subscribe({
      next: words => {
        const nouns = words.filter(w => w.article && w.article !== 'None');
        this.deck.set(shuffle(nouns).slice(0, 12));
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  correctArticle(w: VocabularyWord): string {
    return w.article.toLowerCase();
  }

  choose(article: string): void {
    if (this.chosen()) return;
    const w = this.current();
    if (!w) return;

    const correct = this.correctArticle(w) === article;
    this.chosen.set(article);
    if (correct) {
      this.score.update(s => s + 1);
      this.celebrate.correct();
    } else {
      this.celebrate.wrong();
    }
    this.vocab.review(w.id, correct).subscribe(res => this.game.handleActivity(res.game));

    setTimeout(() => this.next(), 1100);
  }

  private next(): void {
    if (this.index() + 1 >= this.deck().length) {
      this.finished.set(true);
      this.celebrate.confettiBig();
    } else {
      this.index.update(i => i + 1);
      this.chosen.set(null);
    }
  }

  buttonState(article: string): 'correct' | 'wrong' | '' {
    const c = this.chosen();
    if (!c) return '';
    const w = this.current();
    if (article === this.correctArticle(w!)) return 'correct';
    if (article === c) return 'wrong';
    return '';
  }

  restart(): void {
    this.index.set(0);
    this.score.set(0);
    this.chosen.set(null);
    this.finished.set(false);
    this.vocab.getAll().subscribe(words => {
      const nouns = words.filter(w => w.article && w.article !== 'None');
      this.deck.set(shuffle(nouns).slice(0, 12));
    });
  }
}
