import Box from '@mui/material/Box';
import Link from '@mui/material/Link';
import { Link as RouterLink } from 'react-router';

import { PATHS } from '@/core/router';

/** The centred "Back to log in" link under the primary action of every recovery screen. */
export function BackToLoginLink() {
  return (
    <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2.5 }}>
      <Link component={RouterLink} to={PATHS.login} underline="always" color="text.primary" sx={{ fontSize: 14 }}>
        Back to log in
      </Link>
    </Box>
  );
}
