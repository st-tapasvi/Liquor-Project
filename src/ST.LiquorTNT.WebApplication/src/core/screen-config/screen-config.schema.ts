import { z } from 'zod';

import type { FieldConfig } from '../api/contracts';

/**
 * Builds a Zod schema from a screen configuration, so a configured screen validates exactly the fields
 * this excise actually has. Client-side validation remains a usability aid — the API is the
 * authoritative validator (§9) — but it must not contradict the configuration it was given.
 */
export function schemaFromFields(fields: readonly FieldConfig[]): z.ZodObject<Record<string, z.ZodType>> {
  const shape: Record<string, z.ZodType> = {};

  for (const field of fields) {
    if (!field.visible) continue;
    shape[field.name] = fieldSchema(field);
  }

  return z.object(shape);
}

function fieldSchema(field: FieldConfig): z.ZodType {
  switch (field.kind) {
    case 'checkbox':
      return z.boolean();

    case 'number': {
      const base = z.number({ message: `${field.label} must be a number.` });
      return field.required ? base : base.nullable();
    }

    case 'select':
    case 'date':
    case 'text':
    case 'textarea': {
      let base = z.string();
      if (field.maxLength !== null) {
        base = base.max(field.maxLength, `${field.label} must be at most ${field.maxLength} characters.`);
      }
      // A read-only field is never edited by the user, so requiring input would be unsatisfiable.
      return field.required && !field.readOnly ? base.trim().min(1, `${field.label} is required.`) : base;
    }
  }
}

/** Empty form values matching a configuration, so the form is controlled from the first render. */
export function defaultValuesFromFields(fields: readonly FieldConfig[]): Record<string, unknown> {
  const values: Record<string, unknown> = {};
  for (const field of fields) {
    if (!field.visible) continue;
    values[field.name] = field.kind === 'checkbox' ? false : field.kind === 'number' ? null : '';
  }
  return values;
}
