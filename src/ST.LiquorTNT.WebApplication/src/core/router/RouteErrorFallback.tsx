import Alert from '@mui/material/Alert';
import AlertTitle from '@mui/material/AlertTitle';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import { useEffect } from 'react';
import { isRouteErrorResponse, useNavigate, useRouteError } from 'react-router';

import { describeError, isApiError } from '../errors';
import { logger } from '../logging/logger';

import { PATHS } from './paths';

/**
 * Route-level error UI (React Router `errorElement`). A crash in one page keeps the shell usable.
 * The correlation id is shown so support can find the matching API log entry.
 */
export function RouteErrorFallback() {
  const error = useRouteError();
  const navigate = useNavigate();

  // Why an effect: log once per error instance, not on every render.
  useEffect(() => {
    logger.error('route error', { error });
  }, [error]);

  const message = isRouteErrorResponse(error) ? `${error.status} ${error.statusText}` : describeError(error);
  const correlationId = isApiError(error) ? error.correlationId : undefined;

  return (
    <Box sx={{ p: 3, maxWidth: 640 }}>
      <Alert severity="error" role="alert">
        <AlertTitle>This screen could not be shown</AlertTitle>
        <Typography variant="body2">{message}</Typography>
        {correlationId && (
          <Typography variant="caption" sx={{ display: 'block', mt: 1, color: 'text.secondary' }}>
            Reference: {correlationId}
          </Typography>
        )}
      </Alert>
      <Box sx={{ mt: 2, display: 'flex', gap: 1 }}>
        <Button variant="contained" onClick={() => navigate(0)}>
          Reload
        </Button>
        <Button variant="outlined" onClick={() => navigate(PATHS.dashboard)}>
          Go to dashboard
        </Button>
      </Box>
    </Box>
  );
}
