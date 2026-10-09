import { zodResolver } from '@hookform/resolvers/zod';
import Grid from '@mui/material/Grid';
import { useEffect, useState } from 'react';
import { Controller, FormProvider, useForm, useWatch } from 'react-hook-form';

import { useCurrentUser } from '@/core/auth';
import { applyServerErrors, describeError } from '@/core/errors';

import { FormActions, FormCheckbox, FormRootError, FormSelect, FormTextField } from '@/shared/components/forms';
import type { Option } from '@/shared/types';

import { useCompanyOptions, useRoles } from '../api/users.queries';
import {
  type CreateUserFormValues,
  createUserSchema,
  type UpdateUserFormValues,
  updateUserSchema,
} from '../users.schema';

import { assignableRoles, roleLabel } from './role-options';
import { RoleCheckboxList } from './RoleCheckboxList';
import { type UserTab, UserTabPanel, UserTabs } from './UserTabs';

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
      roleIds: [],
      fullName: '',
      email: '',
      phone: '',
      employeeCode: '',
      forcePasswordChange: true,
    },
  });

  const [tab, setTab] = useState<UserTab>('details');
  const companyId = useWatch({ control: form.control, name: 'companyId' });
  const companies = useCompanyOptions(me.isSuperAdmin);
  const roles = useRoles();

  const companyOptions: Option<number>[] = (companies.data ?? []).map((c) => ({
    value: c.id,
    label: c.companyName ?? `Company ${c.id}`,
  }));

  const roleOptions = assignableRoles(roles.data ?? [], me.isSuperAdmin ? companyId : undefined).map((r) => ({
    id: r.id,
    label: roleLabel(r),
  }));

  useEffect(() => {
    if (!me.isSuperAdmin) return;
    form.setValue('roleIds', []);
  }, [companyId, me.isSuperAdmin, form]);

  const submit = form.handleSubmit(
    async (values) => {
      if (me.isSuperAdmin && values.companyId === null) {
        form.setError('companyId', { type: 'required', message: 'Company is required.' });
        setTab('details');
        return;
      }
      try {
        await onSubmit(values);
      } catch (error) {
        if (applyServerErrors(form.setError, error)) return;
        form.setError('root.submit', { type: 'server', message: describeError(error) });
      }
    },
    (errors) => setTab(errors.roleIds && Object.keys(errors).length === 1 ? 'roles' : 'details'),
  );

  return (
    <FormProvider {...form}>
      <form onSubmit={submit} noValidate>
        <FormRootError />
        <UserTabs value={tab} onChange={setTab} />
        <UserTabPanel tab="details" value={tab}>
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
              <Grid size={{ xs: 12, md: 6 }}>
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
            <Grid size={12}>
              <FormCheckbox<CreateUserFormValues>
                name="forcePasswordChange"
                label="User must set their own password at first login"
              />
            </Grid>
          </Grid>
        </UserTabPanel>
        <UserTabPanel tab="roles" value={tab}>
          <Controller
            name="roleIds"
            control={form.control}
            render={({ field, fieldState }) => (
              <RoleCheckboxList
                options={roleOptions}
                value={field.value}
                onChange={field.onChange}
                error={fieldState.error?.message ?? (roles.isError ? 'Roles could not be loaded.' : undefined)}
                emptyText={
                  me.isSuperAdmin && companyId === null
                    ? 'Select a company on the User Details tab first.'
                    : roles.isPending
                      ? 'Loading roles…'
                      : 'No roles available.'
                }
              />
            )}
          />
        </UserTabPanel>
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
