import ArrowBackRounded from '@mui/icons-material/ArrowBackRounded';
import LockOutlined from '@mui/icons-material/LockOutlined';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import { Fragment } from 'react';
import { Link as RouterLink, useLocation, useNavigate } from 'react-router';

import { useCurrentUser } from '@/core/auth';
import { PATHS } from '@/core/router';

const PAGE_NAMES: Readonly<Record<string, string>> = {
  user: 'User',
  role: 'Role',
  company: 'Company',
  suppliercode: 'Supplier code',
  liquorcategory: 'Liquor category',
  securityconfig: 'Security settings',
  passwordpolicy: 'Password policy',
};

const ACTION_NAMES: Readonly<Record<string, string>> = {
  view: 'View',
  add: 'Create',
  edit: 'Edit',
  delete: 'Delete',
  status: 'Activate / deactivate',
  unlock: 'Unlock',
  access: 'Assign roles',
  manageadmin: 'Manage admins',
};

function readState(state: unknown): { from: string; rights: string[] } | null {
  if (typeof state !== 'object' || state === null) return null;
  const { from, rights } = state as Record<string, unknown>;
  if (typeof from !== 'string' || !Array.isArray(rights)) return null;
  return { from, rights: rights.filter((r): r is string => typeof r === 'string') };
}

function unique(values: readonly string[]): string {
  return [...new Set(values)].join(' or ');
}

export function Forbidden() {
  const location = useLocation();
  const navigate = useNavigate();
  const user = useCurrentUser();
  const blocked = readState(location.state);
  const keys = (blocked?.rights ?? []).map((key) => key.split('.'));
  const pageName = unique(keys.map(([page = '']) => PAGE_NAMES[page] ?? page));
  const actionName = unique(keys.map(([, action = '']) => ACTION_NAMES[action] ?? action));
  const supplierCode = user.activeSupplierCode?.displayName;

  return (
    <Stack spacing={2} sx={{ alignItems: 'center', textAlign: 'center', px: 2, py: { xs: 6, md: 10 } }}>
      <Avatar sx={{ width: 72, height: 72, bgcolor: '#FDECEC', color: 'error.main' }}>
        <LockOutlined sx={{ fontSize: 36 }} />
      </Avatar>
      <Typography variant="overline" color="error.main" sx={{ fontWeight: 700, letterSpacing: 2 }}>
        Access denied
      </Typography>
      <Typography variant="h4" component="h1">
        You do not have access to this screen
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mt: -1 }}>
        {blocked
          ? 'Ask your administrator to add this right to one of your roles.'
          : 'Ask an administrator if you need this right.'}
      </Typography>
      {blocked && keys.length > 0 && (
        <Box
          component="dl"
          sx={{
            display: 'grid',
            gridTemplateColumns: 'auto auto',
            columnGap: 2,
            rowGap: 0.75,
            m: 0,
            textAlign: 'left',
            '& dt': { color: 'text.secondary', textAlign: 'right' },
            '& dd': { m: 0, fontWeight: 600 },
          }}
        >
          {[
            ['Page', pageName],
            ['Action', actionName],
            ['Supplier code', supplierCode],
          ].map(
            ([label, value]) =>
              value && (
                <Fragment key={label}>
                  <Typography component="dt" variant="body1">
                    {label}
                  </Typography>
                  <Typography component="dd" variant="body1">
                    {value}
                  </Typography>
                </Fragment>
              ),
          )}
        </Box>
      )}
      <Stack direction="row" spacing={1.5} sx={{ pt: 1 }}>
        <Button variant="outlined" startIcon={<ArrowBackRounded />} onClick={() => void navigate(-1)}>
          Go back
        </Button>
        <Button component={RouterLink} to={PATHS.dashboard} variant="contained">
          Go to dashboard
        </Button>
      </Stack>
    </Stack>
  );
}
