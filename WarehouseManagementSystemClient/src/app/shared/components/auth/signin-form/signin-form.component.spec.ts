import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SigninFormComponent } from './signin-form.component';

describe('SigninForm', () => {
  let component: SigninFormComponent;
  let fixture: ComponentFixture<SigninFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SigninFormComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SigninFormComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
