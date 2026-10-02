import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import { Outlet } from 'react-router';

import { appConfig } from '@/core/config';
import { RequireAnonymous } from '@/core/router';

/** Centered layout for login and recovery screens. */
export function AuthLayout() {
  return (
    <RequireAnonymous>
      <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', p: 2, bgcolor: 'background.default' }}>
        <Box sx={{ width: '100%', display: 'grid', justifyItems: 'center', gap: 2 }}>
          <Outlet />
          <Typography variant="caption" color="text.secondary">
            {appConfig.name} · v{appConfig.version}
          </Typography>
        </Box>
      </Box>
    </RequireAnonymous>
  );
}
