import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { SigninCallbackComponent } from './signin-callback.component';

describe('SigninCallbackComponent', () => {
  let fixture: ComponentFixture<SigninCallbackComponent>;
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    router = { navigateByUrl: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [SigninCallbackComponent],
      providers: [{ provide: Router, useValue: router }]
    }).compileComponents();

    fixture = TestBed.createComponent(SigninCallbackComponent);
  });

  it('navigates back to the app root after sign-in callback', () => {
    fixture.detectChanges();

    expect(router.navigateByUrl).toHaveBeenCalledWith('');
  });
});
