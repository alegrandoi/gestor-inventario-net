import type { Page } from '@playwright/test';

type DashboardSummary = {
  totalProducts: number;
  activeProducts: number;
  totalInventoryValue: number;
  lowStockVariants: number;
  reorderAlerts: unknown[];
  topSellingProducts: unknown[];
  monthlySales: unknown[];
};

const dashboardMock: DashboardSummary = {
  totalProducts: 12,
  activeProducts: 10,
  totalInventoryValue: 12345,
  lowStockVariants: 2,
  reorderAlerts: [],
  topSellingProducts: [],
  monthlySales: []
};

export async function loginAsTestUser(page: Page, baseUrl: string): Promise<void> {
  await page.route('**/api/auth/login', async (route) => {
    const body = await route.request().postDataJSON();
    if (!('usernameOrEmail' in body) || !('password' in body)) {
      throw new Error('Login payload missing credentials');
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        requiresTwoFactor: false,
        token: 'playwright-test-token',
        expiresAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
        user: {
          id: 9999,
          username: body.usernameOrEmail,
          email: `${body.usernameOrEmail}@example.com`,
          role: 'Planner',
          isActive: true
        }
      })
    });
  });

  await page.route('**/api/analytics/dashboard', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(dashboardMock)
    });
  });

  const loginUrl = new URL('/login', baseUrl).toString();
  await page.goto(loginUrl);
  await page.getByLabel('Usuario o correo').fill('playwright');
  await page.getByLabel('Contraseña').fill('FakePassword123');
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();

  await page.waitForURL('**/dashboard');
}
