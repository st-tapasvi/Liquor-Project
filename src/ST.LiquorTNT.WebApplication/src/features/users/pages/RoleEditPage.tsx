import { zodResolver } from '@hookform/resolvers/zod';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Grid from '@mui/material/Grid';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import { useMemo, useState } from 'react';
import { FormProvider, useForm } from 'react-hook-form';
import { Link as RouterLink, useMatch, useNavigate, useParams } from 'react-router';

import type { RoleResponse, RoleRightsResponse } from '@/core/api';
import { Can, useCurrentUser, usePermission } from '@/core/auth';
import { applyServerErrors, describeError } from '@/core/errors';
import { PATHS } from '@/core/router';

import { ErrorState, LoadingOverlay } from '@/shared/components/feedback';
import { FormActions, FormRootError, FormSelect, FormTextField } from '@/shared/components/forms';
import { Section } from '@/shared/components/layout';
import { PageHeader, StatusChip } from '@/shared/components/ui';
import type { Option } from '@/shared/types';

import { useSaveRole } from '../api/users.mutations';
import { usePasswordPolicyOptions, useRole, useRoleRights, useSupplierCodeOptions } from '../api/users.queries';
import { changeCount, pagesForExcise } from '../components/role-rights';
import { RolePagesPicker } from '../components/RolePagesPicker';
import { type EditRoleFormInput, type EditRoleFormValues, editRoleSchema } from '../users.schema';

export default function RoleEditPage() {
  const { id } = useParams<{ id: string }>();
  const roleId = id !== undefined && /^\d+$/.test(id) ? Number(id) : Number.NaN;
  const isEditRoute = useMatch(PATHS.users.roleEdit()) !== null;
  const canEdit = usePermission('role.edit');

  if (!Number.isInteger(roleId) || roleId <= 0) return <ErrorState error={new Error('Invalid role id.')} />;
  return <RoleView key={roleId} roleId={roleId} wantsEdit={isEditRoute && canEdit} />;
}

function RoleView({ roleId, wantsEdit }: { roleId: number; wantsEdit: boolean }) {
  const role = useRole(roleId);
  const rights = useRoleRights(roleId);

  if (role.isPending || rights.isPending) return <LoadingOverlay />;
  if (role.isError) return <ErrorState error={role.error} onRetry={() => void role.refetch()} />;
  if (rights.isError) return <ErrorState error={rights.error} onRetry={() => void rights.refetch()} />;

  return (
    <RoleEditor
      key={`${role.dataUpdatedAt}-${rights.dataUpdatedAt}`}
      role={role.data}
      rights={rights.data}
      wantsEdit={wantsEdit}
    />
  );
}

function grantedIds(rights: RoleRightsResponse): Set<number> {
  return new Set(rights.pages.flatMap((p) => p.actions.filter((a) => a.granted).map((a) => a.pageActionId)));
}

function RoleEditor({
  role,
  rights,
  wantsEdit,
}: {
  role: RoleResponse;
  rights: RoleRightsResponse;
  wantsEdit: boolean;
}) {
  const me = useCurrentUser();
  const navigate = useNavigate();
  const canViewSupplierCodes = usePermission('suppliercode.view');
  const canViewPolicies = usePermission('passwordpolicy.view');
  const canManageAdmins = usePermission('user.manageadmin');

  const locked = role.isSystem || (role.isTemplate && !me.isSuperAdmin);
  const readOnly = !wantsEdit || locked;

  const supplierCodes = useSupplierCodeOptions(canViewSupplierCodes && role.supplierCodeId !== null);
  const policies = usePasswordPolicyOptions(canViewPolicies);
  const save = useSaveRole(role.id);

  const baseline = useMemo(() => grantedIds(rights), [rights]);
  const [selected, setSelected] = useState<Set<number>>(() => new Set(baseline));

  const form = useForm<EditRoleFormInput, unknown, EditRoleFormValues>({
    resolver: zodResolver(editRoleSchema),
    defaultValues: { roleName: role.roleName, passwordPolicyId: role.passwordPolicyId },
  });

  const supplierCode = (canViewSupplierCodes ? supplierCodes.data : me.supplierCodes)?.find(
    (s) => s.id === role.supplierCodeId,
  );
  const exciseId = supplierCode?.exciseId ?? null;
  const pages = exciseId === null ? rights.pages : pagesForExcise(rights.pages, exciseId);

  const policyOptions: Option<number>[] = (policies.data ?? []).map((p) => ({
    value: p.id,
    label: p.policyName ?? p.name,
  }));
  if (role.passwordPolicyId !== null && !policyOptions.some((o) => o.value === role.passwordPolicyId)) {
    policyOptions.push({ value: role.passwordPolicyId, label: `Policy ${role.passwordPolicyId}` });
  }

  const submit = form.handleSubmit(async (values) => {
    const detailsChanged = values.roleName !== role.roleName || values.passwordPolicyId !== role.passwordPolicyId;
    const rightsChanged = changeCount(rights.pages, selected, baseline) > 0;
    if (!detailsChanged && !rightsChanged) {
      void navigate(PATHS.users.roleView(role.id));
      return;
    }
    try {
      await save.mutateAsync({
        request: detailsChanged
          ? {
              roleName: values.roleName,
              description: role.description,
              supplierCodeId: role.supplierCodeId,
              isAdminRole: role.isAdminRole,
              perSupplierCode: role.perSupplierCode,
              passwordPolicyId: values.passwordPolicyId,
            }
          : null,
        pageActionIds: rightsChanged ? [...selected] : null,
      });
      void navigate(PATHS.users.roles);
    } catch (error) {
      if (applyServerErrors(form.setError, error, { fields: ['roleName', 'passwordPolicyId'] })) return;
      form.setError('root.submit', { type: 'server', message: describeError(error) });
    }
  });

  return (
    <>
      <PageHeader
        title={role.displayName}
        subtitle={role.description ?? undefined}
        status={
          <StatusChip label={role.isActive ? 'Active' : 'Inactive'} tone={role.isActive ? 'success' : 'default'} />
        }
        actions={
          readOnly &&
          !locked && (
            <Can right="role.edit">
              <Button
                component={RouterLink}
                to={PATHS.users.roleEdit(role.id)}
                variant="contained"
                startIcon={<EditOutlinedIcon />}
              >
                Edit
              </Button>
            </Can>
          )
        }
      />
      {locked && (
        <Alert severity="info" sx={{ mb: 2 }}>
          {role.isSystem
            ? 'The Admin role has every right and cannot be changed.'
            : 'Default role templates can be changed by the Admin role only.'}
        </Alert>
      )}
      <FormProvider {...form}>
        <form onSubmit={submit} noValidate>
          <FormRootError />
          <Section>
            <fieldset disabled={readOnly} style={{ border: 0, padding: 0, margin: 0, minWidth: 0 }}>
              <Grid container spacing={2}>
                <Grid size={{ xs: 12, md: 6 }}>
                  <FormTextField<EditRoleFormInput>
                    name="roleName"
                    label="Role name"
                    required
                    autoComplete="off"
                    slotProps={{ htmlInput: { maxLength: 100 } }}
                  />
                </Grid>
                <Grid size={{ xs: 12, md: 6 }}>
                  <FormSelect<EditRoleFormInput>
                    name="passwordPolicyId"
                    label="Password policy"
                    required
                    options={policyOptions}
                  />
                </Grid>
                <Grid size={{ xs: 12, md: 6 }}>
                  <TextField
                    label="Supplier code"
                    value={role.supplierCodeName ?? 'All supplier codes of the company'}
                    disabled
                    helperText={readOnly ? undefined : 'The supplier code of a role cannot be changed.'}
                  />
                </Grid>
              </Grid>
            </fieldset>
          </Section>

          <Section title="Pages and rights">
            {role.isSystem ? (
              <Typography variant="body2" color="text.secondary">
                Every right, in every company.
              </Typography>
            ) : (
              <RolePagesPicker
                pages={pages}
                selected={selected}
                baseline={baseline}
                canManageAdmins={canManageAdmins}
                disabled={readOnly || save.isPending}
                onChange={setSelected}
              />
            )}
          </Section>

          {!readOnly && (
            <FormActions
              submitLabel="Save changes"
              onCancel={() => void navigate(PATHS.users.roleView(role.id))}
              busy={save.isPending}
            />
          )}
        </form>
      </FormProvider>
    </>
  );
}
