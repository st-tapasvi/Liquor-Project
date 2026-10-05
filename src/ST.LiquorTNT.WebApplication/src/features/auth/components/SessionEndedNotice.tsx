import Alert from '@mui/material/Alert';

import { type SessionEndReason, useSession, useSessionStore } from '@/core/auth';

const MESSAGES: Record<SessionEndReason, { severity: 'info' | 'warning'; text: string }> = {
  logout: { severity: 'info', text: 'You have been logged out.' },
  timed_out: { severity: 'warning', text: 'Your session ended because it was idle. Please log in again.' },
  invalid: { severity: 'warning', text: 'Your session has ended. Please log in again.' },
  expired: { severity: 'warning', text: 'Your session reached its time limit. Please log in again.' },
  unauthenticated: { severity: 'info', text: 'Please log in to continue.' },
  unreachable: {
    severity: 'warning',
    text: 'The server could not be reached, so your session could not be checked. Try again in a moment.',
  },
};

/** Explains on the login page why the previous session ended. Cleared once shown. */
export function SessionEndedNotice() {
  const { endReason } = useSession();
  const clear = useSessionStore((s) => s.clearEndReason);
  if (!endReason) return null;
  const m = MESSAGES[endReason];
  return (
    <Alert severity={m.severity} onClose={clear} sx={{ mb: 2 }}>
      {m.text}
    </Alert>
  );
}
