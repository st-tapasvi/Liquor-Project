import { readFileSync } from 'node:fs';
import { fileURLToPath, URL } from 'node:url';

import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

/**
 * Build configuration.
 *
 * - `base: '/'` – the production bundle is served by the ASP.NET Core API from wwwroot at the root URL.
 * - Hidden source maps: generated for debugging/symbolication, but no `//# sourceMappingURL` comment is
 *   emitted, so browsers never download them. Do not deploy the `.map` files to the web root.
 * - Manual chunks keep the vendor code cacheable across releases and the initial bundle small.
 * - The dev-server proxy forwards `/api` to the API so the browser sees ONE origin in development too:
 *   the session cookie is first-party, CORS is not needed, and dev behaves like production.
 */
const pkg = JSON.parse(readFileSync(new URL('./package.json', import.meta.url), 'utf8')) as { version: string };

export default defineConfig({
  plugins: [react()],
  define: {
    __APP_VERSION__: JSON.stringify(pkg.version),
    __BUILD_TIME__: JSON.stringify(new Date().toISOString()),
  },
  base: '/',
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: process.env['VITE_DEV_API_TARGET'] ?? 'http://localhost:5180',
        changeOrigin: false,
        secure: false,
      },
    },
  },
  build: {
    target: 'es2022',
    sourcemap: 'hidden',
    cssCodeSplit: true,
    chunkSizeWarningLimit: 600,
    rollupOptions: {
      output: {
        // Vendor chunks stay cacheable across releases; the initial bundle holds only what every page needs.
        manualChunks(id) {
          if (!id.includes('node_modules')) return undefined;
          if (/[\\/]node_modules[\\/](react|react-dom|react-router|scheduler)[\\/]/.test(id)) return 'react';
          if (/[\\/]node_modules[\\/]@mui[\\/]x-data-grid/.test(id)) return 'mui-x';
          if (/[\\/]node_modules[\\/](@mui|@emotion)[\\/]/.test(id)) return 'mui';
          if (/[\\/]node_modules[\\/]@tanstack[\\/]/.test(id)) return 'query';
          if (/[\\/]node_modules[\\/](react-hook-form|@hookform|zod)[\\/]/.test(id)) return 'forms';
          return undefined;
        },
      },
    },
  },
});
