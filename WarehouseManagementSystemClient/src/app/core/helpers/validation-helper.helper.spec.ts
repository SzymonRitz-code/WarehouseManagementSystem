import { FormControl, FormGroup, Validators } from '@angular/forms';
import { clearServerErrors, setServerErrors } from './validation-helper.helper';

describe('validation helper', () => {
  it('maps backend validation errors onto matching form controls', () => {
    const form = new FormGroup({
      sourceWarehouseId: new FormControl('', Validators.required),
      documentNumber: new FormControl('')
    });

    const result = setServerErrors({
      error: {
        errors: {
          SourceWarehouseId: ['Source warehouse is closed.'],
          'document-number': ['Document number already exists.']
        }
      }
    }, form);

    expect(form.get('sourceWarehouseId')?.errors?.['server']).toBe('Source warehouse is closed.');
    expect(form.get('documentNumber')?.errors?.['server']).toBe('Document number already exists.');
    expect(result.summary).toEqual([]);
  });

  it('keeps existing validator errors when adding and clearing server errors', () => {
    const form = new FormGroup({
      code: new FormControl('', Validators.required)
    });
    const code = form.get('code')!;
    code.markAsTouched();
    code.updateValueAndValidity();

    setServerErrors({
      error: {
        errors: {
          code: ['Code already exists.']
        }
      }
    }, form);

    expect(code.errors).toEqual(expect.objectContaining({
      required: true,
      server: 'Code already exists.'
    }));

    clearServerErrors(form);

    expect(code.errors).toEqual({ required: true });
  });

  it('puts unknown validation keys into form summary', () => {
    const form = new FormGroup({
      code: new FormControl('')
    });

    const result = setServerErrors({
      error: {
        errors: {
          businessRule: ['Warehouse cannot be deactivated while it contains stock.']
        }
      }
    }, form);

    expect(result.summary).toEqual(['Warehouse cannot be deactivated while it contains stock.']);
    expect(form.errors?.['serverSummary']).toBe('Warehouse cannot be deactivated while it contains stock.');
  });

  it('adds title with error code for business validation responses', () => {
    const consoleWarn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    const form = new FormGroup({
      quantity: new FormControl(0)
    });

    const result = setServerErrors({
      status: 422,
      error: {
        title: 'Business validation failed',
        errorCode: 'WMS-422',
        errors: {
          quantity: ['Quantity exceeds available stock.']
        }
      }
    }, form);

    expect(form.get('quantity')?.errors?.['server']).toBe('Quantity exceeds available stock.');
    expect(result.summary).toEqual(['Business validation failed (WMS-422)']);
    expect(form.errors?.['serverSummary']).toBe('Business validation failed (WMS-422)');

    consoleWarn.mockRestore();
  });

  it('uses a generic summary when response has no field errors', () => {
    const form = new FormGroup({
      code: new FormControl('')
    });

    const result = setServerErrors({
      error: {
        detail: 'Server is temporarily unavailable.'
      }
    }, form);

    expect(result.summary).toEqual(['Server is temporarily unavailable.']);
    expect(form.errors?.['serverSummary']).toBe('Server is temporarily unavailable.');
  });

  it('clears previous server errors before applying a new response', () => {
    const form = new FormGroup({
      code: new FormControl(''),
      name: new FormControl('')
    });

    setServerErrors({ error: { errors: { code: ['Old error.'] } } }, form);
    setServerErrors({ error: { errors: { name: ['New error.'] } } }, form);

    expect(form.get('code')?.errors).toBeNull();
    expect(form.get('name')?.errors?.['server']).toBe('New error.');
  });
});
