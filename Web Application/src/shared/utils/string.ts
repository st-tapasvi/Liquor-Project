/** Empty or whitespace-only strings become null, so optional API fields are sent as null, not "". */
export function emptyToNull(value: string | null | undefined): string | null {
  if (value === null || value === undefined) return null;
  const trimmed = value.trim();
  return trimmed.length === 0 ? null : trimmed;
}

/** Null/undefined become "" for form default values. */
export function nullToEmpty(value: string | null | undefined): string {
  return value ?? '';
}

/** Initials for an avatar: "Ravi Kumar" → "RK". */
export function initials(name: string | null | undefined, fallback = '?'): string {
  const parts = (name ?? '').trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return fallback;
  return parts
    .slice(0, 2)
    .map((p) => p.charAt(0).toUpperCase())
    .join('');
}
