import Box from '@mui/material/Box';
import Divider from '@mui/material/Divider';
import Drawer from '@mui/material/Drawer';
import List from '@mui/material/List';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemText from '@mui/material/ListItemText';
import ListSubheader from '@mui/material/ListSubheader';
import Toolbar from '@mui/material/Toolbar';
import Typography from '@mui/material/Typography';
import { NavLink } from 'react-router';

import { useCurrentUser } from '@/core/auth';
import { appConfig } from '@/core/config';
import { isModuleEnabled, useModuleFlagsStore } from '@/core/modules';

import { MENU } from './menu.config';

export const SIDEBAR_WIDTH = 240;

/**
 * Navigation built from this installation's modules and the user's rights: an item whose module the
 * installation does not have, or that the user may not open, is not rendered at all.
 *
 * Both filters are for usability. The route carries the same two checks (RequireModule,
 * RequirePermission) and the API enforces the right on every call (§9).
 */
export function Sidebar({
  open,
  onClose,
  variant,
}: {
  open: boolean;
  onClose: () => void;
  variant: 'permanent' | 'temporary';
}) {
  const user = useCurrentUser();
  const enabledModules = useModuleFlagsStore((s) => s.enabled);

  const groups = MENU.map((g) => ({
    ...g,
    items: g.items.filter(
      (i) =>
        isModuleEnabled(i.module, enabledModules) &&
        (!i.permission || i.permission.some((p) => user.permissions.has(p))),
    ),
  })).filter((g) => g.items.length > 0);

  return (
    <Drawer
      variant={variant}
      open={open}
      onClose={onClose}
      slotProps={{ paper: { sx: { width: SIDEBAR_WIDTH, boxSizing: 'border-box' } } }}
      sx={{ width: variant === 'permanent' ? SIDEBAR_WIDTH : 0, flexShrink: 0 }}
    >
      <Toolbar sx={{ px: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {appConfig.name}
        </Typography>
      </Toolbar>
      <Divider />
      <Box component="nav" aria-label="Main navigation" sx={{ overflowY: 'auto' }}>
        {groups.map((group) => (
          <List key={group.title} dense subheader={<ListSubheader disableSticky>{group.title}</ListSubheader>}>
            {group.items.map((item) => (
              <ListItemButton
                key={item.to}
                component={NavLink}
                to={item.to}
                onClick={onClose}
                sx={{ '&.active': { bgcolor: 'action.selected' } }}
              >
                <ListItemText primary={item.label} />
              </ListItemButton>
            ))}
          </List>
        ))}
      </Box>
    </Drawer>
  );
}
