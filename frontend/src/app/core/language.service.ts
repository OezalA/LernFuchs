import { Injectable, signal } from '@angular/core';
import { Language } from './models';

/**
 * Merkt sich – ohne Login, nur im Browser (localStorage) – die aktuell gewählte
 * Lernsprache. Beim Wechsel wird die App an der Startseite neu geladen, damit alle
 * Bereiche (Lesen, Wortschatz, Spiele, Dashboard) frisch in der neuen Sprache starten.
 */
@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly key = 'lernfuchs.language';
  private _language = signal<Language>(this.load());

  /** Aktuell gewählte Sprache (reaktiv). */
  language = this._language.asReadonly();

  /** Aktueller Wert (nicht reaktiv), z. B. für HTTP-Parameter. */
  current(): Language {
    return this._language();
  }

  /** Wechselt die Lernsprache und startet auf der Startseite neu. */
  set(lang: Language): void {
    if (lang === this._language()) return;
    try {
      localStorage.setItem(this.key, lang);
    } catch {
      // localStorage nicht verfügbar – ignorieren.
    }
    this._language.set(lang);
    // Zur Startseite wechseln und neu laden (nicht an Ort und Stelle umschalten).
    window.location.assign('/');
  }

  private load(): Language {
    try {
      const raw = localStorage.getItem(this.key);
      return raw === 'Englisch' || raw === 'Spanisch' || raw === 'Franzoesisch' ? raw : 'Deutsch';
    } catch {
      return 'Deutsch';
    }
  }
}
