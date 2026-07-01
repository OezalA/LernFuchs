import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { VocabularyService } from '../../core/vocabulary.service';
import { CelebrationService } from '../../core/celebration.service';
import { GameService } from '../../core/game.service';
import { ReadStateService } from '../../core/read-state.service';
import { VocabularyWord } from '../../core/models';
import { shuffle, readableWords } from './game-utils';

interface ConjQuestion {
  verb: VocabularyWord;
  pronounIndex: number;
  correct: string;
  options: string[];
}

@Component({
  selector: 'app-verb-game',
  imports: [RouterLink],
  templateUrl: './verb.html',
  styleUrl: './games.css'
})
export class VerbGame implements OnInit {
  private vocab = inject(VocabularyService);
  private celebrate = inject(CelebrationService);
  private game = inject(GameService);
  private readState = inject(ReadStateService);

  readonly pronouns = ['ich', 'du', 'er/sie/es', 'wir', 'ihr', 'sie/Sie'];

  loading = signal(true);
  questions = signal<ConjQuestion[]>([]);
  index = signal(0);
  score = signal(0);
  answered = signal<string | null>(null);
  finished = signal(false);

  current = computed(() => this.questions()[this.index()] ?? null);

  ngOnInit(): void {
    this.vocab.getAll().subscribe({
      next: words => { this.build(words); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  private build(words: VocabularyWord[]): void {
    // Nur Verben aus gelesenen Texten, die eine vollständige Konjugation haben.
    const verbs = readableWords(words, this.readState.ids())
      .filter(w => w.wordType === 'Verb' && w.conjugations && w.conjugations.length === 6);

    const deck = shuffle(verbs).slice(0, 10).map(verb => {
      const pronounIndex = Math.floor(Math.random() * 6);
      const correct = verb.conjugations[pronounIndex];
      const options = shuffle(Array.from(new Set(verb.conjugations)));
      return { verb, pronounIndex, correct, options };
    });
    this.questions.set(deck);
  }

  choose(option: string): void {
    if (this.answered()) return;
    const q = this.current();
    if (!q) return;

    const isCorrect = option === q.correct;
    this.answered.set(option);
    if (isCorrect) {
      this.score.update(s => s + 1);
      this.celebrate.correct();
    } else {
      this.celebrate.wrong();
    }
    this.vocab.review(q.verb.id, isCorrect).subscribe(res => this.game.handleActivity(res.game));

    setTimeout(() => this.next(), 1200);
  }

  private next(): void {
    if (this.index() + 1 >= this.questions().length) {
      this.finished.set(true);
      this.celebrate.confettiBig();
    } else {
      this.index.update(i => i + 1);
      this.answered.set(null);
    }
  }

  optionState(option: string): 'correct' | 'wrong' | '' {
    const chosen = this.answered();
    if (!chosen) return '';
    const q = this.current();
    if (option === q?.correct) return 'correct';
    if (option === chosen) return 'wrong';
    return '';
  }

  restart(): void {
    this.index.set(0);
    this.score.set(0);
    this.answered.set(null);
    this.finished.set(false);
    this.vocab.getAll().subscribe(words => this.build(words));
  }
}
