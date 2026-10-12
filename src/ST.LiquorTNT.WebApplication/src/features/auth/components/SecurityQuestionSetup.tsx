import { zodResolver } from '@hookform/resolvers/zod';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';
import { useMutation, useQuery } from '@tanstack/react-query';
import { FormProvider, useForm } from 'react-hook-form';

import { authApi, loadAccess, NO_ACCESS, useLogout, useSessionStore } from '@/core/auth';
import { applyServerErrors, describeError, isApiError } from '@/core/errors';
import { logger } from '@/core/logging';

import { FormRootError, FormSelect, FormTextField } from '@/shared/components/forms';
import { useSnackbar } from '@/shared/hooks';
import type { Option } from '@/shared/types';

import { authFeatureApi } from '../api/auth-feature.api';
import {
  type SecurityQuestionFormInput,
  type SecurityQuestionFormValues,
  securityQuestionSchema,
} from '../auth.schema';

import { AuthCard } from './AuthCard';

export function SecurityQuestionSetup() {
  const snackbar = useSnackbar();
  const setAuthenticated = useSessionStore((s) => s.setAuthenticated);
  const logout = useLogout();

  const questions = useQuery({
    queryKey: ['auth', 'securityQuestions'],
    queryFn: authFeatureApi.securityQuestions,
    staleTime: Infinity,
    throwOnError: false,
  });

  const save = useMutation({
    mutationKey: ['auth', 'setMySecurityQuestion'],
    meta: { silent: true },
    mutationFn: authFeatureApi.setMySecurityQuestion,
  });

  const form = useForm<SecurityQuestionFormInput, unknown, SecurityQuestionFormValues>({
    resolver: zodResolver(securityQuestionSchema),
    defaultValues: { questionId: null, answer: '', currentPassword: '' },
  });

  const options: Option<number>[] = (questions.data ?? []).map((q) => ({ value: q.id, label: q.questionText }));

  const onSubmit = form.handleSubmit(async (values) => {
    if (values.questionId === null) return;
    try {
      const result = await save.mutateAsync({
        questionId: values.questionId,
        answer: values.answer,
        currentPassword: values.currentPassword,
      });
      const user = await authApi.me();
      const access = await loadAccess().catch((error: unknown) => {
        logger.warn('rights could not be loaded after setting the security question', { error });
        return NO_ACCESS;
      });
      setAuthenticated(user, undefined, access);
      snackbar.success(result.message);
    } catch (error) {
      form.resetField('currentPassword');
      if (applyServerErrors(form.setError, error, { fields: ['questionId', 'answer', 'currentPassword'] })) return;
      if (isApiError(error) && error.is('INVALID_CREDENTIALS')) {
        form.setError('currentPassword', { type: 'server', message: describeError(error) });
        return;
      }
      form.setError('root.submit', { type: 'server', message: describeError(error) });
    }
  });

  const busy = save.isPending || logout.isPending;

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        px: 2,
        py: 8,
        bgcolor: 'background.default',
      }}
    >
      <AuthCard title="Set a security question" subtitle="It is used to reset your password if you forget it.">
        <Alert severity="info" sx={{ mb: 2 }}>
          Choose a security question and answer before you continue.
        </Alert>
        <FormProvider {...form}>
          <form onSubmit={onSubmit} noValidate>
            <FormRootError />
            <Stack spacing={2}>
              <FormSelect<SecurityQuestionFormInput>
                name="questionId"
                label="Security question"
                required
                options={options}
                disabled={questions.isPending}
                helperText={
                  questions.isPending
                    ? 'Loading questions…'
                    : questions.isError
                      ? 'Questions could not be loaded.'
                      : undefined
                }
              />
              {questions.isError && (
                <Button size="small" onClick={() => void questions.refetch()} sx={{ alignSelf: 'flex-start' }}>
                  Retry
                </Button>
              )}
              <FormTextField<SecurityQuestionFormInput>
                name="answer"
                label="Answer"
                autoComplete="off"
                required
                slotProps={{ htmlInput: { maxLength: 200 } }}
              />
              <FormTextField<SecurityQuestionFormInput>
                name="currentPassword"
                label="Current password"
                type="password"
                autoComplete="current-password"
                required
              />
              <Button type="submit" variant="contained" size="large" disabled={busy}>
                {save.isPending ? 'Saving…' : 'Save and continue'}
              </Button>
              <Button variant="text" disabled={busy} onClick={() => logout.mutate()}>
                Sign out
              </Button>
            </Stack>
          </form>
        </FormProvider>
      </AuthCard>
    </Box>
  );
}
