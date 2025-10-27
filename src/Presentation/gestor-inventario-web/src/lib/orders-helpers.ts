import type { SalesOrderDto } from '../types/api';

export type PendingShipAllocation = {
  variantId: number;
  variantSku: string;
  productName: string;
  warehouseId: number;
  warehouseName: string;
  pending: number;
};

export function computePendingShipAllocations(order: SalesOrderDto | null): PendingShipAllocation[] {
  if (!order) {
    return [];
  }

  return order.lines.flatMap((line) =>
    line.allocations
      .map((allocation) => {
        const pending = Number(allocation.quantity) - Number(allocation.fulfilledQuantity);

        if (pending <= 0) {
          return null;
        }

        return {
          variantId: line.variantId,
          variantSku: line.variantSku,
          productName: line.productName,
          warehouseId: allocation.warehouseId,
          warehouseName: allocation.warehouseName,
          pending
        } satisfies PendingShipAllocation;
      })
      .filter((allocation): allocation is PendingShipAllocation => allocation !== null)
  );
}
