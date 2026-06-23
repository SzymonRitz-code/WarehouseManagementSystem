import { FormControl } from '@angular/forms';
import { TextAreaComponent } from './text-area.component';

describe('TextAreaComponent', () => {
  it('emits value changes and touches the control on input', () => {
    const component = new TextAreaComponent();
    const onChange = vi.fn();
    const onTouched = vi.fn();
    const valueChange = vi.fn();
    component.registerOnChange(onChange);
    component.registerOnTouched(onTouched);
    component.valueChange.subscribe(valueChange);
    const event = { target: { value: 'new note' } } as unknown as Event;

    component.onInput(event);

    expect(component.value).toBe('new note');
    expect(onChange).toHaveBeenCalledWith('new note');
    expect(onTouched).toHaveBeenCalledTimes(1);
    expect(valueChange).toHaveBeenCalledWith('new note');
  });

  it('accepts plain values and value wrapper objects from writeValue', () => {
    const component = new TextAreaComponent();

    component.writeValue({ val: 'wrapped' });
    expect(component.value).toBe('wrapped');

    component.writeValue('plain');
    expect(component.value).toBe('plain');
  });

  it('validates required trimmed content', () => {
    const component = new TextAreaComponent();
    component.required = true;

    expect(component.validate(new FormControl('   '))).toEqual({ required: true });
    expect(component.error).toBe(true);

    expect(component.validate(new FormControl('valid'))).toBeNull();
    expect(component.error).toBe(false);
  });

  it('includes disabled and error state in textarea classes', () => {
    const component = new TextAreaComponent();

    component.disabled = true;
    expect(component.textareaClasses).toContain('cursor-not-allowed');

    component.disabled = false;
    component.error = true;
    expect(component.textareaClasses).toContain('focus:ring-error-500/10');
  });
});
