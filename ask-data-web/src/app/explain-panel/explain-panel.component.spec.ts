import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ExplainPanelComponent } from './explain-panel.component';
import { By } from '@angular/platform-browser';

describe('ExplainPanelComponent', () => {
  let fixture: ComponentFixture<ExplainPanelComponent>;
  let component: ExplainPanelComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExplainPanelComponent],  
    }).compileComponents();

    fixture = TestBed.createComponent(ExplainPanelComponent);
    component = fixture.componentInstance;
  });

  it('renders explain info', () => {
    component.explain = {
      question: 'top products by revenue',
      confidence: 0.7,
      elapsedMs: 210,
      rewrittenSql: 'select * from products limit 10',
      notes: ['LIMIT_APPLIED:10'],
      parameters: { limit: 10 }
    };
    fixture.detectChanges();

    const h3 = fixture.debugElement.query(By.css('h3')).nativeElement;
    expect(h3.textContent).toContain('Explain');

    const sql = fixture.debugElement.query(By.css('pre')).nativeElement;
    expect(sql.textContent).toContain('select * from products');

    const notes = fixture.debugElement.queryAll(By.css('li'));
    expect(notes.length).toBe(1);
    expect(notes[0].nativeElement.textContent).toContain('LIMIT_APPLIED');
  });
});
