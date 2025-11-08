import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AskInputComponent } from './ask-input.component';

describe('AskInputComponent', () => {
  let fixture: ComponentFixture<AskInputComponent>;
  let component: AskInputComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AskInputComponent], 
    }).compileComponents();

    fixture = TestBed.createComponent(AskInputComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('emits question on submit', () => {
    const emitted: string[] = [];
    component.ask.subscribe(v => emitted.push(v));

    component.q = 'top products by revenue';
    component.submit();

    expect(emitted.length).toBe(1);
    expect(emitted[0]).toBe('top products by revenue');
  });

  it('does not emit when busy', () => {
    const emitted: string[] = [];
    component.ask.subscribe(v => emitted.push(v));

    component.busy = true;
    component.q = 'should not emit';
    component.submit();

    expect(emitted.length).toBe(0);
  });
});
