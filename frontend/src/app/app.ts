import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CelebrationService } from './core/celebration.service';
import { GameService } from './core/game.service';
import { ConfigService } from './core/config.service';
import { LanguageService } from './core/language.service';
import { Language } from './core/models';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private celebrate = inject(CelebrationService);
  protected game = inject(GameService);
  private config = inject(ConfigService);
  private langService = inject(LanguageService);

  muted = signal(this.celebrate.isMuted);
  language = this.langService.language;

  // Erstauswahl der Lernsprache (Onboarding), solange noch nichts gewählt wurde.
  needsChoice = signal(!this.langService.hasChosen());
  // Kleines Sprachmenü im Kopfbereich.
  langMenuOpen = signal(false);

  /** Füllstand des XP-Balkens im aktuellen Level (0–100 %). */
  levelProgress = computed(() => {
    const s = this.game.state();
    if (!s || s.xpForNextLevel === 0) return 0;
    return Math.round((s.xpIntoLevel / s.xpForNextLevel) * 100);
  });

  ngOnInit(): void {
    this.game.reload();
    this.config.load();
  }

  toggleSound(): void {
    this.muted.set(this.celebrate.toggleMute());
  }

  /** Erstauswahl im Onboarding (kein Neuladen nötig, App startet dann normal). */
  choose(lang: Language): void {
    this.langService.set(lang, false);
    this.needsChoice.set(false);
  }

  /** Sprachwechsel während der Nutzung (lädt neu, damit alle Bereiche umschalten). */
  switchLanguage(lang: Language): void {
    this.langMenuOpen.set(false);
    this.langService.set(lang, true);
  }
}
