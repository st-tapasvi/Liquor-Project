import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Checkbox from '@mui/material/Checkbox';
import Chip from '@mui/material/Chip';
import Divider from '@mui/material/Divider';
import Link from '@mui/material/Link';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import { Fragment, useState } from 'react';

import type { PageResponse } from '@/core/api';

import { ChangeDot } from './ChangeDot';
import { PageActionsDialog } from './PageActionsDialog';
import {
  APPLICATION_TYPES,
  changeCount,
  isGrantable,
  pageChanged,
  pageSelection,
  setActions,
  togglePage,
} from './role-rights';

interface RolePagesPickerProps {
  pages: readonly PageResponse[];
  selected: ReadonlySet<number>;
  baseline: ReadonlySet<number>;
  canManageAdmins: boolean;
  disabled?: boolean;
  onChange: (selected: Set<number>) => void;
}

export function RolePagesPicker({
  pages,
  selected,
  baseline,
  canManageAdmins,
  disabled = false,
  onChange,
}: RolePagesPickerProps) {
  const [openPageId, setOpenPageId] = useState<number | null>(null);
  const openPage = pages.find((p) => p.pageId === openPageId) ?? null;
  const changes = changeCount(pages, selected, baseline);
  const grantableIds = pages.flatMap((p) =>
    p.actions.filter((a) => isGrantable(a, canManageAdmins)).map((a) => a.pageActionId),
  );

  return (
    <Box>
      <Stack direction="row" spacing={1} sx={{ mb: 2, alignItems: 'center', flexWrap: 'wrap' }}>
        {!disabled && (
          <>
            <Button size="small" variant="outlined" onClick={() => onChange(setActions(grantableIds, true, selected))}>
              Select all
            </Button>
            <Button size="small" variant="outlined" onClick={() => onChange(setActions(grantableIds, false, selected))}>
              Unselect all
            </Button>
          </>
        )}
        {changes > 0 && (
          <Chip
            size="small"
            color="warning"
            variant="outlined"
            label={
              <Stack component="span" direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
                <ChangeDot />
                <span>{`${changes} unsaved change${changes === 1 ? '' : 's'}`}</span>
              </Stack>
            }
          />
        )}
      </Stack>

      {APPLICATION_TYPES.map((type, index) => {
        const group = pages.filter((p) => p.applicationType === type.value);
        return (
          <Fragment key={type.value}>
            {index > 0 && <Divider sx={{ my: 2 }} />}
            <Typography variant="subtitle2" sx={{ mb: 1 }}>
              {type.label}
            </Typography>
            {group.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                No pages.
              </Typography>
            ) : (
              <Box
                role="group"
                aria-label={type.label}
                sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(240px, 1fr))', columnGap: 2 }}
              >
                {group.map((page) => {
                  const state = pageSelection(page, selected);
                  const hasGrantable = page.actions.some((a) => isGrantable(a, canManageAdmins));
                  return (
                    <Stack key={page.pageId} direction="row" spacing={0.5} sx={{ alignItems: 'center', minHeight: 40 }}>
                      <Checkbox
                        size="small"
                        checked={state.checked}
                        indeterminate={state.indeterminate}
                        disabled={disabled || !hasGrantable}
                        onChange={() => onChange(togglePage(page, selected, canManageAdmins))}
                        slotProps={{ input: { 'aria-label': `All actions of ${page.pageName}` } }}
                      />
                      <Link
                        component="button"
                        type="button"
                        underline="hover"
                        color="text.primary"
                        onClick={() => setOpenPageId(page.pageId)}
                        sx={{ textAlign: 'left' }}
                      >
                        {page.pageName}
                      </Link>
                      <Typography variant="caption" color="text.secondary">
                        {state.count}/{page.actions.length}
                      </Typography>
                      {pageChanged(page, selected, baseline) && <ChangeDot />}
                    </Stack>
                  );
                })}
              </Box>
            )}
          </Fragment>
        );
      })}

      <PageActionsDialog
        page={openPage}
        selected={selected}
        baseline={baseline}
        canManageAdmins={canManageAdmins}
        disabled={disabled}
        onChange={onChange}
        onClose={() => setOpenPageId(null)}
      />
    </Box>
  );
}
