import Alert from '@mui/material/Alert';
import { useFormContext } from 'react-hook-form';

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
