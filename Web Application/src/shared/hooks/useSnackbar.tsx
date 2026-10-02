import Alert, { type AlertColor } from '@mui/material/Alert';
import Snackbar from '@mui/material/Snackbar';
import { createContext, type ReactNode, useCallback, useContext, useMemo, useState } from 'react';

import { describeError, isApiError } from '@/core/errors';

interface SnackbarMessage {
  id: number;
  severity: AlertColor;
  text: string;
  reference?: string | undefined;
}

interface SnackbarApi {
  success: (text: string) => void;
  info: (text: string) => void;
  warning: (text: string) => void;
  error: (text: string, reference?: string) => void;
  /** Announces any thrown error with its correlation id when it has one. */
  errorFrom: (error: unknown) => void;
}

const SnackbarContext = createContext<SnackbarApi | null>(null);

let nextId = 1;

/** One queue of toast messages for the whole app. Errors stay until dismissed; others auto-hide. */
export function SnackbarProvider({ children }: { children: ReactNode }) {
  const [queue, setQueue] = useState<SnackbarMessage[]>([]);
  const current = queue[0];

  const push = useCallback((severity: AlertColor, text: string, reference?: string) => {
    setQueue((q) => [...q, { id: nextId++, severity, text, reference }]);
  }, []);

  const api = useMemo<SnackbarApi>(
    () => ({
      success: (text) => push('success', text),
      info: (text) => push('info', text),
      warning: (text) => push('warning', text),
      error: (text, reference) => push('error', text, reference),
      errorFrom: (error) => push('error', describeError(error), isApiError(error) ? error.correlationId : undefined),
    }),
    [push],
  );

  const handleClose = () => setQueue((q) => q.slice(1));

  return (
    <SnackbarContext.Provider value={api}>
      {children}
      <Snackbar
        key={current?.id}
        open={current !== undefined}
        autoHideDuration={current?.severity === 'error' ? null : 4000}
        onClose={(_e, reason) => {
          if (reason !== 'clickaway') handleClose();
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        {current && (
          <Alert severity={current.severity} variant="filled" onClose={handleClose} sx={{ minWidth: 320 }}>
            {current.text}
            {current.reference && (
              <span style={{ display: 'block', opacity: 0.85, fontSize: '0.75rem' }}>Ref: {current.reference}</span>
            )}
          </Alert>
        )}
      </Snackbar>
    </SnackbarContext.Provider>
  );
}

export function useSnackbar(): SnackbarApi {
  const ctx = useContext(SnackbarContext);
  if (!ctx) throw new Error('useSnackbar must be used inside <SnackbarProvider>');
  return ctx;
}
