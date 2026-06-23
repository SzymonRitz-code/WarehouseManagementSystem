import { TimePickerComponent } from './time-picker.component';

describe('TimePickerComponent', () => {
  it('destroys flatpickr instance on destroy', () => {
    const component = new TimePickerComponent();
    const destroy = vi.fn();
    (component as any).flatpickrInstance = { destroy };

    component.ngOnDestroy();

    expect(destroy).toHaveBeenCalledTimes(1);
  });

  it('does nothing on destroy when flatpickr was not initialized', () => {
    const component = new TimePickerComponent();

    expect(() => component.ngOnDestroy()).not.toThrow();
  });
});
