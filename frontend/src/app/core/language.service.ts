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

  /** Ob der Nutzer die Lernsprache schon einmal bewusst gewählt hat. */
  hasChosen(): boolean {
    try {
      const v = localStorage.getItem(this.key);
      return v === 'Deutsch' || v === 'Englisch';
    } catch {
      return false;
    }
  }

  /**
   * Setzt die Lernsprache. Beim Wechsel während der Nutzung wird die App neu
   * geladen (<paramref name="reload"/>=true), bei der Erstauswahl nicht nötig.
   */
  set(lang: Language, reload = true): void {
    const changed = lang !== this._language();
    try {
      localStorage.setItem(this.key, lang);
    } catch {
      // localStorage nicht verfügbar – ignorieren.
    }
    this._language.set(lang);
    if (reload && changed) location.reload();
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
