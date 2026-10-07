import Box from '@mui/material/Box';
import ButtonBase from '@mui/material/ButtonBase';
import Typography from '@mui/material/Typography';
import { Link as RouterLink } from 'react-router';

import type { ModuleKey } from '@/core/modules';
import { tokens } from '@/core/theme';

import { ModuleIcon } from '@/shared/components/ui';

const { color, icon } = tokens;

interface ModuleTileProps {
  module: ModuleKey;
  label: string;
  to: string;
  /** False when this installation does not have the module: shown greyed, not clickable. */
  enabled: boolean;
  /** The user's rights in the module, e.g. "view · manage". */
  rights: string;
}

export function ModuleTile({ module, label, to, enabled, rights }: ModuleTileProps) {
  const body = (
    <>
      <Box
        sx={{
          width: 50,
          height: 50,
          borderRadius: '50%',
          display: 'grid',
          placeItems: 'center',
          mb: 1.25,
          bgcolor: enabled ? color.iconTint : color.iconTintMuted,
          color: enabled ? color.accent : color.secondary,
        }}
      >
        <ModuleIcon module={module} sx={{ fontSize: icon.lg }} />
      </Box>
      <Typography variant="subtitle1" sx={{ color: color.textPrimary }}>
        {label}
      </Typography>
      <Typography variant="body2" sx={{ color: color.textSecondary, mt: 0.25 }}>
        {enabled ? rights : 'not enabled'}
      </Typography>
    </>
  );

  const sx = {
    width: '100%',
    height: 148,
    flexDirection: 'column',
    justifyContent: 'flex-start',
    pt: 2,
    px: 1,
    textAlign: 'center',
    bgcolor: color.surface,
    border: `1px solid ${color.border}`,
    borderRadius: 1.5,
  } as const;

  if (!enabled) {
    return (
      <Box
        aria-disabled
        title="This installation does not have this module"
        sx={{ ...sx, display: 'flex', alignItems: 'center', opacity: 0.85 }}
      >
        {body}
      </Box>
    );
  }

  return (
    <ButtonBase
      component={RouterLink}
      to={to}
      sx={{
        ...sx,
        transition: 'border-color 120ms, box-shadow 120ms',
        '&:hover, &.Mui-focusVisible': {
          borderColor: color.accent,
          boxShadow: '0 2px 8px rgba(28, 28, 30, 0.08)',
        },
      }}
    >
      {body}
    </ButtonBase>
  );
}
