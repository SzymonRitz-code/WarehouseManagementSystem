import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { ValidationSummaryComponent } from './validation-summary.component';

describe('ValidationSummaryComponent', () => {
  let fixture: ComponentFixture<ValidationSummaryComponent>;
  let component: ValidationSummaryComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ValidationSummaryComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ValidationSummaryComponent);
    component = fixture.componentInstance;
  });

  it('renders server summary errors from the form', () => {
    const form = new FormGroup({
      name: new FormControl('')
    });
    form.setErrors({ serverSummary: 'Business rule failed.' });
    fixture.componentRef.setInput('form', form);

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Please review the form');
    expect(fixture.nativeElement.textContent).toContain('Business rule failed.');
  });

  it('renders nothing when the form has no server summary', () => {
    fixture.componentRef.setInput('form', new FormGroup({
      name: new FormControl('')
    }));

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent.trim()).toBe('');
  });
});
