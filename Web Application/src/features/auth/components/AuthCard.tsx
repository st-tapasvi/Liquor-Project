import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import type { ReactNode } from 'react';

import { appConfig } from '@/core/config';

/** The card used by every anonymous screen (login, change password, forgot password). */
export function AuthCard({ title, subtitle, children }: { title: string; subtitle?: string; children: ReactNode }) {
  return (
    <Paper sx={{ p: 4, width: '100%', maxWidth: 420 }}>
      <Typography variant="overline" color="text.secondary">
        {appConfig.name}
      </Typography>
      <Typography variant="h5" component="h1" sx={{ mb: subtitle ? 0.5 : 3 }}>
        {title}
      </Typography>
      {subtitle && (
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          {subtitle}
        </Typography>
      )}
      {children}
    </Paper>
  );
}
