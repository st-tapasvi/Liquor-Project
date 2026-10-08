import type { LiquorCategoryResponse } from '@/core/api';

import { type AppGridColumn, AppDataGrid } from '@/shared/components/data-grid';
import { PageHeader, StatusChip } from '@/shared/components/ui';

import { useLiquorCategories } from './api/company.queries';

const columns: AppGridColumn<LiquorCategoryResponse>[] = [
  { field: 'categoryCode', headerName: 'Code', width: 120, rowHeader: true },
  { field: 'categoryName', headerName: 'Name', flex: 1, minWidth: 220 },
  { field: 'description', headerName: 'Description', flex: 2, minWidth: 260, sortable: false },
  {
    field: 'isActive',
    headerName: 'Status',
    width: 120,
    renderCell: ({ row }) => (
      <StatusChip label={row.isActive ? 'Active' : 'Inactive'} tone={row.isActive ? 'success' : 'default'} />
    ),
  },
];

export default function LiquorCategoryPage() {
  const categories = useLiquorCategories();
  return (
    <>
      <PageHeader title="Liquor categories" subtitle="CL, IMFL, FL… used by supplier codes." />
      <AppDataGrid<LiquorCategoryResponse>
        ariaLabel="Liquor categories"
        rows={categories.data}
        columns={columns}
        getRowId={(row) => row.id}
        loading={categories.isPending}
        error={categories.error}
        onRetry={() => void categories.refetch()}
      />
    </>
  );
}
