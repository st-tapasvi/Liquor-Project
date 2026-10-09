import Box from '@mui/material/Box';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import type { ReactNode } from 'react';

interface PageHeaderProps {
  title: string;
  subtitle?: string | undefined;
  status?: ReactNode;
  actions?: ReactNode;
}

export function PageHeader({ title, subtitle, status, actions }: PageHeaderProps) {
  return (
    <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: 'flex-start', justifyContent: 'space-between' }}>
      <Box>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
          <Typography variant="h4" component="h1">
            {title}
          </Typography>
          {status}
        </Stack>
        {subtitle && (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {subtitle}
          </Typography>
        )}
      </Box>
      {actions && (
        <Stack direction="row" spacing={1}>
          {actions}
        </Stack>
      )}
    </Stack>
  );
}
