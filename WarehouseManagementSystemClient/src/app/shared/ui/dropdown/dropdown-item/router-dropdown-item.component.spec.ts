import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterDropdownItemComponent } from './router-dropdown-item.component';

describe('RouterDropdownItemComponent', () => {
  let component: RouterDropdownItemComponent;
  let fixture: ComponentFixture<RouterDropdownItemComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RouterDropdownItemComponent],
      providers: [provideRouter([])]
    }).compileComponents();
    fixture = TestBed.createComponent(RouterDropdownItemComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('to', '/test');
  });

  it('combines base and custom classes', () => {
    fixture.componentRef.setInput('baseClassName', 'base');
    fixture.componentRef.setInput('className', 'custom');

    expect(component.combinedClasses()).toBe('base custom');
  });

  it('emits both click outputs without preventing router navigation', () => {
    const click = vi.fn();
    const itemClick = vi.fn();
    const event = new MouseEvent('click');
    const preventDefault = vi.spyOn(event, 'preventDefault');
    component.click.subscribe(click);
    component.itemClick.subscribe(itemClick);

    component.handleClick();

    expect(preventDefault).not.toHaveBeenCalled();
    expect(click).toHaveBeenCalledTimes(1);
    expect(itemClick).toHaveBeenCalledTimes(1);
  });
});
