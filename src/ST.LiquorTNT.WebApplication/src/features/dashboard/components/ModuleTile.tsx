import Box from '@mui/material/Box';
import ButtonBase from '@mui/material/ButtonBase';
import type { SvgIconProps } from '@mui/material/SvgIcon';
import Typography from '@mui/material/Typography';
import type { ComponentType } from 'react';
import { Link as RouterLink } from 'react-router';

import { tokens } from '@/core/theme';

const { color, icon } = tokens;

interface ModuleTileProps {
  label: string;
  to: string;
  icon: ComponentType<SvgIconProps>;
  rights: string;
}

export function ModuleTile({ label, to, icon: Icon, rights }: ModuleTileProps) {
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
          bgcolor: color.iconTint,
          color: color.accent,
        }}
      >
        <Icon sx={{ fontSize: icon.lg }} />
      </Box>
      <Typography variant="subtitle1" sx={{ color: color.textPrimary }}>
        {label}
      </Typography>
      <Typography variant="body2" sx={{ color: color.textSecondary, mt: 0.25 }}>
        {rights}
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
