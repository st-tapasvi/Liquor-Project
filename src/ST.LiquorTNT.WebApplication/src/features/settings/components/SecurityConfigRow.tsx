import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';
import TableCell from '@mui/material/TableCell';
import TableRow from '@mui/material/TableRow';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import { useState } from 'react';

import type { SecurityConfigResponse } from '@/core/api';
import { isValidationError } from '@/core/errors';

import { formatDateTime } from '@/shared/utils';

import { validateConfigValue } from '../settings.schema';

interface SecurityConfigRowProps {
  entry: SecurityConfigResponse;
  canEdit: boolean;
  onSave: (key: string, value: string) => Promise<void>;
  busy: boolean;
}

/** One editable setting. Edits are per row so a mistake in one key cannot touch another. */
export function SecurityConfigRow({ entry, canEdit, onSave, busy }: SecurityConfigRowProps) {
  const [editing, setEditing] = useState(false);
  const [value, setValue] = useState(entry.value);
  const [error, setError] = useState<string | null>(null);

  const handleSave = async () => {
    const problem = validateConfigValue(entry.dataType, value);
    if (problem) {
      setError(problem);
      return;
    }
    try {
      await onSave(entry.key, value.trim());
      setEditing(false);
      setError(null);
    } catch (e) {
      setError(isValidationError(e) ? (e.errors['value']?.join(' ') ?? 'Invalid value.') : 'Could not save.');
    }
  };

  const isDangerous = entry.key === 'ADMIN_ROLE_ID';

  return (
    <TableRow>
      <TableCell sx={{ fontFamily: 'monospace', whiteSpace: 'nowrap' }}>{entry.key}</TableCell>
      <TableCell sx={{ minWidth: 160 }}>
        {editing ? (
          <TextField
            value={value}
            onChange={(e) => setValue(e.target.value)}
            error={error !== null}
            helperText={error ?? (isDangerous ? 'Careful: a role nobody has locks every admin out.' : undefined)}
            size="small"
            slotProps={{ htmlInput: { 'aria-label': `Value for ${entry.key}`, maxLength: 200 } }}
            autoFocus
          />
        ) : (
          <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
            {entry.value}
          </Typography>
        )}
      </TableCell>
      <TableCell>{entry.dataType}</TableCell>
      <TableCell sx={{ maxWidth: 420 }}>
        <Typography variant="body2" color="text.secondary">
          {entry.description ?? ''}
        </Typography>
      </TableCell>
      <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDateTime(entry.updatedAt)}</TableCell>
      <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
        {canEdit &&
          (editing ? (
            <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
              <Button
                size="small"
                onClick={() => {
                  setEditing(false);
                  setValue(entry.value);
                  setError(null);
                }}
                disabled={busy}
              >
                Cancel
              </Button>
              <Button size="small" variant="contained" onClick={() => void handleSave()} disabled={busy}>
                Save
              </Button>
            </Stack>
          ) : (
            <Button size="small" onClick={() => setEditing(true)} aria-label={`Edit ${entry.key}`}>
              Edit
            </Button>
          ))}
      </TableCell>
    </TableRow>
  );
}
