import { zodResolver } from '@hookform/resolvers/zod';
import Button from '@mui/material/Button';
import Link from '@mui/material/Link';
import Stack from '@mui/material/Stack';
import { useMutation } from '@tanstack/react-query';
import { useState } from 'react';
import { FormProvider, useForm } from 'react-hook-form';
import { Link as RouterLink, useNavigate } from 'react-router';

import type { LoginRequest } from '@/core/api';
import { authApi, useSessionStore } from '@/core/auth';
import {
  applyServerErrors,
  describeError,
  isApiError,
  isSessionLimitError,
  type SessionLimitError,
} from '@/core/errors';
import { logger } from '@/core/logging';
import { PATHS } from '@/core/router';

import { FormRootError, FormTextField } from '@/shared/components/forms';

import { type LoginFormValues, loginSchema } from '../auth.schema';

import { SessionLimitPanel } from './SessionLimitPanel';

/**
 * The device limit is kept with the credentials that produced it, so "sign out and continue" replays
 * exactly the login the server already accepted — editing the form afterwards cannot send the chosen
 * session id under a different user name.
 */
interface DeviceLimit {
  error: SessionLimitError;
  values: LoginFormValues;
}

export function LoginForm() {
  const navigate = useNavigate();
  const setAuthenticated = useSessionStore((s) => s.setAuthenticated);

  const [limit, setLimit] = useState<DeviceLimit | null>(null);
  const [endingSessionId, setEndingSessionId] = useState<number | null>(null);

  const form = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { userName: '', password: '' },
  });

  const login = useMutation({
    mutationKey: ['auth', 'login'],
    meta: { silent: true },
    mutationFn: (request: LoginRequest) => authApi.login(request),
  });

  const submit = async (values: LoginFormValues, endSessionId?: number) => {
    const request: LoginRequest = {
      userName: values.userName,
      password: values.password,
      ...(endSessionId === undefined ? {} : { endSessionId }),
    };

    try {
      const result = await login.mutateAsync(request);
      setLimit(null);
      setAuthenticated(result.user, { expiresAt: result.expiresAt, idleTimeoutMinutes: result.idleTimeoutMinutes });
      logger.info('login succeeded', { endedAnotherSession: endSessionId !== undefined });
      // No navigate here: RequireAnonymous (AuthLayout) reacts to the session and sends the user on.
    } catch (error) {
      if (isSessionLimitError(error)) {
        // Keep the password: the panel's buttons re-submit this same login with a session to end.
        setLimit({ error, values });
        form.clearErrors('root.submit');
        logger.info('login refused: device limit reached', { deviceCount: error.sessions.length });
        return;
      }

      setLimit(null);

      if (applyServerErrors(form.setError, error, { fields: ['userName', 'password'] })) return;

      if (isApiError(error) && (error.is('PASSWORD_CHANGE_REQUIRED') || error.is('PASSWORD_EXPIRED'))) {
        void navigate(PATHS.changePassword, { state: { userName: values.userName, reason: error.code } });
        return;
      }

      form.resetField('password');
      form.setError('root.submit', { type: 'server', message: describeError(error) });
    } finally {
      setEndingSessionId(null);
    }
  };

  const onSubmit = form.handleSubmit(async (values) => {
    await submit(values);
  });

  const handleEndSession = (sessionId: number) => {
    if (limit === null) return;
    setEndingSessionId(sessionId);
    void submit(limit.values, sessionId);
  };

  return (
    <FormProvider {...form}>
      <form onSubmit={onSubmit} noValidate>
        <FormRootError />
        <Stack spacing={2}>
          <FormTextField<LoginFormValues>
            name="userName"
            label="User name"
            autoComplete="username"
            autoFocus
            required
          />
          <FormTextField<LoginFormValues>
            name="password"
            label="Password"
            type="password"
            autoComplete="current-password"
            required
          />
          <Button type="submit" variant="contained" size="large" disabled={login.isPending}>
            {login.isPending ? 'Signing in…' : 'Sign in'}
          </Button>
          <Link component={RouterLink} to={PATHS.forgotPassword} variant="body2" sx={{ alignSelf: 'center' }}>
            Forgot password?
          </Link>
        </Stack>
      </form>

      {limit !== null && (
        <SessionLimitPanel error={limit.error} onEndSession={handleEndSession} busySessionId={endingSessionId} />
      )}
    </FormProvider>
  );
}
