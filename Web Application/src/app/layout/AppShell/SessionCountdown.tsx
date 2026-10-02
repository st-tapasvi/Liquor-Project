import Alert from '@mui/material/Alert';
import { useEffect, useState } from 'react';

import { useSession } from '@/core/auth';
import { appConfig } from '@/core/config';

import { minutesUntil } from '@/shared/utils';

/**
 * Warns before the hard session limit so the user can finish what they are doing.
 * Uses `expiresAt` from the login response; the actual enforcement is the API's.
 */
export function SessionCountdown() {
  const { expiresAt } = useSession();
  // A tick counter forces a re-render once a minute; the remaining minutes are derived during render.
  const [, setTick] = useState(0);

  // Why an effect: an interval is an external system that must be cleared on unmount.
  useEffect(() => {
    const handle = setInterval(() => setTick((t) => t + 1), 60_000);
    return () => clearInterval(handle);
  }, []);

  const minutesLeft = minutesUntil(expiresAt);

  if (minutesLeft === null || minutesLeft > appConfig.sessionWarningMinutes) return null;

  return (
    <Alert severity="warning" sx={{ mb: 2 }}>
      {minutesLeft <= 0
        ? 'Your session has reached its time limit. You will be asked for your password on the next action.'
        : `Your session ends in ${minutesLeft} minute${minutesLeft === 1 ? '' : 's'}. Save your work; you will then be asked for your password.`}
    </Alert>
  );
}
