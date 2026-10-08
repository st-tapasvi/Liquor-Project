import { zodResolver } from '@hookform/resolvers/zod';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';
import { useMutation } from '@tanstack/react-query';
import { FormProvider, useForm } from 'react-hook-form';
import { useLocation, useNavigate } from 'react-router';

import { applyServerErrors, describeError } from '@/core/errors';
import { PATHS } from '@/core/router';

import { FormRootError, FormTextField } from '@/shared/components/forms';
import { useSnackbar } from '@/shared/hooks';

import { authFeatureApi } from '../api/auth-feature.api';
import { type ChangePasswordFormValues, changePasswordSchema } from '../auth.schema';
import { AuthCard } from '../components/AuthCard';
import { BackToLoginLink } from '../components/BackToLoginLink';

interface ChangePasswordState {
  userName?: string;
  reason?: 'PASSWORD_CHANGE_REQUIRED' | 'PASSWORD_EXPIRED';
}

export default function ChangePasswordPage() {
  const navigate = useNavigate();
  const state = (useLocation().state as ChangePasswordState | null) ?? {};
  const snackbar = useSnackbar();

  const form = useForm<ChangePasswordFormValues>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: { userName: state.userName ?? '', currentPassword: '', newPassword: '', confirmPassword: '' },
  });

  const change = useMutation({
    mutationKey: ['auth', 'changePassword'],
    meta: { silent: true },
    mutationFn: authFeatureApi.changePassword,
  });

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      const result = await change.mutateAsync({
        userName: values.userName,
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
      });
      snackbar.success(result.message);
      void navigate(PATHS.login, { replace: true });
    } catch (error) {
      // The API reports policy failures under "password"; the form field is "newPassword".
      if (applyServerErrors(form.setError, error, { rename: { password: 'newPassword' } })) return;
      form.setError('root.submit', { type: 'server', message: describeError(error) });
    }
  });

  return (
    <AuthCard title="Set a new password">
      {state.reason === 'PASSWORD_CHANGE_REQUIRED' && (
        <Alert severity="info" sx={{ mb: 2 }}>
          Your account was created with a temporary password. Choose your own password to continue.
        </Alert>
      )}
      {state.reason === 'PASSWORD_EXPIRED' && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          Your password has expired. Choose a new one to continue.
        </Alert>
      )}
      <FormProvider {...form}>
        <form onSubmit={onSubmit} noValidate>
          <FormRootError />
          <Stack spacing={2}>
            <FormTextField<ChangePasswordFormValues>
              name="userName"
              label="User name"
              autoComplete="username"
              required
              disabled={!!state.userName}
            />
            <FormTextField<ChangePasswordFormValues>
              name="currentPassword"
              label="Current password"
              type="password"
              autoComplete="current-password"
              required
              autoFocus
            />
            <FormTextField<ChangePasswordFormValues>
              name="newPassword"
              label="New password"
              type="password"
              autoComplete="new-password"
              required
            />
            <FormTextField<ChangePasswordFormValues>
              name="confirmPassword"
              label="Confirm new password"
              type="password"
              autoComplete="new-password"
              required
            />
            <Button type="submit" variant="contained" size="large" disabled={change.isPending}>
              Change password
            </Button>
          </Stack>
          <BackToLoginLink />
        </form>
      </FormProvider>
    </AuthCard>
  );
}
