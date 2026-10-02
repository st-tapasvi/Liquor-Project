import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import { Outlet } from 'react-router';

import { appConfig } from '@/core/config';
import { RequireAnonymous } from '@/core/router';

export function AuthLayout() {
  return (
    <RequireAnonymous>
      <Box
        sx={{
          position: 'relative',
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          px: 2,
          py: 8,
          bgcolor: 'background.default',
        }}
      >
        <Box
          aria-hidden
          sx={{
            position: 'absolute',
            top: 0,
            left: 0,
            right: 0,
            height: 6,
            bgcolor: (theme) => theme.palette.error.main,
          }}
        />

        <Outlet />

        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ position: 'absolute', left: 26, bottom: 20, fontSize: 12 }}
        >
          v{appConfig.version}
        </Typography>
      </Box>
    </RequireAnonymous>
  );
}
