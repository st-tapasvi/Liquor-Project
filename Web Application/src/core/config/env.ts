import { z } from 'zod';

/**
 * Validated, typed view of `import.meta.env`.
 *
 * Every VITE_* variable is public (compiled into the bundle), so nothing here is a secret.
 * Validation runs once at module load: a misconfigured build fails immediately with a clear
 * message instead of misbehaving at runtime.
 */
const envSchema = z.object({
  MODE: z.enum(['development', 'production', 'test']),
  DEV: z.boolean(),
  PROD: z.boolean(),
  VITE_API_BASE_URL: z
    .string()
    .trim()
    .min(1)
    .default('/api')
    // Same-origin relative path or an absolute https URL. Plain http is only tolerated in development.
    .refine(
      (v) => v.startsWith('/') || /^https?:\/\//.test(v),
      'VITE_API_BASE_URL must be a relative path or an absolute URL',
    ),
  VITE_USE_MOCKS: z
    .string()
    .optional()
    .transform((v) => v === 'true'),
  VITE_LOG_LEVEL: z.enum(['debug', 'info', 'warn', 'error']).default('info'),
  VITE_APP_NAME: z.string().trim().min(1).default('ST.LiquorTNT'),
});

export type Env = z.infer<typeof envSchema>;

function readEnv(): Env {
  const raw = import.meta.env as unknown as Record<string, unknown>;
  const result = envSchema.safeParse({
    MODE: raw['MODE'],
    DEV: raw['DEV'],
    PROD: raw['PROD'],
    VITE_API_BASE_URL: raw['VITE_API_BASE_URL'],
    VITE_USE_MOCKS: raw['VITE_USE_MOCKS'],
    VITE_LOG_LEVEL: raw['VITE_LOG_LEVEL'],
    VITE_APP_NAME: raw['VITE_APP_NAME'],
  });

  if (!result.success) {
    const problems = result.error.issues.map((i) => `${i.path.join('.')}: ${i.message}`).join('; ');
    throw new Error(`Invalid build configuration: ${problems}`);
  }

  if (result.data.PROD && result.data.VITE_USE_MOCKS) {
    throw new Error('VITE_USE_MOCKS must not be enabled in a production build.');
  }

  if (result.data.PROD && result.data.VITE_API_BASE_URL.startsWith('http://')) {
    throw new Error('VITE_API_BASE_URL must use https in production.');
  }

  return result.data;
}

export const env: Env = readEnv();
