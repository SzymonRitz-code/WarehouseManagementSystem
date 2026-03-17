import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ZonesComponent } from './zones.component';

describe('ZonesComponent', () => {
  let component: ZonesComponent;
  let fixture: ComponentFixture<ZonesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ZonesComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ZonesComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
