import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RadioComponent } from './radio.component';

describe('RadioComponent', () => {
  let component: RadioComponent;
  let fixture: ComponentFixture<RadioComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [RadioComponent] }).compileComponents();
    fixture = TestBed.createComponent(RadioComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('id', 'radio-id');
    fixture.componentRef.setInput('name', 'radio-name');
    fixture.componentRef.setInput('label', 'Radio');
  });

  it('emits selected value when enabled', () => {
    const valueChange = vi.fn();
    fixture.componentRef.setInput('value', 'PZ');
    fixture.componentRef.setInput('disabled', false);
    component.valueChange.subscribe(valueChange);

    component.onChange();

    expect(valueChange).toHaveBeenCalledWith('PZ');
  });

  it('does not emit when disabled', () => {
    const valueChange = vi.fn();
    fixture.componentRef.setInput('value', 'WZ');
    fixture.componentRef.setInput('disabled', true);
    component.valueChange.subscribe(valueChange);

    component.onChange();

    expect(valueChange).not.toHaveBeenCalled();
  });
});
