import Alert from '@mui/material/Alert';
import { useEffect, useState } from 'react';

import { useSession } from '@/core/auth';
import { appConfig } from '@/core/config';

import { minutesUntil } from '@/shared/utils';

export function SessionCountdown() {
  const { expiresAt } = useSession();
  const [, setTick] = useState(0);

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
