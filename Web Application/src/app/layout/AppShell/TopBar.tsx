import LogoutIcon from '@mui/icons-material/Logout';
import MenuIcon from '@mui/icons-material/Menu';
import AppBar from '@mui/material/AppBar';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Chip from '@mui/material/Chip';
import IconButton from '@mui/material/IconButton';
import Toolbar from '@mui/material/Toolbar';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';

import { useCurrentUser, useLogout } from '@/core/auth';
import { env } from '@/core/config';

import { initials } from '@/shared/utils';

import { SIDEBAR_WIDTH } from './Sidebar';

export function TopBar({ onMenuClick, showMenuButton }: { onMenuClick: () => void; showMenuButton: boolean }) {
  const user = useCurrentUser();
  const logout = useLogout();

  return (
    <AppBar
      position="fixed"
      color="default"
      sx={{
        width: showMenuButton ? '100%' : `calc(100% - ${SIDEBAR_WIDTH}px)`,
        ml: showMenuButton ? 0 : `${SIDEBAR_WIDTH}px`,
        bgcolor: 'background.paper',
      }}
    >
      <Toolbar sx={{ gap: 1 }}>
        {showMenuButton && (
          <IconButton edge="start" aria-label="Open navigation" onClick={onMenuClick}>
            <MenuIcon />
          </IconButton>
        )}
        <Box sx={{ flexGrow: 1 }} />
        {env.DEV && <Chip label="Development" size="small" color="warning" variant="outlined" />}
        {user.isAdministrator && <Chip label="Administrator" size="small" color="primary" variant="outlined" />}
        <Avatar sx={{ width: 32, height: 32, fontSize: 14, bgcolor: 'primary.main' }} aria-hidden>
          {initials(user.fullName ?? user.userName)}
        </Avatar>
        <Typography variant="body2" sx={{ mr: 1 }}>
          {user.fullName ?? user.userName}
        </Typography>
        <Tooltip title="Sign out">
          <IconButton aria-label="Sign out" onClick={() => logout.mutate()} disabled={logout.isPending}>
            <LogoutIcon />
          </IconButton>
        </Tooltip>
      </Toolbar>
    </AppBar>
  );
}
