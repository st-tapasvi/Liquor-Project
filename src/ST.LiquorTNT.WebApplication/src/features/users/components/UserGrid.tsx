import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import LockOpenIcon from '@mui/icons-material/LockOpen';
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined';
import IconButton from '@mui/material/IconButton';
import Link from '@mui/material/Link';
import Stack from '@mui/material/Stack';
import Tooltip from '@mui/material/Tooltip';
import { useEffect, useMemo } from 'react';
import { Link as RouterLink } from 'react-router';

import type { UserResponse } from '@/core/api';
import { Can, useCurrentUser } from '@/core/auth';
import { PATHS } from '@/core/router';

import { AppDataGrid, type AppGridColumn, type useServerGrid } from '@/shared/components/data-grid';
import { StatusChip } from '@/shared/components/ui';
import { useConfirm } from '@/shared/hooks';
import { formatDateTime } from '@/shared/utils';

import { useDeleteUser, useUnlockUser } from '../api/users.mutations';
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
  const remove = useDeleteUser();
  const unlock = useUnlockUser();

  useEffect(() => {
    const data = users.data;
    if (!data || users.isPlaceholderData || data.items.length > 0) return;
    const lastPage = Math.max(1, Math.ceil(data.totalCount / grid.state.pageSize));
    if (grid.state.page > lastPage) grid.setPage(lastPage);
  }, [users.data, users.isPlaceholderData, grid]);

  const columns = useMemo<AppGridColumn<UserResponse>[]>(
    () => [
      {
        field: 'userName',
        headerName: 'User name',
        rowHeader: true,
        flex: 1,
        minWidth: 140,
        renderCell: ({ row }) => (
          <Link component={RouterLink} to={PATHS.users.view(row.id)} underline="hover">
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
      { field: 'phone', headerName: 'Phone', width: 140, valueGetter: (v: string | null) => v ?? '' },
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
        headerName: 'Action',
        width: 160,
        sortable: false,
        align: 'center',
        headerAlign: 'center',
        renderCell: ({ row }) => (
          <Stack direction="row" spacing={0.5} sx={{ height: '100%', alignItems: 'center', justifyContent: 'center' }}>
            <Tooltip title="View">
              <IconButton
                size="small"
                component={RouterLink}
                to={PATHS.users.view(row.id)}
                aria-label={`View ${row.userName}`}
              >
                <VisibilityOutlinedIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            <Can right="user.edit">
              <Tooltip title="Edit">
                <IconButton
                  size="small"
                  component={RouterLink}
                  to={PATHS.users.edit(row.id)}
                  aria-label={`Edit ${row.userName}`}
                >
                  <EditOutlinedIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            </Can>
            <Can right="user.delete">
              <Tooltip title={row.id === me.userId ? 'You cannot delete your own account' : 'Delete'}>
                <span>
                  <IconButton
                    size="small"
                    color="error"
                    aria-label={`Delete ${row.userName}`}
                    disabled={row.id === me.userId || remove.isPending}
                    onClick={() => {
                      void confirm({
                        title: 'Delete user?',
                        message: `${row.userName} will be permanently deleted. This cannot be undone.`,
                        confirmLabel: 'Delete',
                        destructive: true,
                      }).then((ok) => {
                        if (ok) remove.mutate(row);
                      });
                    }}
                  >
                    <DeleteOutlinedIcon fontSize="small" />
                  </IconButton>
                </span>
              </Tooltip>
            </Can>
            <Can right="user.unlock">
              {(row.lockedUntil !== null || row.isBlocked) && (
                <Tooltip title="Unlock">
                  <IconButton size="small" aria-label={`Unlock ${row.userName}`} onClick={() => unlock.mutate(row.id)}>
                    <LockOpenIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              )}
            </Can>
          </Stack>
        ),
      },
    ],
    [remove, unlock, confirm, me.userId],
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
      fillViewport
    />
  );
}
