import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ButtonComponent } from './button.component';

describe('ButtonComponent', () => {
  let component: ButtonComponent;
  let fixture: ComponentFixture<ButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ButtonComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ButtonComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('uses the shared primary styling for primary actions', () => {
    component.variant = 'primary';

    expect(component.variantClasses).toContain('bg-brand-500');
    expect(component.variantClasses).toContain('shadow-brand-500/20');
  });

  it('uses compact sizing by default for shared action buttons', () => {
    expect(component.size).toBe('sm');
    expect(component.sizeClasses).toContain('px-3.5');
    expect(component.sizeClasses).toContain('py-2');
  });

  it('uses dark-aware outline styling for secondary actions', () => {
    component.variant = 'outline';

    expect(component.variantClasses).toContain('dark:text-gray-300');
    expect(component.variantClasses).not.toContain('bg-brand-500');
  });
});
