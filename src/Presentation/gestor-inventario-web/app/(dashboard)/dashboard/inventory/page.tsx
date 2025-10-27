'use client';

import { ChangeEvent, FormEvent, useEffect, useMemo, useRef, useState } from 'react';
import { AxiosError } from 'axios';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { apiClient } from '../../../../src/lib/api-client';
import type { InventoryStockDto, ReplenishmentPlanDto, WarehouseDto } from '../../../../src/types/api';
import { Card } from '../../../../components/ui/card';
import { Input } from '../../../../components/ui/input';
import { Badge } from '../../../../components/ui/badge';
import { Button } from '../../../../components/ui/button';
import { Select } from '../../../../components/ui/select';
import { Textarea } from '../../../../components/ui/textarea';

type InventoryAdjustmentDetailNotification = {
  warehouseId: number;
  warehouseName: string;
  quantityBefore: number;
  quantityAfter: number;
};

type InventoryAdjustmentNotification = {
  variantId: number;
  variantSku: string;
  productName: string;
  adjustments: InventoryAdjustmentDetailNotification[];
  transactionType: number;
  quantity: number;
  destinationWarehouseId?: number | null;
  referenceType?: string | null;
  referenceId?: number | null;
  notes?: string | null;
  occurredAt: string;
};

type SalesOrderChangeNotification = {
  orderId: number;
  status: number;
  customerName: string;
  totalAmount: number;
  changeType: string;
  occurredAt: string;
};

type LiveNotification = {
  id: string;
  title: string;
  description: string;
  createdAt: Date;
};

const INVENTORY_TRANSACTION_TYPES = {
  In: 1,
  Out: 2,
  Move: 3,
  Adjust: 4
} as const;

type InventoryTransactionTypeValue = (typeof INVENTORY_TRANSACTION_TYPES)[keyof typeof INVENTORY_TRANSACTION_TYPES];

const TRANSACTION_TYPE_LABELS: Record<InventoryTransactionTypeValue, string> = {
  [INVENTORY_TRANSACTION_TYPES.In]: 'entrada',
  [INVENTORY_TRANSACTION_TYPES.Out]: 'salida',
  [INVENTORY_TRANSACTION_TYPES.Move]: 'traslado',
  [INVENTORY_TRANSACTION_TYPES.Adjust]: 'ajuste'
};

const ORDER_STATUS_LABELS: Record<number, string> = {
  1: 'pendiente',
  2: 'confirmado',
  3: 'enviado',
  4: 'entregado',
  5: 'cancelado'
};

export default function InventoryPage() {
  const [stocks, setStocks] = useState<InventoryStockDto[]>([]);
  const [search, setSearch] = useState('');
  const [onlyAlerts, setOnlyAlerts] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [plan, setPlan] = useState<ReplenishmentPlanDto | null>(null);
  const [isPlanLoading, setIsPlanLoading] = useState(false);
  const [planError, setPlanError] = useState<string | null>(null);
  const [planningWindow, setPlanningWindow] = useState('30');
  const [fromDate, setFromDate] = useState('');
  const [notifications, setNotifications] = useState<LiveNotification[]>([]);
  const [isAdjustmentOpen, setIsAdjustmentOpen] = useState(false);
  const [selectedStock, setSelectedStock] = useState<InventoryStockDto | null>(null);
  const [transactionType, setTransactionType] = useState<InventoryTransactionTypeValue>(INVENTORY_TRANSACTION_TYPES.Out);
  const [movementQuantity, setMovementQuantity] = useState('');
  const [destinationWarehouseId, setDestinationWarehouseId] = useState<number | ''>('');
  const [notes, setNotes] = useState('');
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmittingAdjustment, setIsSubmittingAdjustment] = useState(false);
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([]);
  const [warehousesError, setWarehousesError] = useState<string | null>(null);
  const notificationTimers = useRef<Array<ReturnType<typeof setTimeout>>>([]);

  const transactionTypeOptions = useMemo(
    () =>
      Object.entries(TRANSACTION_TYPE_LABELS).map(([value, label]) => ({
        value: Number(value) as InventoryTransactionTypeValue,
        label: label.charAt(0).toUpperCase() + label.slice(1)
      })),
    []
  );

  const formatChangeType = (changeType: string) => {
    if (changeType === 'created') {
      return 'nuevo pedido creado';
    }

    if (changeType.startsWith('status:')) {
      const [, path] = changeType.split(':');
      const [from, to] = path.split('->');
      return `estado ${from?.toLowerCase()} → ${to?.toLowerCase()}`;
    }

    return changeType;
  };

  const notify = (title: string, description: string) => {
    const id = `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
    const notification: LiveNotification = {
      id,
      title,
      description,
      createdAt: new Date()
    };

    setNotifications((current) => [notification, ...current].slice(0, 5));

    const timeoutId = setTimeout(() => {
      setNotifications((current) => current.filter((item) => item.id !== id));
    }, 12000);

    notificationTimers.current.push(timeoutId);
  };

  useEffect(() => {
    return () => {
      notificationTimers.current.forEach((timer) => clearTimeout(timer));
      notificationTimers.current = [];
    };
  }, []);

  useEffect(() => {
    async function fetchWarehouses() {
      try {
        const response = await apiClient.get<WarehouseDto[]>('/warehouses');
        setWarehouses(response.data);
        setWarehousesError(null);
      } catch (err) {
        console.error(err);
        setWarehousesError('No se pudieron obtener los almacenes.');
      }
    }

    fetchWarehouses().catch((err) => console.error(err));
  }, []);

  useEffect(() => {
    const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5000/api';
    const normalizedBase = baseUrl.endsWith('/api') ? baseUrl.slice(0, -4) : baseUrl;
    const hubUrl = `${normalizedBase}/hubs/inventory`;

    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, { withCredentials: true })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('InventoryAdjusted', (message: InventoryAdjustmentNotification) => {
      const typeLabel = TRANSACTION_TYPE_LABELS[message.transactionType as InventoryTransactionTypeValue] ?? 'movimiento';
      const warehouse = message.adjustments[0]?.warehouseName ?? 'almacén';
      const description = `Se registró una ${typeLabel} de ${message.quantity.toLocaleString('es-ES')} uds para ${message.productName} (${message.variantSku}) en ${warehouse}.`;
      notify('Inventario actualizado', description);
    });

    connection.on('SalesOrderChanged', (message: SalesOrderChangeNotification) => {
      const statusLabel = ORDER_STATUS_LABELS[message.status] ?? 'actualizado';
      const changeSummary = formatChangeType(message.changeType);
      const description = `Pedido #${message.orderId} (${message.customerName}) ${changeSummary} · Estado ${statusLabel}.`;
      notify('Pedido actualizado', description);
    });

    connection
      .start()
      .catch((err) => console.error('No se pudo iniciar la conexión de alertas en vivo', err));

    return () => {
      connection
        .stop()
        .catch((err) => console.error('No se pudo detener la conexión de alertas en vivo', err));
    };
  }, []);

  useEffect(() => {
    async function fetchInventory() {
      try {
        const response = await apiClient.get<InventoryStockDto[]>('/inventory');
        setStocks(response.data);
      } catch (err) {
        console.error(err);
        setError('No se pudo obtener el inventario.');
      } finally {
        setIsLoading(false);
      }
    }

    fetchInventory().catch((err) => console.error(err));
  }, []);

  useEffect(() => {
    if (!isAdjustmentOpen || transactionType !== INVENTORY_TRANSACTION_TYPES.Move || !selectedStock) {
      if (transactionType !== INVENTORY_TRANSACTION_TYPES.Move) {
        setDestinationWarehouseId('');
      }
      return;
    }

    if (typeof destinationWarehouseId === 'number' && destinationWarehouseId !== selectedStock.warehouseId) {
      return;
    }

    const alternatives = warehouses.filter((warehouse) => warehouse.id !== selectedStock.warehouseId);
    if (alternatives.length > 0) {
      setDestinationWarehouseId(alternatives[0].id);
    }
  }, [destinationWarehouseId, isAdjustmentOpen, selectedStock, transactionType, warehouses]);

  function closeAdjustment() {
    setIsAdjustmentOpen(false);
    setSelectedStock(null);
    setMovementQuantity('');
    setDestinationWarehouseId('');
    setNotes('');
    setFormError(null);
  }

  function openAdjustment(
    stock: InventoryStockDto,
    type: InventoryTransactionTypeValue = INVENTORY_TRANSACTION_TYPES.Out
  ) {
    setSelectedStock(stock);
    setTransactionType(type);
    setMovementQuantity('');
    setDestinationWarehouseId('');
    setNotes('');
    setFormError(null);
    setIsAdjustmentOpen(true);
  }

  async function handleGeneratePlan(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPlanError(null);
    setIsPlanLoading(true);

    try {
      const params: Record<string, unknown> = {
        planningWindowDays: Number.parseInt(planningWindow, 10) || 0
      };

      if (fromDate) {
        params.fromDate = fromDate;
      }

      const response = await apiClient.get<ReplenishmentPlanDto>('/inventory/replenishment-plan', {
        params
      });

      setPlan(response.data);
    } catch (planErr) {
      console.error(planErr);
      setPlanError('No se pudo generar el plan de reposición.');
      setPlan(null);
    } finally {
      setIsPlanLoading(false);
    }
  }

  async function handleAdjustmentSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedStock) {
      return;
    }

    setFormError(null);

    const normalized = movementQuantity.replace(',', '.');
    const parsed = Number.parseFloat(normalized);

    if (Number.isNaN(parsed)) {
      setFormError('Introduce una cantidad válida.');
      return;
    }

    if (parsed < 0) {
      setFormError('La cantidad no puede ser negativa.');
      return;
    }

    if (transactionType !== INVENTORY_TRANSACTION_TYPES.Adjust && parsed === 0) {
      setFormError('La cantidad debe ser mayor a cero.');
      return;
    }

    if (
      (transactionType === INVENTORY_TRANSACTION_TYPES.Out || transactionType === INVENTORY_TRANSACTION_TYPES.Move) &&
      parsed > selectedStock.quantity
    ) {
      setFormError('Stock insuficiente en el almacén origen.');
      return;
    }

    let destinationId: number | null = null;
    let destinationName: string | null = null;

    if (transactionType === INVENTORY_TRANSACTION_TYPES.Move) {
      if (typeof destinationWarehouseId !== 'number') {
        setFormError('Selecciona un almacén destino.');
        return;
      }

      if (destinationWarehouseId === selectedStock.warehouseId) {
        setFormError('El almacén destino debe ser diferente al origen.');
        return;
      }

      destinationId = destinationWarehouseId;
      destinationName = warehouses.find((warehouse) => warehouse.id === destinationWarehouseId)?.name ?? null;
    }

    setIsSubmittingAdjustment(true);

    try {
      const response = await apiClient.post<InventoryStockDto[]>('/inventory/adjust', {
        variantId: selectedStock.variantId,
        warehouseId: selectedStock.warehouseId,
        transactionType,
        quantity: parsed,
        destinationWarehouseId: destinationId,
        notes: notes.trim() ? notes.trim() : null
      });

      const updatedStocks = response.data;
      setStocks((current) => {
        const map = new Map(current.map((stockItem) => [`${stockItem.variantId}-${stockItem.warehouseId}`, stockItem]));
        for (const stockItem of updatedStocks) {
          map.set(`${stockItem.variantId}-${stockItem.warehouseId}`, stockItem);
        }
        return Array.from(map.values());
      });

      const actionLabel = transactionTypeOptions.find((option) => option.value === transactionType)?.label ?? 'Movimiento';
      const quantityLabel =
        transactionType === INVENTORY_TRANSACTION_TYPES.Adjust
          ? `stock ajustado a ${parsed.toLocaleString('es-ES')} uds`
          : `${parsed.toLocaleString('es-ES')} uds`;
      const destinationLabel = destinationName ? ` hacia ${destinationName}` : '';

      notify(
        'Inventario actualizado',
        `${actionLabel} de ${quantityLabel} en ${selectedStock.warehouseName}${destinationLabel} para ${selectedStock.productName} (${selectedStock.variantSku}).`
      );

      closeAdjustment();
    } catch (error) {
      console.error(error);
      let message = 'No se pudo registrar el movimiento.';
      if (error instanceof AxiosError) {
        const data = error.response?.data as { detail?: string; title?: string; errors?: Record<string, string[]> } | undefined;
        if (data?.detail) {
          message = data.detail;
        } else if (data?.title) {
          message = data.title;
        } else if (data?.errors) {
          const [firstError] = Object.values(data.errors);
          if (firstError?.length) {
            message = firstError[0];
          }
        } else if (error.message) {
          message = error.message;
        }
      }

      setFormError(message);
    } finally {
      setIsSubmittingAdjustment(false);
    }
  }

  const availableDestinationWarehouses = useMemo(() => {
    if (!selectedStock) {
      return warehouses;
    }

    return warehouses.filter((warehouse) => warehouse.id !== selectedStock.warehouseId);
  }, [selectedStock, warehouses]);

  const adjustmentPreview = useMemo(() => {
    if (!selectedStock) {
      return null;
    }

    const normalized = movementQuantity.replace(',', '.');
    const parsed = Number.parseFloat(normalized);

    if (Number.isNaN(parsed) || parsed < 0) {
      return null;
    }

    const baseAfter = (() => {
      switch (transactionType) {
        case INVENTORY_TRANSACTION_TYPES.In:
          return selectedStock.quantity + parsed;
        case INVENTORY_TRANSACTION_TYPES.Out:
        case INVENTORY_TRANSACTION_TYPES.Move:
          return selectedStock.quantity - parsed;
        case INVENTORY_TRANSACTION_TYPES.Adjust:
          return parsed;
        default:
          return selectedStock.quantity;
      }
    })();

    if (transactionType === INVENTORY_TRANSACTION_TYPES.Move && typeof destinationWarehouseId === 'number') {
      const destinationWarehouse = warehouses.find((warehouse) => warehouse.id === destinationWarehouseId);
      const destinationStock = stocks.find(
        (stock) => stock.variantId === selectedStock.variantId && stock.warehouseId === destinationWarehouseId
      );
      const destinationBefore = destinationStock?.quantity ?? 0;

      return {
        quantity: parsed,
        source: {
          name: selectedStock.warehouseName,
          before: selectedStock.quantity,
          after: baseAfter
        },
        destination: {
          name: destinationWarehouse?.name ?? 'Almacén destino',
          before: destinationBefore,
          after: destinationBefore + parsed
        }
      };
    }

    return {
      quantity: parsed,
      source: {
        name: selectedStock.warehouseName,
        before: selectedStock.quantity,
        after: baseAfter
      }
    };
  }, [destinationWarehouseId, movementQuantity, selectedStock, stocks, transactionType, warehouses]);

  const filtered = useMemo(() => {
    return stocks.filter((stock) => {
      const matchesQuery = `${stock.productName} ${stock.variantSku} ${stock.warehouseName}`
        .toLowerCase()
        .includes(search.toLowerCase());
      const netAvailable = stock.quantity - stock.reservedQuantity;
      const belowMinimum = netAvailable <= stock.minStockLevel;
      return matchesQuery && (!onlyAlerts || belowMinimum);
    });
  }, [onlyAlerts, search, stocks]);

  const totalQuantity = filtered.reduce((acc, stock) => acc + stock.quantity, 0);
  const totalReserved = filtered.reduce((acc, stock) => acc + stock.reservedQuantity, 0);

  return (
    <>
      {notifications.length > 0 && (
        <div className="fixed right-4 top-24 z-40 flex w-full max-w-sm flex-col gap-3">
          {notifications.map((notification) => (
            <div
              key={notification.id}
              className="rounded-2xl border border-primary-200/60 bg-white/95 p-4 shadow-lg shadow-primary-100/40 ring-1 ring-primary-100/40 backdrop-blur"
              role="status"
            >
              <p className="text-sm font-semibold text-primary-700">{notification.title}</p>
              <p className="mt-1 text-xs text-slate-600">{notification.description}</p>
              <p className="mt-2 text-[11px] uppercase tracking-wide text-slate-400">
                {notification.createdAt.toLocaleTimeString('es-ES', {
                  hour: '2-digit',
                  minute: '2-digit',
                  second: '2-digit'
                })}
              </p>
            </div>
          ))}
        </div>
      )}

      {isAdjustmentOpen && selectedStock && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 px-4 py-6 backdrop-blur-sm"
          role="dialog"
          aria-modal="true"
          onClick={closeAdjustment}
        >
          <div
            className="w-full max-w-xl rounded-2xl bg-white p-6 shadow-xl"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="flex items-start justify-between gap-4">
              <div>
                <h2 className="text-lg font-semibold text-slate-900">Registrar movimiento de inventario</h2>
                <p className="mt-1 text-sm text-slate-500">
                  {selectedStock.productName} · SKU {selectedStock.variantSku}
                </p>
                <p className="text-xs text-slate-400">Almacén {selectedStock.warehouseName}</p>
                {warehousesError && (
                  <p className="mt-2 text-xs text-amber-600">{warehousesError}</p>
                )}
              </div>
              <Button type="button" variant="ghost" size="sm" onClick={closeAdjustment}>
                Cerrar
              </Button>
            </div>

            <form className="mt-6 space-y-4" onSubmit={handleAdjustmentSubmit}>
              <Select
                label="Tipo de transacción"
                value={transactionType.toString()}
                onChange={(event: ChangeEvent<HTMLSelectElement>) => {
                  setTransactionType(Number.parseInt(event.target.value, 10) as InventoryTransactionTypeValue);
                  setFormError(null);
                }}
              >
                {transactionTypeOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </Select>

              <Input
                label={transactionType === INVENTORY_TRANSACTION_TYPES.Adjust ? 'Stock final' : 'Cantidad'}
                type="number"
                min={0}
                step="0.01"
                value={movementQuantity}
                onChange={(event: ChangeEvent<HTMLInputElement>) => {
                  setMovementQuantity(event.target.value);
                  setFormError(null);
                }}
                required
                hint={
                  transactionType === INVENTORY_TRANSACTION_TYPES.Adjust
                    ? 'Define el stock resultante en el almacén seleccionado.'
                    : 'Cantidad de unidades para el movimiento.'
                }
              />

              {transactionType === INVENTORY_TRANSACTION_TYPES.Move && (
                <Select
                  label="Almacén destino"
                  value={destinationWarehouseId === '' ? '' : destinationWarehouseId.toString()}
                  onChange={(event: ChangeEvent<HTMLSelectElement>) => {
                    const value = event.target.value;
                    setDestinationWarehouseId(value ? Number.parseInt(value, 10) : '');
                    setFormError(null);
                  }}
                  disabled={availableDestinationWarehouses.length === 0}
                  error={availableDestinationWarehouses.length === 0 ? 'No hay almacenes alternativos para el traslado.' : undefined}
                >
                  <option value="">Selecciona un destino</option>
                  {availableDestinationWarehouses.map((warehouse) => (
                    <option key={warehouse.id} value={warehouse.id}>
                      {warehouse.name}
                    </option>
                  ))}
                </Select>
              )}

              <Textarea
                label="Notas"
                value={notes}
                onChange={(event) => setNotes(event.target.value)}
                hint="Opcional. Se incluirá en el historial del movimiento."
                rows={3}
              />

              {formError && <p className="text-sm text-red-500">{formError}</p>}

              {adjustmentPreview && (
                <div className="rounded-xl border border-slate-200 bg-slate-50 p-4 text-sm text-slate-600">
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Saldo estimado</p>
                  <div className="mt-3 space-y-3">
                    <div className="rounded-lg border border-slate-200 bg-white/80 p-3">
                      <p className="text-xs text-slate-500">Origen · {adjustmentPreview.source.name}</p>
                      <div className="mt-1 flex items-center justify-between text-xs text-slate-500">
                        <span>Disponible actual</span>
                        <span>{adjustmentPreview.source.before.toLocaleString('es-ES')} uds</span>
                      </div>
                      <div className="mt-1 flex items-center justify-between text-sm font-semibold text-slate-900">
                        <span>Saldo final</span>
                        <span>{Math.max(adjustmentPreview.source.after, 0).toLocaleString('es-ES')} uds</span>
                      </div>
                    </div>
                    {adjustmentPreview.destination && (
                      <div className="rounded-lg border border-slate-200 bg-white/80 p-3">
                        <p className="text-xs text-slate-500">Destino · {adjustmentPreview.destination.name}</p>
                        <div className="mt-1 flex items-center justify-between text-xs text-slate-500">
                          <span>Disponible actual</span>
                          <span>{adjustmentPreview.destination.before.toLocaleString('es-ES')} uds</span>
                        </div>
                        <div className="mt-1 flex items-center justify-between text-sm font-semibold text-slate-900">
                          <span>Saldo final</span>
                          <span>{Math.max(adjustmentPreview.destination.after, 0).toLocaleString('es-ES')} uds</span>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              )}

              <div className="flex justify-end gap-2">
                <Button type="button" variant="ghost" onClick={closeAdjustment} disabled={isSubmittingAdjustment}>
                  Cancelar
                </Button>
                <Button
                  type="submit"
                  disabled={
                    isSubmittingAdjustment ||
                    (transactionType === INVENTORY_TRANSACTION_TYPES.Move && availableDestinationWarehouses.length === 0)
                  }
                >
                  {isSubmittingAdjustment ? 'Registrando…' : 'Registrar movimiento'}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      <div className="flex flex-col gap-6">
      <Card title="Inventario consolidado" subtitle="Consulta cantidades por variante y almacén">
        <div className="grid gap-4 md:grid-cols-3">
          <Input
            label="Buscar"
            placeholder="Producto, SKU o almacén"
            value={search}
            onChange={(event: ChangeEvent<HTMLInputElement>) => setSearch(event.target.value)}
          />
          <label className="mt-6 flex items-center gap-2 text-xs text-slate-600">
            <input
              type="checkbox"
              className="h-4 w-4 rounded border-slate-300 text-primary-600 focus:ring-primary-500"
              checked={onlyAlerts}
              onChange={(event) => setOnlyAlerts(event.target.checked)}
            />
            Mostrar solo alertas de reposición
          </label>
          <div className="mt-6 flex items-center justify-end gap-4 text-xs text-slate-500">
            <span>Disponible: <strong className="text-slate-900">{totalQuantity.toLocaleString('es-ES')}</strong></span>
            <span>Reservado: <strong className="text-slate-900">{totalReserved.toLocaleString('es-ES')}</strong></span>
          </div>
        </div>
      </Card>

      <Card
        title="Plan de reposición"
        subtitle="Calcula sugerencias de compra según la demanda reciente y el stock disponible"
      >
        <form
          className="grid gap-4 md:grid-cols-[repeat(3,minmax(0,1fr))] md:items-end"
          onSubmit={handleGeneratePlan}
        >
          <Input
            label="Horizonte de planificación (días)"
            type="number"
            min={7}
            max={180}
            value={planningWindow}
            onChange={(event: ChangeEvent<HTMLInputElement>) => setPlanningWindow(event.target.value)}
            required
          />
          <Input
            label="Desde la fecha"
            type="date"
            value={fromDate}
            onChange={(event: ChangeEvent<HTMLInputElement>) => setFromDate(event.target.value)}
            hint="Opcional, por defecto se usa la fecha actual"
          />
          <div className="flex w-full flex-col gap-1 md:self-stretch">
            <span className="text-sm font-medium text-slate-700 md:invisible">Acciones</span>
            <div className="flex flex-col gap-2 md:flex-row md:justify-end">
              <Button type="submit" className="w-full md:w-auto" disabled={isPlanLoading}>
                {isPlanLoading ? 'Generando…' : 'Generar plan'}
              </Button>
              {plan && (
                <Button
                  type="button"
                  variant="ghost"
                  className="w-full md:w-auto"
                  onClick={() => setPlan(null)}
                  disabled={isPlanLoading}
                >
                  Limpiar
                </Button>
              )}
            </div>
            <div aria-hidden="true" className="min-h-[1.25rem]" />
          </div>
        </form>

        {planError && <p className="mt-3 text-sm text-red-500">{planError}</p>}

        {plan && (
          <div className="mt-6 space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3 text-sm text-slate-600">
              <span>
                Generado el{' '}
                <strong className="text-slate-900">
                  {new Date(plan.generatedAt).toLocaleString('es-ES')}
                </strong>
              </span>
              <span>
                Reposición sugerida:{' '}
                <strong className="text-primary-600">
                  {plan.suggestions
                    .reduce((acc, suggestion) => acc + suggestion.recommendedQuantity, 0)
                    .toLocaleString('es-ES', { minimumFractionDigits: 0, maximumFractionDigits: 2 })}{' '}
                  uds
                </strong>
              </span>
            </div>

            <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
              <table className="min-w-full divide-y divide-slate-200 text-sm">
                <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                  <tr>
                    <th className="px-4 py-3 text-left">Producto</th>
                    <th className="px-4 py-3 text-left">Almacén</th>
                    <th className="px-4 py-3 text-right">Disponible</th>
                    <th className="px-4 py-3 text-right">Reservado</th>
                    <th className="px-4 py-3 text-right">Demanda diaria</th>
                    <th className="px-4 py-3 text-right">Punto de pedido</th>
                    <th className="px-4 py-3 text-right">Recomendado</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {plan.suggestions.length === 0 ? (
                    <tr>
                      <td colSpan={7} className="px-4 py-6 text-center text-sm text-slate-500">
                        No hay recomendaciones de reposición para el periodo seleccionado.
                      </td>
                    </tr>
                  ) : (
                    plan.suggestions.map((suggestion) => {
                      const needsAttention = suggestion.recommendedQuantity > 0;
                      return (
                        <tr
                          key={`${suggestion.variantId}-${suggestion.warehouseId}`}
                          className="text-slate-700"
                        >
                          <td className="px-4 py-3">
                            <p className="font-medium text-slate-900">{suggestion.productName}</p>
                            <p className="text-xs text-slate-500">SKU {suggestion.variantSku}</p>
                          </td>
                          <td className="px-4 py-3 text-xs text-slate-500">{suggestion.warehouseName}</td>
                          <td className="px-4 py-3 text-right text-sm font-semibold text-slate-900">
                            {suggestion.onHand.toLocaleString('es-ES', {
                              minimumFractionDigits: 0,
                              maximumFractionDigits: 2
                            })}
                          </td>
                          <td className="px-4 py-3 text-right text-xs text-slate-500">
                            {suggestion.reserved.toLocaleString('es-ES', {
                              minimumFractionDigits: 0,
                              maximumFractionDigits: 2
                            })}
                          </td>
                          <td className="px-4 py-3 text-right text-xs text-slate-500">
                            {suggestion.averageDailyDemand.toLocaleString('es-ES', {
                              minimumFractionDigits: 0,
                              maximumFractionDigits: 2
                            })}
                          </td>
                          <td className="px-4 py-3 text-right text-xs text-slate-500">
                            {suggestion.reorderPoint != null
                              ? suggestion.reorderPoint.toLocaleString('es-ES', {
                                  minimumFractionDigits: 0,
                                  maximumFractionDigits: 2
                                })
                              : '—'}
                          </td>
                          <td className="px-4 py-3 text-right">
                            {needsAttention ? (
                              <Badge tone="warning">
                                {suggestion.recommendedQuantity.toLocaleString('es-ES', {
                                  minimumFractionDigits: 0,
                                  maximumFractionDigits: 2
                                })}{' '}
                                uds
                              </Badge>
                            ) : (
                              <Badge tone="success">Sin acción</Badge>
                            )}
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </Card>

      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <table className="min-w-full divide-y divide-slate-200 text-sm">
          <thead className="bg-slate-50 text-xs uppercase text-slate-500">
            <tr>
              <th className="px-4 py-3 text-left">Producto</th>
              <th className="px-4 py-3 text-left">Almacén</th>
              <th className="px-4 py-3 text-right">Disponible</th>
              <th className="px-4 py-3 text-right">Reservado</th>
              <th className="px-4 py-3 text-right">Mínimo</th>
              <th className="px-4 py-3 text-right">Estado</th>
              <th className="px-4 py-3 text-right">Acciones</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {isLoading ? (
              <tr>
                <td colSpan={7} className="px-4 py-8 text-center text-sm text-slate-500">
                  Cargando inventario…
                </td>
              </tr>
            ) : error ? (
              <tr>
                <td colSpan={7} className="px-4 py-8 text-center text-sm text-red-500">
                  {error}
                </td>
              </tr>
            ) : filtered.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-4 py-8 text-center text-sm text-slate-500">
                  No se encontraron registros.
                </td>
              </tr>
            ) : (
              filtered.map((stock) => {
                const netAvailable = stock.quantity - stock.reservedQuantity;
                const belowMinimum = netAvailable <= stock.minStockLevel;
                return (
                  <tr key={`${stock.variantId}-${stock.warehouseId}`} className="text-slate-700">
                    <td className="px-4 py-3">
                      <p className="font-medium text-slate-900">{stock.productName}</p>
                      <p className="text-xs text-slate-500">SKU {stock.variantSku}</p>
                    </td>
                    <td className="px-4 py-3 text-xs text-slate-500">{stock.warehouseName}</td>
                    <td className="px-4 py-3 text-right text-sm font-semibold text-slate-900">
                      {stock.quantity.toLocaleString('es-ES')}
                    </td>
                    <td className="px-4 py-3 text-right text-xs text-slate-500">
                      {stock.reservedQuantity.toLocaleString('es-ES')}
                    </td>
                    <td className="px-4 py-3 text-right text-xs text-slate-500">{stock.minStockLevel}</td>
                    <td className="px-4 py-3 text-right">
                      <Badge tone={belowMinimum ? 'warning' : 'success'}>
                        {belowMinimum ? 'Reponer' : 'Óptimo'}
                      </Badge>
                    </td>
                    <td className="px-4 py-3 text-right">
                      <div className="flex flex-wrap justify-end gap-2">
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => openAdjustment(stock, INVENTORY_TRANSACTION_TYPES.Out)}
                        >
                          Registrar venta
                        </Button>
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => openAdjustment(stock, INVENTORY_TRANSACTION_TYPES.In)}
                        >
                          Registrar compra
                        </Button>
                        <Button size="sm" variant="secondary" onClick={() => openAdjustment(stock)}>
                          Otro movimiento
                        </Button>
                      </div>
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
      </div>
    </>
  );
}
