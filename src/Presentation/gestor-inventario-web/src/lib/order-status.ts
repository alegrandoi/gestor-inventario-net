export const salesStatusValue = {
  Pending: 1,
  Confirmed: 2,
  Shipped: 3,
  Delivered: 4,
  Cancelled: 5
} as const;

export type SalesStatusKey = keyof typeof salesStatusValue;

export const salesStatusMap: Record<SalesStatusKey, { label: string; tone: 'success' | 'warning' | 'neutral' }> = {
  Pending: { label: 'Pendiente', tone: 'warning' },
  Confirmed: { label: 'Confirmado', tone: 'neutral' },
  Shipped: { label: 'Enviado', tone: 'neutral' },
  Delivered: { label: 'Entregado', tone: 'success' },
  Cancelled: { label: 'Cancelado', tone: 'warning' }
};

export const purchaseStatusValue = {
  Pending: 1,
  Ordered: 2,
  Received: 3,
  Cancelled: 4
} as const;

export type PurchaseStatusKey = keyof typeof purchaseStatusValue;

export const purchaseStatusMap: Record<PurchaseStatusKey, { label: string; tone: 'success' | 'warning' | 'neutral' }> = {
  Pending: { label: 'Pendiente', tone: 'warning' },
  Ordered: { label: 'Solicitado', tone: 'neutral' },
  Received: { label: 'Recibido', tone: 'success' },
  Cancelled: { label: 'Cancelado', tone: 'warning' }
};

export function resolveSalesStatusKey(status: string | number): SalesStatusKey {
  if (typeof status === 'number') {
    const entry = Object.entries(salesStatusValue).find(([, value]) => value === status);
    if (entry) {
      return entry[0] as SalesStatusKey;
    }
  } else if (status in salesStatusValue) {
    return status as SalesStatusKey;
  }

  return 'Pending';
}

export function resolvePurchaseStatusKey(status: string | number): PurchaseStatusKey {
  if (typeof status === 'number') {
    const entry = Object.entries(purchaseStatusValue).find(([, value]) => value === status);
    if (entry) {
      return entry[0] as PurchaseStatusKey;
    }
  } else if (status in purchaseStatusValue) {
    return status as PurchaseStatusKey;
  }

  return 'Pending';
}
