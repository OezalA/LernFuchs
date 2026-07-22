import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth.service';
import { AdminService, AdminPassage, AdminWord } from '../../core/admin.service';

@Component({
  selector: 'app-admin',
  imports: [FormsModule],
  templateUrl: './admin.html',
  styleUrl: './admin.css'
})
export class Admin {
  private auth = inject(AuthService);
  private api = inject(AdminService);

  account = this.auth.account;
  get isAdmin() { return this.auth.isAdmin; }
  get userName() { return this.auth.userName; }

  readonly languages = ['Deutsch', 'Englisch', 'Spanisch', 'Franzoesisch'];
  language = signal('Englisch');
  passages = signal<AdminPassage[]>([]);
  words = signal<AdminWord[]>([]);
  loading = signal(false);
  busy = signal(false);
  message = signal<string | null>(null);

  topic = signal('');

  constructor() {
    // Falls beim Laden schon angemeldet (aus dem Cache): Inhalte automatisch holen.
    let loaded = false;
    effect(() => {
      if (this.account() && this.isAdmin && !loaded) {
        loaded = true;
        queueMicrotask(() => this.load());
      }
    });
  }

  langLabel(l: string): string { return l === 'Franzoesisch' ? 'Französisch' : l; }

  /** Diagnose: welche Rollen stehen im Token? */
  get tokenRoles(): string {
    const claims = this.account()?.idTokenClaims as Record<string, unknown> | undefined;
    const roles = claims?.['roles'];
    return roles ? JSON.stringify(roles) : '— (keine roles im Token)';
  }

  async login(): Promise<void> {
    try { await this.auth.login(); this.load(); }
    catch { this.message.set('Anmeldung fehlgeschlagen.'); }
  }
  async logout(): Promise<void> { await this.auth.logout(); }

  selectLanguage(l: string): void { this.language.set(l); this.message.set(null); this.load(); }

  load(): void {
    if (!this.isAdmin) return;
    this.loading.set(true);
    const lng = this.language();
    this.api.passages(lng).subscribe({
      next: p => this.passages.set(p),
      error: () => this.message.set('Fehler beim Laden der Texte.')
    });
    this.api.words(lng).subscribe({
      next: w => { this.words.set(w); this.loading.set(false); },
      error: () => { this.message.set('Fehler beim Laden der Wörter.'); this.loading.set(false); }
    });
  }

  deletePassage(p: AdminPassage): void {
    if (!confirm(`Text "${p.title}" löschen?`)) return;
    this.api.deletePassage(p.id).subscribe(() =>
      this.passages.update(list => list.filter(x => x.id !== p.id)));
  }

  deleteWord(w: AdminWord): void {
    this.api.deleteWord(w.id).subscribe(() =>
      this.words.update(list => list.filter(x => x.id !== w.id)));
  }

  deleteAll(): void {
    const lng = this.language();
    if (!confirm(`ALLE ${this.langLabel(lng)}-Inhalte (Texte + Wörter) löschen?`)) return;
    this.busy.set(true);
    this.api.deleteLanguage(lng).subscribe({
      next: r => { this.message.set(`Gelöscht: ${r.deletedPassages} Texte, ${r.deletedWords} Wörter.`); this.busy.set(false); this.load(); },
      error: () => { this.message.set('Löschen fehlgeschlagen.'); this.busy.set(false); }
    });
  }

  generate(): void {
    const topic = this.topic().trim();
    if (!topic) { this.message.set('Bitte ein Thema eingeben.'); return; }
    this.busy.set(true);
    this.api.generate({ topic, language: this.language(), difficulty: 'Leicht', questionCount: 4 }).subscribe({
      next: r => { this.message.set(`Erzeugt: „${r.title}" (+${r.addedWords} Wörter).`); this.topic.set(''); this.busy.set(false); this.load(); },
      error: () => { this.message.set('Erzeugen fehlgeschlagen (evtl. Gemini-Limit).'); this.busy.set(false); }
    });
  }
}
