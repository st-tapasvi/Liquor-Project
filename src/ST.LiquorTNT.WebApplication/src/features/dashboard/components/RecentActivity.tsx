import HistoryRounded from '@mui/icons-material/HistoryRounded';
import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import type { ReactNode } from 'react';

import { tokens } from '@/core/theme';

const { color, icon } = tokens;

/**
 * The latest entries of the user log. The API has no activity endpoint yet (USER_LOG is written but not
 * served), so the card shows an empty state; the table layout of the design goes here once it exists.
 */
export function RecentActivity({ action }: { action: ReactNode }) {
  return (
    <Paper component="section" aria-labelledby="activity-title" sx={{ p: 2.5, borderRadius: 1.5, minHeight: 260 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1.5 }}>
        <Typography id="activity-title" component="h2" variant="h6">
          Recent activity
        </Typography>
        {action}
      </Box>
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 1,
          py: 6,
          color: color.textSecondary,
          borderTop: `1px solid ${color.border}`,
        }}
      >
        <HistoryRounded sx={{ fontSize: icon.xl, color: color.secondary }} />
        <Typography variant="body1">No activity to show yet.</Typography>
        <Typography variant="body2" sx={{ color: color.secondary }}>
          Logins, changes and exports appear here once the user-log report is available.
        </Typography>
      </Box>
    </Paper>
  );
}
