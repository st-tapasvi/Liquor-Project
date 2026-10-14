import AddIcon from '@mui/icons-material/Add';
import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import Link from '@mui/material/Link';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Tooltip from '@mui/material/Tooltip';
import { useMemo, useState } from 'react';
import { Link as RouterLink } from 'react-router';

import type { RoleGroupResponse } from '@/core/api';
import { Can, useCurrentUser } from '@/core/auth';
import { PATHS } from '@/core/router';

import { type AppGridColumn, AppDataGrid } from '@/shared/components/data-grid';
import { PageHeader, StatusChip } from '@/shared/components/ui';
import { useConfirm, useDebounce } from '@/shared/hooks';

import { useDeleteRoleGroup } from '../api/users.mutations';
import { useCompanyOptions, useRoleGroups } from '../api/users.queries';

function matchesSearch(group: RoleGroupResponse, term: string): boolean {
  return [group.groupName, group.description, ...group.roles.map((r) => r.displayName)].some((v) =>
    v?.toLowerCase().includes(term),
  );
}

export default function RoleProfileListPage() {
  const me = useCurrentUser();
  const groups = useRoleGroups();
  const companies = useCompanyOptions(me.isSuperAdmin);
  const [searchText, setSearchText] = useState('');
  const search = useDebounce(searchText, 300).trim().toLowerCase();
  const rows = useMemo(
    () => (search ? groups.data?.filter((g) => matchesSearch(g, search)) : groups.data),
    [groups.data, search],
  );
  const confirm = useConfirm();
  const remove = useDeleteRoleGroup();

  const columns = useMemo<AppGridColumn<RoleGroupResponse>[]>(
    () => [
      {
        field: 'groupName',
        headerName: 'Role profile',
        flex: 1,
        minWidth: 200,
        rowHeader: true,
        renderCell: ({ row }) => (
          <Link component={RouterLink} to={PATHS.users.roleProfileView(row.id)} underline="hover">
            {row.groupName}
          </Link>
        ),
      },
      ...(me.isSuperAdmin
        ? [
            {
              field: 'companyId',
              headerName: 'Company',
              flex: 1,
              minWidth: 180,
              valueGetter: (_v, row) =>
                companies.data?.find((c) => c.id === row.companyId)?.companyName ?? `Company ${row.companyId}`,
            } satisfies AppGridColumn<RoleGroupResponse>,
          ]
        : []),
      { field: 'description', headerName: 'Description', flex: 2, minWidth: 240, sortable: false },
      { field: 'roleCount', headerName: 'Roles', width: 110, valueGetter: (_v, row) => row.roles.length },
      { field: 'userCount', headerName: 'Users', width: 110 },
      {
        field: 'isActive',
        headerName: 'Status',
        width: 120,
        renderCell: ({ row }) => (
          <StatusChip label={row.isActive ? 'Active' : 'Inactive'} tone={row.isActive ? 'success' : 'default'} />
        ),
      },
      {
        field: 'actions',
        disableColumnMenu: true,
        headerName: 'Action',
        width: 140,
        sortable: false,
        align: 'center',
        headerAlign: 'center',
        renderCell: ({ row }) => {
          const inUse = row.userCount > 0;
          return (
            <Stack
              direction="row"
              spacing={0.5}
              sx={{ height: '100%', alignItems: 'center', justifyContent: 'center' }}
            >
              <Tooltip title="View">
                <IconButton
                  size="small"
                  component={RouterLink}
                  to={PATHS.users.roleProfileView(row.id)}
                  aria-label={`View ${row.groupName}`}
                >
                  <VisibilityOutlinedIcon fontSize="small" />
                </IconButton>
              </Tooltip>
              <Can right="role.edit">
                <Tooltip title="Edit">
                  <IconButton
                    size="small"
                    component={RouterLink}
                    to={PATHS.users.roleProfileEdit(row.id)}
                    aria-label={`Edit ${row.groupName}`}
                  >
                    <EditOutlinedIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              </Can>
              <Can right="role.delete">
                <Tooltip title={inUse ? 'Remove it from every user before deleting' : 'Delete'}>
                  <span>
                    <IconButton
                      size="small"
                      color="error"
                      aria-label={`Delete ${row.groupName}`}
                      disabled={inUse || remove.isPending}
                      onClick={() => {
                        void confirm({
                          title: 'Delete role profile?',
                          message: `${row.groupName} will be permanently deleted. Its roles are not deleted.`,
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
            </Stack>
          );
        },
      },
    ],
    [confirm, remove, me.isSuperAdmin, companies.data],
  );

  return (
    <>
      <PageHeader
        title="Role profiles"
        subtitle="A role profile is a set of roles. Give it to a user to give all its roles at once."
        actions={
          <Can right="role.add">
            <Button component={RouterLink} to={PATHS.users.roleProfileNew} variant="contained" startIcon={<AddIcon />}>
              New role profile
            </Button>
          </Can>
        }
      />
      <Box sx={{ mb: 2 }}>
        <TextField
          label="Search"
          placeholder="Profile name, role or description"
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
          sx={{ width: '100%', maxWidth: 360, bgcolor: 'background.paper' }}
          slotProps={{ htmlInput: { maxLength: 100, 'aria-label': 'Search role profiles' } }}
        />
      </Box>
      <AppDataGrid<RoleGroupResponse>
        ariaLabel="Role profiles"
        rows={rows}
        columns={columns}
        getRowId={(row) => row.id}
        loading={groups.isPending}
        error={groups.error}
        onRetry={() => void groups.refetch()}
        fillViewport
      />
    </>
  );
}
