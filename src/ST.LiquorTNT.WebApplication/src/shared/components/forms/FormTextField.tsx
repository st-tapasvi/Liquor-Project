import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import IconButton from '@mui/material/IconButton';
import InputAdornment from '@mui/material/InputAdornment';
import TextField, { type TextFieldProps } from '@mui/material/TextField';
import { useState } from 'react';
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
  const [showPassword, setShowPassword] = useState(false);
  const isPassword = rest.type === 'password';
  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <TextField
          {...rest}
          {...field}
          type={isPassword && showPassword ? 'text' : rest.type}
          value={field.value ?? ''}
          inputRef={field.ref}
          error={fieldState.invalid}
          helperText={fieldState.error?.message ?? helperText}
          slotProps={{
            ...rest.slotProps,
            htmlInput: { ...(rest.slotProps?.htmlInput as object), 'aria-invalid': fieldState.invalid },
            ...(isPassword && {
              input: {
                ...(rest.slotProps?.input as object),
                endAdornment: (
                  <InputAdornment position="end">
                    <IconButton
                      edge="end"
                      size="small"
                      aria-label={showPassword ? 'Hide password' : 'Show password'}
                      aria-pressed={showPassword}
                      disabled={rest.disabled}
                      onClick={() => setShowPassword((s) => !s)}
                      onMouseDown={(e) => e.preventDefault()}
                    >
                      {showPassword ? <VisibilityOffIcon fontSize="small" /> : <VisibilityIcon fontSize="small" />}
                    </IconButton>
                  </InputAdornment>
                ),
              },
            }),
          }}
        />
      )}
    />
  );
}
