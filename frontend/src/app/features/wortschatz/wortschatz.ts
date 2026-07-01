import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { VocabularyService } from '../../core/vocabulary.service';
import { SpeechService } from '../../core/speech.service';
import { CelebrationService } from '../../core/celebration.service';
import { GameService } from '../../core/game.service';
import { ConfigService } from '../../core/config.service';
import { ReadStateService } from '../../core/read-state.service';
import { topicIcon } from '../../core/topic-icon';
import { VocabularyWord, Difficulty } from '../../core/models';

@Component({
  selector: 'app-wortschatz',
  imports: [FormsModule, RouterLink],
  templateUrl: './wortschatz.html',
  styleUrl: './wortschatz.css'
})
export class Wortschatz implements OnInit {
  private vocab = inject(VocabularyService);
  private speech = inject(SpeechService);
  private celebrate = inject(CelebrationService);
  private game = inject(GameService);
  protected config = inject(ConfigService);
  private readState = inject(ReadStateService);

  // Datenbestand
  words = signal<VocabularyWord[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  // Themenfilter
  selectedTopic = signal<string>('');
  canSpeak = this.speech.supported;

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

  // Nur Wörter aus gelesenen Texten (oder eigenständig erstellte Wörter ohne Quelltext).
  availableWords = computed(() => {
    const read = this.readState.ids();
    return this.words().filter(w => w.sourcePassageId == null || read.has(w.sourcePassageId));
  });

  // Verfügbare Themen (aus den freigeschalteten Wörtern)
  topics = computed(() => {
    const set = new Set<string>();
    for (const w of this.availableWords()) if (w.topic) set.add(w.topic);
    return Array.from(set).sort();
  });

  // Gefilterte Wortliste nach ausgewähltem Thema
  filteredWords = computed(() => {
    const t = this.selectedTopic();
    const words = this.availableWords();
    return t ? words.filter(w => w.topic === t) : words;
  });

  // Nach Thema gruppiert (aufklappbar).
  collapsedGroups = signal<Set<string>>(new Set());
  groupedWords = computed(() => {
    const groups = new Map<string, VocabularyWord[]>();
    for (const w of this.availableWords()) {
      const key = w.topic || 'Sonstiges';
      const list = groups.get(key);
      if (list) list.push(w); else groups.set(key, [w]);
    }
    return Array.from(groups, ([topic, words]) => ({ topic, icon: topicIcon(topic), words }))
      .sort((a, b) => a.topic.localeCompare(b.topic));
  });

  toggleGroup(topic: string): void {
    const next = new Set(this.collapsedGroups());
    if (next.has(topic)) next.delete(topic); else next.add(topic);
    this.collapsedGroups.set(next);
  }
  isCollapsed(topic: string): boolean {
    return this.collapsedGroups().has(topic);
  }

  get topicModel2() { return this.selectedTopic(); }
  set topicModel2(v: string) { this.selectedTopic.set(v); }

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

  /** Übt alle (gefilterten) Wörter. */
  startPractice(): void {
    this.beginPractice([...this.filteredWords()]);
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
    // Letzte Karte geschafft -> große Feier.
    if (this.cardIndex() >= this.deck().length) this.celebrate.confettiBig();
  }

  stopPractice(): void {
    this.practicing.set(false);
    this.load(); // aktualisierte Fortschritte holen
  }

  articleLabel(w: VocabularyWord): string {
    return w.article && w.article !== 'None' ? w.article.toLowerCase() : '';
  }
}
