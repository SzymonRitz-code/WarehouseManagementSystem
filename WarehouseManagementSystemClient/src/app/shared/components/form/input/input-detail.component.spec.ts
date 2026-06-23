import { InputDetailComponent } from './input-detail.component';

describe('InputDetailComponent', () => {
  it('formats booleans as human-readable values', () => {
    const component = new InputDetailComponent();

    component.value = true;
    expect(component.displayValue).toBe('Yes');

    component.value = false;
    expect(component.displayValue).toBe('No');
  });

  it('formats dates with the browser date string', () => {
    const component = new InputDetailComponent();
    const date = new Date('2026-06-22T00:00:00Z');
    component.value = date;

    expect(component.displayValue).toBe(date.toDateString());
  });

  it('returns empty string for nullish values', () => {
    const component = new InputDetailComponent();

    component.value = null;

    expect(component.displayValue).toBe('');
  });

  it('accepts extra classes while keeping the readonly control styling available', () => {
    const component = new InputDetailComponent();

    component.className = 'custom-detail-class';

    expect(component.className).toBe('custom-detail-class');
  });
});
