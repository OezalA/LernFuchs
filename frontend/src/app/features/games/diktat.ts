import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { VocabularyService } from '../../core/vocabulary.service';
import { SpeechService } from '../../core/speech.service';
import { CelebrationService } from '../../core/celebration.service';
import { GameService } from '../../core/game.service';
import { VocabularyWord } from '../../core/models';
import { shuffle } from './game-utils';

@Component({
  selector: 'app-diktat-game',
  imports: [RouterLink, FormsModule],
  templateUrl: './diktat.html',
  styleUrl: './games.css'
})
export class DiktatGame implements OnInit {
  private vocab = inject(VocabularyService);
  private speech = inject(SpeechService);
  private celebrate = inject(CelebrationService);
  private game = inject(GameService);

  canSpeak = this.speech.supported;
  loading = signal(true);
  deck = signal<VocabularyWord[]>([]);
  index = signal(0);
  score = signal(0);
  input = signal('');
  checked = signal(false);
  wasCorrect = signal(false);
  finished = signal(false);

  current = computed(() => this.deck()[this.index()] ?? null);

  get inputModel() { return this.input(); }
  set inputModel(v: string) { this.input.set(v); }

  ngOnInit(): void {
    this.vocab.getAll().subscribe({
      next: words => {
        this.deck.set(shuffle(words).slice(0, 10));
        this.loading.set(false);
        setTimeout(() => this.sayCurrent(), 400);
      },
      error: () => this.loading.set(false)
    });
  }

  sayCurrent(): void {
    const w = this.current();
    if (w) this.speech.speak(w.word);
  }

  check(): void {
    if (this.checked()) return;
    const w = this.current();
    if (!w || !this.input().trim()) return;

    const correct = this.input().trim().toLowerCase() === w.word.toLowerCase();
    this.checked.set(true);
    this.wasCorrect.set(correct);
    if (correct) {
      this.score.update(s => s + 1);
      this.celebrate.correct();
    } else {
      this.celebrate.wrong();
    }
    this.vocab.review(w.id, correct).subscribe(res => this.game.handleActivity(res.game));
  }

  next(): void {
    if (this.index() + 1 >= this.deck().length) {
      this.finished.set(true);
      this.celebrate.confettiBig();
    } else {
      this.index.update(i => i + 1);
      this.input.set('');
      this.checked.set(false);
      setTimeout(() => this.sayCurrent(), 250);
    }
  }

  restart(): void {
    this.index.set(0);
    this.score.set(0);
    this.input.set('');
    this.checked.set(false);
    this.finished.set(false);
    this.vocab.getAll().subscribe(words => {
      this.deck.set(shuffle(words).slice(0, 10));
      setTimeout(() => this.sayCurrent(), 300);
    });
  }
}
