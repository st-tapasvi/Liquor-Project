import Paper from '@mui/material/Paper';
import {
  DataGrid,
  type GridColDef,
  type GridPaginationModel,
  type GridRowId,
  type GridValidRowModel,
} from '@mui/x-data-grid';
import { type RefObject, useLayoutEffect, useRef, useState } from 'react';

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
  fillViewport?: boolean;
}

const paginationModel = { page: 0, pageSize: DEFAULT_PAGE_SIZE };

const DEFAULT_HEIGHT = 400;
const BOTTOM_GAP = 24;

function useViewportFillHeight(ref: RefObject<HTMLDivElement | null>, enabled: boolean): number {
  const [height, setHeight] = useState(DEFAULT_HEIGHT);

  useLayoutEffect(() => {
    const element = ref.current;
    if (!enabled || !element) return;

    const update = () => {
      const top = element.getBoundingClientRect().top + window.scrollY;
      setHeight(Math.max(DEFAULT_HEIGHT, Math.floor(window.innerHeight - top - BOTTOM_GAP)));
    };

    update();
    window.addEventListener('resize', update);
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(update);
    if (element.parentElement) observer?.observe(element.parentElement);
    return () => {
      window.removeEventListener('resize', update);
      observer?.disconnect();
    };
  }, [ref, enabled]);

  return height;
}

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
  fillViewport = false,
}: AppDataGridProps<TRow>) {
  const paperRef = useRef<HTMLDivElement>(null);
  const height = useViewportFillHeight(paperRef, fillViewport && !error);

  if (error) {
    return <ErrorState error={error} {...(onRetry ? { onRetry } : {})} />;
  }

  return (
    <Paper ref={paperRef} elevation={1} sx={{ height, width: '100%' }}>
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
