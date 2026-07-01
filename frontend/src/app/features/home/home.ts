import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StatsService } from '../../core/stats.service';
import { LearningStats } from '../../core/models';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home implements OnInit {
  private statsService = inject(StatsService);

  stats = signal<LearningStats | null>(null);

  ngOnInit(): void {
    this.statsService.get().subscribe({
      next: s => this.stats.set(s),
      error: () => this.stats.set(null)
    });
  }
}
