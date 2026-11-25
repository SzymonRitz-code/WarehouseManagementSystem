import { CommonModule } from '@angular/common';
import {
  Component,
  effect,
  HostListener,
  input,
  output
} from '@angular/core';

@Component({
  selector: 'app-modal',
  imports: [
    CommonModule,
  ],
  templateUrl: './modal.component.html',
  styles: ``
})
export class ModalComponent {
  readonly isOpen = input(false);
  readonly close = output<void>();
  readonly className = input('');
  readonly showCloseButton = input(true);
  readonly isFullscreen = input(false);

  constructor() {
    effect(() => {
      document.body.style.overflow = this.isOpen() ? 'hidden' : 'unset';
    });
  }

  ngOnDestroy() {
    document.body.style.overflow = 'unset';
  }

  onBackdropClick(event: MouseEvent) {
    if (!this.isFullscreen()) {
      this.close.emit();
    }
  }

  onContentClick(event: MouseEvent) {
    event.stopPropagation();
  }

 @HostListener('document:keydown.escape')
  onEscape() {
    if (this.isOpen()) {
      this.close.emit();
    }
  }
}
