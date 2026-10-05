import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import { Link as RouterLink } from 'react-router';

import { PATHS } from '@/core/router';

export function NotFound() {
  return (
    <Box sx={{ p: 4, textAlign: 'center' }}>
      <Typography variant="h5" component="h1" gutterBottom>
        Page not found
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        The address does not match any screen.
      </Typography>
      <Button component={RouterLink} to={PATHS.dashboard} variant="contained">
        Go to dashboard
      </Button>
    </Box>
  );
}
