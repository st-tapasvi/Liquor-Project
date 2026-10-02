import { expect, test } from '@playwright/test';

test('device limit: the login page lists the signed-in devices and lets one be ended', async ({ page }) => {
  await page.goto('/login');

  await page.getByLabel(/user name/i).fill('full');
  await page.getByLabel(/^password/i).fill('Admin@123');
  await page.getByRole('button', { name: /sign in/i }).click();

  const panel = page.getByRole('region', { name: /signed-in devices/i });
  await expect(panel).toBeVisible();
  await expect(panel).toContainText(/Chrome on Windows/i);
  await expect(panel).toContainText(/Safari on iOS/i);
  await page.screenshot({ path: '/tmp/claude-0/device-limit.png', fullPage: true });

  await panel
    .getByRole('button', { name: /sign out and continue/i })
    .first()
    .click();

  await expect(page).toHaveURL(/\/dashboard/);
  await page.screenshot({ path: '/tmp/claude-0/device-limit-after.png', fullPage: true });
});
