import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AppComponent } from './app.component';
import { ApiService } from './core/app.service';

describe('AppComponent', () => {
  let fixture: ComponentFixture<AppComponent>;
  let component: AppComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        {
          provide: ApiService,
          useValue: {
            ask: () => of({
              data: [
                { id: 3, name: 'Charlie', revenue: 199 },
                { id: 1, name: 'Alpha', revenue: 109.97 },
              ],
              explain: {
                question: 'top products by revenue',
                rewrittenSql: 'select ...',
                confidence: 0.7,
                notes: [],
                elapsedMs: 123
              }
            })
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AppComponent);
    component = fixture.componentInstance;
  });

  it('loads data into grid after ask', () => {
    component.onAsk('top products by revenue');
    fixture.detectChanges();

    expect(component.rows().length).toBe(2);
    expect(component.columns()).toEqual(['id', 'name', 'revenue']);
    expect(component.explain()).toBeTruthy();
  });
});
