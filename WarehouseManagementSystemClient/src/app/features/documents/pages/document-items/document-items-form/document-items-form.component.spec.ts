import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ProductService } from '../../../../products/services/product-service';
import { ZoneService } from '../../../../zones/services/zone-service';

import { DocumentItemsFormComponent } from './document-items-form.component';

describe('DocumentItemsFormComponent', () => {
  let component: DocumentItemsFormComponent;
  let fixture: ComponentFixture<DocumentItemsFormComponent>;
  let productService: { getProducts: ReturnType<typeof vi.fn> };
  let zoneService: { getZones: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    productService = {
      getProducts: vi.fn().mockReturnValue(of([
        { id: 'prod-1', name: 'Steel Screw' },
        { id: 'prod-2', name: 'Copper Pipe' }
      ]))
    };
    zoneService = {
      getZones: vi.fn().mockReturnValue(of([
        { id: 'zone-1', code: 'A1', name: 'Receiving' },
        { id: 'zone-2', code: 'B2', name: 'Picking' }
      ]))
    };

    await TestBed.configureTestingModule({
      imports: [DocumentItemsFormComponent],
      providers: [
        { provide: ProductService, useValue: productService },
        { provide: ZoneService, useValue: zoneService }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DocumentItemsFormComponent);
    component = fixture.componentInstance;
    component.isOpen = true;
    fixture.detectChanges();
    await fixture.whenStable();
  });
 // test behawioralny sprawdzający, czy komponent ładuje opcje produktów i stref po inicjalizacji modalu.
  it('loads product and zone options when the modal initializes', () => {
    expect(component).toBeTruthy();
    expect(productService.getProducts).toHaveBeenCalledTimes(1);
    expect(zoneService.getZones).toHaveBeenCalledTimes(1);
    expect(component.productOptions).toEqual([
      { value: 'prod-1', label: 'Steel Screw' },
      { value: 'prod-2', label: 'Copper Pipe' }
    ]);
    expect(component.sourceZoneOptions).toEqual([
      { value: 'zone-1', label: 'A1_Receiving' },
      { value: 'zone-2', label: 'B2_Picking' }
    ]);
    expect(component.targetZoneOptions).toEqual(component.sourceZoneOptions);
  });
  // dodałem testy behawioralne do komponentu DocumentItemsFormComponent, które sprawdzają interakcje użytkownika z formularzem modalnym. Testy te obejmują wypełnianie formularza, kliknięcie przycisku "Save" i "Cancel", oraz sprawdzanie, czy odpowiednie zdarzenia są emitowane i czy formularz jest resetowany po zamknięciu modalu.
  it('lets a user complete the modal form and emits the item from the rendered UI', () => {
    const saveSpy = vi.fn();
    component.save.subscribe(saveSpy);
    fixture.detectChanges();

    setSelectValue('app-input-select[formcontrolname="productId"] select', 'prod-1');
    setInputValue('app-input-field[formcontrolname="quantity"] input', '4');
    setSelectValue('app-input-select[formcontrolname="sourceZoneId"] select', 'zone-1');
    setSelectValue('app-input-select[formcontrolname="targetZoneId"] select', 'zone-2');
    fixture.detectChanges();

    buttonByText('Save').click();

    expect(saveSpy).toHaveBeenCalledWith(expect.objectContaining({
      productId: 'prod-1',
      quantity: '4',
      sourceZoneId: 'zone-1',
      targetZoneId: 'zone-2'
    }));
    expect(component.isOpen).toBe(false);
    expect(component.itemForm.value).toEqual({
      productId: null,
      productName: null,
      quantity: null,
      sourceZoneId: null,
      sourceZoneName: null,
      targetZoneId: null,
      targetZoneName: null,
      id: null,
      index: null
    });
  });
  // test sprawdzający, że przycisk "Save" jest nieaktywny, gdy formularz jest niepoprawny, oraz że zdarzenie "save" nie jest emitowane po kliknięciu przycisku.
  it('does not emit save when the rendered form is invalid', () => {
    const saveSpy = vi.fn();
    component.save.subscribe(saveSpy);
    fixture.detectChanges();

    setSelectValue('app-input-select[formcontrolname="productId"] select', '');
    setInputValue('app-input-field[formcontrolname="quantity"] input', '0');
    fixture.detectChanges();

    const saveButton = buttonByText('Save');
    expect(saveButton.disabled).toBe(true);

    saveButton.click();

    expect(saveSpy).not.toHaveBeenCalled();
  });

  // test sprawdzający, że przycisk "Cancel" emituje zdarzenie "cancel" i czyści formularz po kliknięciu.
  it('emits cancel and clears the form when a user clicks Cancel', () => {
    const cancelSpy = vi.fn();
    component.cancel.subscribe(cancelSpy);
    setSelectValue('app-input-select[formcontrolname="productId"] select', 'prod-2');
    setInputValue('app-input-field[formcontrolname="quantity"] input', '3');
    fixture.detectChanges();

    buttonByText('Cancel').click();

    expect(cancelSpy).toHaveBeenCalledTimes(1);
    expect(component.itemForm.value.productId).toBeNull();
    expect(component.itemForm.value.quantity).toBeNull();
  });

  function setInputValue(selector: string, value: string): void {
    const input = fixture.nativeElement.querySelector(selector) as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
  }

  function setSelectValue(selector: string, value: string): void {
    const select = fixture.nativeElement.querySelector(selector) as HTMLSelectElement;
    select.value = value;
    select.dispatchEvent(new Event('change', { bubbles: true }));
  }

  function buttonByText(text: string): HTMLButtonElement {
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    const button = buttons.find(candidate => candidate.textContent?.trim() === text);
    if (!button) {
      throw new Error(`Button "${text}" was not found.`);
    }
    return button;
  }
});
