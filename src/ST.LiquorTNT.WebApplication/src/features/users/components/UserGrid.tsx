import LockOpenIcon from '@mui/icons-material/LockOpen';
import PersonIcon from '@mui/icons-material/Person';
import PersonOffIcon from '@mui/icons-material/PersonOff';
import IconButton from '@mui/material/IconButton';
import Link from '@mui/material/Link';
import Stack from '@mui/material/Stack';
import Tooltip from '@mui/material/Tooltip';
import { useMemo } from 'react';
import { Link as RouterLink } from 'react-router';

import type { UserResponse } from '@/core/api';
import { Can, useCurrentUser } from '@/core/auth';
import { PATHS } from '@/core/router';

import { AppDataGrid, type AppGridColumn, type useServerGrid } from '@/shared/components/data-grid';
import { StatusChip } from '@/shared/components/ui';
import { useConfirm } from '@/shared/hooks';
import { formatDateTime } from '@/shared/utils';

import { useActivateUser, useDeactivateUser, useUnlockUser } from '../api/users.mutations';
import { useUsers } from '../api/users.queries';

import { userStatus } from './user-status';

export function UserGrid({ grid }: { grid: ReturnType<typeof useServerGrid> }) {
  const params = useMemo(
    () => ({
      page: grid.state.page,
      pageSize: grid.state.pageSize,
      ...(grid.state.search ? { search: grid.state.search } : {}),
    }),
    [grid.state.page, grid.state.pageSize, grid.state.search],
  );
  const users = useUsers(params);
  const me = useCurrentUser();
  const confirm = useConfirm();
  const activate = useActivateUser();
  const deactivate = useDeactivateUser();
  const unlock = useUnlockUser();

  const columns = useMemo<AppGridColumn<UserResponse>[]>(
    () => [
      {
        field: 'userName',
        headerName: 'User name',
        rowHeader: true,
        flex: 1,
        minWidth: 140,
        renderCell: ({ row }) => (
          <Link component={RouterLink} to={PATHS.users.edit(row.id)} underline="hover">
            {row.userName}
          </Link>
        ),
      },
      {
        field: 'fullName',
        headerName: 'Full name',
        flex: 1.2,
        minWidth: 160,
        valueGetter: (v: string | null) => v ?? '',
      },
      { field: 'email', headerName: 'E-mail', flex: 1.2, minWidth: 160, valueGetter: (v: string | null) => v ?? '' },
      { field: 'roleId', headerName: 'Role', width: 80 },
      {
        field: 'status',
        headerName: 'Status',
        width: 120,
        sortable: false,
        renderCell: ({ row }) => {
          const s = userStatus(row);
          return <StatusChip label={s.label} tone={s.tone} />;
        },
      },
      {
        field: 'lastLoginAt',
        headerName: 'Last login',
        width: 160,
        valueFormatter: (v: string | null) => formatDateTime(v),
      },
      {
        field: 'actions',
        disableColumnMenu: true,
        headerName: '',
        width: 110,
        sortable: false,
        align: 'right',
        renderCell: ({ row }) => (
          <Can right="users.manage">
            <Stack direction="row" spacing={0.5} sx={{ height: '100%', alignItems: 'center' }}>
              {(row.lockedUntil !== null || row.isBlocked) && (
                <Tooltip title="Unlock">
                  <IconButton size="small" aria-label={`Unlock ${row.userName}`} onClick={() => unlock.mutate(row.id)}>
                    <LockOpenIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              )}
              {row.isActive ? (
                <Tooltip title="Deactivate">
                  <span>
                    <IconButton
                      size="small"
                      aria-label={`Deactivate ${row.userName}`}
                      disabled={row.id === me.userId}
                      onClick={() => {
                        void confirm({
                          title: 'Deactivate user?',
                          message: `${row.userName} will be logged out everywhere and cannot sign in until reactivated.`,
                          confirmLabel: 'Deactivate',
                          destructive: true,
                        }).then((ok) => {
                          if (ok) deactivate.mutate(row.id);
                        });
                      }}
                    >
                      <PersonOffIcon fontSize="small" />
                    </IconButton>
                  </span>
                </Tooltip>
              ) : (
                <Tooltip title="Activate">
                  <IconButton
                    size="small"
                    aria-label={`Activate ${row.userName}`}
                    onClick={() => activate.mutate(row.id)}
                  >
                    <PersonIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              )}
            </Stack>
          </Can>
        ),
      },
    ],
    [activate, deactivate, unlock, confirm, me.userId],
  );

  return (
    <AppDataGrid<UserResponse>
      ariaLabel="Users"
      rows={users.data?.items}
      columns={columns}
      getRowId={(row) => row.id}
      loading={users.isPending || users.isFetching}
      error={users.error}
      onRetry={() => void users.refetch()}
      server={grid.paging(users.data?.totalCount ?? 0)}
    />
  );
}
