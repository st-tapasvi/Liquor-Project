import Alert from '@mui/material/Alert';
import AlertTitle from '@mui/material/AlertTitle';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';

import type { UserResponse } from '@/core/api';
import { Can } from '@/core/auth';

import { formatDateTime, minutesUntil } from '@/shared/utils';

import { useUnlockUser } from '../api/users.mutations';

import { isUserLocked } from './user-status';

export function UserLockNotice({ user }: { user: UserResponse }) {
  const unlock = useUnlockUser();
  const locked = isUserLocked(user);
  if (!user.isBlocked && !locked) return null;

  const minutesLeft = minutesUntil(user.lockedUntil) ?? 0;

  return (
    <Alert
      severity={user.isBlocked ? 'error' : 'warning'}
      sx={{ mb: 2 }}
      action={
        <Can right="user.unlock">
          <Button color="inherit" size="small" disabled={unlock.isPending} onClick={() => unlock.mutate(user.id)}>
            {unlock.isPending ? 'Unlocking…' : 'Unlock'}
          </Button>
        </Can>
      }
    >
      <AlertTitle>{user.isBlocked ? 'Account blocked' : 'Account locked'}</AlertTitle>
      {user.isBlocked && (
        <Typography variant="body2">This account is blocked and cannot log in until it is unlocked.</Typography>
      )}
      {locked && (
        <>
          <Typography variant="body2">
            {`Locked after ${user.failedLoginAttempts} wrong password${user.failedLoginAttempts === 1 ? '' : 's'}.`}
          </Typography>
          {user.lastFailedLoginAt && (
            <Typography variant="body2">{`Locked at: ${formatDateTime(user.lastFailedLoginAt)}`}</Typography>
          )}
          <Typography variant="body2">
            {`Locked until: ${formatDateTime(user.lockedUntil)}${
              minutesLeft > 0 ? ` (${minutesLeft} min left)` : ' (less than a minute left)'
            }`}
          </Typography>
        </>
      )}
    </Alert>
  );
}
