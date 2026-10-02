import { expect, test } from '@playwright/test';

/**
 * Specification §8.2: a module this installation does not have must be absent from the menu AND its
 * routes unreachable — not merely disabled. The mock installation has Plant, Dispatch and Outbox;
 * Plans, Palette and Code pool are switched off.
 */
test.beforeEach(async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel(/user name/i).fill('admin');
  await page.getByLabel(/^password/i).fill('Admin@123');
  await page.getByRole('button', { name: /sign in/i }).click();
  await expect(page).toHaveURL(/\/dashboard/);
});

test('an enabled module appears in the menu and its screen opens', async ({ page }) => {
  const nav = page.getByRole('navigation', { name: /main navigation/i });

  await expect(nav.getByRole('link', { name: 'Dispatch' })).toBeVisible();
  await nav.getByRole('link', { name: 'Dispatch' }).click();

  await expect(page).toHaveURL(/\/dispatch/);
  await expect(page.getByRole('heading', { name: 'Dispatch' })).toBeVisible();
  await page.screenshot({ path: '/tmp/claude-0/modules-enabled.png', fullPage: true });
});

test('a disabled module is absent from the menu', async ({ page }) => {
  const nav = page.getByRole('navigation', { name: /main navigation/i });

  await expect(nav.getByRole('link', { name: 'Production plans' })).toHaveCount(0);
  await expect(nav.getByRole('link', { name: 'Palette' })).toHaveCount(0);
  await expect(nav.getByRole('link', { name: 'Code pool' })).toHaveCount(0);
});

test('a disabled module is unreachable by URL, not merely disabled', async ({ page }) => {
  await page.goto('/plans');

  await expect(page.getByRole('heading', { name: /page not found/i })).toBeVisible();
  // The screen itself must never render.
  await expect(page.getByText(/production plan master/i)).toHaveCount(0);
  await page.screenshot({ path: '/tmp/claude-0/modules-disabled.png', fullPage: true });
});

test('every always-present module is reachable', async ({ page }) => {
  for (const [path, heading] of [
    ['/brands', 'Brands'],
    ['/batches', 'Batches'],
    ['/case-data', 'Case data'],
    ['/portal-sync', 'Portal sync'],
    ['/license', 'Licence'],
  ] as const) {
    await page.goto(path);
    await expect(page.getByRole('heading', { name: heading })).toBeVisible();
  }
});
