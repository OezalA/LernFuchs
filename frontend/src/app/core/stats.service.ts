import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LearningStats, ProgressData } from './models';
import { LanguageService } from './language.service';

@Injectable({ providedIn: 'root' })
export class StatsService {
  private http = inject(HttpClient);
  private lang = inject(LanguageService);

  get(): Observable<LearningStats> {
    return this.http.get<LearningStats>('/api/stats', { params: { language: this.lang.current() } });
  }

  getProgress(): Observable<ProgressData> {
    return this.http.get<ProgressData>('/api/stats/progress', { params: { language: this.lang.current() } });
  }
}
