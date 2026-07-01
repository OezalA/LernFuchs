import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LearningStats, ProgressData } from './models';

@Injectable({ providedIn: 'root' })
export class StatsService {
  private http = inject(HttpClient);

  get(): Observable<LearningStats> {
    return this.http.get<LearningStats>('/api/stats');
  }

  getProgress(): Observable<ProgressData> {
    return this.http.get<ProgressData>('/api/stats/progress');
  }
}
