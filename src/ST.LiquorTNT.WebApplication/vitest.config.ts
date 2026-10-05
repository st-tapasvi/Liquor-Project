import { fileURLToPath, URL } from 'node:url';

import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  define: { __APP_VERSION__: JSON.stringify('0.0.0-test'), __BUILD_TIME__: JSON.stringify('1970-01-01T00:00:00.000Z') },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
    exclude: ['e2e/**', 'node_modules/**'],
    restoreMocks: true,
    coverage: {
      provider: 'v8',
      reporter: ['text', 'html', 'lcov'],
      include: ['src/**/*.{ts,tsx}'],
      exclude: [
        'src/test/**',
        'src/**/*.test.{ts,tsx}',
        'src/main.tsx',
        'src/vite-env.d.ts',
        'src/core/api/generated/**',
      ],
      // Floors, not goals: set just under the scaffold's coverage so CI fails on regression.
      // Raise them with every feature (target from the standards doc: 70% lines overall, 90% for shared/utils and core/errors).
      thresholds: {
        lines: 40,
        functions: 35,
        branches: 40,
        statements: 40,
      },
    },
  },
});
