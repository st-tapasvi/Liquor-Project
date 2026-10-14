import Autocomplete from '@mui/material/Autocomplete';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import { useState } from 'react';

import type { UserAccessResponse } from '@/core/api';
import { useCurrentUser, usePermission } from '@/core/auth';

import { ErrorState, LoadingOverlay } from '@/shared/components/feedback';
import { Section } from '@/shared/components/layout';

import { useUpdateUserAccess } from '../api/users.mutations';
import { useRoleGroups, useRoles, useUserAccess } from '../api/users.queries';

import { assignableRoles, type RoleOption, roleLabel } from './role-options';
import { RoleCheckboxList } from './RoleCheckboxList';

interface UserRolesSectionProps {
  userId: number;
  companyId: number | null;
  editable?: boolean;
}

export function UserRolesSection({ userId, companyId, editable = false }: UserRolesSectionProps) {
  const access = useUserAccess(userId);

  return (
    <Section>
      {access.isPending && <LoadingOverlay />}
      {access.isError && <ErrorState error={access.error} onRetry={() => void access.refetch()} />}
      {access.data && (
        <Stack spacing={2}>
          <div>
            <RolesEditor
              key={access.dataUpdatedAt}
              userId={userId}
              companyId={companyId}
              access={access.data}
              editable={editable}
            />
          </div>
          {access.data.rights.length > 0 && (
            <div>
              <Typography variant="subtitle2" sx={{ mb: 1 }}>
                Custom rights
              </Typography>
              <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
                {access.data.rights.map((r) => (
                  <Chip
                    key={`${r.pageActionId}-${r.supplierCodeId ?? 'all'}`}
                    label={`${r.actionName} (${r.permissionKey}) · ${r.supplierCodeName}`}
                    size="small"
                  />
                ))}
              </Stack>
            </div>
          )}
        </Stack>
      )}
    </Section>
  );
}

interface ProfileOption {
  id: number;
  name: string;
  roles: { id: number; label: string }[];
}

function sameIds(a: readonly number[], b: readonly number[]): boolean {
  const set = new Set(a);
  return a.length === b.length && b.every((id) => set.has(id));
}

function RolesEditor({
  userId,
  companyId,
  access,
  editable,
}: {
  userId: number;
  companyId: number | null;
  access: UserAccessResponse;
  editable: boolean;
}) {
  const me = useCurrentUser();
  const canViewProfiles = usePermission('role.view');
  const roles = useRoles();
  const groups = useRoleGroups(editable && canViewProfiles);
  const update = useUpdateUserAccess(userId);

  const assignedRoles = access.roles.map((r) => r.roleId);
  const assignedGroups = access.roleGroups.map((g) => g.roleGroupId);
  const [direct, setDirect] = useState<number[]>(assignedGroups.length > 0 ? [] : assignedRoles);
  const [groupIds, setGroupIds] = useState<number[]>(assignedGroups);

  const catalog = new Map<number, ProfileOption>();
  for (const g of access.roleGroups) {
    catalog.set(g.roleGroupId, {
      id: g.roleGroupId,
      name: g.groupName,
      roles: g.roles.map((r) => ({ id: r.roleId, label: r.displayName || r.roleName })),
    });
  }
  for (const g of groups.data ?? []) {
    if (!g.isActive && !assignedGroups.includes(g.id)) continue;
    if (companyId !== null && g.companyId !== companyId) continue;
    catalog.set(g.id, {
      id: g.id,
      name: g.groupName,
      roles: g.roles.map((r) => ({ id: r.roleId, label: r.displayName || r.roleName })),
    });
  }
  const profileOptions = [...catalog.values()].sort((a, b) => a.name.localeCompare(b.name));
  const selectedProfiles = groupIds.flatMap((id) => catalog.get(id) ?? []);
  const covered = new Set(selectedProfiles.flatMap((p) => p.roles.map((r) => r.id)));
  const hasProfiles = groupIds.length > 0;
  const ownRoles = hasProfiles ? [] : direct;

  const options: RoleOption[] = assignableRoles(roles.data ?? [], me.isSuperAdmin ? companyId : undefined).map((r) => ({
    id: r.id,
    label: roleLabel(r),
  }));
  for (const r of access.roles) {
    if (!options.some((o) => o.id === r.roleId)) {
      options.push({ id: r.roleId, label: r.displayName || `${r.roleName} · ${r.supplierCodeName}`, locked: true });
    }
  }
  for (const r of selectedProfiles.flatMap((p) => p.roles)) {
    if (!options.some((o) => o.id === r.id)) options.push({ id: r.id, label: r.label });
  }
  const roleOptions = options.map((o) => (hasProfiles ? { ...o, locked: true } : o));

  const rolesDirty = !sameIds(ownRoles, assignedRoles);
  const groupsDirty = !sameIds(groupIds, assignedGroups);
  const dirty = rolesDirty || groupsDirty;
  const empty = ownRoles.length === 0 && groupIds.length === 0;

  const changeProfiles = (next: ProfileOption[]) => {
    setGroupIds(next.map((p) => p.id));
    setDirect([]);
  };

  const reset = () => {
    setDirect(assignedGroups.length > 0 ? [] : assignedRoles);
    setGroupIds(assignedGroups);
  };

  return (
    <>
      <Typography variant="subtitle2" sx={{ mb: 1 }}>
        Role profiles
      </Typography>
      <Autocomplete<ProfileOption, true>
        multiple
        readOnly={!editable}
        disabled={update.isPending}
        options={profileOptions}
        value={selectedProfiles}
        onChange={(_event, next) => changeProfiles(next)}
        getOptionLabel={(o) => o.name}
        isOptionEqualToValue={(o, v) => o.id === v.id}
        filterSelectedOptions
        loading={groups.isFetching}
        noOptionsText={groups.isError ? 'Role profiles could not be loaded.' : 'No role profile available.'}
        renderOption={(props, option) => {
          const { key, ...rest } = props;
          return (
            <li key={key} {...rest}>
              <Stack>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                  {option.name}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {`${option.roles.length} role${option.roles.length === 1 ? '' : 's'}`}
                </Typography>
              </Stack>
            </li>
          );
        }}
        renderInput={(params) => (
          <TextField
            {...params}
            placeholder={editable && groupIds.length === 0 ? 'Select role profile' : undefined}
            slotProps={{
              ...params.slotProps,
              htmlInput: { ...params.slotProps.htmlInput, 'aria-label': 'Role profiles' },
            }}
            sx={{ '& .MuiInputBase-root': { minHeight: 80, alignItems: 'flex-start' } }}
          />
        )}
        sx={{ mb: 1 }}
      />
      {editable && (
        <Typography variant="caption" color="text.secondary" component="p" sx={{ mb: 2 }}>
          Only the roles of the selected role profiles are given, and they cannot be changed here. To pick roles by
          hand, remove the role profiles.
        </Typography>
      )}
      <Typography variant="subtitle2" sx={{ mt: editable ? 0 : 2, mb: 1 }}>
        Roles
      </Typography>
      <RoleCheckboxList
        options={roleOptions}
        value={hasProfiles ? [...covered] : ownRoles}
        onChange={setDirect}
        disabled={!editable || update.isPending}
        error={
          editable && empty
            ? 'Select at least one role or role profile.'
            : editable && roles.isError
              ? 'Roles could not be loaded.'
              : undefined
        }
        emptyText={roles.isPending && editable ? 'Loading roles…' : 'No role assigned.'}
      />
      {editable && (
        <Stack direction="row" spacing={1} sx={{ mt: 2, justifyContent: 'flex-end' }}>
          <Button onClick={reset} disabled={!dirty || update.isPending}>
            Reset
          </Button>
          <Button
            variant="contained"
            disabled={!dirty || empty || update.isPending}
            onClick={() =>
              update.mutate({ roleIds: rolesDirty ? ownRoles : null, roleGroupIds: groupsDirty ? groupIds : null })
            }
          >
            Save roles
          </Button>
        </Stack>
      )}
    </>
  );
}
