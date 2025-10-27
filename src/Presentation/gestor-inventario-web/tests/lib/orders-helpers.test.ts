import { describe, expect, test } from 'vitest';
import type { SalesOrderDto } from '../../src/types/api';
import { computePendingShipAllocations } from '../../src/lib/orders-helpers';

describe('computePendingShipAllocations', () => {
  test('returns an empty array when the order is null', () => {
    expect(computePendingShipAllocations(null)).toEqual([]);
  });

  test('ignores allocations that are already fulfilled', () => {
    const order = {
      id: 1,
      customerId: 1,
      customerName: 'Cliente Demo',
      orderDate: new Date().toISOString(),
      status: 'Pending',
      totalAmount: 100,
      currency: 'EUR',
      lines: [
        {
          id: 10,
          variantId: 200,
          variantSku: 'SKU-200',
          productName: 'Panel solar',
          quantity: 10,
          unitPrice: 10,
          discount: 0,
          totalLine: 100,
          allocations: [
            {
              id: 1,
              warehouseId: 5,
              warehouseName: 'Central',
              quantity: 5,
              fulfilledQuantity: 5,
              status: 'Fulfilled',
              createdAt: new Date().toISOString()
            }
          ]
        }
      ],
      fulfillmentRate: 0,
      shipments: []
    } satisfies SalesOrderDto;

    expect(computePendingShipAllocations(order)).toEqual([]);
  });

  test('returns pending quantities grouped per allocation', () => {
    const order = {
      id: 2,
      customerId: 2,
      customerName: 'Cliente Dos',
      orderDate: new Date().toISOString(),
      status: 'Pending',
      totalAmount: 300,
      currency: 'EUR',
      lines: [
        {
          id: 11,
          variantId: 500,
          variantSku: 'SKU-500',
          productName: 'Controlador',
          quantity: 6,
          unitPrice: 50,
          discount: 0,
          totalLine: 300,
          allocations: [
            {
              id: 7,
              warehouseId: 1,
              warehouseName: 'Principal',
              quantity: 4,
              fulfilledQuantity: 1,
              status: 'Reserved',
              createdAt: new Date().toISOString()
            },
            {
              id: 8,
              warehouseId: 2,
              warehouseName: 'Secundario',
              quantity: 2,
              fulfilledQuantity: 0,
              status: 'Reserved',
              createdAt: new Date().toISOString()
            }
          ]
        }
      ],
      fulfillmentRate: 0,
      shipments: []
    } satisfies SalesOrderDto;

    expect(computePendingShipAllocations(order)).toEqual([
      {
        variantId: 500,
        variantSku: 'SKU-500',
        productName: 'Controlador',
        warehouseId: 1,
        warehouseName: 'Principal',
        pending: 3
      },
      {
        variantId: 500,
        variantSku: 'SKU-500',
        productName: 'Controlador',
        warehouseId: 2,
        warehouseName: 'Secundario',
        pending: 2
      }
    ]);
  });

  test('filters out allocations that would result in negative pending quantities', () => {
    const order = {
      id: 3,
      customerId: 4,
      customerName: 'Cliente Cuatro',
      orderDate: new Date().toISOString(),
      status: 'Pending',
      totalAmount: 80,
      currency: 'EUR',
      lines: [
        {
          id: 21,
          variantId: 700,
          variantSku: 'SKU-700',
          productName: 'Inversor',
          quantity: 4,
          unitPrice: 20,
          discount: 0,
          totalLine: 80,
          allocations: [
            {
              id: 9,
              warehouseId: 3,
              warehouseName: 'Experimental',
              quantity: 2,
              fulfilledQuantity: 5,
              status: 'Error',
              createdAt: new Date().toISOString()
            }
          ]
        }
      ],
      fulfillmentRate: 0,
      shipments: []
    } satisfies SalesOrderDto;

    expect(computePendingShipAllocations(order)).toEqual([]);
  });
});
