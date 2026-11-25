import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SelectComponent } from './select.component';

describe('SelectComponent', () => {
  let component: SelectComponent;
  let fixture: ComponentFixture<SelectComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [SelectComponent] }).compileComponents();
    fixture = TestBed.createComponent(SelectComponent);
    component = fixture.componentInstance;
  });

  it('uses default value on init when value is empty', () => {
    fixture.componentRef.setInput('defaultValue', 'wh-1');

    component.ngOnInit();

    expect(component.valueState()).toBe('wh-1');
  });

  it('keeps explicit value instead of overwriting it with default value', () => {
    fixture.componentRef.setInput('value', 'explicit');
    fixture.componentRef.setInput('defaultValue', 'default');

    component.ngOnInit();

    expect(component.valueState()).toBe('explicit');
  });

  it('emits selected value on change', () => {
    const valueChange = vi.fn();
    component.valueChange.subscribe(valueChange);
    const event = { target: { value: 'zone-1' } } as unknown as Event;

    component.onChange(event);

    expect(component.valueState()).toBe('zone-1');
    expect(valueChange).toHaveBeenCalledWith('zone-1');
  });
});
