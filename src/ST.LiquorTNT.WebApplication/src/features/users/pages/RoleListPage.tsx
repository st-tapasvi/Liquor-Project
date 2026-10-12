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

import type { RoleResponse } from '@/core/api';
import { Can, useCurrentUser } from '@/core/auth';
import { PATHS } from '@/core/router';

import { type AppGridColumn, AppDataGrid } from '@/shared/components/data-grid';
import { PageHeader, StatusChip } from '@/shared/components/ui';
import { useConfirm, useDebounce } from '@/shared/hooks';

import { useDeleteRole } from '../api/users.mutations';
import { useRoles } from '../api/users.queries';

function roleKind(role: RoleResponse): string {
  if (role.isSystem) return 'System';
  if (role.isTemplate) return 'Default template';
  return role.isAdminRole ? 'Admin role' : 'Company role';
}

function matchesSearch(role: RoleResponse, term: string): boolean {
  return [role.roleName, role.displayName, role.description, role.supplierCodeName].some((v) =>
    v?.toLowerCase().includes(term),
  );
}

export default function RoleListPage() {
  const roles = useRoles();
  const [searchText, setSearchText] = useState('');
  const search = useDebounce(searchText, 300).trim().toLowerCase();
  const rows = useMemo(
    () => (search ? roles.data?.filter((r) => matchesSearch(r, search)) : roles.data),
    [roles.data, search],
  );
  const me = useCurrentUser();
  const confirm = useConfirm();
  const remove = useDeleteRole();

  const columns = useMemo<AppGridColumn<RoleResponse>[]>(
    () => [
      {
        field: 'roleName',
        headerName: 'Role',
        flex: 1,
        minWidth: 180,
        rowHeader: true,
        valueGetter: (_v, row) => row.displayName || row.roleName,
        renderCell: ({ row, value }) => (
          <Link component={RouterLink} to={PATHS.users.roleView(row.id)} underline="hover">
            {String(value)}
          </Link>
        ),
      },
      { field: 'description', headerName: 'Description', flex: 2, minWidth: 260, sortable: false },
      { field: 'kind', headerName: 'Type', width: 160, valueGetter: (_v, row) => roleKind(row) },
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
          const locked = row.isSystem || (row.isTemplate && !me.isSuperAdmin);
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
                  to={PATHS.users.roleView(row.id)}
                  aria-label={`View ${row.displayName}`}
                >
                  <VisibilityOutlinedIcon fontSize="small" />
                </IconButton>
              </Tooltip>
              <Can right="role.edit">
                <Tooltip title={locked ? 'This role cannot be changed' : 'Edit'}>
                  <span>
                    <IconButton
                      size="small"
                      component={RouterLink}
                      to={PATHS.users.roleEdit(row.id)}
                      aria-label={`Edit ${row.displayName}`}
                      disabled={locked}
                    >
                      <EditOutlinedIcon fontSize="small" />
                    </IconButton>
                  </span>
                </Tooltip>
              </Can>
              <Can right="role.delete">
                <Tooltip title={locked ? 'This role cannot be deleted' : 'Delete'}>
                  <span>
                    <IconButton
                      size="small"
                      color="error"
                      aria-label={`Delete ${row.displayName}`}
                      disabled={locked || remove.isPending}
                      onClick={() => {
                        void confirm({
                          title: 'Delete role?',
                          message: `${row.displayName} and its rights will be permanently deleted. A role still given to users or inside a role group cannot be deleted.`,
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
    [confirm, remove, me.isSuperAdmin],
  );

  return (
    <>
      <PageHeader
        title="Roles"
        subtitle="Each role is a set of rights. A user gets roles per supplier code."
        actions={
          <Can right="role.add">
            <Button component={RouterLink} to={PATHS.users.roleNew} variant="contained" startIcon={<AddIcon />}>
              New role
            </Button>
          </Can>
        }
      />
      <Box sx={{ mb: 2 }}>
        <TextField
          label="Search"
          placeholder="Role name, supplier code or description"
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
          sx={{ width: '100%', maxWidth: 360, bgcolor: 'background.paper' }}
          slotProps={{ htmlInput: { maxLength: 100, 'aria-label': 'Search roles' } }}
        />
      </Box>
      <AppDataGrid<RoleResponse>
        ariaLabel="Roles"
        rows={rows}
        columns={columns}
        getRowId={(row) => row.id}
        loading={roles.isPending}
        error={roles.error}
        onRetry={() => void roles.refetch()}
        fillViewport
      />
    </>
  );
}
