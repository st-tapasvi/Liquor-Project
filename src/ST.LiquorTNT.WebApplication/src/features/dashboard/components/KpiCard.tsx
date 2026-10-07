import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';

import { tokens } from '@/core/theme';

import type { Kpi } from '../dashboard.config';

const { color } = tokens;

export function KpiCard({ label, value, caption }: Kpi) {
  return (
    <Paper component="section" aria-label={label} sx={{ px: 2.25, py: 1.75, borderRadius: 1.5, minHeight: 108 }}>
      <Typography variant="body2" sx={{ color: color.textSecondary }}>
        {label}
      </Typography>
      <Typography variant="h3" sx={{ my: 0.5, color: value === null ? color.secondary : color.textPrimary }}>
        {value ?? '—'}
      </Typography>
      <Typography variant="caption" sx={{ color: color.textSecondary }}>
        {caption}
      </Typography>
    </Paper>
  );
}
