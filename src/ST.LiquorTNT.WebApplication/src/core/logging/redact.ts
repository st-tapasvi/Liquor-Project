const SENSITIVE_KEY =
  /(password|passwd|pwd|secret|token|answer|hash|authorization|cookie|jwt|sid|signingkey|credential)/i;
const MAX_DEPTH = 4;
const MAX_STRING = 500;

/**
 * Masks anything that looks like a credential before it can reach a log line, in any transport.
 * Keys are matched case-insensitively; nested objects and arrays are walked to a bounded depth.
 * Errors are reduced to name + message (no stack in remote logs; the API has the correlation id).
 */
export function redact(value: unknown, depth = 0): unknown {
  if (value === null || value === undefined) return value;
  if (typeof value === 'string') return value.length > MAX_STRING ? `${value.slice(0, MAX_STRING)}…` : value;
  if (typeof value === 'number' || typeof value === 'boolean') return value;
  if (value instanceof Error) {
    return {
      name: value.name,
      message: value.message,
      ...('code' in value ? { code: (value as { code: unknown }).code } : {}),
    };
  }
  if (depth >= MAX_DEPTH) return '[depth]';
  if (Array.isArray(value)) return value.slice(0, 50).map((v) => redact(v, depth + 1));
  if (typeof value === 'object') {
    const out: Record<string, unknown> = {};
    for (const [k, v] of Object.entries(value as Record<string, unknown>)) {
      out[k] = SENSITIVE_KEY.test(k) ? '***' : redact(v, depth + 1);
    }
    return out;
  }
  return typeof value === 'symbol' || typeof value === 'bigint' || typeof value === 'function'
    ? typeof value
    : '[unknown]';
}
