import MenuItem from '@mui/material/MenuItem';
import TextField, { type TextFieldProps } from '@mui/material/TextField';
import { Controller, type FieldPath, type FieldValues, useFormContext } from 'react-hook-form';

import type { Option } from '@/shared/types';

type FormSelectProps<TFieldValues extends FieldValues> = Omit<
  TextFieldProps,
  'name' | 'value' | 'onChange' | 'error' | 'select'
> & {
  name: FieldPath<TFieldValues>;
  options: readonly Option[];
  allowEmpty?: boolean;
  emptyLabel?: string;
};

export function FormSelect<TFieldValues extends FieldValues>({
  name,
  options,
  allowEmpty,
  emptyLabel = '—',
  helperText,
  ...rest
}: FormSelectProps<TFieldValues>) {
  const { control } = useFormContext<TFieldValues>();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <TextField
          {...rest}
          select
          name={field.name}
          inputRef={field.ref}
          onBlur={field.onBlur}
          value={field.value ?? ''}
          onChange={(e) => {
            const raw = e.target.value;
            if (raw === '') {
              field.onChange(null);
              return;
            }
            const match = options.find((o) => String(o.value) === raw);
            field.onChange(match ? match.value : raw);
          }}
          error={fieldState.invalid}
          helperText={fieldState.error?.message ?? helperText}
        >
          {allowEmpty && <MenuItem value="">{emptyLabel}</MenuItem>}
          {options.map((o) => (
            <MenuItem key={String(o.value)} value={String(o.value)} disabled={o.disabled}>
              {o.label}
            </MenuItem>
          ))}
        </TextField>
      )}
    />
  );
}
