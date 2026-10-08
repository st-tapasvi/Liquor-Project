import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';

import { useCurrentUser } from '@/core/auth';
import { tokens } from '@/core/theme';

import { ModuleTile } from './components/ModuleTile';
import { MODULE_TILES, rightsLabel } from './dashboard.config';

const { color } = tokens;

export default function DashboardPage() {
  const user = useCurrentUser();

  const context = [user.activeSupplierCode?.companyName, user.activeSupplierCode?.displayName].filter(Boolean);

  const tiles = MODULE_TILES.filter((t) => t.permission.some((p) => user.permissions.has(p)));

  return (
    <>
      <Box component="header" sx={{ mb: 2 }}>
        <Box sx={{ display: 'flex', alignItems: 'baseline', flexWrap: 'wrap', columnGap: 3, rowGap: 0.5 }}>
          <Typography component="h1" variant="h4">
            Dashboard
          </Typography>
          {context.length > 0 && (
            <Typography variant="body1" sx={{ color: color.textSecondary }}>
              {context.join(' · ')}
            </Typography>
          )}
        </Box>
      </Box>

      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(170px, 1fr))',
          gap: 2,
        }}
      >
        {tiles.map((t) => (
          <ModuleTile
            key={t.key}
            label={t.label}
            to={t.to}
            icon={t.icon}
            rights={rightsLabel(t.pages, user.permissions)}
          />
        ))}
      </Box>
    </>
  );
}
