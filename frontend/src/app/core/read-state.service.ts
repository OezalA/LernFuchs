import { Injectable, computed, signal } from '@angular/core';

/**
 * Merkt sich – ohne Login, nur im Browser (localStorage) – welche Texte gelesen
 * wurden. Nur Wörter und Spiele aus gelesenen Texten werden angezeigt.
 */
@Injectable({ providedIn: 'root' })
export class ReadStateService {
  private readonly key = 'lernfuchs.readPassages';
  private _ids = signal<Set<number>>(this.load());

  /** IDs der gelesenen Texte. */
  ids = this._ids.asReadonly();
  count = computed(() => this._ids().size);

  isRead(id: number): boolean {
    return this._ids().has(id);
  }

  markRead(id: number): void {
    if (this._ids().has(id)) return;
    const next = new Set(this._ids());
    next.add(id);
    this._ids.set(next);
    this.persist();
  }

  private load(): Set<number> {
    try {
      const raw = localStorage.getItem(this.key);
      return new Set<number>(raw ? JSON.parse(raw) : []);
    } catch {
      return new Set<number>();
    }
  }

  private persist(): void {
    try {
      localStorage.setItem(this.key, JSON.stringify([...this._ids()]));
    } catch {
      // localStorage nicht verfügbar – ignorieren.
    }
  }
}
