import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ResultGridComponent } from './result-grid.component';
import { By } from '@angular/platform-browser';

describe('ResultGridComponent', () => {
  let fixture: ComponentFixture<ResultGridComponent>;
  let component: ResultGridComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ResultGridComponent],  
    }).compileComponents();

    fixture = TestBed.createComponent(ResultGridComponent);
    component = fixture.componentInstance;
  });

  it('shows table when rows exist', () => {
    component.columns = ['id', 'name'];
    component.rows = [
      { id: 1, name: 'Alpha' },
      { id: 2, name: 'Bravo' },
    ];
    fixture.detectChanges();

    const table = fixture.debugElement.query(By.css('table'));
    expect(table).toBeTruthy();

    const headers = fixture.debugElement.queryAll(By.css('th'));
    expect(headers.length).toBe(2);
    expect(headers[0].nativeElement.textContent.trim()).toBe('id');
    expect(headers[1].nativeElement.textContent.trim()).toBe('name');

    const bodyRows = fixture.debugElement.queryAll(By.css('tbody tr'));
    expect(bodyRows.length).toBe(2);
  });

  it('shows "No data." when empty', () => {
    component.columns = [];
    component.rows = [];
    fixture.detectChanges();

    const msg = fixture.debugElement.query(By.css('p'));
    expect(msg.nativeElement.textContent.trim()).toBe('No data.');
  });
});
