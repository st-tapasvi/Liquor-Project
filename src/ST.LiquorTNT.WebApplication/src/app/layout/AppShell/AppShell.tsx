import Box from '@mui/material/Box';
import { useTheme } from '@mui/material/styles';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useState } from 'react';
import { Outlet } from 'react-router';

import { RequireAuth } from '@/core/router';

import { SessionExpiredDialog } from '@/features/auth';

import { SessionCountdown } from './SessionCountdown';
import { Sidebar } from './Sidebar';
import { TopBar } from './TopBar';

export function AppShell() {
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'));
  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);

  const sidebarOpen = isDesktop ? !collapsed : mobileOpen;

  const toggle = () => {
    if (!isDesktop) {
      setMobileOpen((o) => !o);
      return;
    }
    setCollapsed((c) => !c);
  };

  return (
    <RequireAuth>
      <Box sx={{ display: 'flex', minHeight: '100vh', bgcolor: 'background.default' }}>
        <Sidebar
          variant={isDesktop ? 'permanent' : 'temporary'}
          open={sidebarOpen}
          onClose={() => setMobileOpen(false)}
          onExpand={() => setCollapsed(false)}
        />
        <Box sx={{ flexGrow: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
          <TopBar onMenuClick={toggle} sidebarOpen={sidebarOpen} />
          <Box component="main" sx={{ flexGrow: 1, px: { xs: 2, sm: 3 }, pt: 2.25, pb: 3 }}>
            <SessionCountdown />
            <Outlet />
          </Box>
        </Box>
      </Box>
      <SessionExpiredDialog />
    </RequireAuth>
  );
}
