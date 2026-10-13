import HomeOutlined from '@mui/icons-material/HomeOutlined';
import Breadcrumbs from '@mui/material/Breadcrumbs';
import Link from '@mui/material/Link';
import Typography from '@mui/material/Typography';
import { Link as RouterLink, useLocation } from 'react-router';

import { PATHS } from '@/core/router';

import { MENU } from './menu.config';

interface Crumb {
  label: string;
  to?: string;
}

function matches(pathname: string, to: string): boolean {
  return pathname === to || pathname.startsWith(`${to}/`);
}

function tailLabel(rest: string[]): string | null {
  if (rest.length === 0) return null;
  if (rest[0] === 'new') return 'New';
  if (rest[rest.length - 1] === 'edit') return 'Edit';
  return 'View';
}

function buildCrumbs(pathname: string): Crumb[] {
  let best: { label: string; to: string } | null = null;
  for (const item of MENU) {
    const links = item.children ?? [item];
    for (const link of links) {
      if (matches(pathname, link.to) && (!best || link.to.length > best.to.length)) {
        best = { label: link.label, to: link.to };
      }
    }
  }
  if (!best) return [];

  const rest = pathname.slice(best.to.length).split('/').filter(Boolean);
  const tail = tailLabel(rest);
  return [{ label: best.label, ...(tail ? { to: best.to } : {}) }, ...(tail ? [{ label: tail }] : [])];
}

const CRUMB_SIZE = 16;

export function AppBreadcrumbs() {
  const { pathname } = useLocation();
  if (pathname === PATHS.dashboard || pathname === PATHS.root) return null;

  const crumbs = buildCrumbs(pathname);
  if (crumbs.length === 0) return null;

  return (
    <Breadcrumbs
      aria-label="Breadcrumb"
      separator="/"
      sx={{
        mb: 1.5,
        fontSize: CRUMB_SIZE,
        '& .MuiBreadcrumbs-separator': { mx: 1, color: 'text.disabled', fontSize: CRUMB_SIZE },
        '& .MuiBreadcrumbs-li': { display: 'flex', alignItems: 'center' },
      }}
    >
      <Link
        component={RouterLink}
        to={PATHS.dashboard}
        underline="none"
        aria-label="Dashboard"
        sx={{ display: 'flex', alignItems: 'center', color: 'text.secondary', '&:hover': { color: 'text.primary' } }}
      >
        <HomeOutlined sx={{ fontSize: 20 }} />
      </Link>
      {crumbs.map((crumb, index) => {
        if (index === crumbs.length - 1) {
          return (
            <Typography
              key={crumb.label}
              aria-current="page"
              sx={{ fontSize: CRUMB_SIZE, fontWeight: 700, color: 'text.primary', lineHeight: 1.3 }}
            >
              {crumb.label}
            </Typography>
          );
        }
        return crumb.to ? (
          <Link
            key={crumb.label}
            component={RouterLink}
            to={crumb.to}
            underline="hover"
            sx={{
              fontSize: CRUMB_SIZE,
              fontWeight: 500,
              color: 'text.secondary',
              '&:hover': { color: 'text.primary' },
            }}
          >
            {crumb.label}
          </Link>
        ) : (
          <Typography key={crumb.label} sx={{ fontSize: CRUMB_SIZE, fontWeight: 500, color: 'text.secondary' }}>
            {crumb.label}
          </Typography>
        );
      })}
    </Breadcrumbs>
  );
}
