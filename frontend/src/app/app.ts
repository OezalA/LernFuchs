import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CelebrationService } from './core/celebration.service';
import { GameService } from './core/game.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private celebrate = inject(CelebrationService);
  protected game = inject(GameService);

  muted = signal(this.celebrate.isMuted);

  /** Füllstand des XP-Balkens im aktuellen Level (0–100 %). */
  levelProgress = computed(() => {
    const s = this.game.state();
    if (!s || s.xpForNextLevel === 0) return 0;
    return Math.round((s.xpIntoLevel / s.xpForNextLevel) * 100);
  });

  ngOnInit(): void {
    this.game.reload();
  }

  toggleSound(): void {
    this.muted.set(this.celebrate.toggleMute());
  }
}
