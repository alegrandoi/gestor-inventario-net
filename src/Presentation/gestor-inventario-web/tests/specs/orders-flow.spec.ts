import { expect, test } from '@playwright/test';
import { resolveBaseUrl } from './support/base-url';
import { loginAsTestUser } from './support/auth';
import type { PurchaseOrderDto, SalesOrderDto } from '../../src/types/api';

test.describe('Orders dashboard', () => {
  test('manages existing orders and creates a new sales order', async ({ page, baseURL }) => {
    const warehouses = [
      { id: 10, name: 'Central', code: 'CEN', address: 'Calle 1', isActive: true },
      { id: 11, name: 'Secundario', code: 'SEC', address: 'Calle 2', isActive: true }
    ];

    const customersCatalog = [
      { id: 1, name: 'Acme Corp', email: 'ventas@acme.com', phone: '123456789', address: 'Gran Vía 1' },
      { id: 2, name: 'Innova Solar', email: 'contacto@innova.com', phone: '987654321', address: 'Diagonal 200' }
    ];

    const suppliersCatalog = [
      { id: 3, name: 'Proveedor Uno', contactName: 'Laura Gómez', email: 'lgomez@proveedor.com', phone: '555123123', address: 'Calle Proveedor 10' }
    ];

    const carriersCatalog = [
      {
        id: 201,
        name: 'Logística Express',
        contactName: 'Carlos Ruiz',
        email: 'operaciones@logisticaexpress.com',
        phone: '+34 600 111 222',
        trackingUrl: 'https://logisticaexpress.com/track/'
      }
    ];

    const productsCatalog = [
      {
        id: 801,
        code: 'KIT-SOLAR',
        name: 'Kit solar residencial',
        description: 'Kit completo',
        categoryId: null,
        defaultPrice: 1500,
        currency: 'EUR',
        taxRateId: null,
        appliedTaxRate: null,
        finalPrice: null,
        isActive: true,
        requiresSerialTracking: false,
        weightKg: 15,
        heightCm: 40,
        widthCm: 60,
        lengthCm: 80,
        leadTimeDays: 7,
        safetyStock: 5,
        reorderPoint: 10,
        reorderQuantity: 20,
        variants: [{ id: 901, sku: 'KIT-SOLAR-STD', attributes: 'standard', price: 1500, barcode: '123' }],
        images: []
      },
      {
        id: 802,
        code: 'BATT-INDU',
        name: 'Batería industrial',
        description: 'Batería modular para instalaciones industriales',
        categoryId: null,
        defaultPrice: 1800,
        currency: 'EUR',
        taxRateId: null,
        appliedTaxRate: null,
        finalPrice: null,
        isActive: true,
        requiresSerialTracking: false,
        weightKg: 55,
        heightCm: 90,
        widthCm: 70,
        lengthCm: 120,
        leadTimeDays: 14,
        safetyStock: 2,
        reorderPoint: 4,
        reorderQuantity: 6,
        variants: [
          { id: 902, sku: 'BATT-INDU-10K', attributes: '10kWh', price: 2000, barcode: '456' },
          { id: 903, sku: 'BATT-INDU-15K', attributes: '15kWh', price: 2400, barcode: '789' }
        ],
        images: []
      }
    ];

    const salesOrdersState = [
      {
        id: 501,
        customerId: 1,
        customerName: 'Acme Corp',
        orderDate: new Date('2024-03-10T10:00:00Z').toISOString(),
        status: 'Pending',
        totalAmount: 2500,
        currency: 'EUR',
        lines: [],
        fulfillmentRate: 0,
        shipments: []
      }
    ];

    const salesOrderDetail = {
      ...salesOrdersState[0],
      lines: [
        {
          id: 7001,
          variantId: 900,
          variantSku: 'SKU-SALES',
          productName: 'Kit solar',
          quantity: 5,
          unitPrice: 500,
          discount: 0,
          totalLine: 2500,
          allocations: [
            {
              id: 8801,
              warehouseId: 10,
              warehouseName: 'Central',
              quantity: 5,
              fulfilledQuantity: 0,
              status: 'Reserved',
              createdAt: new Date('2024-03-10T10:05:00Z').toISOString()
            }
          ]
        }
      ]
    } satisfies SalesOrderDto;

    const purchaseOrdersState = [
      {
        id: 701,
        supplierId: 3,
        supplierName: 'Proveedor Uno',
        orderDate: new Date('2024-03-09T12:00:00Z').toISOString(),
        status: 'Ordered',
        totalAmount: 800,
        currency: 'EUR',
        lines: [
          {
            id: 9101,
            variantId: 901,
            variantSku: 'SKU-PURCHASE',
            productName: 'Panel repuesto',
            quantity: 8,
            unitPrice: 100,
            totalLine: 800
          }
        ]
      }
    ];

    const purchaseOrderDetail = {
      ...purchaseOrdersState[0]
    } satisfies PurchaseOrderDto;

    const newSalesOrderDetail = {
      id: 902,
      customerId: 2,
      customerName: 'Innova Solar',
      orderDate: new Date('2024-04-01T09:00:00Z').toISOString(),
      status: 'Pending',
      totalAmount: 3800,
      currency: 'EUR',
      notes: 'Instalación urgente en nave industrial',
      shippingAddress: 'Parque Industrial 45',
      lines: [
        {
          id: 9601,
          variantId: 903,
          variantSku: 'BATT-INDU-15K',
          productName: 'Batería industrial',
          quantity: 1,
          unitPrice: 2400,
          discount: 0,
          totalLine: 2400,
          allocations: []
        },
        {
          id: 9602,
          variantId: 901,
          variantSku: 'KIT-SOLAR-STD',
          productName: 'Kit solar residencial',
          quantity: 1,
          unitPrice: 1400,
          discount: 0,
          totalLine: 1400,
          allocations: []
        }
      ],
      fulfillmentRate: 0,
      shipments: []
    } satisfies SalesOrderDto;

    let shipUpdatePayload: Record<string, unknown> | null = null;
    let receiveUpdatePayload: Record<string, unknown> | null = null;
    let createdSalesOrderPayload: Record<string, unknown> | null = null;

    await page.route('**/api/warehouses', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(warehouses)
      });
    });

    await page.route('**/api/salesorders', async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(salesOrdersState)
        });
        return;
      }

      if (route.request().method() === 'POST') {
        createdSalesOrderPayload = await route.request().postDataJSON();
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify(newSalesOrderDetail)
        });
        return;
      }

      await route.fallback();
    });

    await page.route('**/api/salesorders/501', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(salesOrderDetail)
      });
    });

    await page.route('**/api/salesorders/501/status', async (route) => {
      shipUpdatePayload = await route.request().postDataJSON();

      expect(shipUpdatePayload).toMatchObject({
        orderId: 501,
        status: 3,
        allocations: [
          {
            variantId: 900,
            warehouseId: 10,
            quantity: 5
          }
        ]
      });

      salesOrdersState[0] = { ...salesOrdersState[0], status: 'Shipped' };

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ ...salesOrderDetail, status: 'Shipped' })
      });
    });

    await page.route('**/api/purchaseorders', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(purchaseOrdersState)
      });
    });

    await page.route('**/api/purchaseorders/701', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(purchaseOrderDetail)
      });
    });

    await page.route('**/api/purchaseorders/701/status', async (route) => {
      receiveUpdatePayload = await route.request().postDataJSON();

      expect(receiveUpdatePayload).toMatchObject({
        orderId: 701,
        status: 3,
        warehouseId: 11
      });

      purchaseOrdersState[0] = { ...purchaseOrdersState[0], status: 'Received' };

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ ...purchaseOrderDetail, status: 'Received' })
      });
    });

    await page.route('**/customers', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(customersCatalog)
      });
    });

    await page.route('**/suppliers', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(suppliersCatalog)
      });
    });

    await page.route('**/products', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(productsCatalog)
      });
    });

    await page.route('**/carriers', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(carriersCatalog)
      });
    });

    await page.route('**/api/salesorders/902', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(newSalesOrderDetail)
      });
    });

    const resolvedBaseUrl = resolveBaseUrl(baseURL);

    await loginAsTestUser(page, resolvedBaseUrl);

    await page.getByRole('link', { name: 'Pedidos' }).click();
    await page.waitForURL('**/dashboard/orders');

    await expect(page.getByRole('heading', { name: 'Pedidos de venta' })).toBeVisible();

    const salesRow = page.getByRole('row', { name: /#00501/ });
    await expect(salesRow.getByRole('cell', { name: 'Pendiente' })).toBeVisible();

    await salesRow.getByRole('button', { name: 'Ver pedido' }).click();
    await page.waitForURL('**/dashboard/orders/sales/501');
    await expect(page.getByRole('heading', { name: 'Pedido de venta #00501' })).toBeVisible();
    await page.getByRole('button', { name: 'Volver a pedidos' }).click();
    await page.waitForURL('**/dashboard/orders');

    await page.getByRole('button', { name: 'Enviar' }).click();
    const shipmentDialog = page.getByRole('dialog');
    await expect(shipmentDialog.getByRole('heading', { name: 'Confirmar envío' })).toBeVisible();
    await expect(shipmentDialog.getByRole('cell', { name: 'Kit solar' })).toBeVisible();

    await shipmentDialog.getByRole('button', { name: 'Confirmar envío' }).click();

    await expect(shipmentDialog).not.toBeVisible();
    await expect(page.getByText('marcado como enviado.')).toBeVisible();
    await expect(salesRow.getByRole('cell', { name: 'Enviado' })).toBeVisible();
    expect(shipUpdatePayload).not.toBeNull();

    await page.getByRole('button', { name: 'Registrar recepción' }).click();
    const receptionDialog = page.getByRole('dialog');
    await expect(receptionDialog.getByRole('heading', { name: 'Registrar recepción' })).toBeVisible();
    await receptionDialog.getByLabel('Almacén de recepción').selectOption('11');

    await receptionDialog.getByRole('button', { name: 'Registrar recepción' }).click();

    await expect(receptionDialog).not.toBeVisible();
    await expect(page.getByText('recibido correctamente.')).toBeVisible();
    await expect(page.getByRole('row', { name: /#00701/ }).getByRole('cell', { name: 'Recibido' })).toBeVisible();
    expect(receiveUpdatePayload).not.toBeNull();

    await page.getByRole('button', { name: 'Nuevo pedido' }).click();
    await page.waitForURL('**/dashboard/orders/new');

    const customerSelect = page.getByLabel('Cliente');
    await expect(customerSelect).toBeEnabled();
    await expect.poll(async () =>
      customerSelect.evaluate((element) => element instanceof HTMLSelectElement ? element.options.length : 0)
    ).toBeGreaterThan(1);
    await customerSelect.selectOption('2');
    await page.getByLabel('Dirección de envío').fill('Parque Industrial 45');

    const productSelects = page.getByLabel('Producto');
    await productSelects.nth(0).selectOption('903');
    const quantityInputs = page.getByLabel('Cantidad');
    await quantityInputs.nth(0).fill('1');
    const unitPriceInputs = page.getByLabel('Precio unitario');
    await unitPriceInputs.nth(0).fill('2400');

    await page.getByRole('button', { name: 'Añadir producto' }).click();

    await productSelects.nth(1).selectOption('901');
    await quantityInputs.nth(1).fill('1');
    await unitPriceInputs.nth(1).fill('1400');

    await page.getByRole('button', { name: 'Registrar pedido' }).click();

    await page.waitForURL('**/dashboard/orders/sales/902');
    await expect(page.getByRole('heading', { name: 'Pedido de venta #00902' })).toBeVisible();
    await expect(page.getByText('Pedido de venta para Innova Solar')).toBeVisible();
    await expect(page.getByRole('cell', { name: 'Batería industrial' })).toBeVisible();

    expect(createdSalesOrderPayload).not.toBeNull();
    expect(createdSalesOrderPayload).toMatchObject({
      customerId: 2,
      currency: 'EUR',
      lines: [
        { variantId: 903, quantity: 1, unitPrice: 2400 },
        { variantId: 901, quantity: 1, unitPrice: 1400 }
      ]
    });
  });
});
