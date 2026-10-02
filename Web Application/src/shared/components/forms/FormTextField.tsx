import TextField, { type TextFieldProps } from '@mui/material/TextField';
import { Controller, type FieldPath, type FieldValues, useFormContext } from 'react-hook-form';

type FormTextFieldProps<TFieldValues extends FieldValues> = Omit<
  TextFieldProps,
  'name' | 'value' | 'onChange' | 'error'
> & {
  name: FieldPath<TFieldValues>;
};

/**
 * Text input bound to react-hook-form. Shows the field's validation message (client or server) below it.
 * `autoComplete` should be set explicitly by password fields ("current-password" / "new-password").
 */
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
