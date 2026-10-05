import Alert from '@mui/material/Alert';
import AlertTitle from '@mui/material/AlertTitle';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Divider from '@mui/material/Divider';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import type { SessionLimitError } from '@/core/errors';

import { formatDateTime } from '@/shared/utils';

/**
 * Shown under the login form when the account has reached its device limit (409 SESSION_LIMIT_REACHED).
 *
 * The API only sends this list once the password has been accepted, so whoever sees it has already
 * proven they hold the credential. When the server allows it, each row offers to end that session and
 * sign in here; otherwise the list is read-only and the user has to log out on the other device.
 */
export function SessionLimitPanel({
  error,
  onEndSession,
  busySessionId,
}: {
  error: SessionLimitError;
  onEndSession: (sessionId: number) => void;
  busySessionId: number | null;
}) {
  const busy = busySessionId !== null;

  return (
    <Alert severity="warning" sx={{ mt: 2 }} role="region" aria-label="Signed-in devices">
      <AlertTitle>{error.title}</AlertTitle>
      <Typography variant="body2">
        {error.canEndOther
          ? 'You are signed in on these devices. Sign one out to continue here.'
          : 'You are signed in on these devices. Log out on one of them, then sign in here.'}
      </Typography>

      {error.sessions.length === 0 ? (
        <Typography variant="body2" sx={{ mt: 1, fontStyle: 'italic' }}>
          The server did not say which devices are signed in.
        </Typography>
      ) : (
        <Stack divider={<Divider flexItem />} sx={{ mt: 1.5 }}>
          {error.sessions.map((session) => (
            <Box key={session.id} sx={{ py: 1.25 }}>
              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                {describeDevice(session.userAgent)}
              </Typography>
              <Typography variant="caption" color="text.secondary" component="div">
                Signed in {formatDateTime(session.loginAt)}
                {session.ipAddress === null ? '' : ` · ${session.ipAddress}`}
              </Typography>
              <Typography variant="caption" color="text.secondary" component="div">
                {session.lastActivityAt === null
                  ? 'No activity yet'
                  : `Last used ${formatDateTime(session.lastActivityAt)}`}
              </Typography>

              {error.canEndOther && (
                <Button
                  size="small"
                  color="error"
                  variant="outlined"
                  disabled={busy}
                  onClick={() => {
                    onEndSession(session.id);
                  }}
                  sx={{ mt: 1 }}
                >
                  {busySessionId === session.id ? 'Signing out…' : 'Sign out and continue'}
                </Button>
              )}
            </Box>
          ))}
        </Stack>
      )}
    </Alert>
  );
}

/**
 * A short label a person can match to one of their own devices. Deliberately coarse: the raw user agent
 * is long, and the goal is only "which of my two browsers is this", not fingerprinting.
 */
function describeDevice(userAgent: string | null): string {
  if (userAgent === null || userAgent.trim() === '') return 'Unknown device';

  const browser = /\bEdg\//.test(userAgent)
    ? 'Edge'
    : /\bOPR\//.test(userAgent)
      ? 'Opera'
      : /\bFirefox\//.test(userAgent)
        ? 'Firefox'
        : /\bChrome\//.test(userAgent)
          ? 'Chrome'
          : /\bSafari\//.test(userAgent)
            ? 'Safari'
            : 'Browser';

  const platform = /\bWindows\b/.test(userAgent)
    ? 'Windows'
    : /\b(iPhone|iPad|iOS)\b/.test(userAgent)
      ? 'iOS'
      : /\bAndroid\b/.test(userAgent)
        ? 'Android'
        : /\bMac OS X\b/.test(userAgent)
          ? 'macOS'
          : /\bLinux\b/.test(userAgent)
            ? 'Linux'
            : null;

  return platform === null ? browser : `${browser} on ${platform}`;
}
