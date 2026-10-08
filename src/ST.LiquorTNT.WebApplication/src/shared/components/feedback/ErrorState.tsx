import Alert from '@mui/material/Alert';
import AlertTitle from '@mui/material/AlertTitle';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';

import { describeError, isApiError } from '@/core/errors';

interface ErrorStateProps {
  error: unknown;
  title?: string;
  onRetry?: () => void;
}

export function ErrorState({ error, title = 'Could not load data', onRetry }: ErrorStateProps) {
  const reference = isApiError(error) ? error.correlationId : undefined;
  return (
    <Alert
      severity="error"
      role="alert"
      action={
        onRetry && (
          <Button color="inherit" size="small" onClick={onRetry}>
            Retry
          </Button>
        )
      }
    >
      <AlertTitle>{title}</AlertTitle>
      {describeError(error)}
      {reference && (
        <Typography variant="caption" sx={{ display: 'block', mt: 0.5 }}>
          Ref: {reference}
        </Typography>
      )}
    </Alert>
  );
}
