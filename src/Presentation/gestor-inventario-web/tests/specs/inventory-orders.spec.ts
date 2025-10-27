import { expect, test } from '@playwright/test';
import { resolveBaseUrl } from './support/base-url';
import { loginAsTestUser } from './support/auth';

test.describe('Inventory adjustments', () => {
  test('registers a move transaction and updates balances', async ({ page, baseURL }) => {
    const stocks = [
      {
        variantId: 1,
        variantSku: 'SKU-001',
        productName: 'Producto destacado',
        warehouseId: 10,
        warehouseName: 'Central',
        quantity: 20,
        reservedQuantity: 4,
        minStockLevel: 5
      },
      {
        variantId: 1,
        variantSku: 'SKU-001',
        productName: 'Producto destacado',
        warehouseId: 11,
        warehouseName: 'Secundario',
        quantity: 8,
        reservedQuantity: 1,
        minStockLevel: 2
      }
    ];

    const warehouses = [
      { id: 10, name: 'Central', code: 'CEN', address: 'Calle 1', isActive: true },
      { id: 11, name: 'Secundario', code: 'SEC', address: 'Calle 2', isActive: true }
    ];

    let adjustPayload: Record<string, unknown> | null = null;

    const resolvedBaseUrl = resolveBaseUrl(baseURL);

    await loginAsTestUser(page, resolvedBaseUrl);

    await page.route('**/api/inventory**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(stocks)
      });
    });

    await page.route('**/api/warehouses**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(warehouses)
      });
    });

    await page.route('**/api/inventory/adjust', async (route) => {
      adjustPayload = await route.request().postDataJSON();

      expect(adjustPayload).toMatchObject({
        variantId: 1,
        warehouseId: 10,
        transactionType: 3,
        destinationWarehouseId: 11,
        quantity: 5,
        notes: 'Traslado automatizado'
      });

      const updatedStocks = [
        {
          ...stocks[0],
          quantity: 15
        },
        {
          ...stocks[1],
          quantity: 13
        }
      ];

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(updatedStocks)
      });
    });

    await page.getByRole('link', { name: 'Inventario' }).click();
    await page.waitForURL('**/dashboard/inventory');

    const firstInventoryRow = page.locator('tr', { hasText: 'Producto destacado' }).first();
    await expect(firstInventoryRow).toBeVisible();

    await expect(firstInventoryRow.getByRole('button', { name: 'Otro movimiento' })).toBeVisible();

    await firstInventoryRow.getByRole('button', { name: 'Otro movimiento' }).click();

    await expect(page.getByRole('heading', { name: 'Registrar movimiento de inventario' })).toBeVisible();

    const inventoryDialog = page.getByRole('dialog');

    await inventoryDialog.getByLabel('Tipo de transacción').selectOption('3');
    await inventoryDialog.getByLabel('Cantidad').fill('5');
    await inventoryDialog.getByLabel('Almacén destino').selectOption('11');
    await inventoryDialog.getByLabel('Notas').fill('Traslado automatizado');

    const submitButton = inventoryDialog.getByRole('button', { name: 'Registrar movimiento' });
    await submitButton.evaluate((button) => (button as HTMLButtonElement).click());

    await expect(inventoryDialog).not.toBeVisible();
    expect(adjustPayload).not.toBeNull();

    await expect(page.getByText('Inventario actualizado')).toBeVisible();
    await expect(page.getByRole('row', { name: /Central/ }).getByRole('cell', { name: '15' })).toBeVisible();
    await expect(page.getByRole('row', { name: /Secundario/ }).getByRole('cell', { name: '13' })).toBeVisible();
  });
});
