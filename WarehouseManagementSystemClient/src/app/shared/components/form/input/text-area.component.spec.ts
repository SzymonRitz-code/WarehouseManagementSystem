import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';
import { TextAreaComponent } from './text-area.component';

describe('TextAreaComponent', () => {
  let component: TextAreaComponent;
  let fixture: ComponentFixture<TextAreaComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [TextAreaComponent] }).compileComponents();
    fixture = TestBed.createComponent(TextAreaComponent);
    component = fixture.componentInstance;
  });

  it('emits value changes and touches the control on input', () => {
    const onChange = vi.fn();
    const onTouched = vi.fn();
    const valueChange = vi.fn();
    component.registerOnChange(onChange);
    component.registerOnTouched(onTouched);
    component.valueChange.subscribe(valueChange);
    const event = { target: { value: 'new note' } } as unknown as Event;

    component.onInput(event);

    expect(component.valueState()).toBe('new note');
    expect(onChange).toHaveBeenCalledWith('new note');
    expect(onTouched).toHaveBeenCalledTimes(1);
    expect(valueChange).toHaveBeenCalledWith('new note');
  });

  it('accepts plain values and value wrapper objects from writeValue', () => {
    component.writeValue({ val: 'wrapped' });
    expect(component.valueState()).toBe('wrapped');

    component.writeValue('plain');
    expect(component.valueState()).toBe('plain');
  });

  it('validates required trimmed content', () => {
    fixture.componentRef.setInput('required', true);

    expect(component.validate(new FormControl('   '))).toEqual({ required: true });
    expect(component.errorState()).toBe(true);

    expect(component.validate(new FormControl('valid'))).toBeNull();
    expect(component.errorState()).toBe(false);
  });

  it('includes disabled and error state in textarea classes', () => {
    fixture.componentRef.setInput('disabled', true);
    expect(component.textareaClasses).toContain('cursor-not-allowed');

    fixture.componentRef.setInput('disabled', false);
    component.errorState.set(true);
    expect(component.textareaClasses).toContain('focus:ring-error-500/10');
  });
});
