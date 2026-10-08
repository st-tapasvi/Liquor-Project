import Box from '@mui/material/Box';
import TextField from '@mui/material/TextField';
import { useEffect, useMemo, useState } from 'react';

import type { SupplierCodeResponse } from '@/core/api';

import { type AppGridColumn, AppDataGrid, useServerGrid } from '@/shared/components/data-grid';
import { PageHeader, StatusChip } from '@/shared/components/ui';
import { useDebounce } from '@/shared/hooks';
import { formatDateTime } from '@/shared/utils';

import { useSupplierCodes } from './api/company.queries';

const columns: AppGridColumn<SupplierCodeResponse>[] = [
  { field: 'displayName', headerName: 'Supplier code', width: 170, rowHeader: true },
  { field: 'companyName', headerName: 'Company', flex: 1.2, minWidth: 180 },
  { field: 'franchiseName', headerName: 'Franchise', flex: 1, minWidth: 150 },
  { field: 'exciseCode', headerName: 'Excise', width: 100 },
  { field: 'liquorCategoryCode', headerName: 'Category', width: 130 },
  {
    field: 'isActive',
    headerName: 'Status',
    width: 120,
    renderCell: ({ row }) => (
      <StatusChip label={row.isActive ? 'Active' : 'Inactive'} tone={row.isActive ? 'success' : 'default'} />
    ),
  },
  {
    field: 'createdAt',
    headerName: 'Created',
    width: 160,
    valueFormatter: (v: string | null) => formatDateTime(v),
  },
];

export default function SupplierCodePage() {
  const grid = useServerGrid();
  const [searchText, setSearchText] = useState(grid.state.search);
  const debounced = useDebounce(searchText, 300);

  useEffect(() => {
    if (debounced !== grid.state.search) grid.setSearch(debounced);
  }, [debounced]);

  const params = useMemo(
    () => ({
      page: grid.state.page,
      pageSize: grid.state.pageSize,
      ...(grid.state.search ? { search: grid.state.search } : {}),
    }),
    [grid.state.page, grid.state.pageSize, grid.state.search],
  );
  const codes = useSupplierCodes(params);

  return (
    <>
      <PageHeader
        title="Supplier codes"
        subtitle="Each supplier code is one licence: a company in one excise for one liquor category."
      />
      <Box sx={{ mb: 2 }}>
        <TextField
          label="Search"
          placeholder="Supplier code, franchise or company"
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
          sx={{ width: '100%', maxWidth: 360, bgcolor: 'background.paper' }}
          slotProps={{ htmlInput: { maxLength: 100, 'aria-label': 'Search supplier codes' } }}
        />
      </Box>
      <AppDataGrid<SupplierCodeResponse>
        ariaLabel="Supplier codes"
        rows={codes.data?.items}
        columns={columns}
        getRowId={(row) => row.id}
        loading={codes.isPending || codes.isFetching}
        error={codes.error}
        onRetry={() => void codes.refetch()}
        server={grid.paging(codes.data?.totalCount ?? 0)}
      />
    </>
  );
}
