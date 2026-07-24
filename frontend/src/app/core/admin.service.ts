import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface AdminPassage {
  id: number; title: string; text: string; language: string; difficulty: string;
  topic: string | null; wordCount: number; createdAt: string; questionCount: number;
}
export interface AdminWord {
  id: number; word: string; definitionGerman: string; exampleSentence: string | null;
  wordType: string; language: string; difficulty: string; topic: string | null; sourcePassageId: number | null;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);
  private base = '/api/admin';

  passages(language: string) {
    return this.http.get<AdminPassage[]>(`${this.base}/passages`, { params: { language } });
  }
  words(language: string) {
    return this.http.get<AdminWord[]>(`${this.base}/words`, { params: { language } });
  }
  deletePassage(id: number) { return this.http.delete<void>(`${this.base}/passages/${id}`); }
  deleteWord(id: number) { return this.http.delete<void>(`${this.base}/words/${id}`); }
  deleteLanguage(language: string) {
    return this.http.delete<{ deletedPassages: number; deletedWords: number }>(`${this.base}/language/${language}`);
  }
  generate(body: { topic: string; language: string; difficulty?: string; questionCount?: number }) {
    return this.http.post<{ id: number; title: string; addedWords: number }>(`${this.base}/generate`, body);
  }
  updatePassage(id: number, body: { title?: string; text?: string; difficulty?: string }) {
    return this.http.put<{ id: number; title: string; wordCount: number; difficulty: string }>(`${this.base}/passages/${id}`, body);
  }
  updateWord(id: number, body: { word?: string; definitionGerman?: string; exampleSentence?: string }) {
    return this.http.put<{ id: number; word: string; definitionGerman: string; exampleSentence: string | null }>(`${this.base}/words/${id}`, body);
  }
}
