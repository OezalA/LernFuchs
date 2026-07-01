import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ReadingService } from '../../core/reading.service';
import {
  ReadingPassage, ReadingPassageSummary, CheckResult, Difficulty
} from '../../core/models';

@Component({
  selector: 'app-leseverstaendnis',
  imports: [FormsModule],
  templateUrl: './leseverstaendnis.html',
  styleUrl: './leseverstaendnis.css'
})
export class Leseverstaendnis implements OnInit {
  private reading = inject(ReadingService);

  passages = signal<ReadingPassageSummary[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  // Formular
  topic = signal('');
  difficulty = signal<Difficulty>('Mittel');
  questionCount = signal(4);
  generating = signal(false);

  // Geöffneter Text
  current = signal<ReadingPassage | null>(null);
  loadingText = signal(false);
  answers = signal<Record<number, string>>({});
  result = signal<CheckResult | null>(null);
  checking = signal(false);

  // ngModel-Brücken
  get topicModel() { return this.topic(); }
  set topicModel(v: string) { this.topic.set(v); }
  get difficultyModel() { return this.difficulty(); }
  set difficultyModel(v: Difficulty) { this.difficulty.set(v); }
  get countModel() { return this.questionCount(); }
  set countModel(v: number) { this.questionCount.set(v); }

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

  generate(): void {
    const topic = this.topic().trim();
    if (!topic) { this.error.set('Bitte gib ein Thema ein.'); return; }
    this.generating.set(true);
    this.error.set(null);
    this.reading.generate({
      topic, difficulty: this.difficulty(), questionCount: this.questionCount()
    }).subscribe({
      next: created => {
        this.generating.set(false);
        this.topic.set('');
        this.load();
        this.open(created.id);
      },
      error: () => {
        this.error.set('Beim Erstellen ist etwas schiefgelaufen. Versuche es später noch einmal.');
        this.generating.set(false);
      }
    });
  }

  open(id: number): void {
    this.loadingText.set(true);
    this.result.set(null);
    this.answers.set({});
    this.reading.getById(id).subscribe({
      next: p => { this.current.set(p); this.loadingText.set(false); },
      error: () => { this.error.set('Der Text konnte nicht geöffnet werden.'); this.loadingText.set(false); }
    });
  }

  setAnswer(questionId: number, value: string): void {
    this.answers.update(a => ({ ...a, [questionId]: value }));
  }

  submit(): void {
    const passage = this.current();
    if (!passage) return;
    const payload = passage.questions.map(q => ({
      questionId: q.id,
      answer: this.answers()[q.id] ?? ''
    }));
    this.checking.set(true);
    this.reading.check(passage.id, payload).subscribe({
      next: r => { this.result.set(r); this.checking.set(false); },
      error: () => { this.error.set('Die Antworten konnten nicht geprüft werden.'); this.checking.set(false); }
    });
  }

  feedbackFor(questionId: number) {
    return this.result()?.feedback.find(f => f.questionId === questionId) ?? null;
  }

  backToList(): void {
    this.current.set(null);
    this.result.set(null);
    this.load();
  }

  deletePassage(id: number, event: Event): void {
    event.stopPropagation();
    this.reading.delete(id).subscribe({
      next: () => this.passages.update(list => list.filter(p => p.id !== id))
    });
  }
}
