import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';
import { useFormContext } from 'react-hook-form';

interface FormActionsProps {
  submitLabel?: string;
  cancelLabel?: string;
  onCancel?: () => void;
  /** Overrides the submitting state derived from the form (e.g. a mutation's isPending). */
  busy?: boolean;
}

/** Save / Cancel row. Submit is disabled while submitting; Cancel is always available. */
export function FormActions({ submitLabel = 'Save', cancelLabel = 'Cancel', onCancel, busy }: FormActionsProps) {
  const {
    formState: { isSubmitting },
  } = useFormContext();
  const disabled = busy ?? isSubmitting;
  return (
    <Stack direction="row" spacing={1} sx={{ mt: 3, justifyContent: 'flex-end' }}>
      {onCancel && (
        <Button type="button" onClick={onCancel} disabled={disabled}>
          {cancelLabel}
        </Button>
      )}
      <Button type="submit" variant="contained" disabled={disabled}>
        {submitLabel}
      </Button>
    </Stack>
  );
}
