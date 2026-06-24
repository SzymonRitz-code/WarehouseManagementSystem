import { AbstractControl, FormArray, ValidationErrors } from "@angular/forms";

export function minFormArrayLength(min: number) {
  return (control: AbstractControl): ValidationErrors | null => {

    const formArray = control as FormArray;

    if (!formArray || !formArray.controls) {
      return { minArrayLength: true };
    }

    return formArray.controls.length >= min
      ? null
      : { minArrayLength: true };
  };
}

// export function minFormArrayLength(min: number) {
//   return (control: AbstractControl): ValidationErrors | null => {

//     const formArray = control as FormArray;

//     if (!formArray) return null;

//     const length = formArray.length ?? 0;

//     return length >= min
//       ? null
//       : { minArrayLength: { required: min, actual: length } };
//   };
// }