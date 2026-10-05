import Alert from '@mui/material/Alert';
import { useFormContext } from 'react-hook-form';

/** Shows the form-level server error (`root.server`) set by applyServerErrors, or a custom root error. */
export function FormRootError() {
  const {
    formState: { errors },
  } = useFormContext();
  const root = errors.root as Record<string, { message?: string } | undefined> | undefined;
  const message = root?.['server']?.message ?? root?.['submit']?.message;
  if (!message) return null;
  return (
    <Alert severity="error" role="alert" sx={{ mb: 2 }}>
      {message}
    </Alert>
  );
}
