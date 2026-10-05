import Alert from '@mui/material/Alert';
import Chip from '@mui/material/Chip';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { type ModuleKey, MODULES } from '@/core/modules';

import { PageHeader } from './PageHeader';

/**
 * The screen a registered-but-not-yet-built module shows.
 *
 * Every module of specification §3.2 is registered from day one — flag, permission keys, route and menu
 * entry — so the shell reflects the whole platform and the wiring is exercised before any screen exists.
 * This component is what those routes render until the module's real pages are written from its SOP.
 *
 * It reads the module registry rather than repeating the text, so the responsibility shown here and the
 * responsibility in the specification cannot drift apart.
 */
export function ModulePlaceholder({ module }: { module: ModuleKey }) {
  const definition = MODULES[module];

  return (
    <>
      <PageHeader
        title={definition.title}
        subtitle={definition.responsibility}
        actions={
          <Chip
            size="small"
            variant="outlined"
            label={definition.availability === 'flag' ? 'Optional module' : 'Core module'}
          />
        }
      />
      <Alert severity="info">
        <Stack spacing={0.5}>
          <Typography variant="body2">
            This module is registered and routed, but its screens have not been built yet.
          </Typography>
          <Typography variant="caption" color="text.secondary">
            Module key: {definition.key}
          </Typography>
        </Stack>
      </Alert>
    </>
  );
}
