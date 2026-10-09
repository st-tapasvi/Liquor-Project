import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Checkbox from '@mui/material/Checkbox';
import FormControlLabel from '@mui/material/FormControlLabel';
import FormHelperText from '@mui/material/FormHelperText';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import type { RoleOption } from './role-options';

interface RoleCheckboxListProps {
  options: readonly RoleOption[];
  value: readonly number[];
  onChange: (roleIds: number[]) => void;
  disabled?: boolean;
  error?: string | undefined;
  emptyText?: string;
}

export function RoleCheckboxList({
  options,
  value,
  onChange,
  disabled = false,
  error,
  emptyText = 'No roles available.',
}: RoleCheckboxListProps) {
  const selected = new Set(value);
  const sorted = [...options].sort((a, b) => a.label.localeCompare(b.label));
  const editable = sorted.filter((o) => !o.locked);

  const toggle = (id: number, checked: boolean) => {
    const next = new Set(selected);
    if (checked) next.add(id);
    else next.delete(id);
    onChange([...next]);
  };

  const setAll = (checked: boolean) => {
    const next = new Set(selected);
    for (const o of editable) {
      if (checked) next.add(o.id);
      else next.delete(o.id);
    }
    onChange([...next]);
  };

  if (sorted.length === 0) {
    return (
      <Box>
        <Typography variant="body2" color="text.secondary">
          {emptyText}
        </Typography>
        {error && <FormHelperText error>{error}</FormHelperText>}
      </Box>
    );
  }

  return (
    <Box>
      {!disabled && editable.length > 0 && (
        <Stack direction="row" spacing={1} sx={{ mb: 1.5 }}>
          <Button size="small" variant="outlined" onClick={() => setAll(true)}>
            Select all
          </Button>
          <Button size="small" variant="outlined" onClick={() => setAll(false)}>
            Unselect all
          </Button>
        </Stack>
      )}
      <Box
        role="group"
        aria-label="Roles"
        sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(240px, 1fr))', columnGap: 2 }}
      >
        {sorted.map((o) => (
          <FormControlLabel
            key={o.id}
            label={o.label}
            disabled={disabled || o.locked === true}
            control={
              <Checkbox size="small" checked={selected.has(o.id)} onChange={(e) => toggle(o.id, e.target.checked)} />
            }
          />
        ))}
      </Box>
      {error && <FormHelperText error>{error}</FormHelperText>}
    </Box>
  );
}
