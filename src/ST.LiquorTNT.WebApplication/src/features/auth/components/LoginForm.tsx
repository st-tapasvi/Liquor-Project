import { zodResolver } from '@hookform/resolvers/zod';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Link from '@mui/material/Link';
import Stack from '@mui/material/Stack';
import { useMutation } from '@tanstack/react-query';
import { useState } from 'react';
import { FormProvider, useForm } from 'react-hook-form';
import { Link as RouterLink, useNavigate } from 'react-router';

import type { LoginRequest } from '@/core/api';
import { authApi, loadAccess, type SessionAccess, useSessionStore } from '@/core/auth';
import {
  applyServerErrors,
  isApiError,
  isNetworkError,
  isSessionLimitError,
  type SessionLimitError,
} from '@/core/errors';
import { logger } from '@/core/logging';
import { PATHS } from '@/core/router';

import { FormTextField } from '@/shared/components/forms';
import { useSnackbar } from '@/shared/hooks';

import { type LoginFormValues, loginSchema } from '../auth.schema';

import { SessionLimitPanel } from './SessionLimitPanel';

interface DeviceLimit {
  error: SessionLimitError;
  values: LoginFormValues;
}

const LOGIN_TOAST_ID = 'login';

async function accessAfterLogin(
  supplierCodes: SessionAccess['supplierCodes'],
  activeSupplierCode: SessionAccess['activeSupplierCode'],
): Promise<SessionAccess> {
  try {
    return await loadAccess();
  } catch (error) {
    logger.warn('rights could not be loaded after login', { error });
    return { isSuperAdmin: false, granted: [], activeSupplierCode, supplierCodes };
  }
}

export function LoginForm() {
  const navigate = useNavigate();
  const snackbar = useSnackbar();
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
      snackbar.dismiss(LOGIN_TOAST_ID);
      setAuthenticated(
        result.user,
        { expiresAt: result.expiresAt, idleTimeoutMinutes: result.idleTimeoutMinutes },
        await accessAfterLogin(result.supplierCodes, result.activeSupplierCode),
      );
      logger.info('login succeeded', { endedAnotherSession: endSessionId !== undefined });
    } catch (error) {
      if (isSessionLimitError(error)) {
        setLimit({ error, values });
        snackbar.dismiss(LOGIN_TOAST_ID);
        logger.info('login refused: device limit reached', { deviceCount: error.sessions.length });
        return;
      }

      setLimit(null);

      if (applyServerErrors(form.setError, error, { fields: ['userName', 'password'] })) return;

      if (isApiError(error) && (error.is('PASSWORD_CHANGE_REQUIRED') || error.is('PASSWORD_EXPIRED'))) {
        void navigate(PATHS.changePassword, { state: { userName: values.userName, reason: error.code } });
        return;
      }


      if (isNetworkError(error) && !error.isTimeout) {
        snackbar.dismiss(LOGIN_TOAST_ID);
        return;
      }

      form.resetField('password');
      snackbar.errorFrom(error, { id: LOGIN_TOAST_ID });
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
        <Stack spacing={2.5}>
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
        </Stack>

        <Box sx={{ display: 'flex', justifyContent: 'flex-end', mt: 1.125, mb: 2.25 }}>
          <Link component={RouterLink} to={PATHS.forgotPassword} variant="body2">
            Forgot password?
          </Link>
        </Box>

        <Button type="submit" variant="contained" fullWidth disabled={login.isPending}>
          {login.isPending ? 'Logging in…' : 'Log in'}
        </Button>
      </form>

      {limit !== null && (
        <SessionLimitPanel error={limit.error} onEndSession={handleEndSession} busySessionId={endingSessionId} />
      )}
    </FormProvider>
  );
}
