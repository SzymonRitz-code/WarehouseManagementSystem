import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RadioSmComponent } from './radio-sm.component';

describe('RadioSmComponent', () => {
  let component: RadioSmComponent;
  let fixture: ComponentFixture<RadioSmComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [RadioSmComponent] }).compileComponents();
    fixture = TestBed.createComponent(RadioSmComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('id', 'radio-id');
    fixture.componentRef.setInput('name', 'radio-name');
    fixture.componentRef.setInput('label', 'Radio');
  });

  it('emits selected value on change', () => {
    const valueChange = vi.fn();
    fixture.componentRef.setInput('value', 'active');
    component.valueChange.subscribe(valueChange);

    component.onChange();

    expect(valueChange).toHaveBeenCalledWith('active');
  });
});
