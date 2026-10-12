import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Checkbox from '@mui/material/Checkbox';
import Chip from '@mui/material/Chip';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import FormControlLabel from '@mui/material/FormControlLabel';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import type { PageActionResponse, PageResponse } from '@/core/api';

import { ChangeDot } from './ChangeDot';
import { isChanged, isGrantable, setActions } from './role-rights';

interface PageActionsDialogProps {
  page: PageResponse | null;
  selected: ReadonlySet<number>;
  baseline: ReadonlySet<number>;
  canManageAdmins: boolean;
  disabled?: boolean;
  onChange: (selected: Set<number>) => void;
  onClose: () => void;
}

function scopeLabel(action: PageActionResponse): string | null {
  if (action.grantScope === 'SYSTEM') return 'Admin role only';
  if (action.grantScope === 'ADMIN') return 'Needs Manage admin users';
  return null;
}

export function PageActionsDialog({
  page,
  selected,
  baseline,
  canManageAdmins,
  disabled = false,
  onChange,
  onClose,
}: PageActionsDialogProps) {
  const grantableIds = (page?.actions ?? []).filter((a) => isGrantable(a, canManageAdmins)).map((a) => a.pageActionId);

  return (
    <Dialog open={page !== null} onClose={onClose} aria-labelledby="page-actions-title" fullWidth maxWidth="sm">
      {page && (
        <>
          <DialogTitle id="page-actions-title">
            {page.pageName}
            <Typography variant="body2" color="text.secondary">
              {[page.moduleName, page.applicationType].filter(Boolean).join(' · ')}
            </Typography>
          </DialogTitle>
          <DialogContent dividers>
            {!disabled && grantableIds.length > 0 && (
              <Stack direction="row" spacing={1} sx={{ mb: 1.5 }}>
                <Button
                  size="small"
                  variant="outlined"
                  onClick={() => onChange(setActions(grantableIds, true, selected))}
                >
                  Select all
                </Button>
                <Button
                  size="small"
                  variant="outlined"
                  onClick={() => onChange(setActions(grantableIds, false, selected))}
                >
                  Unselect all
                </Button>
              </Stack>
            )}
            <Box
              role="group"
              aria-label={`Actions of ${page.pageName}`}
              sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))', columnGap: 2 }}
            >
              {page.actions.map((action) => {
                const scope = scopeLabel(action);
                const changed = isChanged(action.pageActionId, selected, baseline);
                return (
                  <Stack key={action.pageActionId} direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
                    <FormControlLabel
                      label={action.actionName}
                      disabled={disabled || !isGrantable(action, canManageAdmins)}
                      control={
                        <Checkbox
                          size="small"
                          checked={selected.has(action.pageActionId)}
                          onChange={(e) => onChange(setActions([action.pageActionId], e.target.checked, selected))}
                          slotProps={{ input: { 'aria-label': `${page.pageName}: ${action.actionName}` } }}
                        />
                      }
                      sx={{ mr: 0 }}
                    />
                    {changed && <ChangeDot />}
                    {scope && <Chip label={scope} size="small" variant="outlined" />}
                  </Stack>
                );
              })}
            </Box>
          </DialogContent>
          <DialogActions>
            <Button variant="contained" onClick={onClose}>
              Done
            </Button>
          </DialogActions>
        </>
      )}
    </Dialog>
  );
}
