// src/app/app.component.ts
import { Component, signal } from '@angular/core';
import { NgIf, NgFor } from '@angular/common';   // 👈 add this
import { ApiService } from './core/app.service';

// child components
import { AskInputComponent } from './ask-input/ask-input.component';
import { ResultGridComponent } from './result-grid/result-grid.component';
import { ExplainPanelComponent } from './explain-panel/explain-panel.component';

@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  // 👇 IMPORTANT: add NgIf (and NgFor if you ever ngFor here)
  imports: [
    NgIf,
    NgFor,
    AskInputComponent,
    ResultGridComponent,
    ExplainPanelComponent
  ],
})
export class AppComponent {
  busy = signal(false);
  rows = signal<any[]>([]);
  columns = signal<string[]>([]);
  explain = signal<any | null>(null);
  error = signal<string | null>(null);

  constructor(private api: ApiService) {}

  onAsk(question: string) {
    this.busy.set(true);
    this.error.set(null);

    this.api.ask(question, 10).subscribe({
      next: (res) => {
        const data = res?.data ?? [];
        this.rows.set(data);
        this.columns.set(data.length ? Object.keys(data[0]) : []);
        this.explain.set(res?.explain ?? null);
      },
      error: () => {
        this.error.set('Request failed');
      },
      complete: () => this.busy.set(false),
    });
  }
}
