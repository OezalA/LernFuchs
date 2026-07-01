import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  VocabularyWord, VocabularyProgress, GenerateVocabularyRequest, Difficulty
} from './models';

@Injectable({ providedIn: 'root' })
export class VocabularyService {
  private http = inject(HttpClient);
  private base = '/api/vocabulary';

  getAll(topic?: string, difficulty?: Difficulty): Observable<VocabularyWord[]> {
    const params: Record<string, string> = {};
    if (topic) params['topic'] = topic;
    if (difficulty) params['difficulty'] = difficulty;
    return this.http.get<VocabularyWord[]>(this.base, { params });
  }

  getDue(limit = 20): Observable<VocabularyWord[]> {
    return this.http.get<VocabularyWord[]>(`${this.base}/due`, { params: { limit } });
  }

  generate(req: GenerateVocabularyRequest): Observable<VocabularyWord[]> {
    return this.http.post<VocabularyWord[]>(`${this.base}/generate`, req);
  }

  review(id: number, correct: boolean): Observable<VocabularyProgress> {
    return this.http.post<VocabularyProgress>(`${this.base}/${id}/review`, { correct });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
