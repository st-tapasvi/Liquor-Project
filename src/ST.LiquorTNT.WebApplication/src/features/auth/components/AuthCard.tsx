import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import type { ReactNode } from 'react';

import { appConfig } from '@/core/config';

const LOGO_SRC = '/emblem.png';
const COMPANY = 'Sundaram Technologies';

export function AuthCard({
  title = appConfig.name,
  subtitle,
  children,
}: {
  title?: string;
  subtitle?: string;
  children: ReactNode;
}) {
  const line = subtitle ?? (title === appConfig.name ? `${COMPANY} - sign in to continue` : COMPANY);

  return (
    <Paper
      component="section"
      aria-labelledby="auth-card-title"
      sx={{ width: '100%', maxWidth: 460, px: { xs: 3, sm: 5.5 }, pt: 4.5, pb: 3.75, textAlign: 'center' }}
    >
      <Box sx={{ display: 'flex', justifyContent: 'center', mb: 1.75 }}>
        <Box component="img" src={LOGO_SRC} alt={COMPANY} sx={{ height: 64, width: 'auto', display: 'block' }} />
      </Box>
      <Typography id="auth-card-title" component="h1" variant="h5">
        {title}
      </Typography>
      <Typography variant="body2" sx={{ color: 'text.secondary', mt: 0.75 }}>
        {line}
      </Typography>
      <Box sx={{ textAlign: 'left', mt: 3.25 }}>{children}</Box>
    </Paper>
  );
}
