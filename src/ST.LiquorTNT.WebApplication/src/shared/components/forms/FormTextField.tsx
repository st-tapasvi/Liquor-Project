import TextField, { type TextFieldProps } from '@mui/material/TextField';
import { Controller, type FieldPath, type FieldValues, useFormContext } from 'react-hook-form';

type FormTextFieldProps<TFieldValues extends FieldValues> = Omit<
  TextFieldProps,
  'name' | 'value' | 'onChange' | 'error'
> & {
  name: FieldPath<TFieldValues>;
};

export function FormTextField<TFieldValues extends FieldValues>({
  name,
  helperText,
  ...rest
}: FormTextFieldProps<TFieldValues>) {
  const { control } = useFormContext<TFieldValues>();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <TextField
          {...rest}
          {...field}
          value={field.value ?? ''}
          inputRef={field.ref}
          error={fieldState.invalid}
          helperText={fieldState.error?.message ?? helperText}
          slotProps={{
            ...rest.slotProps,
            htmlInput: { ...(rest.slotProps?.htmlInput as object), 'aria-invalid': fieldState.invalid },
          }}
        />
      )}
    />
  );
}
