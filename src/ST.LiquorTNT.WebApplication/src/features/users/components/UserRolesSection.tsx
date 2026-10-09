import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import { useState } from 'react';

import type { UserAccessResponse } from '@/core/api';
import { useCurrentUser } from '@/core/auth';

import { ErrorState, LoadingOverlay } from '@/shared/components/feedback';
import { Section } from '@/shared/components/layout';

import { useUpdateUserRoles } from '../api/users.mutations';
import { useRoles, useUserAccess } from '../api/users.queries';

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
    <Section title="Roles and rights">
      {access.isPending && <LoadingOverlay />}
      {access.isError && <ErrorState error={access.error} onRetry={() => void access.refetch()} />}
      {access.data && (
        <Stack spacing={2}>
          <div>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>
              Roles
            </Typography>
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
  const roles = useRoles();
  const update = useUpdateUserRoles(userId);
  const assigned = access.roles.map((r) => r.roleId);
  const [selected, setSelected] = useState<number[]>(assigned);

  const options: RoleOption[] = assignableRoles(roles.data ?? [], me.isSuperAdmin ? companyId : undefined).map((r) => ({
    id: r.id,
    label: roleLabel(r),
  }));
  for (const r of access.roles) {
    if (!options.some((o) => o.id === r.roleId)) {
      options.push({ id: r.roleId, label: r.displayName || `${r.roleName} · ${r.supplierCodeName}`, locked: true });
    }
  }

  const dirty = selected.length !== assigned.length || selected.some((id) => !assigned.includes(id));
  const empty = selected.length === 0;

  return (
    <>
      <RoleCheckboxList
        options={options}
        value={selected}
        onChange={setSelected}
        disabled={!editable || update.isPending}
        error={
          editable && empty
            ? 'Select at least one role.'
            : editable && roles.isError
              ? 'Roles could not be loaded.'
              : undefined
        }
        emptyText={roles.isPending && editable ? 'Loading roles…' : 'No role assigned.'}
      />
      {editable && (
        <Stack direction="row" spacing={1} sx={{ mt: 2, justifyContent: 'flex-end' }}>
          <Button onClick={() => setSelected(assigned)} disabled={!dirty || update.isPending}>
            Reset
          </Button>
          <Button
            variant="contained"
            disabled={!dirty || empty || update.isPending}
            onClick={() => update.mutate({ roles: selected.map((roleId) => ({ roleId })) })}
          >
            Save roles
          </Button>
        </Stack>
      )}
    </>
  );
}
