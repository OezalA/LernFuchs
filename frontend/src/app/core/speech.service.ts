import { Injectable, inject } from '@angular/core';
import { LanguageService } from './language.service';

/**
 * Liest Text laut vor – über die kostenlose Web Speech API des Browsers.
 * Die Sprache (deutsch/englisch/spanisch) richtet sich nach der gewählten Lernsprache,
 * damit englische Inhalte auch englisch ausgesprochen werden.
 * Keine Internetverbindung oder API-Schlüssel nötig.
 */
@Injectable({ providedIn: 'root' })
export class SpeechService {
  private lang = inject(LanguageService);

  private get synth(): SpeechSynthesis | null {
    return typeof window !== 'undefined' && 'speechSynthesis' in window
      ? window.speechSynthesis
      : null;
  }

  /** Ob der Browser Sprachausgabe unterstützt. */
  get supported(): boolean {
    return this.synth !== null;
  }

  /** Liest den Text vor – in der aktuellen Lernsprache (bricht vorherige Ausgabe ab). */
  speak(text: string): void {
    const synth = this.synth;
    if (!synth || !text) return;

    const current = this.lang.current();
    const { code, tag } =
    current === 'Englisch'     ? { code: 'en', tag: 'en-US' } :
    current === 'Spanisch'     ? { code: 'es', tag: 'es-ES' } :
    current === 'Franzoesisch' ? { code: 'fr', tag: 'fr-FR' } :
                                 { code: 'de', tag: 'de-DE' };

    synth.cancel();
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = tag;
    utterance.rate = 0.9; // etwas langsamer, kindgerecht

    const voice = synth.getVoices().find(v => v.lang.startsWith(code));
    if (voice) utterance.voice = voice;


    synth.speak(utterance);
  }

  stop(): void {
    this.synth?.cancel();
  }
}
