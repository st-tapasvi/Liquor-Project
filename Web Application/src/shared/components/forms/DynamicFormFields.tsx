import Stack from '@mui/material/Stack';

import type { FieldConfig } from '@/core/api';

import { FormCheckbox } from './FormCheckbox';
import { FormNumberField } from './FormNumberField';
import { FormSelect } from './FormSelect';
import { FormTextField } from './FormTextField';

/**
 * Renders the fields of a backend-supplied screen configuration (§8.2). This component is the reason
 * the frontend contains no excise-specific code: whether the brand identifier is GTIN, ETIN or brand
 * code plus packing code is a difference in the `fields` it is handed, not in any code path here.
 *
 * It is generic on purpose — it knows about field *kinds*, never about brands, batches or excises — so
 * it belongs in `shared/` and passes the §6 test: it would still be useful in an unrelated application.
 *
 * Must be rendered inside a react-hook-form `FormProvider` whose schema came from `schemaFromFields`.
 */
export function DynamicFormFields({
  fields,
  disabled = false,
}: {
  /** Already filtered to visible fields and sorted by sequence (see `visibleFields`). */
  fields: readonly FieldConfig[];
  /** Disables every field, e.g. while the form is submitting. */
  disabled?: boolean;
}) {
  return (
    <Stack spacing={2}>
      {fields.map((field) => (
        <DynamicField key={field.name} field={field} disabled={disabled} />
      ))}
    </Stack>
  );
}

function DynamicField({ field, disabled }: { field: FieldConfig; disabled: boolean }) {
  // The configuration owns label, requiredness and editability; the screen never overrides them.
  const common = {
    name: field.name as never,
    label: field.label,
    required: field.required,
    disabled: disabled || field.readOnly,
    ...(field.helperText === null ? {} : { helperText: field.helperText }),
  };

  switch (field.kind) {
    case 'checkbox':
      return <FormCheckbox {...common} />;

    case 'number':
      return <FormNumberField {...common} />;

    case 'select':
      return (
        <FormSelect
          {...common}
          options={field.options.map((o) => ({ value: o.value, label: o.label }))}
          allowEmpty={!field.required}
        />
      );

    case 'date':
      return <FormTextField {...common} type="date" slotProps={{ inputLabel: { shrink: true } }} />;

    case 'textarea':
      return <FormTextField {...common} multiline minRows={3} />;

    case 'text':
      return (
        <FormTextField
          {...common}
          {...(field.maxLength === null ? {} : { slotProps: { htmlInput: { maxLength: field.maxLength } } })}
        />
      );
  }
}
