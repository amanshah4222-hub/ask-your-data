import { Component, signal } from '@angular/core';
import { ApiService } from './core/app.service';
import { AskInputComponent } from './ask-input/ask-input.component';
import { ResultGridComponent } from './result-grid/result-grid.component';
import { ExplainPanelComponent } from './explain-panel/explain-panel.component';

@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  imports: [AskInputComponent, ResultGridComponent, ExplainPanelComponent],
})
export class AppComponent {
  title = 'Ask Data';
  busy = signal(false);
  rows = signal<any[]>([]);
  columns = signal<string[]>([]);
  explain = signal<any | null>(null);
  error = signal<string | null>(null);

  constructor(private api: ApiService) {}

  onAsk(question: string) {
    if (!question.trim()) return;
    this.busy.set(true);
    this.error.set(null);

    this.api.ask(question, 10).subscribe({
      next: (res) => {
        const data = res.data || [];
        this.rows.set(data);
        this.columns.set(data.length ? Object.keys(data[0]) : []);
        this.explain.set(res.explain);
      },
      error: (err) => {
        this.error.set(err?.error?.error ?? 'Something went wrong');
      },
      complete: () => this.busy.set(false)
    });
  }
}
