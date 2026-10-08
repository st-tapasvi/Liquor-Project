import type { FieldValues, Path, UseFormSetError } from 'react-hook-form';

import { isValidationError } from './app-error';

export interface ApplyServerErrorsOptions<TFieldValues extends FieldValues> {
  fields?: readonly Path<TFieldValues>[] | undefined;
  rename?: Readonly<Partial<Record<string, Path<TFieldValues>>>> | undefined;
}


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
