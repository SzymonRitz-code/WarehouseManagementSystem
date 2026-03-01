import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BackdropComponent } from './backdrop.component';

describe('BackdropComponent', () => {
  let component: BackdropComponent;
  let fixture: ComponentFixture<BackdropComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BackdropComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BackdropComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
