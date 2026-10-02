import DevicesIcon from '@mui/icons-material/Devices';
import GroupIcon from '@mui/icons-material/Group';
import PolicyIcon from '@mui/icons-material/Policy';
import SecurityIcon from '@mui/icons-material/Security';
import Card from '@mui/material/Card';
import CardActionArea from '@mui/material/CardActionArea';
import CardContent from '@mui/material/CardContent';
import Grid from '@mui/material/Grid';
import Typography from '@mui/material/Typography';
import type { ReactNode } from 'react';
import { Link as RouterLink } from 'react-router';

import { type PermissionKey, useCurrentUser, useSession } from '@/core/auth';
import { PATHS } from '@/core/router';

import { PageHeader } from '@/shared/components/ui';
import { formatDateTime } from '@/shared/utils';

interface Tile {
  title: string;
  description: string;
  to: string;
  icon: ReactNode;
  permission?: readonly PermissionKey[];
}

const TILES: readonly Tile[] = [
  {
    title: 'Users',
    description: 'Accounts, roles, locks.',
    to: PATHS.users.list,
    icon: <GroupIcon />,
    permission: ['users.view', 'users.manage'],
  },
  {
    title: 'Security settings',
    description: 'Login locking and session limits.',
    to: PATHS.settings.security,
    icon: <SecurityIcon />,
    permission: ['settings.view', 'settings.manage'],
  },
  {
    title: 'Password policies',
    description: 'Rules for new passwords.',
    to: PATHS.settings.passwordPolicies,
    icon: <PolicyIcon />,
    permission: ['settings.view', 'settings.manage'],
  },
  {
    title: 'My sessions',
    description: 'Devices where you are signed in.',
    to: PATHS.settings.sessions,
    icon: <DevicesIcon />,
  },
];

/** Landing page: tiles for every module the user may open (menu and tiles come from the same rights). */
export default function DashboardPage() {
  const user = useCurrentUser();
  const { expiresAt } = useSession();

  const visible = TILES.filter((t) => !t.permission || t.permission.some((p) => user.permissions.has(p)));

  return (
    <>
      <PageHeader
        title={`Welcome, ${user.fullName ?? user.userName}`}
        subtitle={expiresAt ? `This session ends at ${formatDateTime(expiresAt)} at the latest.` : undefined}
      />
      <Grid container spacing={2}>
        {visible.map((tile) => (
          <Grid key={tile.to} size={{ xs: 12, sm: 6, md: 4, lg: 3 }}>
            <Card>
              <CardActionArea component={RouterLink} to={tile.to} sx={{ height: '100%' }}>
                <CardContent sx={{ display: 'flex', gap: 2, alignItems: 'flex-start' }}>
                  <span style={{ color: 'var(--mui-palette-primary-main)' }}>{tile.icon}</span>
                  <span>
                    <Typography variant="subtitle1" component="h2">
                      {tile.title}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {tile.description}
                    </Typography>
                  </span>
                </CardContent>
              </CardActionArea>
            </Card>
          </Grid>
        ))}
      </Grid>
    </>
  );
}
