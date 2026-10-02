// @ts-check
import js from '@eslint/js';
import prettier from 'eslint-config-prettier';
import boundaries from 'eslint-plugin-boundaries';
import { createTypeScriptImportResolver } from 'eslint-import-resolver-typescript';
import importX from 'eslint-plugin-import-x';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import globals from 'globals';
import tseslint from 'typescript-eslint';

/**
 * ESLint (flat config).
 *
 * Three groups of rules:
 *  1. Correctness  – typescript-eslint (type-aware), react-hooks, import hygiene.
 *  2. Architecture – layer boundaries (app → features → entities → shared → core) and module entry points.
 *  3. Security     – no innerHTML, no eval-like APIs, no browser storage for anything, no raw axios/fetch
 *                    outside core, no console output in committed code.
 *
 * `npm run lint` runs with --max-warnings 0, so a warning here is as blocking as an error.
 */
export default tseslint.config(
  {
    ignores: [
      'dist/**',
      'coverage/**',
      'node_modules/**',
      'src/core/api/generated/**',
      'playwright-report/**',
      'test-results/**',
    ],
  },

  js.configs.recommended,
  ...tseslint.configs.strictTypeChecked.map((c) => ({ ...c, files: ['**/*.{ts,tsx}'] })),
  ...tseslint.configs.stylisticTypeChecked.map((c) => ({ ...c, files: ['**/*.{ts,tsx}'] })),

  {
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      ecmaVersion: 2023,
      globals: { ...globals.browser, ...globals.es2023 },
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
      'import-x': importX,
      boundaries,
    },
    settings: {
      'import-x/resolver-next': [
        createTypeScriptImportResolver({ project: ['tsconfig.app.json', 'tsconfig.node.json'] }),
      ],
      // eslint-plugin-boundaries resolves imports through the classic `import/resolver` setting.
      'import/resolver': { typescript: { project: ['tsconfig.app.json', 'tsconfig.node.json'] } },
      'boundaries/elements': [
        { type: 'app', pattern: 'src/app/**' },
        { type: 'features', pattern: 'src/features/*', capture: ['feature'], mode: 'folder' },
        { type: 'entities', pattern: 'src/entities/*', capture: ['entity'], mode: 'folder' },
        { type: 'shared', pattern: 'src/shared/**' },
        { type: 'core', pattern: 'src/core/**' },
        { type: 'test', pattern: 'src/test/**' },
      ],
      'boundaries/ignore': ['**/*.test.{ts,tsx}', 'src/main.tsx'],
    },
    rules: {
      // ---- React ----
      ...reactHooks.configs['recommended-latest'].rules,
      'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],

      // ---- TypeScript strictness (on top of strictTypeChecked) ----
      '@typescript-eslint/no-explicit-any': 'error',
      '@typescript-eslint/explicit-module-boundary-types': 'off',
      '@typescript-eslint/consistent-type-imports': [
        'error',
        { prefer: 'type-imports', fixStyle: 'inline-type-imports' },
      ],
      '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_', varsIgnorePattern: '^_' }],
      '@typescript-eslint/no-non-null-assertion': 'error',
      '@typescript-eslint/no-floating-promises': 'error',
      '@typescript-eslint/no-misused-promises': ['error', { checksVoidReturn: { attributes: false } }],
      '@typescript-eslint/restrict-template-expressions': ['error', { allowNumber: true }],
      '@typescript-eslint/no-unnecessary-condition': 'off',
      '@typescript-eslint/no-confusing-void-expression': 'off',
      '@typescript-eslint/switch-exhaustiveness-check': 'error',
      '@typescript-eslint/dot-notation': ['error', { allowIndexSignaturePropertyAccess: true }],
      '@typescript-eslint/ban-ts-comment': [
        'error',
        { 'ts-expect-error': 'allow-with-description', 'ts-ignore': true },
      ],

      // ---- Imports ----
      'import-x/no-duplicates': 'error',
      'import-x/no-cycle': ['error', { maxDepth: 4 }],
      'import-x/order': [
        'error',
        {
          groups: ['builtin', 'external', 'internal', 'parent', 'sibling', 'index'],
          pathGroups: [
            { pattern: '@/core/**', group: 'internal', position: 'before' },
            { pattern: '@/shared/**', group: 'internal', position: 'before' },
            { pattern: '@/entities/**', group: 'internal', position: 'before' },
            { pattern: '@/features/**', group: 'internal', position: 'before' },
            { pattern: '@/app/**', group: 'internal', position: 'before' },
          ],
          pathGroupsExcludedImportTypes: ['builtin'],
          'newlines-between': 'always',
          alphabetize: { order: 'asc', caseInsensitive: true },
        },
      ],

      // ---- Architecture: one-way layer dependencies, module entry points ----
      'boundaries/dependencies': [
        'error',
        {
          default: 'disallow',
          policies: [
            {
              from: { element: { type: 'app' } },
              allow: [
                { to: { element: { type: 'app' } } },
                { to: { element: { type: ['features', 'entities'], fileInternalPath: 'index.ts' } } },
                { to: { element: { type: ['shared', 'core'] } } },
              ],
            },
            {
              from: { element: { type: 'features' } },
              allow: [
                // A feature may import its own files freely, other features never.
                { to: { element: { type: 'features', captured: { feature: '{{ from.element.captured.feature }}' } } } },
                { to: { element: { type: 'entities', fileInternalPath: 'index.ts' } } },
                { to: { element: { type: ['shared', 'core'] } } },
              ],
            },
            {
              from: { element: { type: 'entities' } },
              allow: [
                { to: { element: { type: 'entities', captured: { entity: '{{ from.element.captured.entity }}' } } } },
                { to: { element: { type: 'entities', fileInternalPath: 'index.ts' } } },
                { to: { element: { type: ['shared', 'core'] } } },
              ],
            },
            { from: { element: { type: 'shared' } }, allow: { to: { element: { type: ['shared', 'core'] } } } },
            { from: { element: { type: 'core' } }, allow: { to: { element: { type: 'core' } } } },
            { from: { element: { type: 'test' } }, allow: { to: { element: { type: '*' } } } },
          ],
        },
      ],
      'boundaries/no-unknown-files': 'off',

      // ---- Security ----
      'no-console': 'error',
      'no-eval': 'error',
      'no-implied-eval': 'error',
      'no-new-func': 'error',
      'no-script-url': 'error',
      'no-restricted-globals': [
        'error',
        {
          name: 'localStorage',
          message: 'Browser storage is forbidden for application data (see SECURITY.md). Use Zustand or the URL.',
        },
        { name: 'sessionStorage', message: 'Browser storage is forbidden for application data (see SECURITY.md).' },
      ],
      'no-restricted-properties': [
        'error',
        { object: 'window', property: 'localStorage', message: 'Browser storage is forbidden for application data.' },
        { object: 'window', property: 'sessionStorage', message: 'Browser storage is forbidden for application data.' },
        {
          object: 'document',
          property: 'cookie',
          message: 'The session cookie is HttpOnly; JavaScript never reads or writes cookies.',
        },
        { object: 'document', property: 'write', message: 'document.write is an XSS sink.' },
      ],
      'no-restricted-syntax': [
        'error',
        {
          selector: "JSXAttribute[name.name='dangerouslySetInnerHTML']",
          message:
            'dangerouslySetInnerHTML is forbidden. Render text; if HTML from the API is ever needed, add a sanitising SafeHtml component in shared/ (DOMPurify) and review it.',
        },
        {
          selector:
            "MemberExpression[property.name='innerHTML'], MemberExpression[property.name='outerHTML'], CallExpression[callee.property.name='insertAdjacentHTML']",
          message: 'Direct HTML injection is an XSS sink.',
        },
      ],
      'no-restricted-imports': [
        'error',
        {
          paths: [
            { name: 'axios', message: 'Only src/core/api/http.ts may import axios. Use the api functions / hooks.' },
            { name: '@mui/x-data-grid', message: 'Use AppDataGrid from @/shared/components/data-grid.' },
          ],
        },
      ],
    },
  },

  // The single place that owns the HTTP client.
  {
    files: ['src/core/api/http.ts', 'src/core/api/http.test.ts', 'src/core/api/interceptors/**'],
    rules: { 'no-restricted-imports': 'off' },
  },
  // The grid wrapper may import MUI X.
  {
    files: ['src/shared/components/data-grid/**'],
    rules: {
      'no-restricted-imports': [
        'error',
        { paths: [{ name: 'axios', message: 'Only core/api/http.ts may import axios.' }] },
      ],
    },
  },
  // The console transport is the only permitted console user.
  {
    files: ['src/core/logging/transports/console.transport.ts'],
    rules: { 'no-console': 'off' },
  },
  // Provider + hook pairs live in one file by design (context is private to the file).
  {
    files: ['src/shared/hooks/**', 'src/core/auth/**'],
    rules: { 'react-refresh/only-export-components': 'off' },
  },
  // Tests: relaxed typing for focused component and unit test fixtures.
  {
    files: ['src/**/*.test.{ts,tsx}', 'src/test/**', 'e2e/**'],
    rules: {
      '@typescript-eslint/no-non-null-assertion': 'off',
      '@typescript-eslint/no-unsafe-assignment': 'off',
      '@typescript-eslint/no-unsafe-member-access': 'off',
      '@typescript-eslint/no-unsafe-argument': 'off',
      'react-refresh/only-export-components': 'off',
    },
  },
  // Node-side config files: plain JS is linted without type information.
  {
    files: ['*.config.js'],
    languageOptions: { globals: { ...globals.node } },
  },
  {
    files: ['*.config.ts'],
    languageOptions: { globals: { ...globals.node } },
    rules: { 'boundaries/dependencies': 'off' },
  },

  prettier,
);
