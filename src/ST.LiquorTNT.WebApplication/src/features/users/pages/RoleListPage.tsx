import AddIcon from '@mui/icons-material/Add';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import { useMemo, useState } from 'react';
import { Link as RouterLink } from 'react-router';

import type { RoleResponse } from '@/core/api';
import { Can } from '@/core/auth';
import { PATHS } from '@/core/router';

import { type AppGridColumn, AppDataGrid } from '@/shared/components/data-grid';
import { PageHeader, StatusChip } from '@/shared/components/ui';
import { useDebounce } from '@/shared/hooks';

import { useRoles } from '../api/users.queries';

function roleKind(role: RoleResponse): string {
  if (role.isSystem) return 'System';
  if (role.isTemplate) return 'Default template';
  return role.isAdminRole ? 'Admin role' : 'Company role';
}

const columns: AppGridColumn<RoleResponse>[] = [
  { field: 'roleName', headerName: 'Role', flex: 1, minWidth: 180, rowHeader: true },
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
];

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
