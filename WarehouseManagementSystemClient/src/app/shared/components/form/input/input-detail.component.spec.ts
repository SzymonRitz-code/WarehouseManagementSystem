import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InputDetailComponent } from './input-detail.component';

describe('InputDetailComponent', () => {
  let component: InputDetailComponent;
  let fixture: ComponentFixture<InputDetailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [InputDetailComponent] }).compileComponents();
    fixture = TestBed.createComponent(InputDetailComponent);
    component = fixture.componentInstance;
  });

  it('formats booleans as human-readable values', () => {
    fixture.componentRef.setInput('value', true);
    expect(component.displayValue()).toBe('Yes');

    fixture.componentRef.setInput('value', false);
    expect(component.displayValue()).toBe('No');
  });

  it('formats dates with the browser date string', () => {
    const date = new Date('2026-06-22T00:00:00Z');
    fixture.componentRef.setInput('value', date);

    expect(component.displayValue()).toBe(date.toDateString());
  });

  it('returns empty string for nullish values', () => {
    fixture.componentRef.setInput('value', null);

    expect(component.displayValue()).toBe('');
  });

  it('accepts extra classes while keeping the readonly control styling available', () => {
    fixture.componentRef.setInput('className', 'custom-detail-class');

    expect(component.className()).toBe('custom-detail-class');
  });
});
