import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GeneratorLayoutComponent } from './generator-layout.component';

describe('GeneratorLayoutComponent', () => {
  let component: GeneratorLayoutComponent;
  let fixture: ComponentFixture<GeneratorLayoutComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GeneratorLayoutComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GeneratorLayoutComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
