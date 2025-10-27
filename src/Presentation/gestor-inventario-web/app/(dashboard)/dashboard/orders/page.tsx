'use client';

import {
  ChangeEvent,
  Fragment,
  ReactNode,
  useCallback,
  useEffect,
  useId,
  useMemo,
  useRef,
  useState
} from 'react';
import { ChevronDownIcon, FunnelIcon } from '@heroicons/react/24/outline';
import { AxiosError } from 'axios';
import { apiClient } from 'src/lib/api-client';
import { computePendingShipAllocations } from 'src/lib/orders-helpers';
import {
  purchaseStatusMap,
  purchaseStatusValue,
  resolvePurchaseStatusKey,
  resolveSalesStatusKey,
  salesStatusMap,
  salesStatusValue,
  type PurchaseStatusKey,
  type SalesStatusKey
} from 'src/lib/order-status';
import type { PurchaseOrderDto, SalesOrderDto, WarehouseDto } from 'src/types/api';
import { Card } from '../../../../components/ui/card';
import { Badge } from '../../../../components/ui/badge';
import { Button } from '../../../../components/ui/button';
import { Select } from '../../../../components/ui/select';
import { useConfigureApiClient } from 'src/hooks/use-configure-api-client';
import { useRouter } from 'next/navigation';

function extractErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof AxiosError) {
    const data = error.response?.data as { detail?: string; title?: string; errors?: Record<string, string[]> } | undefined;
    if (data?.detail) {
      return data.detail;
    }

    if (data?.title) {
      return data.title;
    }

    if (data?.errors) {
      const [firstError] = Object.values(data.errors);
      if (firstError?.length) {
        return firstError[0];
      }
    }

    if (error.message) {
      return error.message;
    }
  }

  return fallback;
}

type OrdersRowData<T> = {
  id: number;
  primary: string;
  secondary: string;
  date: string;
  amount: string;
  statusKey: string;
  data: T;
};

export default function OrdersPage() {
  const [salesOrders, setSalesOrders] = useState<SalesOrderDto[]>([]);
  const [purchaseOrders, setPurchaseOrders] = useState<PurchaseOrderDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([]);
  const [warehousesError, setWarehousesError] = useState<string | null>(null);
  const [processingSalesOrderId, setProcessingSalesOrderId] = useState<number | null>(null);
  const [processingPurchaseOrderId, setProcessingPurchaseOrderId] = useState<number | null>(null);
  const [shipOrderDetail, setShipOrderDetail] = useState<SalesOrderDto | null>(null);
  const [receiveOrderDetail, setReceiveOrderDetail] = useState<PurchaseOrderDto | null>(null);
  const [isShipModalOpen, setIsShipModalOpen] = useState(false);
  const [isReceiveModalOpen, setIsReceiveModalOpen] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionMessage, setActionMessage] = useState<string | null>(null);
  const [isActionSubmitting, setIsActionSubmitting] = useState(false);
  const [selectedWarehouseId, setSelectedWarehouseId] = useState<number | ''>('');
  const [salesStatusFilter, setSalesStatusFilter] = useState<SalesStatusKey | 'ALL'>('ALL');
  const [purchaseStatusFilter, setPurchaseStatusFilter] = useState<PurchaseStatusKey | 'ALL'>('ALL');
  const { isConfigured: isApiClientConfigured, tenantId: activeTenantId } = useConfigureApiClient();
  const router = useRouter();
  const shipModalTitleId = useId();
  const shipModalDescriptionId = useId();
  const receiveModalTitleId = useId();
  const receiveModalDescriptionId = useId();
  const isMountedRef = useRef(true);

  useEffect(() => {
    isMountedRef.current = true;

    return () => {
      isMountedRef.current = false;
    };
  }, []);

  const closeShipModal = useCallback(() => {
    setIsShipModalOpen(false);
    setShipOrderDetail(null);
  }, []);

  const closeReceiveModal = useCallback(() => {
    setIsReceiveModalOpen(false);
    setReceiveOrderDetail(null);
    setSelectedWarehouseId('');
  }, []);

  const pendingShipAllocations = useMemo(() => computePendingShipAllocations(shipOrderDetail), [shipOrderDetail]);

  useEffect(() => {
    if (typeof window === 'undefined' || (!isShipModalOpen && !isReceiveModalOpen)) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        if (isShipModalOpen && !isActionSubmitting) {
          closeShipModal();
        }

        if (isReceiveModalOpen && !isActionSubmitting) {
          closeReceiveModal();
        }
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => {
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [closeReceiveModal, closeShipModal, isActionSubmitting, isReceiveModalOpen, isShipModalOpen]);

  const salesStatusOptions = useMemo(
    () => (Object.keys(salesStatusMap) as SalesStatusKey[]).map((status) => ({
      value: status,
      label: salesStatusMap[status].label
    })),
    []
  );

  const purchaseStatusOptions = useMemo(
    () => (Object.keys(purchaseStatusMap) as PurchaseStatusKey[]).map((status) => ({
      value: status,
      label: purchaseStatusMap[status].label
    })),
    []
  );

  const filteredSalesOrders = useMemo(() => {
    if (salesStatusFilter === 'ALL') {
      return salesOrders;
    }

    return salesOrders.filter((order) => resolveSalesStatusKey(order.status) === salesStatusFilter);
  }, [salesOrders, salesStatusFilter]);

  const filteredPurchaseOrders = useMemo(() => {
    if (purchaseStatusFilter === 'ALL') {
      return purchaseOrders;
    }

    return purchaseOrders.filter((order) => resolvePurchaseStatusKey(order.status) === purchaseStatusFilter);
  }, [purchaseOrders, purchaseStatusFilter]);

  const salesSummary = useMemo(() => {
    const totalOrders = salesOrders.length;
    const pendingOrders = salesOrders.filter((order) => resolveSalesStatusKey(order.status) === 'Pending').length;
    const fulfilledOrders = salesOrders.filter((order) => resolveSalesStatusKey(order.status) === 'Delivered').length;
    const currency = salesOrders[0]?.currency ?? 'EUR';
    const totalValue = salesOrders.reduce((accumulator, order) => accumulator + order.totalAmount, 0);

    return {
      totalOrders,
      pendingOrders,
      fulfilledOrders,
      totalValue,
      currency
    };
  }, [salesOrders]);

  const purchaseSummary = useMemo(() => {
    const totalOrders = purchaseOrders.length;
    const pendingOrders = purchaseOrders.filter((order) => resolvePurchaseStatusKey(order.status) === 'Pending').length;
    const receivedOrders = purchaseOrders.filter((order) => resolvePurchaseStatusKey(order.status) === 'Received').length;
    const currency = purchaseOrders[0]?.currency ?? 'EUR';
    const totalValue = purchaseOrders.reduce((accumulator, order) => accumulator + order.totalAmount, 0);

    return {
      totalOrders,
      pendingOrders,
      receivedOrders,
      totalValue,
      currency
    };
  }, [purchaseOrders]);

  async function openShipModal(orderId: number) {
    setActionError(null);
    setActionMessage(null);
    setProcessingSalesOrderId(orderId);

    try {
      const response = await apiClient.get<SalesOrderDto>(`/salesorders/${orderId}`);
      if (!isMountedRef.current) {
        return;
      }

      setShipOrderDetail(response.data);
      setIsShipModalOpen(true);
    } catch (error) {
      console.error(error);
      if (!isMountedRef.current) {
        return;
      }

      setActionError(extractErrorMessage(error, 'No se pudo cargar el pedido de venta.'));
    } finally {
      if (isMountedRef.current) {
        setProcessingSalesOrderId(null);
      }
    }
  }

  async function updateSalesStatus(
    orderId: number,
    status: number,
    allocations: Array<{ variantId: number; warehouseId: number; quantity: number }>
  ) {
    setActionError(null);
    setActionMessage(null);
    setProcessingSalesOrderId(orderId);

    try {
      await apiClient.put<SalesOrderDto>(`/salesorders/${orderId}/status`, {
        orderId,
        status,
        allocations
      });

      if (!isMountedRef.current) {
        return;
      }

      const statusKey = resolveSalesStatusKey(status);
      const statusLabel = salesStatusMap[statusKey]?.label ?? 'actualizado';
      setActionMessage(`Pedido #${orderId.toString().padStart(5, '0')} actualizado a ${statusLabel.toLowerCase()}.`);
      await loadOrders();
    } catch (error) {
      console.error(error);
      if (!isMountedRef.current) {
        return;
      }

      setActionError(extractErrorMessage(error, 'No se pudo actualizar el pedido de venta.'));
    } finally {
      if (isMountedRef.current) {
        setProcessingSalesOrderId(null);
      }
    }
  }

  async function submitShipOrder() {
    if (!shipOrderDetail) {
      return;
    }

    if (pendingShipAllocations.length === 0) {
      setActionError('No hay unidades pendientes por enviar.');
      return;
    }

    setProcessingSalesOrderId(shipOrderDetail.id);
    setIsActionSubmitting(true);

    try {
      await apiClient.put<SalesOrderDto>(`/salesorders/${shipOrderDetail.id}/status`, {
        orderId: shipOrderDetail.id,
        status: salesStatusValue.Shipped,
        allocations: pendingShipAllocations.map((allocation) => ({
          variantId: allocation.variantId,
          warehouseId: allocation.warehouseId,
          quantity: allocation.pending
        }))
      });

      if (!isMountedRef.current) {
        return;
      }

      setActionMessage(`Pedido #${shipOrderDetail.id.toString().padStart(5, '0')} marcado como enviado.`);
      closeShipModal();
      await loadOrders();
    } catch (error) {
      console.error(error);
      if (!isMountedRef.current) {
        return;
      }

      setActionError(extractErrorMessage(error, 'No se pudo registrar el envío del pedido.'));
    } finally {
      if (isMountedRef.current) {
        setIsActionSubmitting(false);
        setProcessingSalesOrderId(null);
      }
    }
  }

  async function openReceiveModal(orderId: number) {
    setActionError(null);
    setActionMessage(null);
    setProcessingPurchaseOrderId(orderId);

    try {
      const response = await apiClient.get<PurchaseOrderDto>(`/purchaseorders/${orderId}`);
      if (!isMountedRef.current) {
        return;
      }

      setReceiveOrderDetail(response.data);
      setSelectedWarehouseId((previous) => {
        if (typeof previous === 'number') {
          return previous;
        }

        return warehouses[0]?.id ?? '';
      });
      setIsReceiveModalOpen(true);
    } catch (error) {
      console.error(error);
      if (!isMountedRef.current) {
        return;
      }

      setActionError(extractErrorMessage(error, 'No se pudo cargar el pedido de compra.'));
    } finally {
      if (isMountedRef.current) {
        setProcessingPurchaseOrderId(null);
      }
    }
  }

  async function submitReceiveOrder() {
    if (!receiveOrderDetail) {
      return;
    }

    if (typeof selectedWarehouseId !== 'number') {
      setActionError('Selecciona un almacén de recepción.');
      return;
    }

    setProcessingPurchaseOrderId(receiveOrderDetail.id);
    setIsActionSubmitting(true);

    try {
      await apiClient.put<PurchaseOrderDto>(`/purchaseorders/${receiveOrderDetail.id}/status`, {
        orderId: receiveOrderDetail.id,
        status: purchaseStatusValue.Received,
        warehouseId: selectedWarehouseId
      });

      if (!isMountedRef.current) {
        return;
      }

      setActionMessage(`Pedido de compra #${receiveOrderDetail.id.toString().padStart(5, '0')} recibido correctamente.`);
      closeReceiveModal();
      await loadOrders();
    } catch (error) {
      console.error(error);
      if (!isMountedRef.current) {
        return;
      }

      setActionError(extractErrorMessage(error, 'No se pudo registrar la recepción.'));
    } finally {
      if (isMountedRef.current) {
        setIsActionSubmitting(false);
        setProcessingPurchaseOrderId(null);
      }
    }
  }

  const renderSalesActions = (row: OrdersRowData<SalesOrderDto>) => {
    const statusKey = resolveSalesStatusKey(row.data.status);
    const isProcessing = processingSalesOrderId === row.id || isActionSubmitting;
    const viewButton = (
      <Button
        size="sm"
        variant="outline"
        type="button"
        onClick={() => router.push(`/dashboard/orders/sales/${row.id}`)}
      >
        Ver pedido
      </Button>
    );

    if (statusKey === 'Pending') {
      return (
        <div className="flex justify-end gap-2">
          {viewButton}
          <Button
            size="sm"
            variant="secondary"
            disabled={isProcessing}
            onClick={() => updateSalesStatus(row.data.id, salesStatusValue.Confirmed, [])}
          >
            {isProcessing ? 'Procesando…' : 'Confirmar'}
          </Button>
          <Button size="sm" disabled={isProcessing} onClick={() => openShipModal(row.data.id)}>
            {isProcessing ? 'Procesando…' : 'Enviar'}
          </Button>
        </div>
      );
    }

    if (statusKey === 'Confirmed') {
      return (
        <div className="flex justify-end gap-2">
          {viewButton}
          <Button size="sm" disabled={isProcessing} onClick={() => openShipModal(row.data.id)}>
            {isProcessing ? 'Procesando…' : 'Enviar'}
          </Button>
        </div>
      );
    }

    if (statusKey === 'Shipped') {
      return (
        <div className="flex justify-end gap-2">
          {viewButton}
          <Button
            size="sm"
            disabled={isProcessing}
            onClick={() => updateSalesStatus(row.data.id, salesStatusValue.Delivered, [])}
          >
            {isProcessing ? 'Procesando…' : 'Marcar como entregado'}
          </Button>
        </div>
      );
    }

    return <div className="flex justify-end">{viewButton}</div>;
  };

  const renderPurchaseActions = (row: OrdersRowData<PurchaseOrderDto>) => {
    const statusKey = resolvePurchaseStatusKey(row.data.status);
    const isProcessing = processingPurchaseOrderId === row.id || isActionSubmitting;
    const viewButton = (
      <Button
        size="sm"
        variant="outline"
        type="button"
        onClick={() => router.push(`/dashboard/orders/purchase/${row.id}`)}
      >
        Ver pedido
      </Button>
    );

    if (statusKey === 'Pending' || statusKey === 'Ordered') {
      return (
        <div className="flex justify-end gap-2">
          {viewButton}
          <Button size="sm" disabled={isProcessing} onClick={() => openReceiveModal(row.data.id)}>
            {isProcessing ? 'Procesando…' : 'Registrar recepción'}
          </Button>
        </div>
      );
    }

    return <div className="flex justify-end">{viewButton}</div>;
  };
  const loadOrders = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const [salesResponse, purchaseResponse] = await Promise.all([
        apiClient.get<SalesOrderDto[]>('/salesorders'),
        apiClient.get<PurchaseOrderDto[]>('/purchaseorders')
      ]);

      if (!isMountedRef.current) {
        return;
      }

      setSalesOrders(salesResponse.data);
      setPurchaseOrders(purchaseResponse.data);
    } catch (error) {
      console.error(error);

      if (!isMountedRef.current) {
        return;
      }

      setError(extractErrorMessage(error, 'No se pudieron cargar los pedidos.'));
    } finally {
      if (isMountedRef.current) {
        setIsLoading(false);
      }
    }
  }, []);

  useEffect(() => {
    if (!isApiClientConfigured) {
      return;
    }

    loadOrders().catch((error) => console.error(error));
  }, [activeTenantId, isApiClientConfigured, loadOrders]);

  useEffect(() => {
    if (!isApiClientConfigured) {
      return;
    }

    let isActive = true;

    const fetchWarehouses = async () => {
      if (isMountedRef.current && isActive) {
        setWarehousesError(null);
      }

      try {
        const response = await apiClient.get<WarehouseDto[]>('/warehouses');

        if (!isMountedRef.current || !isActive) {
          return;
        }

        setWarehouses(response.data);
        setWarehousesError(null);
      } catch (error) {
        console.error(error);

        if (!isMountedRef.current || !isActive) {
          return;
        }

        setWarehouses([]);
        setWarehousesError(extractErrorMessage(error, 'No se pudieron cargar los almacenes.'));
      }
    };

    fetchWarehouses().catch((error) => console.error(error));

    return () => {
      isActive = false;
    };
  }, [isApiClientConfigured]);

  useEffect(() => {
    if (isReceiveModalOpen && warehouses.length > 0 && typeof selectedWarehouseId !== 'number') {
      setSelectedWarehouseId(warehouses[0].id);
    }
  }, [isReceiveModalOpen, selectedWarehouseId, warehouses]);

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">Pedidos</h1>
          <p className="text-sm text-slate-500">
            Supervisa el estado de ventas y compras y crea nuevos pedidos cuando los necesites.
          </p>
        </div>
        <Button type="button" onClick={() => router.push('/dashboard/orders/new')}>
          Nuevo pedido
        </Button>
      </div>

      {actionMessage && (
        <div
          className="rounded-xl border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700"
          role="status"
          aria-live="polite"
        >
          {actionMessage}
        </div>
      )}

      {actionError && (
        <div
          className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600"
          role="alert"
          aria-live="assertive"
        >
          {actionError}
        </div>
      )}

      <OrdersSnapshot salesSummary={salesSummary} purchaseSummary={purchaseSummary} />

      <div className="grid grid-cols-1 gap-6">
        <Card
          title="Pedidos de venta"
          subtitle="Seguimiento del pipeline comercial"
          action={
            <OrdersStatusFilter
              label="Filtrar ventas"
              value={salesStatusFilter}
              options={salesStatusOptions}
              onChange={(value) => setSalesStatusFilter(value)}
            />
          }
        >
          <OrdersTable
            isLoading={isLoading}
            error={error}
            headers={['Pedido', 'Cliente', 'Fecha', 'Importe', 'Estado']}
            rows={filteredSalesOrders.map((order) => {
              const statusKey = resolveSalesStatusKey(order.status);
              return {
                id: order.id,
                primary: `#${order.id.toString().padStart(5, '0')}`,
                secondary: order.customerName,
                date: new Date(order.orderDate).toLocaleDateString('es-ES'),
                amount: order.totalAmount.toLocaleString('es-ES', { style: 'currency', currency: order.currency }),
                statusKey,
                data: order
              } satisfies OrdersRowData<SalesOrderDto>;
            })}
            statusMap={salesStatusMap}
            emptyMessage="No hay pedidos de venta registrados."
            renderActions={renderSalesActions}
            renderDetails={(row) => {
              const order = row.data;
              if (order.lines.length === 0) {
                return <p className="text-xs text-slate-500">Este pedido no tiene productos registrados.</p>;
              }

              return (
                <div className="space-y-3">
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Productos incluidos</p>
                  <ul className="grid gap-2 sm:grid-cols-2">
                    {order.lines.map((line) => (
                      <li
                        key={line.id}
                        className="flex items-start justify-between rounded-xl border border-slate-200 bg-white px-4 py-3"
                      >
                        <div className="max-w-[65%]">
                          <p className="text-sm font-medium text-slate-900">{line.productName}</p>
                          <p className="text-xs text-slate-500">SKU {line.variantSku}</p>
                        </div>
                        <div className="text-right">
                          <p className="text-sm font-semibold text-slate-900">
                            {line.quantity.toLocaleString('es-ES')} uds
                          </p>
                          <p className="text-xs text-slate-500">
                            {line.totalLine.toLocaleString('es-ES', { style: 'currency', currency: order.currency })}
                          </p>
                        </div>
                      </li>
                    ))}
                  </ul>
                </div>
              );
            }}
          />
        </Card>

        <Card
          title="Pedidos de compra"
          subtitle="Reposición y abastecimiento"
          action={
            <OrdersStatusFilter
              label="Filtrar compras"
              value={purchaseStatusFilter}
              options={purchaseStatusOptions}
              onChange={(value) => setPurchaseStatusFilter(value)}
            />
          }
        >
          <OrdersTable
            isLoading={isLoading}
            error={error}
            headers={['Pedido', 'Proveedor', 'Fecha', 'Importe', 'Estado']}
            rows={filteredPurchaseOrders.map((order) => {
              const statusKey = resolvePurchaseStatusKey(order.status);
              return {
                id: order.id,
                primary: `#${order.id.toString().padStart(5, '0')}`,
                secondary: order.supplierName,
                date: new Date(order.orderDate).toLocaleDateString('es-ES'),
                amount: order.totalAmount.toLocaleString('es-ES', { style: 'currency', currency: order.currency }),
                statusKey,
                data: order
              } satisfies OrdersRowData<PurchaseOrderDto>;
            })}
            statusMap={purchaseStatusMap}
            emptyMessage="No hay pedidos de compra registrados."
            renderActions={renderPurchaseActions}
            renderDetails={(row) => {
              const order = row.data;
              if (order.lines.length === 0) {
                return <p className="text-xs text-slate-500">Este pedido no tiene artículos asociados.</p>;
              }

              return (
                <div className="space-y-3">
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Productos solicitados</p>
                  <ul className="grid gap-2 sm:grid-cols-2">
                    {order.lines.map((line) => (
                      <li
                        key={line.id}
                        className="flex items-start justify-between rounded-xl border border-slate-200 bg-white px-4 py-3"
                      >
                        <div className="max-w-[65%]">
                          <p className="text-sm font-medium text-slate-900">{line.productName}</p>
                          <p className="text-xs text-slate-500">SKU {line.variantSku}</p>
                        </div>
                        <div className="text-right">
                          <p className="text-sm font-semibold text-slate-900">
                            {line.quantity.toLocaleString('es-ES')} uds
                          </p>
                          <p className="text-xs text-slate-500">
                            {line.totalLine.toLocaleString('es-ES', { style: 'currency', currency: order.currency })}
                          </p>
                        </div>
                      </li>
                    ))}
                  </ul>
                </div>
              );
            }}
          />
        </Card>
      </div>

      {isShipModalOpen && shipOrderDetail && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 px-4 py-6 backdrop-blur-sm"
          role="dialog"
          aria-modal="true"
          aria-labelledby={shipModalTitleId}
          aria-describedby={shipModalDescriptionId}
          onClick={() => {
            if (!isActionSubmitting) {
              closeShipModal();
            }
          }}
        >
          <div className="w-full max-w-2xl rounded-2xl bg-white p-6 shadow-xl" onClick={(event) => event.stopPropagation()}>
            <div className="flex items-start justify-between gap-4">
              <div>
                <h2 id={shipModalTitleId} className="text-lg font-semibold text-slate-900">
                  Confirmar envío
                </h2>
                <p id={shipModalDescriptionId} className="mt-1 text-sm text-slate-500">
                  Pedido #{shipOrderDetail.id.toString().padStart(5, '0')} · {shipOrderDetail.customerName}
                </p>
              </div>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => {
                  if (!isActionSubmitting) {
                    closeShipModal();
                  }
                }}
                aria-label="Cerrar modal de envío"
                disabled={isActionSubmitting}
              >
                Cerrar
              </Button>
            </div>

            <div className="mt-6 space-y-4">
              {pendingShipAllocations.length === 0 ? (
                <p className="text-sm text-slate-500">No hay unidades pendientes por despachar.</p>
              ) : (
                <div className="overflow-hidden rounded-xl border border-slate-200">
                  <table className="min-w-full divide-y divide-slate-200 text-sm">
                    <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                      <tr>
                        <th className="px-4 py-3 text-left">Producto</th>
                        <th className="px-4 py-3 text-left">Almacén</th>
                        <th className="px-4 py-3 text-right">Pendiente</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {pendingShipAllocations.map((allocation) => (
                        <tr key={`${allocation.variantId}-${allocation.warehouseId}`} className="text-slate-700">
                          <td className="px-4 py-3">
                            <p className="font-medium text-slate-900">{allocation.productName}</p>
                            <p className="text-xs text-slate-500">SKU {allocation.variantSku}</p>
                          </td>
                          <td className="px-4 py-3 text-xs text-slate-500">{allocation.warehouseName}</td>
                          <td className="px-4 py-3 text-right text-sm font-semibold text-slate-900">
                            {allocation.pending.toLocaleString('es-ES')} uds
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>

            <div className="mt-6 flex justify-end gap-2">
              <Button
                type="button"
                variant="ghost"
                onClick={() => {
                  if (!isActionSubmitting) {
                    closeShipModal();
                  }
                }}
                disabled={isActionSubmitting}
              >
                Cancelar
              </Button>
              <Button
                type="button"
                onClick={submitShipOrder}
                disabled={isActionSubmitting || pendingShipAllocations.length === 0}
              >
                {isActionSubmitting ? 'Registrando envío…' : 'Confirmar envío'}
              </Button>
            </div>
          </div>
        </div>
      )}

      {isReceiveModalOpen && receiveOrderDetail && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 px-4 py-6 backdrop-blur-sm"
          role="dialog"
          aria-modal="true"
          aria-labelledby={receiveModalTitleId}
          aria-describedby={receiveModalDescriptionId}
          onClick={() => {
            if (!isActionSubmitting) {
              closeReceiveModal();
            }
          }}
        >
          <div className="w-full max-w-xl rounded-2xl bg-white p-6 shadow-xl" onClick={(event) => event.stopPropagation()}>
            <div className="flex items-start justify-between gap-4">
              <div>
                <h2 id={receiveModalTitleId} className="text-lg font-semibold text-slate-900">
                  Registrar recepción
                </h2>
                <p id={receiveModalDescriptionId} className="mt-1 text-sm text-slate-500">
                  Pedido #{receiveOrderDetail.id.toString().padStart(5, '0')} · {receiveOrderDetail.supplierName}
                </p>
              </div>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => {
                  if (!isActionSubmitting) {
                    closeReceiveModal();
                  }
                }}
                aria-label="Cerrar modal de recepción"
                disabled={isActionSubmitting}
              >
                Cerrar
              </Button>
            </div>

            <div className="mt-6 space-y-4">
              <div className="overflow-hidden rounded-xl border border-slate-200">
                <table className="min-w-full divide-y divide-slate-200 text-sm">
                  <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                    <tr>
                      <th className="px-4 py-3 text-left">Producto</th>
                      <th className="px-4 py-3 text-right">Cantidad</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {receiveOrderDetail.lines.map((line) => (
                      <tr key={line.id} className="text-slate-700">
                        <td className="px-4 py-3">
                          <p className="font-medium text-slate-900">{line.productName}</p>
                          <p className="text-xs text-slate-500">SKU {line.variantSku}</p>
                        </td>
                        <td className="px-4 py-3 text-right text-sm font-semibold text-slate-900">
                          {line.quantity.toLocaleString('es-ES')} uds
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <Select
                label="Almacén de recepción"
                value={selectedWarehouseId === '' ? '' : selectedWarehouseId.toString()}
                onChange={(event: ChangeEvent<HTMLSelectElement>) =>
                  setSelectedWarehouseId(event.target.value ? Number.parseInt(event.target.value, 10) : '')
                }
                disabled={warehouses.length === 0}
                error={warehousesError ?? undefined}
              >
                <option value="">Selecciona un almacén</option>
                {warehouses.map((warehouse) => (
                  <option key={warehouse.id} value={warehouse.id}>
                    {warehouse.name}
                  </option>
                ))}
              </Select>
            </div>

            <div className="mt-6 flex justify-end gap-2">
              <Button
                type="button"
                variant="ghost"
                onClick={() => {
                  if (!isActionSubmitting) {
                    closeReceiveModal();
                  }
                }}
                disabled={isActionSubmitting}
              >
                Cancelar
              </Button>
              <Button
                type="button"
                onClick={submitReceiveOrder}
                disabled={isActionSubmitting || typeof selectedWarehouseId !== 'number'}
              >
                {isActionSubmitting ? 'Registrando recepción…' : 'Registrar recepción'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

interface OrdersSnapshotProps {
  salesSummary: {
    totalOrders: number;
    pendingOrders: number;
    fulfilledOrders: number;
    totalValue: number;
    currency: string;
  };
  purchaseSummary: {
    totalOrders: number;
    pendingOrders: number;
    receivedOrders: number;
    totalValue: number;
    currency: string;
  };
}

function OrdersSnapshot({ salesSummary, purchaseSummary }: OrdersSnapshotProps) {
  const salesTotalValue = salesSummary.totalValue.toLocaleString('es-ES', {
    style: 'currency',
    currency: salesSummary.currency
  });

  const purchaseTotalValue = purchaseSummary.totalValue.toLocaleString('es-ES', {
    style: 'currency',
    currency: purchaseSummary.currency
  });

  return (
    <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
      <SummaryTile
        title="Pipeline de ventas"
        value={salesSummary.totalOrders.toLocaleString('es-ES')}
        description={`Importe total: ${salesTotalValue}`}
      />
      <SummaryTile
        title="Ventas completadas"
        value={salesSummary.fulfilledOrders.toLocaleString('es-ES')}
        description={`Pendientes: ${salesSummary.pendingOrders.toLocaleString('es-ES')}`}
        tone="success"
      />
      <SummaryTile
        title="Plan de compras"
        value={purchaseSummary.totalOrders.toLocaleString('es-ES')}
        description={`Inversión: ${purchaseTotalValue}`}
      />
      <SummaryTile
        title="Recepciones pendientes"
        value={purchaseSummary.pendingOrders.toLocaleString('es-ES')}
        description={`Recibidas: ${purchaseSummary.receivedOrders.toLocaleString('es-ES')}`}
        tone="warning"
      />
    </section>
  );
}

type SummaryTone = 'neutral' | 'success' | 'warning';

interface SummaryTileProps {
  title: string;
  value: string;
  description: string;
  tone?: SummaryTone;
}

function SummaryTile({ title, value, description, tone = 'neutral' }: SummaryTileProps) {
  const toneStyles: Record<SummaryTone, string> = {
    neutral: 'border-slate-200',
    success: 'border-emerald-200',
    warning: 'border-amber-200'
  };

  const accentStyles: Record<SummaryTone, string> = {
    neutral: 'text-primary-600 bg-primary-50',
    success: 'text-emerald-600 bg-emerald-50',
    warning: 'text-amber-600 bg-amber-50'
  };

  return (
    <article
      className={`flex flex-col gap-2 rounded-2xl border ${toneStyles[tone]} bg-white/80 p-5 shadow-sm backdrop-blur`}
      role="status"
      aria-live="polite"
    >
      <span className={`w-fit rounded-full px-3 py-1 text-xs font-semibold uppercase tracking-wide ${accentStyles[tone]}`}>
        {title}
      </span>
      <span className="text-2xl font-semibold text-slate-900">{value}</span>
      <span className="text-xs text-slate-500">{description}</span>
    </article>
  );
}

interface OrdersStatusFilterProps<K extends string> {
  label: string;
  value: K | 'ALL';
  options: { value: K; label: string }[];
  onChange: (value: K | 'ALL') => void;
}

function OrdersStatusFilter<K extends string>({ label, value, options, onChange }: OrdersStatusFilterProps<K>) {
  return (
    <div className="grid gap-3 sm:grid-cols-[auto,minmax(0,1fr)] sm:items-center">
      <span className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary-50">
        <FunnelIcon aria-hidden className="h-4 w-4 text-primary-600" />
      </span>
      <div className="min-w-[12rem] sm:min-w-[15rem]">
        <Select
          value={value}
          onChange={(event) => onChange(event.target.value as K | 'ALL')}
          label={label}
        >
          <option value="ALL">Todos los estados</option>
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </Select>
      </div>
    </div>
  );
}

interface OrdersTableProps<T> {
  headers: string[];
  rows: OrdersRowData<T>[];
  statusMap: Record<string, { label: string; tone: 'success' | 'warning' | 'neutral' }>;
  emptyMessage: string;
  isLoading: boolean;
  error: string | null;
  renderActions?: (row: OrdersRowData<T>) => ReactNode;
  renderDetails?: (row: OrdersRowData<T>) => ReactNode;
}

function OrdersTable<T>({
  headers,
  rows,
  statusMap,
  emptyMessage,
  isLoading,
  error,
  renderActions,
  renderDetails
}: OrdersTableProps<T>) {
  const [expandedRows, setExpandedRows] = useState<Set<number>>(() => new Set());
  const totalColumns = headers.length + (renderActions ? 1 : 0) + (renderDetails ? 1 : 0);

  useEffect(() => {
    setExpandedRows((previous) => {
      const availableIds = new Set(rows.map((row) => row.id));
      const next = new Set<number>();
      previous.forEach((rowId) => {
        if (availableIds.has(rowId)) {
          next.add(rowId);
        }
      });

      if (next.size === previous.size) {
        let identical = true;
        next.forEach((rowId) => {
          if (!previous.has(rowId)) {
            identical = false;
          }
        });

        if (identical) {
          return previous;
        }
      }

      return next;
    });
  }, [rows]);

  const toggleRowDetails = (rowId: number) => {
    setExpandedRows((previous) => {
      const next = new Set(previous);
      if (next.has(rowId)) {
        next.delete(rowId);
      } else {
        next.add(rowId);
      }

      return next;
    });
  };

  return (
    <div className="rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-slate-200 text-sm">
          <thead className="bg-slate-50 text-xs uppercase text-slate-500">
            <tr>
              {renderDetails && (
                <th className="px-3 py-3 text-left">
                  <span className="sr-only">Detalle</span>
                </th>
              )}
              {headers.map((header) => (
                <th key={header} className="px-4 py-3 text-left">
                  {header}
                </th>
              ))}
              {renderActions && <th className="px-4 py-3 text-right">Acciones</th>}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {isLoading ? (
              <tr>
                <td colSpan={totalColumns} className="px-4 py-8 text-center text-sm text-slate-500">
                  Cargando pedidos…
                </td>
              </tr>
            ) : error ? (
              <tr>
                <td colSpan={totalColumns} className="px-4 py-8 text-center text-sm text-red-500">
                  {error}
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={totalColumns} className="px-4 py-8 text-center text-sm text-slate-500">
                  {emptyMessage}
                </td>
              </tr>
            ) : (
              rows.map((row) => {
                const statusInfo = statusMap[row.statusKey] ?? {
                  label: row.statusKey,
                  tone: 'neutral' as const
                };
                const isExpanded = expandedRows.has(row.id);
                const detailsRowId = `order-${row.id}-details`;
                return (
                  <Fragment key={row.id}>
                    <tr className="text-slate-700">
                      {renderDetails && (
                        <td className="px-3 py-3">
                          <Button
                            type="button"
                            variant="ghost"
                            size="sm"
                            onClick={() => toggleRowDetails(row.id)}
                            className="h-8 w-8 p-0"
                            aria-expanded={isExpanded}
                            aria-controls={detailsRowId}
                            aria-label={
                              isExpanded
                                ? 'Ocultar detalles del pedido'
                                : 'Mostrar detalles del pedido'
                            }
                          >
                            <ChevronDownIcon
                              aria-hidden
                              className={`h-4 w-4 transition-transform ${isExpanded ? 'rotate-180' : 'rotate-0'}`}
                            />
                          </Button>
                        </td>
                      )}
                      <td className="px-4 py-3 text-sm font-semibold text-slate-900">{row.primary}</td>
                      <td className="px-4 py-3 text-xs text-slate-500">{row.secondary}</td>
                      <td className="px-4 py-3 text-xs text-slate-500">{row.date}</td>
                      <td className="px-4 py-3 text-sm font-semibold text-slate-900">{row.amount}</td>
                      <td className="px-4 py-3 text-right">
                        <Badge tone={statusInfo.tone}>{statusInfo.label}</Badge>
                      </td>
                      {renderActions && <td className="px-4 py-3 text-right">{renderActions(row)}</td>}
                    </tr>
                    {renderDetails && isExpanded && (
                      <tr className="bg-slate-50/60">
                        <td id={detailsRowId} colSpan={totalColumns} className="px-4 py-4">
                          {renderDetails(row)}
                        </td>
                      </tr>
                    )}
                  </Fragment>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
