import Paper from '@mui/material/Paper';
import {
  DataGrid,
  type GridColDef,
  type GridPaginationModel,
  type GridRowId,
  type GridValidRowModel,
} from '@mui/x-data-grid';

import { tokens } from '@/core/theme';

import { DEFAULT_PAGE_SIZE, PAGE_SIZE_OPTIONS } from '@/shared/constants';

import { ErrorState } from '../feedback';

export type AppGridColumn<TRow extends GridValidRowModel> = GridColDef<TRow>;

export interface ServerPaging {
  rowCount: number;
  paginationModel: GridPaginationModel;
  onPaginationModelChange: (model: GridPaginationModel) => void;
}

interface AppDataGridProps<TRow extends GridValidRowModel> {
  rows: readonly TRow[] | undefined;
  columns: readonly AppGridColumn<TRow>[];
  getRowId: (row: TRow) => GridRowId;
  ariaLabel: string;
  loading?: boolean;
  error?: unknown;
  onRetry?: () => void;
  server?: ServerPaging;
}

const paginationModel = { page: 0, pageSize: DEFAULT_PAGE_SIZE };

const gridSx = {
  border: 0,
  '& .MuiDataGrid-columnHeader, & .MuiDataGrid-columnHeaders .MuiDataGrid-filler, & .MuiDataGrid-scrollbarFiller--header':
    { backgroundColor: tokens.shell.sidebarDivider },
  '& .MuiDataGrid-columnHeader': { color: tokens.color.gridHeaderText },
  '& .MuiDataGrid-columnHeader .MuiIconButton-root, & .MuiDataGrid-columnHeader .MuiCheckbox-root': {
    color: tokens.color.gridHeaderIcon,
  },
  '& .MuiDataGrid-columnHeader .MuiCheckbox-root.Mui-checked, & .MuiDataGrid-columnHeader .MuiCheckbox-indeterminate': {
    color: tokens.color.gridHeaderText,
  },
  '& .MuiDataGrid-columnHeader .MuiIconButton-root:hover': { backgroundColor: tokens.shell.sidebarHoverBg },
  '& .MuiDataGrid-columnSeparator': { color: tokens.color.gridSeparator, opacity: 1 },
  '& .MuiDataGrid-columnHeader .MuiDataGrid-menuIcon': { width: 'auto', visibility: 'visible' },
  '& .MuiDataGrid-row.odd:not(:hover):not(.Mui-selected)': { backgroundColor: tokens.color.gridStripe },
} as const;

export function AppDataGrid<TRow extends GridValidRowModel>({
  rows,
  columns,
  getRowId,
  ariaLabel,
  loading = false,
  error,
  onRetry,
  server,
}: AppDataGridProps<TRow>) {
  if (error) {
    return <ErrorState error={error} {...(onRetry ? { onRetry } : {})} />;
  }

  return (
    <Paper elevation={1} sx={{ height: 400, width: '100%' }}>
      <DataGrid<TRow>
        aria-label={ariaLabel}
        rows={rows ?? []}
        columns={columns as GridColDef<TRow>[]}
        getRowId={getRowId}
        loading={loading}
        initialState={{ pagination: { paginationModel } }}
        pageSizeOptions={[...PAGE_SIZE_OPTIONS]}
        checkboxSelection
        getRowClassName={({ indexRelativeToCurrentPage }) => (indexRelativeToCurrentPage % 2 ? 'odd' : 'even')}
        sx={gridSx}
        {...(server
          ? {
              paginationMode: 'server' as const,
              rowCount: server.rowCount,
              paginationModel: server.paginationModel,
              onPaginationModelChange: server.onPaginationModelChange,
            }
          : {})}
      />
    </Paper>
  );
}
