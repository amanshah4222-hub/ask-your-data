import { Component, Input } from '@angular/core';
import { NgIf, NgFor } from '@angular/common';

@Component({
  selector: 'app-result-grid',
  standalone: true,
  imports: [NgIf, NgFor],
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
    .grid-shell { background:#fff; border:1px solid #e5e7eb; border-radius:.5rem; overflow:auto; }
    table { width:100%; border-collapse: collapse; }
    th, td { border-bottom:1px solid #e5e7eb; padding:.4rem .6rem; }
    th { background:#f3f4f6; text-align:left; }
  `]
})
export class ResultGridComponent {
  @Input() columns: string[] = [];
  @Input() rows: any[] = [];
}
