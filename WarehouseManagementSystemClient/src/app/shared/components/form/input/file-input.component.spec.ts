import { FileInputComponent } from './file-input.component';

describe('FileInputComponent', () => {
  it('emits the original change event', () => {
    const component = new FileInputComponent();
    const change = vi.fn();
    const event = new Event('change');
    component.change.subscribe(change);

    component.onChange(event);

    expect(change).toHaveBeenCalledWith(event);
  });
});
