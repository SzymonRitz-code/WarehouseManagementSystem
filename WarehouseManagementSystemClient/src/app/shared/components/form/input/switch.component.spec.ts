import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SwitchComponent } from './switch.component';

describe('SwitchComponent', () => {
  let component: SwitchComponent;
  let fixture: ComponentFixture<SwitchComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [SwitchComponent] }).compileComponents();
    fixture = TestBed.createComponent(SwitchComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('label', 'Toggle');
  });

  it('starts from defaultChecked value', () => {
    fixture.componentRef.setInput('defaultChecked', true);

    component.ngOnInit();

    expect(component.isChecked).toBe(true);
  });

  it('toggles and emits the new value when enabled', () => {
    const valueChange = vi.fn();
    component.valueChange.subscribe(valueChange);

    component.handleToggle();

    expect(component.isChecked).toBe(true);
    expect(valueChange).toHaveBeenCalledWith(true);
  });

  it('does not toggle when disabled', () => {
    const valueChange = vi.fn();
    fixture.componentRef.setInput('disabled', true);
    component.valueChange.subscribe(valueChange);

    component.handleToggle();

    expect(component.isChecked).toBe(false);
    expect(valueChange).not.toHaveBeenCalled();
  });

  it('uses gray palette when configured', () => {
    fixture.componentRef.setInput('color', 'gray');
    component.isChecked = true;

    expect(component.switchColors.background).toContain('bg-gray-800');
  });
});
