import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import { Link as RouterLink } from 'react-router';

import { PATHS } from '@/core/router';

export function Forbidden() {
  return (
    <Box sx={{ p: 4, textAlign: 'center' }}>
      <Typography variant="h5" component="h1" gutterBottom>
        You do not have access to this screen
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Ask an administrator if you need this right.
      </Typography>
      <Button component={RouterLink} to={PATHS.dashboard} variant="contained">
        Go to dashboard
      </Button>
    </Box>
  );
}
