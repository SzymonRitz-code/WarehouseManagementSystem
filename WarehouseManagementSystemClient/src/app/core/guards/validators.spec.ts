import { FormArray, FormControl } from '@angular/forms';
import { minFormArrayLength } from './validators';

describe('minFormArrayLength', () => {
  it('returns null when FormArray length is at least the required minimum', () => {
    const validator = minFormArrayLength(2);
    const formArray = new FormArray([new FormControl('first'), new FormControl('second')]);

    expect(validator(formArray)).toBeNull();
  });

  it('returns an error when FormArray is shorter than the required minimum', () => {
    const validator = minFormArrayLength(2);
    const formArray = new FormArray([new FormControl('only')]);

    expect(validator(formArray)).toEqual({ minArrayLength: true });
  });

  it('returns an error for controls that are not FormArray-like', () => {
    const validator = minFormArrayLength(1);

    expect(validator(new FormControl('value'))).toEqual({ minArrayLength: true });
  });

  it('reacts to dynamic FormArray length changes', () => {
    const validator = minFormArrayLength(1);
    const formArray = new FormArray<FormControl<string | null>>([]);

    expect(validator(formArray)).toEqual({ minArrayLength: true });

    formArray.push(new FormControl('added'));

    expect(validator(formArray)).toBeNull();
  });
});
