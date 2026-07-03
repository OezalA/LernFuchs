import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LanguageService } from '../../core/language.service';

@Component({
  selector: 'app-games-hub',
  imports: [RouterLink],
  templateUrl: './games-hub.html',
  styleUrl: './games.css'
})
export class GamesHub {
  private lang = inject(LanguageService);

  // "germanOnly": Spiele, die es nur im Deutschen gibt (Artikel der/die/das, Verbkonjugation).
  private readonly allGames = [
    { path: 'quiz', icon: '❓', title: 'Quiz', desc: 'Welches Wort passt zur Erklärung?', germanOnly: false },
    { path: 'artikel', icon: '🏷️', title: 'der/die/das', desc: 'Wähle den richtigen Artikel.', germanOnly: true },
    { path: 'diktat', icon: '✍️', title: 'Diktat', desc: 'Höre das Wort und schreibe es.', germanOnly: false },
    { path: 'memory', icon: '🧠', title: 'Memory', desc: 'Finde Wort und Erklärung als Paar.', germanOnly: false },
    { path: 'verben', icon: '🔤', title: 'Verben', desc: 'Konjugiere die Verben richtig.', germanOnly: true },
  ];

  // Im Englischen die deutsch-spezifischen Spiele ausblenden.
  games = computed(() =>
    this.lang.language() === 'Englisch'
      ? this.allGames.filter(g => !g.germanOnly)
      : this.allGames);
}
