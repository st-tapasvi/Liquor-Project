import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import { useState } from 'react';

import type { SecurityConfigResponse } from '@/core/api';
import { isValidationError } from '@/core/errors';

import { AppDataGrid, type AppGridColumn } from '@/shared/components/data-grid';
import { useSnackbar } from '@/shared/hooks';
import { formatDateTime } from '@/shared/utils';

import { validateConfigValue } from '../settings.schema';

interface SecurityConfigGridProps {
  rows: readonly SecurityConfigResponse[] | undefined;
  loading: boolean;
  error: unknown;
  onRetry: () => void;
  canEdit: boolean;
  busy: boolean;
  onSave: (key: string, value: string) => Promise<void>;
}

export function SecurityConfigGrid({ rows, loading, error, onRetry, canEdit, busy, onSave }: SecurityConfigGridProps) {
  const snackbar = useSnackbar();
  const [editingKey, setEditingKey] = useState<string | null>(null);
  const [draft, setDraft] = useState('');
  const [problem, setProblem] = useState<string | null>(null);

  const startEdit = (entry: SecurityConfigResponse) => {
    setEditingKey(entry.key);
    setDraft(entry.value);
    setProblem(null);
    if (entry.key === 'ADMIN_ROLE_ID') snackbar.warning('Careful: a role nobody has locks every admin out.');
  };

  const cancel = () => {
    setEditingKey(null);
    setProblem(null);
  };

  const fail = (message: string) => {
    setProblem(message);
    snackbar.error(message);
  };

  const save = async (entry: SecurityConfigResponse) => {
    const invalid = validateConfigValue(entry.dataType, draft);
    if (invalid) {
      fail(invalid);
      return;
    }
    try {
      await onSave(entry.key, draft.trim());
      cancel();
    } catch (e) {
      fail(isValidationError(e) ? (e.errors['value']?.join(' ') ?? 'Invalid value.') : 'Could not save.');
    }
  };

  const columns: AppGridColumn<SecurityConfigResponse>[] = [
    { field: 'key', headerName: 'Key', width: 240, rowHeader: true },
    {
      field: 'value',
      headerName: 'Value',
      width: 190,
      sortable: false,
      renderCell: ({ row }) =>
        row.key === editingKey ? (
          <TextField
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onKeyDown={(e) => e.stopPropagation()}
            error={problem !== null}
            slotProps={{ htmlInput: { 'aria-label': `Value for ${row.key}`, maxLength: 200 } }}
            autoFocus
          />
        ) : (
          row.value
        ),
    },
    { field: 'dataType', headerName: 'Type', width: 100 },
    { field: 'description', headerName: 'Description', flex: 1, minWidth: 200, sortable: false },
    {
      field: 'updatedAt',
      headerName: 'Updated',
      width: 150,
      valueFormatter: (v: string | null) => formatDateTime(v),
    },
    {
      field: 'actions',
      disableColumnMenu: true,
      headerName: '',
      width: 160,
      sortable: false,
      align: 'right',
      renderCell: ({ row }) => {
        if (!canEdit) return null;
        if (row.key !== editingKey) {
          return (
            <Button
              size="small"
              onClick={() => startEdit(row)}
              disabled={editingKey !== null}
              aria-label={`Edit ${row.key}`}
            >
              Edit
            </Button>
          );
        }
        return (
          <Stack direction="row" spacing={1} sx={{ height: '100%', alignItems: 'center', justifyContent: 'flex-end' }}>
            <Button size="small" onClick={cancel} disabled={busy}>
              Cancel
            </Button>
            <Button size="small" variant="contained" onClick={() => void save(row)} disabled={busy}>
              Save
            </Button>
          </Stack>
        );
      },
    },
  ];

  return (
    <AppDataGrid<SecurityConfigResponse>
      ariaLabel="Security settings"
      rows={rows}
      columns={columns}
      getRowId={(row) => row.key}
      loading={loading}
      error={error}
      onRetry={onRetry}
    />
  );
}
