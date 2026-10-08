import type { RoleResponse } from '@/core/api';

import { type AppGridColumn, AppDataGrid } from '@/shared/components/data-grid';
import { PageHeader, StatusChip } from '@/shared/components/ui';

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

export default function RoleListPage() {
  const roles = useRoles();
  return (
    <>
      <PageHeader title="Roles" subtitle="Each role is a set of rights. A user gets roles per supplier code." />
      <AppDataGrid<RoleResponse>
        ariaLabel="Roles"
        rows={roles.data}
        columns={columns}
        getRowId={(row) => row.id}
        loading={roles.isPending}
        error={roles.error}
        onRetry={() => void roles.refetch()}
      />
    </>
  );
}
