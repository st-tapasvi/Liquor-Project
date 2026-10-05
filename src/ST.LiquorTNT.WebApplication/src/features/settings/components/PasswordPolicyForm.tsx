import { zodResolver } from '@hookform/resolvers/zod';
import Grid from '@mui/material/Grid';
import Typography from '@mui/material/Typography';
import { FormProvider, useForm, useWatch } from 'react-hook-form';

import type { PasswordPolicyResponse } from '@/core/api';
import { applyServerErrors, describeError } from '@/core/errors';

import { FormActions, FormCheckbox, FormNumberField, FormRootError } from '@/shared/components/forms';
import { Section } from '@/shared/components/layout';

import { fromPolicy, type PasswordPolicyFormValues, passwordPolicySchema, toPolicyRequest } from '../settings.schema';

interface PasswordPolicyFormProps {
  policy: PasswordPolicyResponse;
  canEdit: boolean;
  busy: boolean;
  onSave: (id: number, values: PasswordPolicyFormValues) => Promise<void>;
}

export function PasswordPolicyForm({ policy, canEdit, busy, onSave }: PasswordPolicyFormProps) {
  const form = useForm<PasswordPolicyFormValues>({
    resolver: zodResolver(passwordPolicySchema),
    defaultValues: fromPolicy(policy),
  });
  const expiryOn = useWatch({ control: form.control, name: 'passwordExpiryEnabled' });

  const submit = form.handleSubmit(async (values) => {
    try {
      await onSave(policy.id, toPolicyRequest(values));
      form.reset(values);
    } catch (error) {
      if (applyServerErrors(form.setError, error)) return;
      form.setError('root.submit', { type: 'server', message: describeError(error) });
    }
  });

  return (
    <Section title={`Policy: ${policy.name}`}>
      <FormProvider {...form}>
        <form onSubmit={submit} noValidate aria-label={`Password policy ${policy.name}`}>
          <FormRootError />
          <fieldset disabled={!canEdit} style={{ border: 0, padding: 0, margin: 0, minWidth: 0 }}>
            <Grid container spacing={2}>
              <Grid size={{ xs: 6, md: 3 }}>
                <FormNumberField<PasswordPolicyFormValues> name="minLength" label="Minimum length" min={1} max={128} />
              </Grid>
              <Grid size={{ xs: 6, md: 3 }}>
                <FormNumberField<PasswordPolicyFormValues> name="maxLength" label="Maximum length" min={1} max={128} />
              </Grid>
              <Grid size={{ xs: 6, md: 3 }}>
                <FormNumberField<PasswordPolicyFormValues>
                  name="passwordHistoryCount"
                  label="Remember last N passwords"
                  min={0}
                  max={50}
                />
              </Grid>
              <Grid size={{ xs: 6, md: 3 }}>
                <FormNumberField<PasswordPolicyFormValues>
                  name="passwordExpiryDays"
                  label="Expires after (days)"
                  min={1}
                  disabled={!expiryOn}
                />
              </Grid>
              <Grid size={12}>
                <Typography variant="subtitle2" sx={{ mt: 1 }}>
                  Requirements
                </Typography>
              </Grid>
              <Grid size={{ xs: 6, md: 3 }}>
                <FormCheckbox<PasswordPolicyFormValues> name="requireUppercase" label="Uppercase letter" />
              </Grid>
              <Grid size={{ xs: 6, md: 3 }}>
                <FormCheckbox<PasswordPolicyFormValues> name="requireLowercase" label="Lowercase letter" />
              </Grid>
              <Grid size={{ xs: 6, md: 3 }}>
                <FormCheckbox<PasswordPolicyFormValues> name="requireNumber" label="Number" />
              </Grid>
              <Grid size={{ xs: 6, md: 3 }}>
                <FormCheckbox<PasswordPolicyFormValues> name="requireSpecialCharacter" label="Special character" />
              </Grid>
              <Grid size={{ xs: 12, md: 4 }}>
                <FormCheckbox<PasswordPolicyFormValues> name="passwordExpiryEnabled" label="Passwords expire" />
              </Grid>
              <Grid size={{ xs: 12, md: 4 }}>
                <FormCheckbox<PasswordPolicyFormValues>
                  name="allowUsernameInPassword"
                  label="Allow the user name inside the password"
                />
              </Grid>
              <Grid size={{ xs: 12, md: 4 }}>
                <FormCheckbox<PasswordPolicyFormValues> name="allowCommonPassword" label="Allow common passwords" />
              </Grid>
            </Grid>
            {canEdit && <FormActions submitLabel="Save policy" busy={busy} />}
          </fieldset>
        </form>
      </FormProvider>
    </Section>
  );
}
