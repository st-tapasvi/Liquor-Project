import ExpandMore from '@mui/icons-material/ExpandMore';
import Box from '@mui/material/Box';
import Collapse from '@mui/material/Collapse';
import Drawer from '@mui/material/Drawer';
import List from '@mui/material/List';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import type { SxProps, Theme } from '@mui/material/styles';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import { type ReactElement, useState } from 'react';
import { NavLink, useLocation } from 'react-router';

import { type SessionUser, useCurrentUser } from '@/core/auth';
import { isModuleEnabled, MODULES, useModuleFlagsStore } from '@/core/modules';
import { tokens } from '@/core/theme';

import { ModuleIcon } from '@/shared/components/ui';

import { type MenuItem, type MenuLink, MENU } from './menu.config';

const { shell } = tokens;

const OPEN_WIDTH = shell.sidebarWidth;
const MINI_WIDTH = 68;

const COMPANY = 'Sundaram Technologies';

const widthTransition = (t: Theme) =>
  t.transitions.create('width', { easing: t.transitions.easing.sharp, duration: t.transitions.duration.shorter });

const rowSx: SxProps<Theme> = {
  minHeight: 42,
  py: 0,
  pl: 2.75,
  pr: 2,
  color: shell.sidebarText,
  borderLeft: '3px solid transparent',
  whiteSpace: 'nowrap',
  '& .MuiListItemIcon-root': { minWidth: 34, color: shell.sidebarIcon },
  '& .MuiListItemText-primary': { fontSize: tokens.font.body1, lineHeight: 1.35 },
  '&:hover': { bgcolor: shell.sidebarHoverBg },
  '&.active': {
    bgcolor: shell.sidebarActiveBg,
    borderLeftColor: tokens.color.accent,
    color: shell.sidebarActiveText,
    '& .MuiListItemIcon-root': { color: tokens.color.accent },
    '& .MuiListItemText-primary': { fontWeight: 600 },
  },
  '&.Mui-focusVisible': { outline: `2px solid ${tokens.color.accent}`, outlineOffset: -2 },
};

const hiddenWhenMini = (mini: boolean): { opacity: number; transition: string } => ({
  opacity: mini ? 0 : 1,
  transition: 'opacity 120ms',
});

function canSee(link: MenuLink, user: SessionUser): boolean {
  return !link.permission || link.permission.some((p) => user.permissions.has(p));
}

function RailTooltip({ title, mini, children }: { title: string; mini: boolean; children: ReactElement }) {
  return (
    <Tooltip title={mini ? title : ''} placement="right" disableInteractive>
      {children}
    </Tooltip>
  );
}

export function Sidebar({
  open,
  onClose,
  onExpand,
  variant,
}: {
  open: boolean;
  onClose: () => void;
  /** Asked when a collapsed group is clicked in the rail: the drawer opens so its pages can be shown. */
  onExpand?: () => void;
  variant: 'permanent' | 'temporary';
}) {
  const user = useCurrentUser();
  const enabledModules = useModuleFlagsStore((s) => s.enabled);

  const mini = variant === 'permanent' && !open;
  const width = mini ? MINI_WIDTH : OPEN_WIDTH;

  const items = MENU.filter((item) => isModuleEnabled(item.module, enabledModules)).flatMap((item): MenuItem[] => {
    if (!item.children) return canSee(item, user) ? [item] : [];
    const children = item.children.filter((c) => canSee(c, user));
    return children.length > 0 ? [{ ...item, children }] : [];
  });

  const closeIfTemporary = variant === 'temporary' ? onClose : undefined;

  return (
    <Drawer
      variant={variant}
      open={variant === 'permanent' ? true : open}
      onClose={onClose}
      slotProps={{
        paper: {
          sx: {
            width,
            overflowX: 'hidden',
            boxSizing: 'border-box',
            bgcolor: shell.sidebarBg,
            color: shell.sidebarText,
            border: 'none',
            transition: widthTransition,
          },
        },
      }}
      sx={{
        width: variant === 'permanent' ? width : 0,
        flexShrink: 0,
        whiteSpace: 'nowrap',
        transition: widthTransition,
      }}
    >
      <Brand mini={mini} />

      <Box component="nav" aria-label="Main navigation" sx={{ flex: 1, overflowY: 'auto', overflowX: 'hidden', py: 1 }}>
        <List disablePadding>
          {items.map((item) =>
            item.children ? (
              <GroupItem
                key={item.label}
                item={item}
                links={item.children}
                mini={mini}
                onNavigate={closeIfTemporary}
                onExpand={onExpand}
              />
            ) : (
              <RailTooltip key={item.to} title={item.label} mini={mini}>
                <ListItemButton component={NavLink} to={item.to} onClick={closeIfTemporary} sx={rowSx}>
                  <ListItemIcon>
                    <ModuleIcon module={item.module} sx={{ fontSize: tokens.icon.md }} />
                  </ListItemIcon>
                  <ListItemText primary={item.label} sx={hiddenWhenMini(mini)} />
                  {MODULES[item.module].availability === 'flag' && <FlagDot mini={mini} />}
                </ListItemButton>
              </RailTooltip>
            ),
          )}
        </List>
      </Box>
    </Drawer>
  );
}

function Brand({ mini }: { mini: boolean }) {
  return (
    <Box
      sx={{
        height: shell.topBarHeight + 1,
        flexShrink: 0,
        display: 'flex',
        alignItems: 'center',
        gap: 1.25,
        px: mini ? 2 : 2.5,
        borderBottom: `1px solid ${shell.sidebarDivider}`,
        transition: 'padding 150ms',
      }}
    >
      <Box
        component="img"
        src="/emblem.png"
        alt=""
        sx={{ width: 32, height: 'auto', display: 'block', flexShrink: 0 }}
      />
      <Box sx={{ minWidth: 0, ...hiddenWhenMini(mini) }}>
        <Typography
          component="div"
          sx={{
            color: '#fff',
            fontSize: tokens.font.subtitle,
            fontWeight: 700,
            letterSpacing: '0.04em',
            lineHeight: 1.2,
          }}
        >
          EXCISE T&amp;T
        </Typography>
        <Typography
          component="div"
          sx={{
            color: shell.sidebarTextMuted,
            fontSize: tokens.font.overline,
            letterSpacing: '0.05em',
            textTransform: 'uppercase',
          }}
        >
          {COMPANY}
        </Typography>
      </Box>
    </Box>
  );
}

function FlagDot({ mini }: { mini: boolean }) {
  return (
    <Box
      component="span"
      title="Available only where the installation enables it"
      sx={{
        width: 7,
        height: 7,
        borderRadius: '50%',
        bgcolor: shell.sidebarTextMuted,
        flexShrink: 0,
        ...hiddenWhenMini(mini),
      }}
    />
  );
}

function GroupItem({
  item,
  links,
  mini,
  onNavigate,
  onExpand,
}: {
  item: MenuItem;
  links: readonly MenuLink[];
  mini: boolean;
  onNavigate: (() => void) | undefined;
  onExpand: (() => void) | undefined;
}) {
  const { pathname } = useLocation();
  const containsCurrent = links.some((l) => pathname.startsWith(l.to));
  const [expanded, setExpanded] = useState(false);
  const open = !mini && (expanded || containsCurrent);

  const handleClick = () => {
    if (mini) {
      setExpanded(true);
      onExpand?.();
      return;
    }
    setExpanded((e) => !e);
  };

  return (
    <>
      <RailTooltip title={item.label} mini={mini}>
        <ListItemButton
          onClick={handleClick}
          aria-expanded={open}
          className={containsCurrent ? 'active' : undefined}
          sx={rowSx}
        >
          <ListItemIcon>
            <ModuleIcon module={item.module} sx={{ fontSize: tokens.icon.md }} />
          </ListItemIcon>
          <ListItemText primary={item.label} sx={hiddenWhenMini(mini)} />
          <ExpandMore
            sx={{
              fontSize: tokens.icon.md,
              color: shell.sidebarIcon,
              transform: open ? 'rotate(180deg)' : 'none',
              transition: 'transform 150ms, opacity 120ms',
              opacity: mini ? 0 : 1,
            }}
          />
        </ListItemButton>
      </RailTooltip>
      <Collapse in={open} timeout="auto" unmountOnExit>
        <List disablePadding>
          {links.map((link) => (
            <ListItemButton
              key={link.to}
              component={NavLink}
              to={link.to}
              onClick={onNavigate}
              sx={{
                minHeight: 36,
                py: 0,
                pl: 7.5,
                color: shell.sidebarTextMuted,
                whiteSpace: 'nowrap',
                '& .MuiListItemText-primary': { fontSize: tokens.font.body2 },
                '&:hover': { bgcolor: shell.sidebarHoverBg, color: shell.sidebarText },
                '&.active': { color: '#fff', '& .MuiListItemText-primary': { fontWeight: 600 } },
              }}
            >
              <ListItemText primary={link.label} />
            </ListItemButton>
          ))}
        </List>
      </Collapse>
    </>
  );
}
