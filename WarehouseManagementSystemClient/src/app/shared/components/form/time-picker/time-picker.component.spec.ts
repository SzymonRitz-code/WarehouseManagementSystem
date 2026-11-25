import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TimePickerComponent } from './time-picker.component';

describe('TimePickerComponent', () => {
  let component: TimePickerComponent;
  let fixture: ComponentFixture<TimePickerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [TimePickerComponent] }).compileComponents();
    fixture = TestBed.createComponent(TimePickerComponent);
    component = fixture.componentInstance;
  });

  it('destroys flatpickr instance on destroy', () => {
    const destroy = vi.fn();
    (component as any).flatpickrInstance = { destroy };

    component.ngOnDestroy();

    expect(destroy).toHaveBeenCalledTimes(1);
  });

  it('does nothing on destroy when flatpickr was not initialized', () => {
    expect(() => component.ngOnDestroy()).not.toThrow();
  });
});
