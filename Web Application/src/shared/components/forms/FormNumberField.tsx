import TextField, { type TextFieldProps } from '@mui/material/TextField';
import { Controller, type FieldPath, type FieldValues, useFormContext } from 'react-hook-form';

type FormNumberFieldProps<TFieldValues extends FieldValues> = Omit<
  TextFieldProps,
  'name' | 'value' | 'onChange' | 'error' | 'type'
> & {
  name: FieldPath<TFieldValues>;
  min?: number;
  max?: number;
};

/** Integer input. The form value is a `number`, or `null` when the box is empty (never a string). */
export function FormNumberField<TFieldValues extends FieldValues>({
  name,
  min,
  max,
  helperText,
  ...rest
}: FormNumberFieldProps<TFieldValues>) {
  const { control } = useFormContext<TFieldValues>();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <TextField
          {...rest}
          type="number"
          name={field.name}
          inputRef={field.ref}
          onBlur={field.onBlur}
          value={field.value === null || field.value === undefined ? '' : String(field.value)}
          onChange={(e) => {
            const raw = e.target.value;
            if (raw === '') {
              field.onChange(null);
              return;
            }
            const n = Number(raw);
            field.onChange(Number.isFinite(n) ? n : null);
          }}
          error={fieldState.invalid}
          helperText={fieldState.error?.message ?? helperText}
          slotProps={{
            ...rest.slotProps,
            htmlInput: { ...(rest.slotProps?.htmlInput as object), min, max, step: 1, inputMode: 'numeric' },
          }}
        />
      )}
    />
  );
}
