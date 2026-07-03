import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StatsService } from '../../core/stats.service';
import { GameService } from '../../core/game.service';
import { LanguageService } from '../../core/language.service';
import { LearningStats, ProgressData, Language } from '../../core/models';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home implements OnInit {
  private statsService = inject(StatsService);
  protected game = inject(GameService);
  private langService = inject(LanguageService);

  language = this.langService.language;

  chooseLanguage(lang: Language): void {
    this.langService.set(lang);
  }

  stats = signal<LearningStats | null>(null);
  progress = signal<ProgressData | null>(null);

  readonly boxLabels = ['Neu', 'Box 1', 'Box 2', 'Box 3', 'Box 4', 'Gelernt'];

  /** Höchster Wert der Boxverteilung (für die Balkenhöhe). */
  maxBox = computed(() => Math.max(1, ...(this.progress()?.boxes ?? [0])));
  /** Höchste Tages-XP der Woche (für die Balkenhöhe). */
  maxXp = computed(() => Math.max(1, ...(this.progress()?.last7Days.map(d => d.xp) ?? [0])));

  barHeight(value: number, max: number): number {
    return Math.round((value / max) * 100);
  }

  /** Tagesziel-Fortschritt in Prozent (0–100). */
  goalPercent = computed(() => {
    const s = this.game.state();
    if (!s || s.dailyGoal === 0) return 0;
    return Math.min(100, Math.round((s.reviewsToday / s.dailyGoal) * 100));
  });

  /** Für den SVG-Kreis: Umfang minus gefüllter Anteil. */
  goalDashOffset = computed(() => {
    const circumference = 2 * Math.PI * 52;
    return circumference * (1 - this.goalPercent() / 100);
  });
  readonly goalCircumference = 2 * Math.PI * 52;

  ngOnInit(): void {
    this.statsService.get().subscribe({
      next: s => this.stats.set(s),
      error: () => this.stats.set(null)
    });
    this.statsService.getProgress().subscribe({
      next: p => this.progress.set(p),
      error: () => this.progress.set(null)
    });
    this.game.reload();
  }
}
