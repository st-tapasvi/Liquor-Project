import { zodResolver } from '@hookform/resolvers/zod';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import PeopleOutlined from '@mui/icons-material/PeopleOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Grid from '@mui/material/Grid';
import MenuItem from '@mui/material/MenuItem';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import { useState } from 'react';
import { FormProvider, useForm } from 'react-hook-form';
import { Link as RouterLink, useMatch, useNavigate, useParams } from 'react-router';

import type { RoleGroupResponse, SaveRoleGroupRequest } from '@/core/api';
import { Can, useCurrentUser, usePermission } from '@/core/auth';
import { applyServerErrors, describeError, isApiError } from '@/core/errors';
import { PATHS } from '@/core/router';

import { ErrorState, LoadingOverlay } from '@/shared/components/feedback';
import { FormActions, FormRootError, FormTextField } from '@/shared/components/forms';
import { Section } from '@/shared/components/layout';
import { PageHeader, StatusChip } from '@/shared/components/ui';
import { useSnackbar } from '@/shared/hooks';
import { emptyToNull } from '@/shared/utils';

import { RoleGroupRolesError, useSaveRoleGroup } from '../api/users.mutations';
import { useCompanyOptions, useRoleGroup, useRoles } from '../api/users.queries';
import { assignableRoles, type RoleOption, roleLabel } from '../components/role-options';
import { RoleCheckboxList } from '../components/RoleCheckboxList';
import { type RoleProfileFormValues, roleProfileSchema } from '../users.schema';

/** Serves /role-profiles/new, /role-profiles/:id (read-only view) and /role-profiles/:id/edit. */
export default function RoleProfileEditPage() {
  const { id } = useParams<{ id: string }>();
  const isEditRoute = useMatch(PATHS.users.roleProfileEdit()) !== null;
  const canEdit = usePermission('role.edit');

  if (id === undefined) return <RoleProfileEditor group={null} readOnly={false} />;
  const groupId = /^\d+$/.test(id) ? Number(id) : Number.NaN;
  if (!Number.isInteger(groupId) || groupId <= 0) {
    return <ErrorState error={new Error('Invalid role profile id.')} />;
  }
  return <RoleProfileView key={groupId} groupId={groupId} readOnly={!isEditRoute || !canEdit} />;
}

function RoleProfileView({ groupId, readOnly }: { groupId: number; readOnly: boolean }) {
  const group = useRoleGroup(groupId);
  if (group.isPending) return <LoadingOverlay />;
  if (group.isError) return <ErrorState error={group.error} onRetry={() => void group.refetch()} />;
  return <RoleProfileEditor key={group.dataUpdatedAt} group={group.data} readOnly={readOnly} />;
}

function sameIds(a: readonly number[], b: readonly number[]): boolean {
  const set = new Set(a);
  return a.length === b.length && b.every((id) => set.has(id));
}

function RoleProfileEditor({ group, readOnly }: { group: RoleGroupResponse | null; readOnly: boolean }) {
  const me = useCurrentUser();
  const navigate = useNavigate();
  const snackbar = useSnackbar();
  const roles = useRoles();
  const save = useSaveRoleGroup(group?.id);

  const companies = useCompanyOptions(me.isSuperAdmin);
  const [pickedCompanyId, setPickedCompanyId] = useState<number | null>(
    me.isSuperAdmin ? null : (me.activeSupplierCode?.companyId ?? me.companyId),
  );
  const [companyError, setCompanyError] = useState<string | null>(null);
  const onlyCompanyId = companies.data?.length === 1 ? (companies.data.at(0)?.id ?? null) : null;
  const companyId = group?.companyId ?? pickedCompanyId ?? onlyCompanyId;
  const companyName = companies.data?.find((c) => c.id === companyId)?.companyName ?? null;
  const baseline = group?.roles.map((r) => r.roleId) ?? [];
  const [selected, setSelected] = useState<number[]>(baseline);

  const form = useForm<RoleProfileFormValues>({
    resolver: zodResolver(roleProfileSchema),
    defaultValues: { groupName: group?.groupName ?? '', description: group?.description ?? '' },
  });

  const options: RoleOption[] = assignableRoles(roles.data ?? [], companyId ?? undefined).map((r) => ({
    id: r.id,
    label: roleLabel(r),
  }));
  for (const r of group?.roles ?? []) {
    if (!options.some((o) => o.id === r.roleId)) {
      options.push({ id: r.roleId, label: r.displayName || r.roleName, locked: true });
    }
  }

  const backPath = group ? PATHS.users.roleProfileView(group.id) : PATHS.users.roleProfiles;

  const submit = form.handleSubmit(async (values) => {
    if (group === null && companyId === null) {
      setCompanyError('Choose the company of the role profile.');
      return;
    }
    const request: SaveRoleGroupRequest = {
      groupName: values.groupName,
      description: emptyToNull(values.description),
      ...(group === null && me.isSuperAdmin ? { companyId } : {}),
    };
    const detailsChanged = request.groupName !== group?.groupName || request.description !== group.description;
    const rolesChanged = !sameIds(selected, baseline);
    if (!detailsChanged && !rolesChanged) {
      void navigate(backPath);
      return;
    }
    try {
      await save.mutateAsync({ request: detailsChanged ? request : null, roleIds: rolesChanged ? selected : null });
      void navigate(PATHS.users.roleProfiles);
    } catch (error) {
      if (error instanceof RoleGroupRolesError) {
        snackbar.error(`${error.message} ${describeError(error.reason)}`);
        void navigate(PATHS.users.roleProfileEdit(error.groupId));
        return;
      }
      if (isApiError(error) && error.is('ROLE_GROUP_NAME_TAKEN')) {
        form.setError('groupName', { type: 'server', message: describeError(error) });
        return;
      }
      if (applyServerErrors(form.setError, error, { fields: ['groupName', 'description'] })) return;
      form.setError('root.submit', { type: 'server', message: describeError(error) });
    }
  });

  return (
    <>
      <PageHeader
        title={group?.groupName ?? 'New role profile'}
        subtitle={group?.description ?? undefined}
        status={
          group && (
            <StatusChip label={group.isActive ? 'Active' : 'Inactive'} tone={group.isActive ? 'success' : 'default'} />
          )
        }
        actions={
          group &&
          readOnly && (
            <Can right="role.edit">
              <Button
                component={RouterLink}
                to={PATHS.users.roleProfileEdit(group.id)}
                variant="contained"
                startIcon={<EditOutlinedIcon />}
              >
                Edit
              </Button>
            </Can>
          )
        }
      />
      {group && (
        <Box
          sx={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 1,
            mb: 2,
            px: 1.5,
            py: 0.75,
            border: 1,
            borderColor: 'divider',
            borderRadius: 1.5,
            bgcolor: 'background.paper',
          }}
          aria-label={`${group.userCount} user${group.userCount === 1 ? '' : 's'} have this role profile`}
        >
          <PeopleOutlined fontSize="small" color="action" />
          <Typography variant="body2" sx={{ fontWeight: 600 }}>
            User
          </Typography>
          <Box
            component="span"
            sx={{
              minWidth: 24,
              px: 0.75,
              borderRadius: 1,
              bgcolor: 'action.selected',
              fontSize: 12,
              fontWeight: 700,
              textAlign: 'center',
              lineHeight: '20px',
            }}
          >
            {group.userCount}
          </Box>
        </Box>
      )}
      <FormProvider {...form}>
        <form onSubmit={submit} noValidate>
          <FormRootError />
          <Section>
            <fieldset disabled={readOnly} style={{ border: 0, padding: 0, margin: 0, minWidth: 0 }}>
              <Grid container spacing={2}>
                {me.isSuperAdmin && (
                  <Grid size={{ xs: 12, md: 6 }}>
                    <TextField
                      select={group === null}
                      label="Company"
                      required
                      value={group === null ? (companyId ?? '') : (companyName ?? `Company ${group.companyId}`)}
                      disabled={group !== null}
                      error={companyError !== null}
                      helperText={companyError ?? (companies.isError ? 'Companies could not be loaded.' : undefined)}
                      onChange={(e) => {
                        setPickedCompanyId(Number(e.target.value));
                        setSelected([]);
                        setCompanyError(null);
                      }}
                    >
                      {(companies.data ?? []).map((c) => (
                        <MenuItem key={c.id} value={c.id}>
                          {c.companyName ?? `Company ${c.id}`}
                        </MenuItem>
                      ))}
                    </TextField>
                  </Grid>
                )}
                <Grid size={{ xs: 12, md: 6 }}>
                  <FormTextField<RoleProfileFormValues>
                    name="groupName"
                    label="Role profile name"
                    required
                    autoComplete="off"
                    autoFocus={group === null}
                    slotProps={{ htmlInput: { maxLength: 100 } }}
                  />
                </Grid>
                <Grid size={{ xs: 12, md: 6 }}>
                  <FormTextField<RoleProfileFormValues>
                    name="description"
                    label="Description"
                    autoComplete="off"
                    slotProps={{ htmlInput: { maxLength: 255 } }}
                  />
                </Grid>
              </Grid>
            </fieldset>
            <Typography variant="subtitle2" sx={{ mt: 3, mb: 1 }}>
              Roles
            </Typography>
            {roles.isError ? (
              <ErrorState error={roles.error} onRetry={() => void roles.refetch()} />
            ) : (
              <RoleCheckboxList
                options={options}
                value={selected}
                onChange={setSelected}
                disabled={readOnly || save.isPending}
                emptyText={roles.isPending ? 'Loading roles…' : 'No roles available.'}
              />
            )}
          </Section>
          {!readOnly && (
            <FormActions
              submitLabel={group ? 'Save changes' : 'Create role profile'}
              onCancel={() => void navigate(backPath)}
              busy={save.isPending}
            />
          )}
        </form>
      </FormProvider>
    </>
  );
}
