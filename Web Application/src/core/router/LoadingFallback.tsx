import Box from '@mui/material/Box';
import CircularProgress from '@mui/material/CircularProgress';

/** Minimal spinner owned by core (core cannot import shared/). Pages use shared feedback components. */
export function LoadingFallback({ label = 'Loading…' }: { label?: string }) {
  return (
    <Box
      role="status"
      aria-live="polite"
      aria-label={label}
      sx={{ display: 'grid', placeItems: 'center', minHeight: 240, p: 4 }}
    >
      <CircularProgress size={32} />
    </Box>
  );
}
