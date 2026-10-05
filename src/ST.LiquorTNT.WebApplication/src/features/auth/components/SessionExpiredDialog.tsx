import { zodResolver } from '@hookform/resolvers/zod';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogContentText from '@mui/material/DialogContentText';
import DialogTitle from '@mui/material/DialogTitle';
import Stack from '@mui/material/Stack';
import { useMutation } from '@tanstack/react-query';
import { FormProvider, useForm } from 'react-hook-form';

import { authApi, useReauthStore, useSession, useSessionStore } from '@/core/auth';
import { ApiError, applyServerErrors, describeError } from '@/core/errors';
import { logger } from '@/core/logging';

import { FormRootError, FormTextField } from '@/shared/components/forms';

import { type ReauthFormValues, reauthSchema } from '../auth.schema';

/**
 * Opens when a request answered 401 SESSION_EXPIRED (the 24 h hard limit). The user re-enters the
 * password in place; the API opens a new session (new cookie) and every parked request is retried.
 * Nothing on the screen is lost. Cancelling ends the session and shows the login page.
 */
export function SessionExpiredDialog() {
  const isOpen = useReauthStore((s) => s.isOpen);
  const resolve = useReauthStore((s) => s.resolve);
  const reject = useReauthStore((s) => s.reject);
  const { user } = useSession();
  const renew = useSessionStore((s) => s.renew);
  const setAnonymous = useSessionStore((s) => s.setAnonymous);

  const form = useForm<ReauthFormValues>({ resolver: zodResolver(reauthSchema), defaultValues: { password: '' } });

  const login = useMutation({
    mutationKey: ['auth', 'reauth'],
    meta: { silent: true },
    mutationFn: (password: string) => authApi.login({ userName: user?.userName ?? '', password }, { forReauth: true }),
  });

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      const result = await login.mutateAsync(values.password);
      renew({ expiresAt: result.expiresAt, idleTimeoutMinutes: result.idleTimeoutMinutes });
      form.reset();
      logger.info('session renewed after hard limit');
      resolve();
    } catch (error) {
      if (applyServerErrors(form.setError, error)) return;
      form.resetField('password');
      form.setError('root.submit', { type: 'server', message: describeError(error) });
    }
  });

  const handleCancel = () => {
    form.reset();
    reject(
      new ApiError({
        status: 401,
        code: 'SESSION_EXPIRED',
        title: 'Session expired',
        detail: 'Re-authentication was cancelled.',
      }),
    );
    setAnonymous('expired');
  };

  return (
    <Dialog open={isOpen} aria-labelledby="reauth-title" maxWidth="xs" fullWidth>
      <FormProvider {...form}>
        <form onSubmit={onSubmit} noValidate>
          <DialogTitle id="reauth-title">Confirm your password</DialogTitle>
          <DialogContent>
            <DialogContentText sx={{ mb: 2 }}>
              For security, your session has reached its time limit. Enter your password to continue where you left off.
            </DialogContentText>
            <FormRootError />
            <Stack spacing={2}>
              <FormTextField<ReauthFormValues>
                name="password"
                label={`Password for ${user?.userName ?? ''}`}
                type="password"
                autoComplete="current-password"
                autoFocus
                required
              />
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button type="button" onClick={handleCancel} disabled={login.isPending}>
              Sign out
            </Button>
            <Button type="submit" variant="contained" disabled={login.isPending}>
              Continue
            </Button>
          </DialogActions>
        </form>
      </FormProvider>
    </Dialog>
  );
}
