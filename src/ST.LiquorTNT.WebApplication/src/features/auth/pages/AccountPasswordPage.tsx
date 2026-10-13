import { zodResolver } from '@hookform/resolvers/zod';
import Alert from '@mui/material/Alert';
import Grid from '@mui/material/Grid';
import { useMutation } from '@tanstack/react-query';
import { FormProvider, useForm } from 'react-hook-form';
import { useNavigate } from 'react-router';

import { useCurrentUser, useSessionStore } from '@/core/auth';
import { applyServerErrors, describeError } from '@/core/errors';
import { PATHS } from '@/core/router';

import { FormActions, FormRootError, FormTextField } from '@/shared/components/forms';
import { Section } from '@/shared/components/layout';
import { PageHeader } from '@/shared/components/ui';
import { useConfirm, useSnackbar } from '@/shared/hooks';

import { authFeatureApi } from '../api/auth-feature.api';
import { type ChangePasswordFormValues, changePasswordSchema } from '../auth.schema';

export default function AccountPasswordPage() {
  const user = useCurrentUser();
  const navigate = useNavigate();
  const snackbar = useSnackbar();
  const confirm = useConfirm();
  const setAnonymous = useSessionStore((s) => s.setAnonymous);

  const form = useForm<ChangePasswordFormValues>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: { userName: user.userName, currentPassword: '', newPassword: '', confirmPassword: '' },
  });

  const change = useMutation({
    mutationKey: ['auth', 'changeMyPassword'],
    meta: { silent: true },
    mutationFn: authFeatureApi.changePassword,
  });

  const onSubmit = form.handleSubmit(async (values) => {
    const ok = await confirm({
      title: 'Change your password?',
      message: 'You will be signed out on every device and must log in again with the new password.',
      confirmLabel: 'Change password',
    });
    if (!ok) return;
    try {
      const result = await change.mutateAsync({
        userName: user.userName,
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
      });
      snackbar.success(result.message);
      setAnonymous(null);
    } catch (error) {
      form.resetField('currentPassword');
      if (applyServerErrors(form.setError, error, { rename: { password: 'newPassword' } })) return;
      form.setError('root.submit', { type: 'server', message: describeError(error) });
    }
  });

  return (
    <>
      <PageHeader title="Change password" subtitle={`Set a new password for ${user.userName}.`} />
      <Section>
        <Alert severity="info" sx={{ mb: 2 }}>
          After the change you are signed out on every device and log in again with the new password.
        </Alert>
        <FormProvider {...form}>
          <form onSubmit={onSubmit} noValidate>
            <FormRootError />
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, md: 6 }}>
                <FormTextField<ChangePasswordFormValues>
                  name="currentPassword"
                  label="Current password"
                  type="password"
                  autoComplete="current-password"
                  required
                  autoFocus
                />
              </Grid>
              <Grid size={{ xs: 12, md: 6 }} />
              <Grid size={{ xs: 12, md: 6 }}>
                <FormTextField<ChangePasswordFormValues>
                  name="newPassword"
                  label="New password"
                  type="password"
                  autoComplete="new-password"
                  required
                />
              </Grid>
              <Grid size={{ xs: 12, md: 6 }}>
                <FormTextField<ChangePasswordFormValues>
                  name="confirmPassword"
                  label="Confirm new password"
                  type="password"
                  autoComplete="new-password"
                  required
                />
              </Grid>
            </Grid>
            <FormActions
              submitLabel="Change password"
              onCancel={() => void navigate(PATHS.dashboard)}
              busy={change.isPending}
            />
          </form>
        </FormProvider>
      </Section>
    </>
  );
}
