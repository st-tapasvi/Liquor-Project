import type { CompanyResponse } from '@/core/api';

import { type AppGridColumn, AppDataGrid } from '@/shared/components/data-grid';
import { PageHeader, StatusChip } from '@/shared/components/ui';

import { useCompanies } from './api/company.queries';

const columns: AppGridColumn<CompanyResponse>[] = [
  { field: 'companyName', headerName: 'Company', flex: 1.4, minWidth: 200, rowHeader: true },
  { field: 'aliasName', headerName: 'Alias', flex: 1, minWidth: 150 },
  { field: 'city', headerName: 'City', width: 160 },
  { field: 'supplierCodeCount', headerName: 'Supplier codes', width: 150, type: 'number' },
  {
    field: 'isActive',
    headerName: 'Status',
    width: 130,
    renderCell: ({ row }) => (
      <StatusChip label={row.isActive ? 'Active' : 'Inactive'} tone={row.isActive ? 'success' : 'default'} />
    ),
  },
];

export default function CompanyPage() {
  const companies = useCompanies();
  return (
    <>
      <PageHeader title="Companies" subtitle="Customers of this installation and how many supplier codes each has." />
      <AppDataGrid<CompanyResponse>
        ariaLabel="Companies"
        rows={companies.data}
        columns={columns}
        getRowId={(row) => row.id}
        loading={companies.isPending}
        error={companies.error}
        onRetry={() => void companies.refetch()}
      />
    </>
  );
}
