import { Component, Input } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';

@Component({
  selector: 'app-result-grid',
  standalone: true,
  imports: [NgFor, NgIf],
  template: `
  <div class="grid-shell">
    <table *ngIf="rows?.length">
      <thead>
        <tr>
          <th *ngFor="let c of columns">{{ c }}</th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let r of rows">
          <td *ngFor="let c of columns">{{ r[c] }}</td>
        </tr>
      </tbody>
    </table>
    <p *ngIf="!rows?.length">No data.</p>
  </div>
  `,
  styles: [`
    .grid-shell { margin-top:1rem; }
    table { width:100%; border-collapse: collapse; }
    th, td { border:1px solid #ddd; padding:.4rem .6rem; }
    th { background:#f3f4f6; text-align:left; }
  `]
})
export class ResultGridComponent {
  @Input() columns: string[] = [];
  @Input() rows: any[] = [];
}
