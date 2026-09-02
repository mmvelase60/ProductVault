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

test.describe('catalogue data rendering', () => {
  test('renders categories and products as soon as their API responses arrive', async ({ page }) => {
    await page.addInitScript(() => localStorage.setItem('productvault-session', JSON.stringify({
      accessToken: 'test-token',
      expiresAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
      email: 'candidate@example.com',
      roles: ['User']
    })));
    await page.route('**/api/categories', async route => route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify([{ categoryId: 1, name: 'Cleaning material', categoryCode: 'CLE001', isActive: true, productCount: 1, rowVersion: 'AQ==' }])
    }));
    await page.route('**/api/products?**', async route => route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify({ items: [{ productId: 1, productCode: '202609-001', name: 'Handy andy', description: 'Cleaning product', price: 57.99, quantityInStock: 5, reorderLevel: 0, isLowStock: false, categoryId: 1, categoryName: 'Cleaning material', rowVersion: 'AQ==' }], page: 1, pageSize: 10, totalCount: 1 })
    }));

    await page.goto('/products');
    await expect(page.getByText('1 product in your private workspace.')).toBeVisible();
    await expect(page.getByRole('cell', { name: 'Handy andy' })).toBeVisible();
    await expect(page.getByRole('combobox', { name: 'Filter by category' })).toContainText('Cleaning material');

    await page.goto('/categories');
    await expect(page.getByRole('cell', { name: 'Cleaning material' })).toBeVisible();
  });
});
