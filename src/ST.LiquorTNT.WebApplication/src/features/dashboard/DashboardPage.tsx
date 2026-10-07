import Box from '@mui/material/Box';
import Link from '@mui/material/Link';
import Typography from '@mui/material/Typography';
import { Link as RouterLink } from 'react-router';

import { useCurrentUser } from '@/core/auth';
import { isModuleEnabled, useModuleFlagsStore } from '@/core/modules';
import { PATHS } from '@/core/router';
import { useTenantStore } from '@/core/tenant';
import { tokens } from '@/core/theme';

import { KpiCard } from './components/KpiCard';
import { ModuleTile } from './components/ModuleTile';
import { RecentActivity } from './components/RecentActivity';
import { KPIS, MODULE_TILES, rightsLabel } from './dashboard.config';

const { color } = tokens;

const TODAY = new Intl.DateTimeFormat('en-IN', { day: 'numeric', month: 'long', year: 'numeric' });

/**
 * Landing page (Penpot "2.1 App shell dashboard"): today's figures, one tile per module the user may open,
 * and the latest activity. Tiles and sidebar come from the same rights and feature flags.
 */
export default function DashboardPage() {
  const user = useCurrentUser();
  const enabled = useModuleFlagsStore((s) => s.enabled);
  const companyName = useTenantStore((s) => s.companyName);
  const companyId = useTenantStore((s) => s.companyId);
  const exciseCode = useTenantStore((s) => s.exciseCode);

  const context = [
    companyName ? `${companyName}${companyId ? ` (${companyId})` : ''}` : null,
    exciseCode ? `${exciseCode} excise` : null,
    TODAY.format(new Date()),
  ].filter(Boolean);

  const tiles = MODULE_TILES.filter((t) => t.permission.some((p) => user.permissions.has(p))).map((t) => ({
    ...t,
    enabled: isModuleEnabled(t.module, enabled),
    rights: rightsLabel(t.module, user.permissions),
  }));

  const canOpenLog =
    isModuleEnabled('reports', enabled) &&
    (user.permissions.has('reports.view') || user.permissions.has('reports.export'));

  return (
    <>
      <Box component="header" sx={{ mb: 2 }}>
        <Typography variant="body2" sx={{ color: color.textSecondary, mb: 0.25 }}>
          Home
        </Typography>
        <Box sx={{ display: 'flex', alignItems: 'baseline', flexWrap: 'wrap', columnGap: 3, rowGap: 0.5 }}>
          <Typography component="h1" variant="h4">
            Dashboard
          </Typography>
          <Typography variant="body1" sx={{ color: color.textSecondary }}>
            {context.join(' · ')}
          </Typography>
        </Box>
      </Box>

      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', lg: 'repeat(4, 1fr)' },
          gap: 2,
          mb: 2.5,
        }}
      >
        {KPIS.map((kpi) => (
          <KpiCard key={kpi.label} {...kpi} />
        ))}
      </Box>

      <Box component="section" aria-labelledby="modules-title" sx={{ mb: 2 }}>
        <Box sx={{ display: 'flex', alignItems: 'baseline', flexWrap: 'wrap', columnGap: 2.5, mb: 1.25 }}>
          <Typography id="modules-title" component="h2" variant="h6">
            Modules
          </Typography>
          <Typography variant="body2" sx={{ color: color.textSecondary }}>
            Same list as the sidebar — both come from your rights and this installation&apos;s modules
          </Typography>
        </Box>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(170px, 1fr))',
            gap: 2,
          }}
        >
          {tiles.map((t) => (
            <ModuleTile key={t.module} {...t} />
          ))}
        </Box>
      </Box>

      <RecentActivity
        action={
          canOpenLog ? (
            <Link component={RouterLink} to={PATHS.reports.activity} variant="body2">
              Open the full user log
            </Link>
          ) : null
        }
      />
    </>
  );
}
