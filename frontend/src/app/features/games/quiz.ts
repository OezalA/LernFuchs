import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { VocabularyService } from '../../core/vocabulary.service';
import { CelebrationService } from '../../core/celebration.service';
import { GameService } from '../../core/game.service';
import { ReadStateService } from '../../core/read-state.service';
import { VocabularyWord } from '../../core/models';
import { shuffle, wordWithArticle, readableWords } from './game-utils';

interface QuizQuestion {
  word: VocabularyWord;
  options: VocabularyWord[];
}

@Component({
  selector: 'app-quiz-game',
  imports: [RouterLink],
  templateUrl: './quiz.html',
  styleUrl: './games.css'
})
export class QuizGame implements OnInit {
  private vocab = inject(VocabularyService);
  private celebrate = inject(CelebrationService);
  private game = inject(GameService);
  private readState = inject(ReadStateService);

  loading = signal(true);
  questions = signal<QuizQuestion[]>([]);
  index = signal(0);
  score = signal(0);
  answered = signal<VocabularyWord | null>(null); // gewählte Antwort
  finished = signal(false);

  current = computed(() => this.questions()[this.index()] ?? null);
  label = wordWithArticle;

  ngOnInit(): void {
    this.vocab.getAll().subscribe({
      next: words => { this.build(readableWords(words, this.readState.ids())); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  private build(words: VocabularyWord[]): void {
    if (words.length < 4) { this.questions.set([]); return; }
    const picked = shuffle([...words]).slice(0, 10);
    const questions = picked.map(word => {
      const distractors = shuffle(words.filter(w => w.id !== word.id)).slice(0, 3);
      return { word, options: shuffle([word, ...distractors]) };
    });
    this.questions.set(questions);
  }

  choose(option: VocabularyWord): void {
    if (this.answered()) return;
    const q = this.current();
    if (!q) return;

    const correct = option.id === q.word.id;
    this.answered.set(option);
    if (correct) {
      this.score.update(s => s + 1);
      this.celebrate.correct();
    } else {
      this.celebrate.wrong();
    }
    this.vocab.review(q.word.id, correct).subscribe(res => this.game.handleActivity(res.game));

    setTimeout(() => this.next(), 1100);
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

  optionState(option: VocabularyWord): 'correct' | 'wrong' | '' {
    const chosen = this.answered();
    if (!chosen) return '';
    const q = this.current();
    if (option.id === q?.word.id) return 'correct';
    if (option.id === chosen.id) return 'wrong';
    return '';
  }

  restart(): void {
    this.index.set(0);
    this.score.set(0);
    this.answered.set(null);
    this.finished.set(false);
    this.vocab.getAll().subscribe(words => this.build(readableWords(words, this.readState.ids())));
  }
}
