import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-games-hub',
  imports: [RouterLink],
  templateUrl: './games-hub.html',
  styleUrl: './games.css'
})
export class GamesHub {
  games = [
    { path: 'quiz', icon: '❓', title: 'Quiz', desc: 'Welches Wort passt zur Erklärung?' },
    { path: 'artikel', icon: '🏷️', title: 'der/die/das', desc: 'Wähle den richtigen Artikel.' },
    { path: 'diktat', icon: '✍️', title: 'Diktat', desc: 'Höre das Wort und schreibe es.' },
    { path: 'memory', icon: '🧠', title: 'Memory', desc: 'Finde Wort und Erklärung als Paar.' },
  ];
}
