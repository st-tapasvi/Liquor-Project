import Chip from '@mui/material/Chip';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { ErrorState, LoadingOverlay } from '@/shared/components/feedback';
import { Section } from '@/shared/components/layout';

import { useUserAccess } from '../api/users.queries';

export function UserRolesSection({ userId }: { userId: number }) {
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
            {access.data.roles.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                No role assigned.
              </Typography>
            ) : (
              <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
                {access.data.roles.map((r) => (
                  <Chip
                    key={`${r.roleId}-${r.supplierCodeId ?? 'all'}`}
                    label={`${r.roleName} · ${r.supplierCodeName}`}
                    variant="outlined"
                  />
                ))}
              </Stack>
            )}
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
