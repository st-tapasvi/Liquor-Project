import type { FieldValues, Path, UseFormSetError } from 'react-hook-form';

import { isValidationError } from './app-error';

export interface ApplyServerErrorsOptions<TFieldValues extends FieldValues> {
  /** When given, only these fields receive messages; the rest go to the form-level `root.server` error. */
  fields?: readonly Path<TFieldValues>[] | undefined;
  /** API field name → form field name, for the cases where they differ (`password` → `newPassword`). */
  rename?: Readonly<Partial<Record<string, Path<TFieldValues>>>> | undefined;
}

/**
 * Maps a 400 VALIDATION_FAILED response onto react-hook-form fields.
 * Field names from the API are camelCase and match the form's field names by convention; `rename`
 * covers the exceptions. Messages for fields the form does not have go to `root.server` so nothing is lost.
 *
 * Returns true when the error was a validation error (and has been applied), false otherwise so the
 * caller can let other errors reach its own handling.
 */
export function applyServerErrors<TFieldValues extends FieldValues>(
  setError: UseFormSetError<TFieldValues>,
  error: unknown,
  options?: ApplyServerErrorsOptions<TFieldValues>,
): boolean {
  if (!isValidationError(error)) return false;

  const unmatched: string[] = [];
  for (const [apiField, messages] of Object.entries(error.errors)) {
    const message = messages.join(' ');
    const field = options?.rename?.[apiField] ?? (apiField as Path<TFieldValues>);
    const known = !options?.fields || (options.fields as readonly string[]).includes(field);
    if (known) setError(field, { type: 'server', message });
    else unmatched.push(message);
  }

  if (unmatched.length > 0) {
    setError('root.server', { type: 'server', message: unmatched.join(' ') });
  }

  return true;
}
