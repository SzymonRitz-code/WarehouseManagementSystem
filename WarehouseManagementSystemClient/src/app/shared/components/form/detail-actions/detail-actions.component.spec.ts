import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DetailActionsComponent } from './detail-actions.component';

describe('DetailActionsComponent', () => {
  let component: DetailActionsComponent;
  let fixture: ComponentFixture<DetailActionsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DetailActionsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DetailActionsComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
