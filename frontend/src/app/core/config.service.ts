import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

/** Lädt öffentliche Feature-Flags vom Backend. */
@Injectable({ providedIn: 'root' })
export class ConfigService {
  private http = inject(HttpClient);

  /** Dürfen Nutzer eigene Inhalte erzeugen? (In der öffentlichen Version aus.) */
  userGenerationEnabled = signal(true);

  load(): void {
    this.http.get<{ userGenerationEnabled: boolean }>('/api/config').subscribe({
      next: c => this.userGenerationEnabled.set(c.userGenerationEnabled),
      error: () => this.userGenerationEnabled.set(true)
    });
  }
}
