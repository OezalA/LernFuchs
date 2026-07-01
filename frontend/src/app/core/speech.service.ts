import { Injectable } from '@angular/core';

/**
 * Liest deutschen Text laut vor – über die kostenlose Web Speech API des Browsers.
 * Keine Internetverbindung oder API-Schlüssel nötig.
 */
@Injectable({ providedIn: 'root' })
export class SpeechService {
  private get synth(): SpeechSynthesis | null {
    return typeof window !== 'undefined' && 'speechSynthesis' in window
      ? window.speechSynthesis
      : null;
  }

  /** Ob der Browser Sprachausgabe unterstützt. */
  get supported(): boolean {
    return this.synth !== null;
  }

  /** Liest den Text auf Deutsch vor (bricht vorherige Ausgabe ab). */
  speak(text: string): void {
    const synth = this.synth;
    if (!synth || !text) return;

    synth.cancel();
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = 'de-DE';
    utterance.rate = 0.9; // etwas langsamer, kindgerecht

    const germanVoice = synth.getVoices().find(v => v.lang.startsWith('de'));
    if (germanVoice) utterance.voice = germanVoice;

    synth.speak(utterance);
  }

  stop(): void {
    this.synth?.cancel();
  }
}
