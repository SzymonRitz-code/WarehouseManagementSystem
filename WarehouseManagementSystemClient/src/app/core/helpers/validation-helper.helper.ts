import { AbstractControl, FormGroup } from '@angular/forms';

export interface ServerValidationResult {
  summary: string[];
}

export function setServerErrors(error: any, form: FormGroup): ServerValidationResult {
  clearServerErrors(form);

  const validationErrors = error?.error?.errors;
  const title = error?.error?.title ?? error?.error?.detail ?? 'Unexpected server error';
  const status = error?.status ?? error?.error?.status;
  const errorCode = error?.error?.errorCode;

  const summary: string[] = [];

  if (validationErrors && typeof validationErrors === 'object') {
    for (const [key, messages] of Object.entries(validationErrors)) {
      const text = normalizeMessages(messages);
      const control = findControl(form, key);

      if (control) {
        setControlServerError(control, text);
      } else {
        summary.push(text);
      }
    }
  }

  if (status === 422 || errorCode) {
    summary.push(errorCode ? `${title} (${errorCode})` : title);
  } else if (summary.length === 0 && !validationErrors) {
    summary.push(title);
  }

  if (summary.length > 0) {
    form.setErrors({
      ...form.errors,
      serverSummary: summary.join(' ')
    });
  }

  return { summary };
}

export function clearServerErrors(form: FormGroup) {
  Object.values(form.controls).forEach(control => clearControlServerErrors(control));

  if (form.errors?.['serverSummary']) {
    const { serverSummary, ...rest } = form.errors;
    form.setErrors(Object.keys(rest).length ? rest : null);
  }
}

function findControl(form: FormGroup, key: string): AbstractControl | null {
  const normalized = normalizeKey(key);
  return Object.entries(form.controls).find(([name]) => normalizeKey(name) === normalized)?.[1] ?? null;
}

function clearControlServerErrors(control: AbstractControl) {
  if (!control.errors?.['server']) return;
  const { server, ...rest } = control.errors;
  control.setErrors(Object.keys(rest).length ? rest : null);
}

function setControlServerError(control: AbstractControl, message: string) {
  control.setErrors({
    ...control.errors,
    server: message
  });
}

function normalizeKey(value: string): string {
  return value.replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
}

function normalizeMessages(messages: unknown): string {
  if (Array.isArray(messages)) {
    return messages.filter(Boolean).join(' ');
  }

  return String(messages ?? '');
}
