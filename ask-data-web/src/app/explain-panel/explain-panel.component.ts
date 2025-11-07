import { Component, Input } from '@angular/core';
import { NgIf, NgFor, DecimalPipe, JsonPipe} from '@angular/common';

@Component({
  selector: 'app-explain-panel',
  standalone: true,
  imports: [NgIf, NgFor, DecimalPipe, JsonPipe],
  template: `
    <div class="explain">
      <h3>Explain</h3>
      <p><strong>Question:</strong> {{ explain?.question }}</p>
      <p><strong>Confidence:</strong> {{ explain?.confidence | number:'1.0-2' }}</p>
      <p><strong>Elapsed:</strong> {{ explain?.elapsedMs }} ms</p>

      <div class="sql" *ngIf="explain?.rewrittenSql">
        <h4>Rewritten SQL</h4>
        <pre>{{ explain?.rewrittenSql }}</pre>
      </div>

      <div *ngIf="explain?.notes?.length">
        <h4>Notes</h4>
        <ul>
          <li *ngFor="let n of explain.notes">{{ n }}</li>
        </ul>
      </div>

      <div *ngIf="explain?.parameters">
        <h4>Parameters</h4>
        <pre>{{ explain?.parameters | json }}</pre>
      </div>
    </div>
  `,
  styles: [`
    .explain { background:#fff; border:1px solid #e5e7eb; border-radius:.5rem; padding:1rem; }
    .sql pre { background:#0f172a; color:#e2e8f0; padding:.5rem; border-radius:.3rem; text-wrap: auto }
  `]
})
export class ExplainPanelComponent {
  @Input() explain: any;
}
