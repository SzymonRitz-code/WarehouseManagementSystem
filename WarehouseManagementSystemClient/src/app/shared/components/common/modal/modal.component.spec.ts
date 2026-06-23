import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ModalComponent } from './modal.component';

describe('ModalComponent', () => {
  let component: ModalComponent;
  let fixture: ComponentFixture<ModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ModalComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ModalComponent);
    component = fixture.componentInstance;
    document.body.style.overflow = 'unset';
  });

  afterEach(() => {
    document.body.style.overflow = 'unset';
  });

  it('locks body scroll while open and restores it when closed', () => {
    component.isOpen = true;
    component.ngOnChanges();

    expect(document.body.style.overflow).toBe('hidden');

    component.isOpen = false;
    component.ngOnChanges();

    expect(document.body.style.overflow).toBe('unset');
  });

  it('emits close on backdrop click in regular mode', () => {
    const close = vi.fn();
    component.close.subscribe(close);
    component.isFullscreen = false;

    component.onBackdropClick(new MouseEvent('click'));

    expect(close).toHaveBeenCalledTimes(1);
  });

  it('does not emit close on backdrop click in fullscreen mode', () => {
    const close = vi.fn();
    component.close.subscribe(close);
    component.isFullscreen = true;

    component.onBackdropClick(new MouseEvent('click'));

    expect(close).not.toHaveBeenCalled();
  });

  it('emits close on Escape only when open', () => {
    const close = vi.fn();
    component.close.subscribe(close);

    component.isOpen = false;
    component.onEscape();

    component.isOpen = true;
    component.onEscape();

    expect(close).toHaveBeenCalledTimes(1);
  });

  it('stops content clicks from bubbling to the backdrop', () => {
    const event = new MouseEvent('click');
    const stopPropagation = vi.spyOn(event, 'stopPropagation');

    component.onContentClick(event);

    expect(stopPropagation).toHaveBeenCalledTimes(1);
  });
});
