import { Component, inject, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CelebrationService } from './core/celebration.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private celebrate = inject(CelebrationService);
  muted = signal(this.celebrate.isMuted);

  toggleSound(): void {
    this.muted.set(this.celebrate.toggleMute());
  }
}
