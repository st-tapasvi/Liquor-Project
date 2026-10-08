import { z } from 'zod';

const envSchema = z.object({
  MODE: z.enum(['development', 'production', 'test']),
  DEV: z.boolean(),
  PROD: z.boolean(),
  VITE_API_BASE_URL: z
    .string()
    .trim()
    .min(1)
    .default('/api')
    .refine(
      (v) => v.startsWith('/') || /^https?:\/\//.test(v),
      'VITE_API_BASE_URL must be a relative path or an absolute URL',
    ),
  VITE_LOG_LEVEL: z.enum(['debug', 'info', 'warn', 'error']).default('info'),
  VITE_APP_NAME: z.string().trim().min(1).default('Excise Track & Trace'),
});

export type Env = z.infer<typeof envSchema>;

function readEnv(): Env {
  const raw = import.meta.env as unknown as Record<string, unknown>;
  const result = envSchema.safeParse({
    MODE: raw['MODE'],
    DEV: raw['DEV'],
    PROD: raw['PROD'],
    VITE_API_BASE_URL: raw['VITE_API_BASE_URL'],
    VITE_LOG_LEVEL: raw['VITE_LOG_LEVEL'],
    VITE_APP_NAME: raw['VITE_APP_NAME'],
  });

  if (!result.success) {
    const problems = result.error.issues.map((i) => `${i.path.join('.')}: ${i.message}`).join('; ');
    throw new Error(`Invalid build configuration: ${problems}`);
  }

  if (result.data.PROD && result.data.VITE_API_BASE_URL.startsWith('http://')) {
    throw new Error('VITE_API_BASE_URL must use https in production.');
  }

  return result.data;
}

export const env: Env = readEnv();
