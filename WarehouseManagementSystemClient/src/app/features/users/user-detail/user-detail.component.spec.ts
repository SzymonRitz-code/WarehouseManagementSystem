import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UserDetailComponentnent } from './user-detail.component';

describe('UserDetailComponentnent', () => {
  let component: UserDetailComponentnent;
  let fixture: ComponentFixture<UserDetailComponentnent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UserDetailComponentnent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(UserDetailComponentnent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
