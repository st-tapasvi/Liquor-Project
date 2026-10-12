import { zodResolver } from '@hookform/resolvers/zod';
import Button from '@mui/material/Button';
import Grid from '@mui/material/Grid';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import { FormProvider, useForm } from 'react-hook-form';

import type { UserResponse } from '@/core/api';
import { applyServerErrors, describeError } from '@/core/errors';

import { FormRootError, FormTextField } from '@/shared/components/forms';
import { Section } from '@/shared/components/layout';
import { useConfirm } from '@/shared/hooks';

import { useResetUserPassword } from '../api/users.mutations';
import { type ResetPasswordFormValues, resetPasswordSchema } from '../users.schema';

export function ResetPasswordSection({ user }: { user: UserResponse }) {
  const reset = useResetUserPassword(user.id);
  const confirm = useConfirm();
  const form = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { newPassword: '', confirmPassword: '' },
  });

  const submit = form.handleSubmit(async (values) => {
    const ok = await confirm({
      title: 'Set a temporary password?',
      message: `${user.userName} will be signed out everywhere. At the next login they must choose a new password and a new security question.`,
      confirmLabel: 'Set password',
    });
    if (!ok) return;
    try {
      await reset.mutateAsync({ newPassword: values.newPassword });
      form.reset();
    } catch (error) {
      if (applyServerErrors(form.setError, error, { rename: { password: 'newPassword' } })) return;
      form.setError('root.submit', { type: 'server', message: describeError(error) });
    }
  });

  return (
    <Section title="Reset password">
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        For a user who forgot their password. Give them this temporary password, their security question is cleared and
        they must set a new password and security question at the next login.
      </Typography>
      <FormProvider {...form}>
        <form onSubmit={submit} noValidate>
          <FormRootError />
          <Grid container spacing={2}>
            <Grid size={{ xs: 12, md: 6 }}>
              <FormTextField<ResetPasswordFormValues>
                name="newPassword"
                label="Temporary password"
                type="password"
                required
                autoComplete="new-password"
              />
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <FormTextField<ResetPasswordFormValues>
                name="confirmPassword"
                label="Confirm temporary password"
                type="password"
                required
                autoComplete="new-password"
              />
            </Grid>
          </Grid>
          <Stack direction="row" sx={{ mt: 2, justifyContent: 'flex-end' }}>
            <Button type="submit" variant="contained" disabled={reset.isPending}>
              {reset.isPending ? 'Saving…' : 'Set temporary password'}
            </Button>
          </Stack>
        </form>
      </FormProvider>
    </Section>
  );
}
