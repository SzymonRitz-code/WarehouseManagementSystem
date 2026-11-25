import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DropdownItemComponent } from './dropdown-item.component';

describe('DropdownItemComponent', () => {
  let component: DropdownItemComponent;
  let fixture: ComponentFixture<DropdownItemComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [DropdownItemComponent] }).compileComponents();
    fixture = TestBed.createComponent(DropdownItemComponent);
    component = fixture.componentInstance;
  });

  it('combines base and custom classes without losing defaults', () => {
    fixture.componentRef.setInput('baseClassName', 'base');
    fixture.componentRef.setInput('className', 'custom');

    expect(component.combinedClasses()).toBe('base custom');
  });

  it('prevents default navigation and emits both click outputs', () => {
    const click = vi.fn();
    const itemClick = vi.fn();
    const event = new MouseEvent('click');
    const preventDefault = vi.spyOn(event, 'preventDefault');
    component.click.subscribe(click);
    component.itemClick.subscribe(itemClick);

    component.handleClick(event);

    expect(preventDefault).toHaveBeenCalledTimes(1);
    expect(click).toHaveBeenCalledTimes(1);
    expect(itemClick).toHaveBeenCalledTimes(1);
  });
});
