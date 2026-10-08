import { zodResolver } from '@hookform/resolvers/zod';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';
import Step from '@mui/material/Step';
import StepLabel from '@mui/material/StepLabel';
import Stepper from '@mui/material/Stepper';
import Typography from '@mui/material/Typography';
import { useMutation } from '@tanstack/react-query';
import { useState } from 'react';
import { FormProvider, useForm } from 'react-hook-form';
import { useNavigate } from 'react-router';

import { applyServerErrors, describeError } from '@/core/errors';
import { PATHS } from '@/core/router';

import { FormRootError, FormTextField } from '@/shared/components/forms';
import { useSnackbar } from '@/shared/hooks';

import { authFeatureApi } from '../api/auth-feature.api';
import {
  type ForgotResetFormValues,
  type ForgotStartFormValues,
  type ForgotVerifyFormValues,
  forgotResetSchema,
  forgotStartSchema,
  forgotVerifySchema,
} from '../auth.schema';
import { AuthCard } from '../components/AuthCard';
import { BackToLoginLink } from '../components/BackToLoginLink';

const STEPS = ['User name', 'Security question', 'New password'];

export default function ForgotPasswordPage() {
  const [step, setStep] = useState(0);
  const [requestToken, setRequestToken] = useState<string | null>(null);
  const [questionText, setQuestionText] = useState<string>('');
  const navigate = useNavigate();
  const snackbar = useSnackbar();

  return (
    <AuthCard title="Forgot password">
      <Stepper activeStep={step} sx={{ mb: 3 }}>
        {STEPS.map((label) => (
          <Step key={label}>
            <StepLabel>{label}</StepLabel>
          </Step>
        ))}
      </Stepper>

      {step === 0 && (
        <StartStep
          onDone={(token, question) => {
            setRequestToken(token);
            setQuestionText(question);
            setStep(1);
          }}
        />
      )}
      {step === 1 && requestToken && (
        <VerifyStep requestToken={requestToken} questionText={questionText} onDone={() => setStep(2)} />
      )}
      {step === 2 && requestToken && (
        <ResetStep
          requestToken={requestToken}
          onDone={(message) => {
            snackbar.success(message);
            void navigate(PATHS.login, { replace: true });
          }}
        />
      )}

      <BackToLoginLink />
    </AuthCard>
  );
}

function StartStep({ onDone }: { onDone: (requestToken: string, questionText: string) => void }) {
  const form = useForm<ForgotStartFormValues>({
    resolver: zodResolver(forgotStartSchema),
    defaultValues: { userName: '' },
  });
  const start = useMutation({
    mutationKey: ['auth', 'forgot', 'start'],
    meta: { silent: true },
    mutationFn: authFeatureApi.forgotPasswordStart,
  });

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      const r = await start.mutateAsync({ userName: values.userName });
      onDone(r.requestToken, r.questionText);
    } catch (error) {
      if (applyServerErrors(form.setError, error)) return;
      form.setError('root.submit', { type: 'server', message: describeError(error) });
    }
  });

  return (
    <FormProvider {...form}>
      <form onSubmit={onSubmit} noValidate>
        <FormRootError />
        <Stack spacing={2}>
          <FormTextField<ForgotStartFormValues>
            name="userName"
            label="User name"
            autoComplete="username"
            autoFocus
            required
          />
          <Button type="submit" variant="contained" disabled={start.isPending}>
            Continue
          </Button>
        </Stack>
      </form>
    </FormProvider>
  );
}

function VerifyStep({
  requestToken,
  questionText,
  onDone,
}: {
  requestToken: string;
  questionText: string;
  onDone: () => void;
}) {
  const form = useForm<ForgotVerifyFormValues>({
    resolver: zodResolver(forgotVerifySchema),
    defaultValues: { answer: '' },
  });
  const verify = useMutation({
    mutationKey: ['auth', 'forgot', 'verify'],
    meta: { silent: true },
    mutationFn: authFeatureApi.forgotPasswordVerify,
  });

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      await verify.mutateAsync({ requestToken, answer: values.answer });
      onDone();
    } catch (error) {
      if (applyServerErrors(form.setError, error)) return;
      form.setError('root.submit', { type: 'server', message: describeError(error) });
    }
  });

  return (
    <FormProvider {...form}>
      <form onSubmit={onSubmit} noValidate>
        <FormRootError />
        <Stack spacing={2}>
          <Typography variant="body1">{questionText}</Typography>
          <FormTextField<ForgotVerifyFormValues>
            name="answer"
            label="Your answer"
            autoComplete="off"
            autoFocus
            required
          />
          <Alert severity="info">Wrong answers count as failed logins and can lock the account.</Alert>
          <Button type="submit" variant="contained" disabled={verify.isPending}>
            Verify
          </Button>
        </Stack>
      </form>
    </FormProvider>
  );
}

function ResetStep({ requestToken, onDone }: { requestToken: string; onDone: (message: string) => void }) {
  const form = useForm<ForgotResetFormValues>({
    resolver: zodResolver(forgotResetSchema),
    defaultValues: { newPassword: '', confirmPassword: '' },
  });
  const reset = useMutation({
    mutationKey: ['auth', 'forgot', 'reset'],
    meta: { silent: true },
    mutationFn: authFeatureApi.forgotPasswordReset,
  });

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      const r = await reset.mutateAsync({ requestToken, newPassword: values.newPassword });
      onDone(r.message);
    } catch (error) {
      if (applyServerErrors(form.setError, error, { rename: { password: 'newPassword' } })) return;
      form.setError('root.submit', { type: 'server', message: describeError(error) });
    }
  });

  return (
    <FormProvider {...form}>
      <form onSubmit={onSubmit} noValidate>
        <FormRootError />
        <Stack spacing={2}>
          <FormTextField<ForgotResetFormValues>
            name="newPassword"
            label="New password"
            type="password"
            autoComplete="new-password"
            autoFocus
            required
          />
          <FormTextField<ForgotResetFormValues>
            name="confirmPassword"
            label="Confirm new password"
            type="password"
            autoComplete="new-password"
            required
          />
          <Button type="submit" variant="contained" disabled={reset.isPending}>
            Set password
          </Button>
        </Stack>
      </form>
    </FormProvider>
  );
}
