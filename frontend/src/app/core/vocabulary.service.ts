import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  VocabularyWord, VocabularyProgress, GenerateVocabularyRequest, Difficulty, GameActivityResult
} from './models';
import { LanguageService } from './language.service';

@Injectable({ providedIn: 'root' })
export class VocabularyService {
  private http = inject(HttpClient);
  private lang = inject(LanguageService);
  private base = '/api/vocabulary';

  getAll(topic?: string, difficulty?: Difficulty): Observable<VocabularyWord[]> {
    const params: Record<string, string> = { language: this.lang.current() };
    if (topic) params['topic'] = topic;
    if (difficulty) params['difficulty'] = difficulty;
    return this.http.get<VocabularyWord[]>(this.base, { params });
  }

  getDue(limit = 20): Observable<VocabularyWord[]> {
    return this.http.get<VocabularyWord[]>(`${this.base}/due`,
      { params: { limit, language: this.lang.current() } });
  }

  generate(req: GenerateVocabularyRequest): Observable<VocabularyWord[]> {
    return this.http.post<VocabularyWord[]>(`${this.base}/generate`, req);
  }

  review(id: number, correct: boolean): Observable<{ progress: VocabularyProgress; game: GameActivityResult }> {
    return this.http.post<{ progress: VocabularyProgress; game: GameActivityResult }>(
      `${this.base}/${id}/review`, { correct });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
