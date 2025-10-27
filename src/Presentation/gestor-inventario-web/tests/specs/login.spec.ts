import { test, expect } from '@playwright/test';

const fallbackBaseURL = 'http://127.0.0.1:3000';

function resolveBaseUrl(candidate?: string | null): string {
  const trimmed = candidate?.trim();

  if (trimmed && trimmed.length > 0) {
    return trimmed.includes('://') ? trimmed : `http://${trimmed}`;
  }

  return fallbackBaseURL;
}

const dashboardMock = {
  totalProducts: 12,
  activeProducts: 10,
  totalInventoryValue: 12345,
  lowStockVariants: 2,
  reorderAlerts: [],
  topSellingProducts: [],
  monthlySales: []
};

test('completes MFA challenge and redirects to dashboard', async ({ page, baseURL }) => {
  await page.route('**/auth/login', async (route) => {
    const requestBody = await route.request().postDataJSON();
    expect(requestBody.usernameOrEmail).toBe('twofactor');

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        requiresTwoFactor: true,
        twoFactorSessionId: 'session-abc',
        sessionExpiresAt: new Date(Date.now() + 5 * 60 * 1000).toISOString(),
        user: {
          id: 42,
          username: 'twofactor',
          email: 'twofactor@example.com',
          role: 'Planner',
          isActive: true
        }
      })
    });
  });

  await page.route('**/auth/login/mfa', async (route) => {
    const requestBody = await route.request().postDataJSON();
    expect(requestBody.sessionId).toBe('session-abc');
    expect(requestBody.verificationCode).toBe('123456');

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        requiresTwoFactor: false,
        token: 'fake-jwt-token',
        expiresAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
        user: {
          id: 42,
          username: 'twofactor',
          email: 'twofactor@example.com',
          role: 'Planner',
          isActive: true
        }
      })
    });
  });

  await page.route('**/analytics/dashboard', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(dashboardMock)
    });
  });

  const loginUrl = new URL('/login', resolveBaseUrl(baseURL)).toString();

  await page.goto(loginUrl);
  await page.getByLabel('Usuario o correo').fill('twofactor');
  await page.getByLabel('Contraseña').fill('FakePassword123');
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();

  await expect(page.getByLabel('Código MFA')).toBeVisible();
  await page.getByLabel('Código MFA').fill('123456');
  await page.getByRole('button', { name: 'Validar código MFA' }).click();

  await page.waitForURL('**/dashboard');
  await expect(page.getByText('Productos totales')).toBeVisible();
});
