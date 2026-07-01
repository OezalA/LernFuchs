import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { LowerCasePipe } from '@angular/common';
import { ReadingService } from '../../core/reading.service';
import { SpeechService } from '../../core/speech.service';
import { CelebrationService } from '../../core/celebration.service';
import { GameService } from '../../core/game.service';
import { ReadStateService } from '../../core/read-state.service';
import { categoryFor } from '../../core/category';
import {
  ReadingPassage, ReadingPassageSummary, PassageWord, ComprehensionQuestion, CheckResult
} from '../../core/models';

type View = 'list' | 'reading' | 'quiz' | 'result';
interface TextToken { text: string; word?: PassageWord; }

@Component({
  selector: 'app-leseverstaendnis',
  imports: [LowerCasePipe],
  templateUrl: './leseverstaendnis.html',
  styleUrl: './leseverstaendnis.css'
})
export class Leseverstaendnis implements OnInit {
  private reading = inject(ReadingService);
  private speech = inject(SpeechService);
  private celebrate = inject(CelebrationService);
  private game = inject(GameService);
  private readState = inject(ReadStateService);

  canSpeak = this.speech.supported;

  view = signal<View>('list');
  passages = signal<ReadingPassageSummary[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  // Geöffneter Text
  current = signal<ReadingPassage | null>(null);
  loadingText = signal(false);
  activeWord = signal<PassageWord | null>(null); // angetipptes schwieriges Wort

  // Quiz
  qIndex = signal(0);
  chosen = signal<string | null>(null);
  answers = signal<{ questionId: number; answer: string }[]>([]);
  checkResult = signal<CheckResult | null>(null);

  currentQuestion = computed<ComprehensionQuestion | null>(
    () => this.current()?.questions[this.qIndex()] ?? null);

  readCount = computed(() => this.passages().filter(p => this.readState.isRead(p.id)).length);

  // Ausgewählte Kategorie (null = alle anzeigen).
  selectedCategory = signal<string | null>(null);

  // Nach Kategorie gruppierte Kacheln
  groupedPassages = computed(() => {
    const groups = new Map<string, { icon: string; items: ReadingPassageSummary[] }>();
    for (const p of this.passages()) {
      const cat = categoryFor(p.topic);
      const g = groups.get(cat.name);
      if (g) g.items.push(p); else groups.set(cat.name, { icon: cat.icon, items: [p] });
    }
    return Array.from(groups, ([name, g]) => ({ name, icon: g.icon, items: g.items }))
      .sort((a, b) => a.name.localeCompare(b.name));
  });

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

  // Text in Tokens zerlegt; schwierige Wörter sind markiert.
  textTokens = computed<TextToken[]>(() => {
    const p = this.current();
    if (!p) return [];
    const map = new Map<string, PassageWord>();
    for (const w of p.words) map.set(w.word.toLowerCase(), w);
    return p.text.split(/(\s+)/).map(tok => {
      const core = tok.replace(/^[^A-Za-zÀ-ÿ]+|[^A-Za-zÀ-ÿ]+$/g, '');
      const word = core ? map.get(core.toLowerCase()) : undefined;
      return { text: tok, word };
    });
  });

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
    this.reading.getById(id).subscribe({
      next: p => { this.current.set(p); this.loadingText.set(false); this.view.set('reading'); },
      error: () => { this.error.set('Der Text konnte nicht geöffnet werden.'); this.loadingText.set(false); }
    });
  }

  startQuiz(): void {
    this.speech.stop();
    this.qIndex.set(0);
    this.chosen.set(null);
    this.answers.set([]);
    this.checkResult.set(null);
    this.view.set('quiz');
  }

  backToList(): void {
    this.speech.stop();
    this.current.set(null);
    this.activeWord.set(null);
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
