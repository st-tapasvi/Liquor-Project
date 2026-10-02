import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';

import { appConfig } from '@/core/config';

/** Last-resort page when something outside the router crashes. No stack trace is shown to the user. */
export function AppCrash({ error, onReset }: { error: Error; onReset: () => void }) {
  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', p: 3, textAlign: 'center' }}>
      <Box sx={{ maxWidth: 480 }}>
        <Typography variant="h5" component="h1" gutterBottom>
          {appConfig.name} ran into a problem
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          {error.name}: {error.message}
        </Typography>
        <Button variant="contained" onClick={() => window.location.reload()} sx={{ mr: 1 }}>
          Reload
        </Button>
        <Button variant="outlined" onClick={onReset}>
          Try again
        </Button>
        <Typography variant="caption" sx={{ display: 'block', mt: 3, color: 'text.secondary' }}>
          Version {appConfig.version}
        </Typography>
      </Box>
    </Box>
  );
}
