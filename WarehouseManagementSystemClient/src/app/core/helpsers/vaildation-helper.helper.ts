import { FormGroup } from "@angular/forms";

export function setServerErrors(error: any, form: FormGroup) {
    if (!error?.error?.errors) return;

    const serverErrors = error.error.errors;

    Object.keys(serverErrors).forEach((key) => {
        const controlName = toCamelCase(key);
        const formControl = form.get(controlName);
        const messages = serverErrors[key].join(' ');

        if (formControl) {
            formControl.setErrors({
                ...formControl.errors,
                server: messages
            });
        } else {
            // błędy globalne
            form.setErrors({
                ...form.errors,
                server: messages
            });
        }
    });
}

function toCamelCase(value: string): string {
    return value.charAt(0).toLowerCase() + value.slice(1);
}
