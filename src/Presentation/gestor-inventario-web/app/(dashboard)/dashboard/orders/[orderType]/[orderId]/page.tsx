'use client';

import { useEffect, useMemo, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { ArrowLeftIcon, TruckIcon } from '@heroicons/react/24/outline';
import { apiClient } from 'src/lib/api-client';
import {
  purchaseStatusMap,
  resolvePurchaseStatusKey,
  resolveSalesStatusKey,
  salesStatusMap,
  type PurchaseStatusKey,
  type SalesStatusKey
} from 'src/lib/order-status';
import type { PurchaseOrderDto, SalesOrderDto } from 'src/types/api';
import { useConfigureApiClient } from 'src/hooks/use-configure-api-client';
import { Badge } from '../../../../../../components/ui/badge';
import { Button } from '../../../../../../components/ui/button';
import { Card } from '../../../../../../components/ui/card';

interface OrderDetailState {
  type: 'sales' | 'purchase';
  order: SalesOrderDto | PurchaseOrderDto;
}

function formatCurrency(value: number, currency: string) {
  try {
    return value.toLocaleString('es-ES', { style: 'currency', currency });
  } catch (error) {
    console.error(error);
    return `${value.toLocaleString('es-ES')} ${currency}`;
  }
}

function formatDate(value: string | undefined) {
  if (!value) {
    return '—';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '—';
  }

  return date.toLocaleDateString('es-ES', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  });
}

function buildSubtitle(state: OrderDetailState | null) {
  if (!state) {
    return 'Consulta el estado del pedido y sus movimientos.';
  }

  if (state.type === 'sales') {
    const order = state.order as SalesOrderDto;
    return `Pedido de venta para ${order.customerName}`;
  }

  const order = state.order as PurchaseOrderDto;
  return `Pedido de compra a ${order.supplierName}`;
}

function isSalesOrder(state: OrderDetailState | null): state is { type: 'sales'; order: SalesOrderDto } {
  return state?.type === 'sales';
}

export default function OrderDetailPage() {
  const params = useParams<{ orderType: string; orderId: string }>();
  const router = useRouter();
  const { isConfigured: isApiClientConfigured } = useConfigureApiClient();
  const [orderState, setOrderState] = useState<OrderDetailState | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const orderTypeParam = params?.orderType;
  const orderIdParam = params?.orderId;

  useEffect(() => {
    if (!isApiClientConfigured) {
      return;
    }

    if (!orderTypeParam || !orderIdParam) {
      setError('No se proporcionó información del pedido.');
      setIsLoading(false);
      return;
    }

    const orderId = Number(orderIdParam);
    if (Number.isNaN(orderId)) {
      setError('El identificador del pedido no es válido.');
      setIsLoading(false);
      return;
    }

    const normalizedType = orderTypeParam === 'sales' || orderTypeParam === 'purchase' ? orderTypeParam : null;

    if (!normalizedType) {
      setError('El tipo de pedido indicado no es válido.');
      setIsLoading(false);
      return;
    }

    let isActive = true;
    setIsLoading(true);
    setError(null);
    setOrderState(null);

    const fetchOrder = async () => {
      try {
        const endpoint = normalizedType === 'sales' ? `/salesorders/${orderId}` : `/purchaseorders/${orderId}`;
        const response = await apiClient.get<SalesOrderDto | PurchaseOrderDto>(endpoint);

        if (!isActive) {
          return;
        }

        setOrderState({ type: normalizedType, order: response.data });
        setIsLoading(false);
      } catch (requestError) {
        console.error(requestError);
        if (!isActive) {
          return;
        }

        setError('No se pudieron cargar los detalles del pedido.');
        setIsLoading(false);
      }
    };

    fetchOrder().catch((requestError) => console.error(requestError));

    return () => {
      isActive = false;
    };
  }, [isApiClientConfigured, orderIdParam, orderTypeParam]);

  const statusInfo = useMemo(() => {
    if (!orderState) {
      return null;
    }

    if (orderState.type === 'sales') {
      const key = resolveSalesStatusKey((orderState.order as SalesOrderDto).status);
      return salesStatusMap[key as SalesStatusKey];
    }

    const key = resolvePurchaseStatusKey((orderState.order as PurchaseOrderDto).status);
    return purchaseStatusMap[key as PurchaseStatusKey];
  }, [orderState]);

  const title = useMemo(() => {
    if (!orderState) {
      return 'Detalle de pedido';
    }

    const order = orderState.order;
    const code = `#${order.id.toString().padStart(5, '0')}`;
    return orderState.type === 'sales' ? `Pedido de venta ${code}` : `Pedido de compra ${code}`;
  }, [orderState]);

  const subtitle = useMemo(() => buildSubtitle(orderState), [orderState]);

  const lineItems = useMemo(() => {
    if (!orderState) {
      return [] as Array<{ id: number; sku: string; name: string; quantity: number; unitPrice: number; total: number }>;
    }

    if (orderState.type === 'sales') {
      const order = orderState.order as SalesOrderDto;
      return order.lines.map((line) => ({
        id: line.id,
        sku: line.variantSku,
        name: line.productName,
        quantity: line.quantity,
        unitPrice: line.unitPrice,
        total: line.totalLine
      }));
    }

    const order = orderState.order as PurchaseOrderDto;
    return order.lines.map((line) => ({
      id: line.id,
      sku: line.variantSku,
      name: line.productName,
      quantity: line.quantity,
      unitPrice: line.unitPrice,
      total: line.totalLine ?? line.unitPrice * line.quantity
    }));
  }, [orderState]);

  const totalUnits = useMemo(() => lineItems.reduce((sum, line) => sum + line.quantity, 0), [lineItems]);

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-4 rounded-3xl border border-slate-200 bg-white/80 p-6 shadow-sm backdrop-blur">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-4">
            <Button
              type="button"
              variant="ghost"
              className="h-10 w-10 rounded-full border border-slate-200 p-0 text-slate-600 hover:border-primary-200 hover:text-primary-600"
              onClick={() => router.push('/dashboard/orders')}
            >
              <ArrowLeftIcon aria-hidden className="h-5 w-5" />
              <span className="sr-only">Volver a pedidos</span>
            </Button>
            <div>
              <p className="text-xs uppercase tracking-wide text-slate-500">Resumen del pedido</p>
              <h1 className="text-2xl font-semibold text-slate-900">{title}</h1>
              <p className="text-sm text-slate-500">{subtitle}</p>
            </div>
          </div>
          {statusInfo && <Badge tone={statusInfo.tone}>{statusInfo.label}</Badge>}
        </div>
        {orderState && (
          <dl className="grid gap-6 sm:grid-cols-3">
            <div>
              <dt className="text-xs uppercase tracking-wide text-slate-500">Fecha</dt>
              <dd className="text-base font-medium text-slate-900">
                {formatDate(orderState.order.orderDate)}
              </dd>
            </div>
            <div>
              <dt className="text-xs uppercase tracking-wide text-slate-500">Importe total</dt>
              <dd className="text-base font-medium text-slate-900">
                {formatCurrency(orderState.order.totalAmount, orderState.order.currency)}
              </dd>
            </div>
            <div>
              <dt className="text-xs uppercase tracking-wide text-slate-500">Unidades totales</dt>
              <dd className="text-base font-medium text-slate-900">{totalUnits.toLocaleString('es-ES')} uds</dd>
            </div>
          </dl>
        )}
      </div>

      {isLoading ? (
        <div className="grid gap-6 lg:grid-cols-3">
          <div className="h-52 animate-pulse rounded-2xl border border-slate-200 bg-white/60" />
          <div className="h-52 animate-pulse rounded-2xl border border-slate-200 bg-white/60" />
          <div className="h-52 animate-pulse rounded-2xl border border-slate-200 bg-white/60" />
        </div>
      ) : error ? (
        <div className="rounded-2xl border border-red-200 bg-red-50 p-6 text-sm text-red-600">{error}</div>
      ) : (
        orderState && (
          <div className="flex flex-col gap-6">
            <section className="grid gap-6 lg:grid-cols-3">
              <Card
                title={orderState.type === 'sales' ? 'Cliente' : 'Proveedor'}
                subtitle={
                  orderState.type === 'sales'
                    ? 'Datos de contacto asociados al pedido de venta'
                    : 'Datos de contacto del proveedor'
                }
              >
                <dl className="grid gap-4 text-sm text-slate-600">
                  <div>
                    <dt className="text-xs uppercase tracking-wide text-slate-500">Nombre</dt>
                    <dd className="text-base font-medium text-slate-900">
                      {orderState.type === 'sales'
                        ? (orderState.order as SalesOrderDto).customerName
                        : (orderState.order as PurchaseOrderDto).supplierName}
                    </dd>
                  </div>
                  {orderState.type === 'sales' && (orderState.order as SalesOrderDto).shippingAddress && (
                    <div>
                      <dt className="text-xs uppercase tracking-wide text-slate-500">Dirección de envío</dt>
                      <dd>{(orderState.order as SalesOrderDto).shippingAddress}</dd>
                    </div>
                  )}
                  {orderState.order.notes && (
                    <div>
                      <dt className="text-xs uppercase tracking-wide text-slate-500">Notas</dt>
                      <dd>{orderState.order.notes}</dd>
                    </div>
                  )}
                </dl>
              </Card>

              <Card title="Detalles del pedido" subtitle="Información operativa relevante">
                <dl className="grid gap-4 text-sm text-slate-600">
                  <div>
                    <dt className="text-xs uppercase tracking-wide text-slate-500">Identificador</dt>
                    <dd className="text-base font-medium text-slate-900">
                      #{orderState.order.id.toString().padStart(5, '0')}
                    </dd>
                  </div>
                  {orderState.type === 'sales' ? (
                    <div>
                      <dt className="text-xs uppercase tracking-wide text-slate-500">Nivel de cumplimiento</dt>
                      <dd>
                        <div className="mt-1 flex items-center gap-3">
                          <div className="h-2 w-full rounded-full bg-slate-100">
                            <div
                              className="h-2 rounded-full bg-primary-500"
                              style={{ width: `${Math.min((orderState.order as SalesOrderDto).fulfillmentRate, 100)}%` }}
                            />
                          </div>
                          <span className="text-xs font-semibold text-slate-500">
                            {(orderState.order as SalesOrderDto).fulfillmentRate.toFixed(0)}%
                          </span>
                        </div>
                      </dd>
                    </div>
                  ) : (
                    <div>
                      <dt className="text-xs uppercase tracking-wide text-slate-500">Estado</dt>
                      <dd>{purchaseStatusMap[resolvePurchaseStatusKey(orderState.order.status)].label}</dd>
                    </div>
                  )}
                  <div>
                    <dt className="text-xs uppercase tracking-wide text-slate-500">Moneda</dt>
                    <dd>{orderState.order.currency}</dd>
                  </div>
                  {isSalesOrder(orderState) && (orderState.order as SalesOrderDto).carrierName && (
                    <div>
                      <dt className="text-xs uppercase tracking-wide text-slate-500">Transportista asignado</dt>
                      <dd>{(orderState.order as SalesOrderDto).carrierName}</dd>
                    </div>
                  )}
                  {isSalesOrder(orderState) && (orderState.order as SalesOrderDto).estimatedDeliveryDate && (
                    <div>
                      <dt className="text-xs uppercase tracking-wide text-slate-500">Entrega estimada</dt>
                      <dd>{formatDate((orderState.order as SalesOrderDto).estimatedDeliveryDate)}</dd>
                    </div>
                  )}
                </dl>
              </Card>

              <Card title="Resumen económico" subtitle="Valores calculados del pedido">
                <dl className="grid gap-4 text-sm text-slate-600">
                  <div>
                    <dt className="text-xs uppercase tracking-wide text-slate-500">Líneas</dt>
                    <dd>{lineItems.length}</dd>
                  </div>
                  <div>
                    <dt className="text-xs uppercase tracking-wide text-slate-500">Unidades</dt>
                    <dd>{totalUnits.toLocaleString('es-ES')}</dd>
                  </div>
                  <div>
                    <dt className="text-xs uppercase tracking-wide text-slate-500">Importe total</dt>
                    <dd className="text-base font-semibold text-slate-900">
                      {formatCurrency(orderState.order.totalAmount, orderState.order.currency)}
                    </dd>
                  </div>
                </dl>
              </Card>
            </section>

            <Card title="Artículos del pedido" subtitle="Detalle de productos y cantidades">
              {lineItems.length === 0 ? (
                <p className="text-sm text-slate-500">No hay líneas registradas en este pedido.</p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-slate-200 text-sm">
                    <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                      <tr>
                        <th className="px-4 py-3 text-left">Producto</th>
                        <th className="px-4 py-3 text-left">SKU</th>
                        <th className="px-4 py-3 text-right">Cantidad</th>
                        <th className="px-4 py-3 text-right">Precio unitario</th>
                        <th className="px-4 py-3 text-right">Importe</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {lineItems.map((line) => (
                        <tr key={line.id} className="text-slate-700">
                          <td className="px-4 py-3 font-medium text-slate-900">{line.name}</td>
                          <td className="px-4 py-3 text-xs text-slate-500">{line.sku}</td>
                          <td className="px-4 py-3 text-right font-medium text-slate-900">
                            {line.quantity.toLocaleString('es-ES')} uds
                          </td>
                          <td className="px-4 py-3 text-right">
                            {formatCurrency(line.unitPrice, orderState.order.currency)}
                          </td>
                          <td className="px-4 py-3 text-right font-semibold text-slate-900">
                            {formatCurrency(line.total, orderState.order.currency)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </Card>

            {isSalesOrder(orderState) && orderState.order.shipments.length > 0 && (
              <Card title="Envíos asociados" subtitle="Seguimiento de entregas por almacén">
                <ul className="grid gap-4 md:grid-cols-2">
                  {orderState.order.shipments.map((shipment) => (
                    <li
                      key={shipment.id}
                      className="flex flex-col gap-3 rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-sm"
                    >
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-3">
                          <span className="flex h-9 w-9 items-center justify-center rounded-full bg-primary-50 text-primary-600">
                            <TruckIcon aria-hidden className="h-4 w-4" />
                          </span>
                          <div>
                            <p className="text-sm font-semibold text-slate-900">{shipment.warehouseName}</p>
                            <p className="text-xs text-slate-500">#{shipment.id.toString().padStart(4, '0')}</p>
                          </div>
                        </div>
                        <Badge tone="neutral">{shipment.status}</Badge>
                      </div>
                      <dl className="grid gap-2 text-xs text-slate-500">
                        <div className="flex items-center justify-between">
                          <dt>Creado</dt>
                          <dd className="font-medium text-slate-700">{formatDate(shipment.createdAt)}</dd>
                        </div>
                        <div className="flex items-center justify-between">
                          <dt>Envío</dt>
                          <dd className="font-medium text-slate-700">{formatDate(shipment.shippedAt)}</dd>
                        </div>
                        <div className="flex items-center justify-between">
                          <dt>Entrega</dt>
                          <dd className="font-medium text-slate-700">{formatDate(shipment.deliveredAt)}</dd>
                        </div>
                        {shipment.carrierName && (
                          <div className="flex items-center justify-between">
                            <dt>Transportista</dt>
                            <dd className="font-medium text-slate-700">{shipment.carrierName}</dd>
                          </div>
                        )}
                        {shipment.estimatedDeliveryDate && (
                          <div className="flex items-center justify-between">
                            <dt>Entrega estimada</dt>
                            <dd className="font-medium text-slate-700">{formatDate(shipment.estimatedDeliveryDate)}</dd>
                          </div>
                        )}
                        {shipment.trackingNumber && (
                          <div className="flex items-center justify-between">
                            <dt>Tracking</dt>
                            <dd className="font-medium text-slate-700">{shipment.trackingNumber}</dd>
                          </div>
                        )}
                      </dl>
                    </li>
                  ))}
                </ul>
              </Card>
            )}
          </div>
        )
      )}
    </div>
  );
}
