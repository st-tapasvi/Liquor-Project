import { useEffect } from 'react';

import { type SessionEndReason, useSession, useSessionStore } from '@/core/auth';

import { type ToastSeverity, useSnackbar } from '@/shared/hooks';

const MESSAGES: Record<
  Exclude<SessionEndReason, 'unreachable'>,
  { severity: Exclude<ToastSeverity, 'error'>; text: string }
> = {
  logout: { severity: 'success', text: 'You have been logged out.' },
  timed_out: { severity: 'warning', text: 'Your session ended because it was idle. Please log in again.' },
  invalid: { severity: 'warning', text: 'Your session has ended. Please log in again.' },
  expired: { severity: 'warning', text: 'Your session reached its time limit. Please log in again.' },
  unauthenticated: { severity: 'info', text: 'Please log in to continue.' },
};

export function SessionEndedNotice() {
  const { endReason } = useSession();
  const clear = useSessionStore((s) => s.clearEndReason);
  const snackbar = useSnackbar();

  useEffect(() => {
    if (!endReason || endReason === 'unreachable') return;
    const m = MESSAGES[endReason];
    snackbar[m.severity](m.text, { id: 'session-ended' });
    clear();
  }, [endReason, clear, snackbar]);

  return null;
}
