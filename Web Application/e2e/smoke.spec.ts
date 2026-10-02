import { expect, test } from '@playwright/test';

/**
 * Smoke flow against a real local or deployed API. Credentials are mandatory and are never committed.
 * Covers: login → dashboard → users grid → sign out.
 */
const USER = process.env['E2E_USER'] ?? '';
const PASSWORD = process.env['E2E_PASSWORD'] ?? '';

test.beforeEach(() => {
  test.skip(!USER || !PASSWORD, 'Set E2E_USER and E2E_PASSWORD for a real API account.');
});

test('login, open users, sign out', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveURL(/\/login$/);

  await page.getByLabel(/user name/i).fill(USER);
  await page.getByLabel(/^password/i).fill(PASSWORD);
  await page.getByRole('button', { name: /log in/i }).click();

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
  await page.getByRole('button', { name: /log in/i }).click();
  await expect(page.getByRole('alert')).toContainText(/incorrect/i);
  await expect(page).toHaveURL(/\/login$/);
});

test('deep link to a protected page redirects to login and back', async ({ page }) => {
  await page.goto('/settings/security');
  await expect(page).toHaveURL(/\/login$/);
  await page.getByLabel(/user name/i).fill(USER);
  await page.getByLabel(/^password/i).fill(PASSWORD);
  await page.getByRole('button', { name: /log in/i }).click();
  await expect(page).toHaveURL(/\/settings\/security$/);
  await expect(page.getByRole('heading', { name: /security settings/i })).toBeVisible();
});
