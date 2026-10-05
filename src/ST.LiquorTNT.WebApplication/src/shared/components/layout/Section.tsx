import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import type { ReactNode } from 'react';

/** A titled card that groups related fields or a grid on a page. */
export function Section({ title, children, dense }: { title?: string; children: ReactNode; dense?: boolean }) {
  return (
    <Paper sx={{ p: dense ? 1.5 : 2.5, mb: 2 }}>
      {title && (
        <Typography variant="subtitle1" component="h2" sx={{ mb: 1.5, fontWeight: 600 }}>
          {title}
        </Typography>
      )}
      {children}
    </Paper>
  );
}
