import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ComponentCardComponent } from './component-card.component';

describe('ComponentCardComponent', () => {
  let component: ComponentCardComponent;
  let fixture: ComponentFixture<ComponentCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ComponentCardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ComponentCardComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
