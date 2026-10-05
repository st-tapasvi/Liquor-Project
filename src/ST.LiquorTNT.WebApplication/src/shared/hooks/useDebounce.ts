import { useEffect, useState } from 'react';

/** Delays a fast-changing value (search text) so it hits the query key at most every `delayMs`. */
export function useDebounce<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value);

  // Why an effect: a timer is an external side effect that must be cancelled on change/unmount.
  useEffect(() => {
    const handle = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(handle);
  }, [value, delayMs]);

  return debounced;
}
