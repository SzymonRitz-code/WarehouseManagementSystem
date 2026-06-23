import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';

import { SignoutCallbackComponent } from './signout-callback.component';

describe('SignoutCallbackComponent', () => {
  let component: SignoutCallbackComponent;
  let fixture: ComponentFixture<SignoutCallbackComponent>;
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    router = { navigateByUrl: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [SignoutCallbackComponent],
      providers: [{ provide: Router, useValue: router }]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SignoutCallbackComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('navigates back to the app root after sign-out callback', () => {
    fixture.detectChanges();

    expect(router.navigateByUrl).toHaveBeenCalledWith('');
  });
});
