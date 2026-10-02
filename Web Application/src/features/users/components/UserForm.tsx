import { zodResolver } from '@hookform/resolvers/zod';
import Grid from '@mui/material/Grid';
import { FormProvider, useForm } from 'react-hook-form';

import { applyServerErrors, describeError } from '@/core/errors';

import { FormActions, FormCheckbox, FormNumberField, FormRootError, FormTextField } from '@/shared/components/forms';

import {
  type CreateUserFormValues,
  createUserSchema,
  type UpdateUserFormValues,
  updateUserSchema,
} from '../users.schema';

interface CreateProps {
  mode: 'create';
  onSubmit: (values: CreateUserFormValues) => Promise<void>;
  onCancel: () => void;
  busy: boolean;
}

interface EditProps {
  mode: 'edit';
  defaultValues: UpdateUserFormValues;
  onSubmit: (values: UpdateUserFormValues) => Promise<void>;
  onCancel: () => void;
  busy: boolean;
}

type UserFormProps = CreateProps | EditProps;

/**
 * Create and edit share one layout. Role and company are plain numbers until the Roles / Company
 * modules exist and provide select components in entities/.
 */
export function UserForm(props: UserFormProps) {
  return props.mode === 'create' ? <CreateForm {...props} /> : <EditForm {...props} />;
}

function CreateForm({ onSubmit, onCancel, busy }: CreateProps) {
  const form = useForm<CreateUserFormValues>({
    resolver: zodResolver(createUserSchema),
    defaultValues: {
      userName: '',
      password: '',
      roleId: null as unknown as number,
      companyId: null,
      fullName: '',
      email: '',
      phone: '',
      employeeCode: '',
      forcePasswordChange: true,
    },
  });

  const submit = form.handleSubmit(async (values) => {
    try {
      await onSubmit(values);
    } catch (error) {
      if (applyServerErrors(form.setError, error)) return;
      form.setError('root.submit', { type: 'server', message: describeError(error) });
    }
  });

  return (
    <FormProvider {...form}>
      <form onSubmit={submit} noValidate>
        <FormRootError />
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, md: 6 }}>
            <FormTextField<CreateUserFormValues>
              name="userName"
              label="User name"
              required
              autoComplete="off"
              autoFocus
            />
          </Grid>
          <Grid size={{ xs: 12, md: 6 }}>
            <FormTextField<CreateUserFormValues>
              name="password"
              label="Temporary password"
              type="password"
              required
              autoComplete="new-password"
            />
          </Grid>
          <CommonFields />
          <Grid size={12}>
            <FormCheckbox<CreateUserFormValues>
              name="forcePasswordChange"
              label="User must set their own password at first login"
            />
          </Grid>
        </Grid>
        <FormActions submitLabel="Create user" onCancel={onCancel} busy={busy} />
      </form>
    </FormProvider>
  );
}

function EditForm({ defaultValues, onSubmit, onCancel, busy }: EditProps) {
  const form = useForm<UpdateUserFormValues>({ resolver: zodResolver(updateUserSchema), defaultValues });

  const submit = form.handleSubmit(async (values) => {
    try {
      await onSubmit(values);
    } catch (error) {
      if (applyServerErrors(form.setError, error)) return;
      form.setError('root.submit', { type: 'server', message: describeError(error) });
    }
  });

  return (
    <FormProvider {...form}>
      <form onSubmit={submit} noValidate>
        <FormRootError />
        <Grid container spacing={2}>
          <CommonFields />
        </Grid>
        <FormActions submitLabel="Save changes" onCancel={onCancel} busy={busy} />
      </form>
    </FormProvider>
  );
}

/** Fields shared by create and edit. UpdateUserFormValues is a subset of CreateUserFormValues, so the typing holds for both. */
function CommonFields() {
  return (
    <>
      <Grid size={{ xs: 12, md: 6 }}>
        <FormTextField<UpdateUserFormValues> name="fullName" label="Full name" autoComplete="off" />
      </Grid>
      <Grid size={{ xs: 12, md: 6 }}>
        <FormTextField<UpdateUserFormValues> name="email" label="E-mail" type="email" autoComplete="off" />
      </Grid>
      <Grid size={{ xs: 12, md: 4 }}>
        <FormTextField<UpdateUserFormValues> name="phone" label="Phone" autoComplete="off" />
      </Grid>
      <Grid size={{ xs: 12, md: 4 }}>
        <FormTextField<UpdateUserFormValues> name="employeeCode" label="Employee code" autoComplete="off" />
      </Grid>
      <Grid size={{ xs: 6, md: 2 }}>
        <FormNumberField<UpdateUserFormValues> name="roleId" label="Role id" required min={1} />
      </Grid>
      <Grid size={{ xs: 6, md: 2 }}>
        <FormNumberField<UpdateUserFormValues> name="companyId" label="Company id" min={1} />
      </Grid>
    </>
  );
}
