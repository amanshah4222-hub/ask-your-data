import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-ask-input',
  standalone: true,
  imports: [FormsModule],
  template: `
  <div class="ask-input">
    <input
      [(ngModel)]="q"
      (keyup.enter)="submit()"
      placeholder="e.g. top products by revenue"
    />
    <button (click)="submit()" [disabled]="busy">Ask</button>
  </div>
  `,
  styles: [`
    .ask-input { display:flex; gap:.5rem; }
    input { flex:1; padding:.4rem .6rem; }
    button { padding:.4rem .8rem; }
  `]
})
export class AskInputComponent {
  @Input() busy = false;
  @Output() ask = new EventEmitter<string>();
  q = 'top products by revenue';

  submit() {
    if (!this.q.trim() || this.busy) return;
    this.ask.emit(this.q.trim());
  }
}
