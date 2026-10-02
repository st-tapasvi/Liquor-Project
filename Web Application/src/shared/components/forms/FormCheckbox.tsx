import Checkbox from '@mui/material/Checkbox';
import FormControlLabel from '@mui/material/FormControlLabel';
import FormHelperText from '@mui/material/FormHelperText';
import { Controller, type FieldPath, type FieldValues, useFormContext } from 'react-hook-form';

interface FormCheckboxProps<TFieldValues extends FieldValues> {
  name: FieldPath<TFieldValues>;
  label: string;
  disabled?: boolean;
}

export function FormCheckbox<TFieldValues extends FieldValues>({
  name,
  label,
  disabled,
}: FormCheckboxProps<TFieldValues>) {
  const { control } = useFormContext<TFieldValues>();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <>
          <FormControlLabel
            control={
              <Checkbox
                checked={Boolean(field.value)}
                onChange={(e) => field.onChange(e.target.checked)}
                slotProps={{ input: { ref: field.ref } }}
              />
            }
            label={label}
            disabled={disabled}
          />
          {fieldState.error && <FormHelperText error>{fieldState.error.message}</FormHelperText>}
        </>
      )}
    />
  );
}
