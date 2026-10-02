import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import { useTheme } from '@mui/material/styles';
import Toolbar from '@mui/material/Toolbar';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useState } from 'react';
import { Outlet } from 'react-router';

import { RequireAuth } from '@/core/router';

import { SessionExpiredDialog } from '@/features/auth';

import { SessionCountdown } from './SessionCountdown';
import { Sidebar } from './Sidebar';
import { TopBar } from './TopBar';

/** Authenticated layout: sidebar + top bar + page outlet, plus the app-wide re-authentication dialog. */
export function AppShell() {
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'));
  const [mobileOpen, setMobileOpen] = useState(false);

  return (
    <RequireAuth>
      <Box sx={{ display: 'flex', minHeight: '100vh' }}>
        <TopBar showMenuButton={!isDesktop} onMenuClick={() => setMobileOpen(true)} />
        <Sidebar
          variant={isDesktop ? 'permanent' : 'temporary'}
          open={isDesktop || mobileOpen}
          onClose={() => setMobileOpen(false)}
        />
        <Box component="main" sx={{ flexGrow: 1, minWidth: 0 }}>
          <Toolbar />
          <Container maxWidth="xl" sx={{ py: 3 }}>
            <SessionCountdown />
            <Outlet />
          </Container>
        </Box>
      </Box>
      <SessionExpiredDialog />
    </RequireAuth>
  );
}
