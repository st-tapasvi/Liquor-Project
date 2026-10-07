import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import type { ReactNode } from 'react';
import { Link as RouterLink } from 'react-router';

import { type ModuleKey, useModuleEnabled, useModuleFlagsLoaded } from '../../modules';
import { LoadingFallback } from '../LoadingFallback';
import { PATHS } from '../paths';

/**
 * Route-level feature-flag check (specification §8.2).
 *
 * A module this installation does not have must be "absent from the menu and its routes unreachable,
 * not merely disabled". So the route renders the same page-not-found screen a made-up URL would,
 * rather than a disabled screen or an explanatory message: from the user's side the address simply
 * does not exist, which is also the honest answer — the module is not part of their installation.
 *
 * While the flags are still loading the route shows the normal loading fallback, so a slow start-up
 * never briefly reports a real module as missing.
 */
export function RequireModule({ module, children }: { module: ModuleKey; children: ReactNode }) {
  const loaded = useModuleFlagsLoaded();
  const enabled = useModuleEnabled(module);

  if (enabled) return children;
  if (!loaded) return <LoadingFallback />;

  return (
    <Box sx={{ p: 4, textAlign: 'center' }}>
      <Typography variant="h4" component="h1" gutterBottom>
        Page not found
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        The address does not match any screen.
      </Typography>
      <Button component={RouterLink} to={PATHS.dashboard} variant="contained">
        Go to dashboard
      </Button>
    </Box>
  );
}
