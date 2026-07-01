import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ReadingPassage, ReadingPassageSummary, CheckResult,
  GenerateReadingRequest, Difficulty
} from './models';

@Injectable({ providedIn: 'root' })
export class ReadingService {
  private http = inject(HttpClient);
  private base = '/api/reading';

  getAll(topic?: string, difficulty?: Difficulty): Observable<ReadingPassageSummary[]> {
    const params: Record<string, string> = {};
    if (topic) params['topic'] = topic;
    if (difficulty) params['difficulty'] = difficulty;
    return this.http.get<ReadingPassageSummary[]>(this.base, { params });
  }

  getById(id: number): Observable<ReadingPassage> {
    return this.http.get<ReadingPassage>(`${this.base}/${id}`);
  }

  generate(req: GenerateReadingRequest): Observable<{ id: number; title: string }> {
    return this.http.post<{ id: number; title: string }>(`${this.base}/generate`, req);
  }

  check(id: number, answers: { questionId: number; answer: string }[]): Observable<CheckResult> {
    return this.http.post<CheckResult>(`${this.base}/${id}/check`, answers);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
