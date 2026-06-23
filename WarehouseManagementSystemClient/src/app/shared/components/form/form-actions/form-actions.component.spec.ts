import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FormActionsComponent } from './form-actions.component';

describe('FormActionsComponent', () => {
  let component: FormActionsComponent;
  let fixture: ComponentFixture<FormActionsComponent>; 
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormActionsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FormActionsComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('renders shared buttons for back and submit actions', () => {
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll('app-button');

    expect(buttons).toHaveLength(2);
    expect(fixture.nativeElement.textContent).toContain('Back');
    expect(fixture.nativeElement.textContent).toContain('Save');
  });
});
