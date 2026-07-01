import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StatsService } from '../../core/stats.service';
import { GameService } from '../../core/game.service';
import { LearningStats } from '../../core/models';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home implements OnInit {
  private statsService = inject(StatsService);
  protected game = inject(GameService);

  stats = signal<LearningStats | null>(null);

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
    this.game.reload();
  }
}
