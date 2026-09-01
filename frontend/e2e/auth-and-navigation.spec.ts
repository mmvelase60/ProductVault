import { expect, test } from '@playwright/test';

test.describe('authentication and navigation', () => {
  test('redirects anonymous visitors to sign in and provides a keyboard skip link', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login$/);
    await expect(page.getByRole('heading', { name: 'Sign in to ProductVault' })).toBeVisible();

    await page.keyboard.press('Tab');
    const skipLink = page.getByRole('link', { name: 'Skip to main content' });
    await expect(skipLink).toBeFocused();
    await skipLink.press('Enter');
    await expect(page.locator('#main-content')).toBeFocused();
  });

  test('registration describes the generated username and links to sign in', async ({ page }) => {
    await page.goto('/register');

    await expect(page.getByRole('heading', { name: 'Create your account' })).toBeVisible();
    await expect(page.getByText('Mthokozisi Mvelase → MMvelase.')).toBeVisible();
    await page.getByRole('link', { name: 'Sign in' }).last().click();
    await expect(page).toHaveURL(/\/login$/);
  });
});

test.describe('responsive navigation', () => {
  test('shows a usable account menu on a small viewport', async ({ page }) => {
    await page.addInitScript(() => localStorage.setItem('productvault-session', JSON.stringify({
      accessToken: 'test-token',
      expiresAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
      email: 'candidate@example.com',
      roles: ['User']
    })));
    await page.route('**/api/dashboard', async route => route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify({ productCount: 0, activeCategoryCount: 0, totalCategoryCount: 0, catalogueValue: 0, lowStockCount: 0, recentProducts: [], activity: [] })
    }));
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/dashboard');

    const menu = page.getByRole('button', { name: 'Toggle navigation' });
    await expect(menu).toBeVisible();
    await menu.click();
    await expect(page.getByRole('link', { name: 'Products' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Sign out' })).toBeVisible();
  });
});
