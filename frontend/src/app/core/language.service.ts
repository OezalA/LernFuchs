import { Injectable, signal } from '@angular/core';
import { Language } from './models';

/**
 * Merkt sich – ohne Login, nur im Browser (localStorage) – die aktuell gewählte
 * Lernsprache (Deutsch oder Englisch als Fremdsprache). Beim Umschalten wird die
 * Seite neu geladen, damit alle Bereiche (Lesen, Wortschatz, Spiele) in der neuen
 * Sprache frisch laden.
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

  /** Wechselt die Sprache und lädt die App neu. */
  set(lang: Language): void {
    if (lang === this._language()) return;
    try {
      localStorage.setItem(this.key, lang);
    } catch {
      // localStorage nicht verfügbar – ignorieren.
    }
    this._language.set(lang);
    location.reload();
  }

  private load(): Language {
    try {
      const raw = localStorage.getItem(this.key);
      return raw === 'Englisch' ? 'Englisch' : 'Deutsch';
    } catch {
      return 'Deutsch';
    }
  }
}
