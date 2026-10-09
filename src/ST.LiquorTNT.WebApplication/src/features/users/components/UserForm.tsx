import { zodResolver } from '@hookform/resolvers/zod';
import Grid from '@mui/material/Grid';
import { useEffect } from 'react';
import { FormProvider, useForm, useWatch } from 'react-hook-form';

import { useCurrentUser } from '@/core/auth';
import { applyServerErrors, describeError } from '@/core/errors';

import { FormActions, FormCheckbox, FormRootError, FormSelect, FormTextField } from '@/shared/components/forms';
import type { Option } from '@/shared/types';

import { useCompanyOptions, useRoles, useSupplierCodeOptions } from '../api/users.queries';
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
  readOnly?: boolean;
  onSubmit: (values: UpdateUserFormValues) => Promise<void>;
  onCancel: () => void;
  busy: boolean;
}

type UserFormProps = CreateProps | EditProps;

export function UserForm(props: UserFormProps) {
  return props.mode === 'create' ? <CreateForm {...props} /> : <EditForm {...props} />;
}

function CreateForm({ onSubmit, onCancel, busy }: CreateProps) {
  const me = useCurrentUser();
  const form = useForm<CreateUserFormValues>({
    resolver: zodResolver(createUserSchema),
    defaultValues: {
      userName: '',
      password: '',
      companyId: me.isSuperAdmin ? null : (me.activeSupplierCode?.companyId ?? me.companyId),
      roleId: null as unknown as number,
      supplierCodeId: me.activeSupplierCode?.id ?? null,
      fullName: '',
      email: '',
      phone: '',
      employeeCode: '',
      forcePasswordChange: true,
    },
  });

  const companyId = useWatch({ control: form.control, name: 'companyId' });
  const companies = useCompanyOptions(me.isSuperAdmin);
  const allSupplierCodes = useSupplierCodeOptions(me.isSuperAdmin);
  const roles = useRoles();

  const companyOptions: Option<number>[] = (companies.data ?? []).map((c) => ({
    value: c.id,
    label: c.companyName ?? `Company ${c.id}`,
  }));

  const supplierCodes = me.isSuperAdmin
    ? (allSupplierCodes.data ?? []).filter((s) => s.companyId === companyId)
    : me.supplierCodes;
  const supplierCodeOptions: Option<number>[] = supplierCodes.map((s) => ({ value: s.id, label: s.displayName }));

  const roleOptions: Option<number>[] = (roles.data ?? [])
    .filter((r) => r.isActive && !r.isSystem && !r.isTemplate)
    .filter((r) => !me.isSuperAdmin || r.companyId === companyId)
    .map((r) => ({ value: r.id, label: r.roleName }));

  useEffect(() => {
    if (!me.isSuperAdmin) return;
    form.resetField('roleId');
    form.setValue('supplierCodeId', null);
  }, [companyId, me.isSuperAdmin, form]);

  const submit = form.handleSubmit(async (values) => {
    if (me.isSuperAdmin && values.companyId === null) {
      form.setError('companyId', { type: 'required', message: 'Company is required.' });
      return;
    }
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
          <ProfileFields />
          {me.isSuperAdmin && (
            <Grid size={{ xs: 12, md: 4 }}>
              <FormSelect<CreateUserFormValues>
                name="companyId"
                label="Company"
                required
                options={companyOptions}
                allowEmpty
                emptyLabel="Select a company"
              />
            </Grid>
          )}
          <Grid size={{ xs: 12, md: 4 }}>
            <FormSelect<CreateUserFormValues>
              name="roleId"
              label="Role"
              required
              options={roleOptions}
              disabled={me.isSuperAdmin && companyId === null}
              helperText={roles.isError ? 'Roles could not be loaded.' : undefined}
            />
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <FormSelect<CreateUserFormValues>
              name="supplierCodeId"
              label="Supplier code"
              options={supplierCodeOptions}
              allowEmpty
              emptyLabel="All supplier codes"
              disabled={me.isSuperAdmin && companyId === null}
            />
          </Grid>
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

function EditForm({ defaultValues, readOnly = false, onSubmit, onCancel, busy }: EditProps) {
  const form = useForm<UpdateUserFormValues>({ resolver: zodResolver(updateUserSchema), defaultValues });

  const submit = form.handleSubmit(async (values) => {
    if (readOnly) return;
    try {
      await onSubmit(values);
    } catch (error) {
      if (applyServerErrors(form.setError, error)) return;
      form.setError('root.submit', { type: 'server', message: describeError(error) });
    }
  });

  return (
    <FormProvider {...form}>
      <form onSubmit={submit} noValidate aria-readonly={readOnly || undefined}>
        <FormRootError />
        <fieldset disabled={readOnly} style={{ border: 0, padding: 0, margin: 0, minWidth: 0 }}>
          <Grid container spacing={2}>
            <ProfileFields />
          </Grid>
        </fieldset>
        {!readOnly && <FormActions submitLabel="Save changes" onCancel={onCancel} busy={busy} />}
      </form>
    </FormProvider>
  );
}

function ProfileFields() {
  return (
    <>
      <Grid size={{ xs: 12, md: 6 }}>
        <FormTextField<UpdateUserFormValues> name="fullName" label="Full name" autoComplete="off" />
      </Grid>
      <Grid size={{ xs: 12, md: 6 }}>
        <FormTextField<UpdateUserFormValues> name="email" label="E-mail" type="email" autoComplete="off" />
      </Grid>
      <Grid size={{ xs: 12, md: 6 }}>
        <FormTextField<UpdateUserFormValues> name="phone" label="Phone" autoComplete="off" />
      </Grid>
      <Grid size={{ xs: 12, md: 6 }}>
        <FormTextField<UpdateUserFormValues> name="employeeCode" label="Employee code" autoComplete="off" />
      </Grid>
    </>
  );
}
