import DevicesOutlined from '@mui/icons-material/DevicesOutlined';
import KeyboardArrowDownRounded from '@mui/icons-material/KeyboardArrowDownRounded';
import LockResetOutlined from '@mui/icons-material/LockResetOutlined';
import LogoutRounded from '@mui/icons-material/LogoutRounded';
import MenuRounded from '@mui/icons-material/MenuRounded';
import AppBar from '@mui/material/AppBar';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import ButtonBase from '@mui/material/ButtonBase';
import Divider from '@mui/material/Divider';
import IconButton from '@mui/material/IconButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Toolbar from '@mui/material/Toolbar';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import { useState } from 'react';
import { useNavigate } from 'react-router';

import { useCurrentUser, useLogout } from '@/core/auth';
import { appConfig } from '@/core/config';
import { useConnectivity } from '@/core/network';
import { PATHS } from '@/core/router';
import { tokens } from '@/core/theme';

import { initials } from '@/shared/utils';

import { useSwitchSupplierCode } from './useSupplierCode';

const { shell, color, font, icon } = tokens;

export function TopBar({ onMenuClick, sidebarOpen }: { onMenuClick: () => void; sidebarOpen: boolean }) {
  return (
    <AppBar
      position="sticky"
      elevation={0}
      sx={{
        bgcolor: color.surface,
        color: color.textPrimary,
        borderBottom: `1px solid ${color.border}`,
        zIndex: (t) => t.zIndex.drawer - 1,
      }}
    >
      <Toolbar sx={{ minHeight: `${shell.topBarHeight - 1}px !important`, px: { xs: 1.5, sm: 2 }, gap: 1.5 }}>
        <IconButton
          onClick={onMenuClick}
          aria-label={sidebarOpen ? 'Collapse navigation' : 'Expand navigation'}
          aria-expanded={sidebarOpen}
          sx={{ width: 38, height: 36, borderRadius: 1, border: `1px solid ${color.border}`, color: color.textPrimary }}
        >
          <MenuRounded sx={{ fontSize: icon.md }} />
        </IconButton>
        <TenantSummary />
        <Box sx={{ flexGrow: 1 }} />
        <ConnectionChip />
        <Typography sx={{ fontSize: font.body2, color: color.textSecondary, display: { xs: 'none', sm: 'block' } }}>
          v{appConfig.version}
        </Typography>
        <UserMenu />
      </Toolbar>
    </AppBar>
  );
}

function TenantSummary() {
  const user = useCurrentUser();
  const select = useSwitchSupplierCode();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  const active = user.activeSupplierCode;
  const canSwitch = user.supplierCodes.length > 1 || (!active && user.supplierCodes.length > 0);
  if (!active && !canSwitch) return null;

  const parts: { label: string; value: string }[] = active
    ? [
        ...(active.companyName ? [{ label: 'Company', value: active.companyName }] : []),
        { label: 'Supplier code', value: active.displayName },
      ]
    : [];

  return (
    <>
      <ButtonBase
        aria-label="Working context"
        aria-haspopup={canSwitch ? 'menu' : undefined}
        disabled={!canSwitch}
        onClick={(e) => setAnchor(e.currentTarget)}
        sx={{
          display: { xs: 'none', md: 'flex' },
          alignItems: 'center',
          height: 38,
          px: 1.5,
          border: `1px solid ${color.border}`,
          borderRadius: 1,
          bgcolor: color.surface,
          fontSize: font.body1,
          whiteSpace: 'nowrap',
          '&.Mui-disabled': { color: 'inherit' },
        }}
      >
        {!active && <Box component="span">Select supplier code</Box>}
        {parts.map((p, i) => (
          <Box key={p.label} component="span" sx={{ display: 'flex', alignItems: 'center' }}>
            {i > 0 && <Box component="span" sx={{ mx: 1.5, width: '1px', height: 18, bgcolor: color.border }} />}
            <Box component="span" sx={{ mr: 0.75 }}>
              {p.label}:
            </Box>
            <Box component="span" sx={{ fontWeight: 700 }}>
              {p.value}
            </Box>
          </Box>
        ))}
        {canSwitch && <KeyboardArrowDownRounded sx={{ ml: 1, fontSize: icon.md, color: color.textSecondary }} />}
      </ButtonBase>
      <Menu
        anchorEl={anchor}
        open={anchor !== null}
        onClose={() => setAnchor(null)}
        slotProps={{ paper: { sx: { mt: 0.75, minWidth: 260, '& .MuiMenuItem-root': { fontSize: font.body1 } } } }}
      >
        {user.supplierCodes.map((code) => (
          <MenuItem
            key={code.id}
            selected={code.id === active?.id}
            disabled={select.isPending}
            onClick={() => {
              setAnchor(null);
              if (code.id !== active?.id) select.mutate(code.id);
            }}
          >
            <ListItemText primary={code.displayName} secondary={code.companyName ?? undefined} />
          </MenuItem>
        ))}
      </Menu>
    </>
  );
}

function ConnectionChip() {
  const online = useConnectivity() === 'online';
  return (
    <Box
      role="status"
      sx={{
        display: 'flex',
        alignItems: 'center',
        gap: 0.75,
        height: 30,
        px: 1.25,
        borderRadius: 13,
        bgcolor: online ? shell.liveBg : color.accentTint,
        color: online ? color.success : color.error,
        fontSize: font.caption,
        fontWeight: 600,
        letterSpacing: '0.04em',
      }}
    >
      <Box component="span" sx={{ width: 8, height: 8, borderRadius: '50%', bgcolor: 'currentColor' }} />
      {online ? 'LIVE' : 'OFFLINE'}
    </Box>
  );
}

function UserMenu() {
  const user = useCurrentUser();
  const logout = useLogout();
  const navigate = useNavigate();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  const name = user.fullName ?? user.userName;
  const role = user.isSuperAdmin ? 'Super Admin' : user.activeSupplierCode?.displayName;
  const go = (to: string) => {
    setAnchor(null);
    void navigate(to);
  };

  return (
    <>
      <Tooltip title="Account">
        <ButtonBase
          onClick={(e) => setAnchor(e.currentTarget)}
          aria-haspopup="menu"
          aria-expanded={anchor !== null}
          aria-label={`Account menu for ${name}`}
          sx={{
            height: 38,
            pl: 0.375,
            pr: 1,
            gap: 1,
            borderRadius: 19,
            border: `1px solid ${color.border}`,
            fontSize: font.body1,
            '&:hover': { bgcolor: color.head },
          }}
        >
          <Avatar
            sx={{
              width: 30,
              height: 30,
              fontSize: font.caption,
              fontWeight: 700,
              bgcolor: shell.avatarBg,
              color: shell.avatarText,
            }}
          >
            {initials(name)}
          </Avatar>
          <Box component="span" sx={{ display: { xs: 'none', sm: 'inline' } }}>
            {role ? `${user.userName} · ${role}` : user.userName}
          </Box>
          <KeyboardArrowDownRounded sx={{ fontSize: icon.md, color: color.textSecondary }} />
        </ButtonBase>
      </Tooltip>
      <Menu
        anchorEl={anchor}
        open={anchor !== null}
        onClose={() => setAnchor(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{
          paper: { sx: { mt: 0.75, minWidth: 220, '& .MuiMenuItem-root': { fontSize: font.body1, minHeight: 40 } } },
        }}
      >
        <Box sx={{ px: 2, py: 1 }}>
          <Typography variant="subtitle2">{name}</Typography>
          <Typography variant="body2" sx={{ color: color.textSecondary }}>
            {role ? `${user.userName} · ${role}` : user.userName}
          </Typography>
        </Box>
        <Divider />
        <MenuItem onClick={() => go(PATHS.settings.sessions)}>
          <ListItemIcon>
            <DevicesOutlined fontSize="small" />
          </ListItemIcon>
          My sessions
        </MenuItem>
        <MenuItem onClick={() => go(PATHS.changePassword)}>
          <ListItemIcon>
            <LockResetOutlined fontSize="small" />
          </ListItemIcon>
          Change password
        </MenuItem>
        <Divider />
        <MenuItem
          disabled={logout.isPending}
          onClick={() => {
            setAnchor(null);
            logout.mutate();
          }}
        >
          <ListItemIcon>
            <LogoutRounded fontSize="small" />
          </ListItemIcon>
          Sign out
        </MenuItem>
      </Menu>
    </>
  );
}
