import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DropdownComponent } from './dropdown.component';

describe('DropdownComponent', () => {
  let component: DropdownComponent;
  let fixture: ComponentFixture<DropdownComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DropdownComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(DropdownComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    fixture.destroy();
  });

  it('emits close when an open dropdown receives an outside click', () => {
    const close = vi.fn();
    const outside = document.createElement('button');
    document.body.appendChild(outside);
    component.close.subscribe(close);
    component.isOpen = true;
    fixture.detectChanges();

    outside.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));

    expect(close).toHaveBeenCalledTimes(1);

    outside.remove();
  });

  it('keeps open when click happens inside the dropdown', () => {
    const close = vi.fn();
    component.close.subscribe(close);
    component.isOpen = true;
    fixture.detectChanges();

    const dropdown = fixture.nativeElement.querySelector('div');
    dropdown.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));

    expect(close).not.toHaveBeenCalled();
  });

  it('keeps open when click comes from a dropdown toggle', () => {
    const close = vi.fn();
    const toggle = document.createElement('button');
    toggle.className = 'dropdown-toggle';
    document.body.appendChild(toggle);
    component.close.subscribe(close);
    component.isOpen = true;
    fixture.detectChanges();

    toggle.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));

    expect(close).not.toHaveBeenCalled();

    toggle.remove();
  });
});
