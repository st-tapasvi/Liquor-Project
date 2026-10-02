import { expect, test } from '@playwright/test';

/**
 * Smoke flow against the dev server with the mock API (VITE_USE_MOCKS=true) or against a staging API
 * (E2E_BASE_URL + E2E_USER / E2E_PASSWORD). Covers: login → dashboard → users grid → sign out.
 */
const USER = process.env['E2E_USER'] ?? 'admin';
const PASSWORD = process.env['E2E_PASSWORD'] ?? 'Admin@123';

test('login, open users, sign out', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveURL(/\/login$/);

  await page.getByLabel(/user name/i).fill(USER);
  await page.getByLabel(/^password/i).fill(PASSWORD);
  await page.getByRole('button', { name: /sign in/i }).click();

  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(page.getByRole('heading', { name: /welcome/i })).toBeVisible();

  await page.getByRole('link', { name: 'Users' }).first().click();
  await expect(page).toHaveURL(/\/users$/);
  await expect(page.getByRole('grid', { name: 'Users' })).toBeVisible();

  await page.getByRole('button', { name: /sign out/i }).click();
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByText(/logged out/i)).toBeVisible();
});

test('wrong password shows the API message and stays on login', async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel(/user name/i).fill(USER);
  await page.getByLabel(/^password/i).fill('definitely-wrong');
  await page.getByRole('button', { name: /sign in/i }).click();
  await expect(page.getByRole('alert')).toContainText(/incorrect/i);
  await expect(page).toHaveURL(/\/login$/);
});

test('deep link to a protected page redirects to login and back', async ({ page }) => {
  await page.goto('/settings/security');
  await expect(page).toHaveURL(/\/login$/);
  await page.getByLabel(/user name/i).fill(USER);
  await page.getByLabel(/^password/i).fill(PASSWORD);
  await page.getByRole('button', { name: /sign in/i }).click();
  await expect(page).toHaveURL(/\/settings\/security$/);
  await expect(page.getByRole('heading', { name: /security settings/i })).toBeVisible();
});
