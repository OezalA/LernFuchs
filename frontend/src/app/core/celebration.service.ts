import { Injectable } from '@angular/core';
import confetti from 'canvas-confetti';

/**
 * Kleine Feier-Effekte: Konfetti und kindgerechte Klänge.
 * Klänge werden per Web Audio API erzeugt – keine Audiodateien nötig.
 */
@Injectable({ providedIn: 'root' })
export class CelebrationService {
  private audioCtx: AudioContext | null = null;
  private muted = false;

  get isMuted() { return this.muted; }
  toggleMute() { this.muted = !this.muted; return this.muted; }

  /** Kurzes Konfetti (z. B. bei einer richtigen Antwort). */
  confettiSmall(): void {
    confetti({ particleCount: 40, spread: 55, origin: { y: 0.7 }, scalar: 0.8 });
  }

  /** Großes Konfetti (z. B. am Ende einer Übung). */
  confettiBig(): void {
    const end = Date.now() + 700;
    const colors = ['#f57c1f', '#3fa34d', '#2f9bd8', '#ffcf3f', '#e5484d'];
    const frame = () => {
      confetti({ particleCount: 6, angle: 60, spread: 60, origin: { x: 0 }, colors });
      confetti({ particleCount: 6, angle: 120, spread: 60, origin: { x: 1 }, colors });
      if (Date.now() < end) requestAnimationFrame(frame);
    };
    frame();
  }

  /** Freundlicher Klang + Konfetti bei richtiger Antwort. */
  correct(): void {
    this.confettiSmall();
    this.playTones([660, 880], 0.12);
  }

  /** Sanfter Hinweiston bei falscher Antwort (nicht bestrafend). */
  wrong(): void {
    this.playTones([300, 240], 0.14, 'triangle');
  }

  /** Fanfare bei Levelaufstieg / neuem Abzeichen. */
  fanfare(): void {
    this.confettiBig();
    this.playTones([523, 659, 784, 1047], 0.13);
  }

  // --- Tonerzeugung ---
  private playTones(freqs: number[], noteLength: number, type: OscillatorType = 'sine'): void {
    if (this.muted || typeof window === 'undefined' || !('AudioContext' in window)) return;
    try {
      this.audioCtx ??= new AudioContext();
      const ctx = this.audioCtx;
      let start = ctx.currentTime;
      for (const freq of freqs) {
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.type = type;
        osc.frequency.value = freq;
        gain.gain.setValueAtTime(0.0001, start);
        gain.gain.exponentialRampToValueAtTime(0.2, start + 0.01);
        gain.gain.exponentialRampToValueAtTime(0.0001, start + noteLength);
        osc.connect(gain).connect(ctx.destination);
        osc.start(start);
        osc.stop(start + noteLength);
        start += noteLength;
      }
    } catch {
      // Audio ist optional – Fehler ignorieren.
    }
  }
}
