import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LearningStats } from './models';

@Injectable({ providedIn: 'root' })
export class StatsService {
  private http = inject(HttpClient);

  get(): Observable<LearningStats> {
    return this.http.get<LearningStats>('/api/stats');
  }
}
