import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { GameState, GameActivityResult, AchievementView } from './models';
import { CelebrationService } from './celebration.service';

/**
 * Hält den Spielstand (XP, Level, Serie, Abzeichen) im Frontend
 * und reagiert auf Aktivitäts-Ergebnisse (Levelaufstieg, neue Abzeichen).
 */
@Injectable({ providedIn: 'root' })
export class GameService {
  private http = inject(HttpClient);
  private celebrate = inject(CelebrationService);

  /** Aktueller Spielstand (null solange nicht geladen). */
  state = signal<GameState | null>(null);

  /** Zuletzt neu freigeschaltetes Abzeichen – für eine kurze Einblendung. */
  latestAchievement = signal<AchievementView | null>(null);

  reload(): void {
    this.http.get<GameState>('/api/game').subscribe(s => this.state.set(s));
  }

  /** Verarbeitet das Spiel-Ergebnis einer Aktivität (aus Review/Check). */
  handleActivity(result: GameActivityResult | undefined | null): void {
    if (!result) return;
    this.reload();

    if (result.newAchievements?.length) {
      const badge = result.newAchievements[0];
      this.latestAchievement.set(badge);
      this.celebrate.fanfare();
      setTimeout(() => this.latestAchievement.set(null), 5000);
    } else if (result.leveledUp) {
      this.celebrate.fanfare();
    }
  }
}
