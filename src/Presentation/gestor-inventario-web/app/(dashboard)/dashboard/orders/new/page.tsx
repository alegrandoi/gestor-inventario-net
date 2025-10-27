'use client';

import { FormEvent, useEffect, useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import { PlusIcon, TrashIcon } from '@heroicons/react/24/outline';
import { apiClient } from 'src/lib/api-client';
import { useConfigureApiClient } from 'src/hooks/use-configure-api-client';
import type {
  CarrierDto,
  CustomerDto,
  PagedResult,
  ProductDto,
  PurchaseOrderDto,
  SalesOrderDto,
  SupplierDto
} from 'src/types/api';
import { Button } from '../../../../../components/ui/button';
import { Card } from '../../../../../components/ui/card';
import { Input } from '../../../../../components/ui/input';
import { Select } from '../../../../../components/ui/select';
import { Textarea } from '../../../../../components/ui/textarea';

const defaultCurrency = 'EUR';
const productPageSize = 200;

type OrderType = 'sales' | 'purchase';

type NumericValue = number | '';

interface OrderLineFormState {
  id: string;
  variantId: number | '';
  quantity: NumericValue;
  unitPrice: NumericValue;
  discount: NumericValue;
}

interface OrderFormState {
  orderType: OrderType;
  orderDate: string;
  currency: string;
  customerId: number | '';
  supplierId: number | '';
  carrierId: number | '';
  estimatedDeliveryDate: string;
  shippingAddress: string;
  notes: string;
  lines: OrderLineFormState[];
}

interface VariantOption {
  id: number;
  label: string;
  currency: string;
  defaultPrice: number;
}

function createEmptyLine(): OrderLineFormState {
  return {
    id: Math.random().toString(36).slice(2),
    variantId: '',
    quantity: '',
    unitPrice: '',
    discount: ''
  };
}

function normalizeDate(date: Date) {
  return date.toISOString().split('T')[0];
}

function extractErrorMessage(error: unknown, fallback: string) {
  if (error instanceof Error && error.message) {
    return error.message;
  }

  return fallback;
}

export default function NewOrderPage() {
  const router = useRouter();
  const { isConfigured: isApiClientConfigured } = useConfigureApiClient();
  const [customers, setCustomers] = useState<CustomerDto[]>([]);
  const [suppliers, setSuppliers] = useState<SupplierDto[]>([]);
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [carriers, setCarriers] = useState<CarrierDto[]>([]);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [isLoadingCatalog, setIsLoadingCatalog] = useState(true);
  const [form, setForm] = useState<OrderFormState>(() => ({
    orderType: 'sales',
    orderDate: normalizeDate(new Date()),
    currency: defaultCurrency,
    customerId: '',
    supplierId: '',
    carrierId: '',
    estimatedDeliveryDate: '',
    shippingAddress: '',
    notes: '',
    lines: [createEmptyLine()]
  }));
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!isApiClientConfigured) {
      return;
    }

    let isActive = true;
    setIsLoadingCatalog(true);
    setLoadError(null);

    const fetchCatalog = async () => {
      try {
        const [customersResponse, suppliersResponse, productsResponse, carriersResponse] = await Promise.all([
          apiClient.get<CustomerDto[]>('/customers'),
          apiClient.get<SupplierDto[]>('/suppliers'),
          apiClient.get<PagedResult<ProductDto>>('/products', {
            params: { pageSize: productPageSize, isActive: true }
          }),
          apiClient.get<CarrierDto[]>('/carriers')
        ]);

        if (!isActive) {
          return;
        }

        setCustomers(customersResponse.data);
        setSuppliers(suppliersResponse.data);
        setProducts(productsResponse.data.items ?? []);
        setCarriers(carriersResponse.data);
        setIsLoadingCatalog(false);
      } catch (error) {
        console.error(error);
        if (!isActive) {
          return;
        }

        setLoadError('No se pudieron cargar los catálogos de clientes, proveedores, transportistas y productos.');
        setIsLoadingCatalog(false);
      }
    };

    fetchCatalog().catch((error) => console.error(error));

    return () => {
      isActive = false;
    };
  }, [isApiClientConfigured]);

  const variantOptions = useMemo<VariantOption[]>(
    () =>
      products.flatMap((product) =>
        product.variants.map((variant) => ({
          id: variant.id,
          label: `${product.name} · ${variant.sku}`,
          currency: product.currency,
          defaultPrice: variant.price ?? product.defaultPrice
        }))
      ),
    [products]
  );

  const carrierOptions = useMemo(() => carriers.map((carrier) => ({ value: carrier.id, label: carrier.name })), [carriers]);

  const variantPriceMap = useMemo(() => {
    const map = new Map<number, { price: number; currency: string }>();
    variantOptions.forEach((variant) => {
      map.set(variant.id, { price: variant.defaultPrice, currency: variant.currency });
    });
    return map;
  }, [variantOptions]);

  const totalAmount = useMemo(() => {
    return form.lines.reduce((accumulator, line) => {
      if (typeof line.quantity !== 'number' || typeof line.unitPrice !== 'number') {
        return accumulator;
      }

      const subtotal = line.quantity * line.unitPrice;
      const discount = typeof line.discount === 'number' ? line.discount : 0;
      return accumulator + Math.max(subtotal - discount, 0);
    }, 0);
  }, [form.lines]);

  const totalUnits = useMemo(() => {
    return form.lines.reduce((accumulator, line) => {
      if (typeof line.quantity !== 'number') {
        return accumulator;
      }

      return accumulator + line.quantity;
    }, 0);
  }, [form.lines]);

  const counterpartOptions = form.orderType === 'sales' ? customers : suppliers;

  const handleOrderTypeChange = (value: OrderType) => {
    setForm((previous) => ({
      ...previous,
      orderType: value,
      customerId: value === 'sales' ? previous.customerId : '',
      supplierId: value === 'purchase' ? previous.supplierId : '',
      carrierId: value === 'sales' ? previous.carrierId : '',
      estimatedDeliveryDate: value === 'sales' ? previous.estimatedDeliveryDate : '',
      shippingAddress: value === 'sales' ? previous.shippingAddress : ''
    }));
  };

  const updateLine = (lineId: string, changes: Partial<OrderLineFormState>) => {
    setForm((previous) => ({
      ...previous,
      lines: previous.lines.map((line) => (line.id === lineId ? { ...line, ...changes } : line))
    }));
  };

  const addLine = () => {
    setForm((previous) => ({
      ...previous,
      lines: [...previous.lines, createEmptyLine()]
    }));
  };

  const removeLine = (lineId: string) => {
    setForm((previous) => ({
      ...previous,
      lines: previous.lines.length <= 1 ? previous.lines : previous.lines.filter((line) => line.id !== lineId)
    }));
  };

  const handleLineVariantChange = (line: OrderLineFormState, variantId: number | '') => {
    if (variantId === '') {
      updateLine(line.id, { variantId, unitPrice: '' });
      return;
    }

    const pricing = variantPriceMap.get(variantId);
    updateLine(line.id, {
      variantId,
      unitPrice: pricing ? pricing.price : ''
    });

    if (pricing) {
      setForm((previous) => ({
        ...previous,
        currency: previous.currency ? previous.currency : pricing.currency
      }));
    }
  };

  const parseNumericInput = (value: string): NumericValue => {
    if (value.trim() === '') {
      return '';
    }

    const parsed = Number(value);
    if (Number.isNaN(parsed) || parsed < 0) {
      return '';
    }

    return parsed;
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFormError(null);

    if (isSubmitting) {
      return;
    }

    if (form.orderType === 'sales' && typeof form.customerId !== 'number') {
      setFormError('Selecciona el cliente del pedido.');
      return;
    }

    if (form.orderType === 'purchase' && typeof form.supplierId !== 'number') {
      setFormError('Selecciona el proveedor del pedido.');
      return;
    }

    if (!form.orderDate) {
      setFormError('Indica la fecha del pedido.');
      return;
    }

    if (!form.currency) {
      setFormError('Especifica la moneda del pedido.');
      return;
    }

    if (
      form.orderType === 'sales' &&
      form.estimatedDeliveryDate &&
      new Date(form.estimatedDeliveryDate).getTime() < new Date(form.orderDate).getTime()
    ) {
      setFormError('La fecha estimada de entrega no puede ser anterior a la fecha del pedido.');
      return;
    }

    const preparedLines = form.lines
      .map((line) => {
        if (typeof line.variantId !== 'number' || typeof line.quantity !== 'number' || typeof line.unitPrice !== 'number') {
          return null;
        }

        const baseLine = {
          variantId: line.variantId,
          quantity: line.quantity,
          unitPrice: line.unitPrice
        };

        const discount = typeof line.discount === 'number' && line.discount > 0 ? line.discount : undefined;

        return discount !== undefined ? { ...baseLine, discount } : baseLine;
      })
      .filter((line): line is { variantId: number; quantity: number; unitPrice: number; discount?: number } => line !== null);

    if (preparedLines.length === 0) {
      setFormError('Añade al menos un producto con cantidad y precio válidos.');
      return;
    }

    setIsSubmitting(true);

    try {
      const payload = {
        orderDate: new Date(form.orderDate).toISOString(),
        currency: form.currency,
        notes: form.notes || undefined,
        lines: preparedLines
      } as Record<string, unknown>;

      if (form.orderType === 'sales') {
        payload['customerId'] = form.customerId;
        payload['shippingAddress'] = form.shippingAddress || undefined;
        if (typeof form.carrierId === 'number') {
          payload['carrierId'] = form.carrierId;
        }
        if (form.estimatedDeliveryDate) {
          payload['estimatedDeliveryDate'] = new Date(form.estimatedDeliveryDate).toISOString();
        }
      } else {
        payload['supplierId'] = form.supplierId;
      }

      const endpoint = form.orderType === 'sales' ? '/salesorders' : '/purchaseorders';
      const response = await apiClient.post<SalesOrderDto | PurchaseOrderDto>(endpoint, payload);

      const createdOrder = response.data;
      const orderId = createdOrder.id;
      router.push(`/dashboard/orders/${form.orderType === 'sales' ? 'sales' : 'purchase'}/${orderId}`);
    } catch (error) {
      console.error(error);
      setFormError(extractErrorMessage(error, 'No se pudo registrar el pedido. Revisa los datos e inténtalo de nuevo.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold text-slate-900">Nuevo pedido</h1>
        <p className="text-sm text-slate-500">
          Registra un pedido de venta o compra seleccionando los productos, cantidades y condiciones acordadas.
        </p>
      </div>

      {loadError && <div className="rounded-2xl border border-red-200 bg-red-50 p-4 text-sm text-red-600">{loadError}</div>}

      <form className="flex flex-col gap-6" onSubmit={handleSubmit} noValidate>
        <Card title="Tipo de pedido" subtitle="Elige si corresponde a una venta o a una compra">
          <div className="flex flex-wrap gap-3">
            <Button
              type="button"
              variant={form.orderType === 'sales' ? 'primary' : 'outline'}
              onClick={() => handleOrderTypeChange('sales')}
            >
              Pedido de venta
            </Button>
            <Button
              type="button"
              variant={form.orderType === 'purchase' ? 'primary' : 'outline'}
              onClick={() => handleOrderTypeChange('purchase')}
            >
              Pedido de compra
            </Button>
          </div>
        </Card>

        <Card title="Información general" subtitle="Datos básicos para tramitar el pedido">
          <div className="grid gap-4 md:grid-cols-2">
            <Select
              label={form.orderType === 'sales' ? 'Cliente' : 'Proveedor'}
              value={form.orderType === 'sales' ? form.customerId : form.supplierId}
              onChange={(event) => {
                const value = event.target.value === '' ? '' : Number(event.target.value);
                if (form.orderType === 'sales') {
                  setForm((previous) => ({ ...previous, customerId: value }));
                } else {
                  setForm((previous) => ({ ...previous, supplierId: value }));
                }
              }}
              disabled={isLoadingCatalog}
            >
              <option value="">Selecciona una opción</option>
              {counterpartOptions.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.name}
                </option>
              ))}
            </Select>
            <Input
              type="date"
              label="Fecha del pedido"
              value={form.orderDate}
              onChange={(event) => setForm((previous) => ({ ...previous, orderDate: event.target.value }))}
            />
            <Input
              label="Moneda"
              value={form.currency}
              onChange={(event) => setForm((previous) => ({ ...previous, currency: event.target.value }))}
              placeholder="EUR"
            />
            {form.orderType === 'sales' && (
              <Input
                label="Dirección de envío"
                value={form.shippingAddress}
                onChange={(event) => setForm((previous) => ({ ...previous, shippingAddress: event.target.value }))}
                placeholder="Dirección completa para la entrega"
              />
            )}
            {form.orderType === 'sales' && (
              <Select
                label="Transportista"
                value={form.carrierId}
                onChange={(event) => {
                  const value = event.target.value === '' ? '' : Number(event.target.value);
                  setForm((previous) => ({ ...previous, carrierId: value }));
                }}
                disabled={isLoadingCatalog}
              >
                <option value="">Selecciona un transportista</option>
                {carrierOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </Select>
            )}
            {form.orderType === 'sales' && (
              <Input
                type="date"
                label="Entrega estimada"
                value={form.estimatedDeliveryDate}
                onChange={(event) =>
                  setForm((previous) => ({ ...previous, estimatedDeliveryDate: event.target.value }))
                }
                hint="Fecha objetivo para comparar entregas a tiempo"
              />
            )}
            <Textarea
              label="Notas internas"
              value={form.notes}
              onChange={(event) => setForm((previous) => ({ ...previous, notes: event.target.value }))}
              placeholder="Condiciones especiales, referencias o comentarios"
              className="md:col-span-2"
            />
          </div>
        </Card>

        <Card title="Productos" subtitle="Añade todas las líneas incluidas en el pedido">
          <div className="flex flex-col gap-4">
            {form.lines.map((line) => (
              <div
                key={line.id}
                className="rounded-2xl border border-slate-200 bg-white/60 p-4 shadow-sm"
              >
                <div className="grid gap-3 md:grid-cols-[2fr,1fr,1fr,1fr,auto] md:items-end">
                  <Select
                    label="Producto"
                    value={line.variantId}
                    onChange={(event) => {
                      const value = event.target.value === '' ? '' : Number(event.target.value);
                      handleLineVariantChange(line, value);
                    }}
                    disabled={isLoadingCatalog}
                  >
                    <option value="">Selecciona un producto</option>
                    {variantOptions.map((option) => (
                      <option key={option.id} value={option.id}>
                        {option.label}
                      </option>
                    ))}
                  </Select>
                  <Input
                    label="Cantidad"
                    value={line.quantity}
                    onChange={(event) => updateLine(line.id, { quantity: parseNumericInput(event.target.value) })}
                    placeholder="0"
                    inputMode="decimal"
                  />
                  <Input
                    label="Precio unitario"
                    value={line.unitPrice}
                    onChange={(event) => updateLine(line.id, { unitPrice: parseNumericInput(event.target.value) })}
                    placeholder="0"
                    inputMode="decimal"
                  />
                  <Input
                    label="Descuento"
                    value={line.discount}
                    onChange={(event) => updateLine(line.id, { discount: parseNumericInput(event.target.value) })}
                    placeholder="0"
                    inputMode="decimal"
                  />
                  <div className="flex flex-col items-end gap-2">
                    <span className="text-xs uppercase tracking-wide text-slate-500">Importe</span>
                    <span className="text-sm font-semibold text-slate-900">
                      {typeof line.quantity === 'number' && typeof line.unitPrice === 'number'
                        ? (line.quantity * line.unitPrice - (typeof line.discount === 'number' ? line.discount : 0)).toLocaleString(
                            'es-ES',
                            { minimumFractionDigits: 2, maximumFractionDigits: 2 }
                          )
                        : '—'}
                    </span>
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      className="text-xs text-red-500 hover:text-red-600"
                      onClick={() => removeLine(line.id)}
                    >
                      <TrashIcon aria-hidden className="mr-2 h-4 w-4" /> Eliminar
                    </Button>
                  </div>
                </div>
              </div>
            ))}

            <Button type="button" variant="secondary" onClick={addLine} className="self-start">
              <PlusIcon aria-hidden className="mr-2 h-4 w-4" /> Añadir producto
            </Button>
          </div>
        </Card>

        <Card title="Resumen" subtitle="Totales calculados según las líneas del pedido">
          <dl className="grid gap-4 md:grid-cols-3">
            <div>
              <dt className="text-xs uppercase tracking-wide text-slate-500">Productos</dt>
              <dd className="text-base font-medium text-slate-900">{form.lines.length}</dd>
            </div>
            <div>
              <dt className="text-xs uppercase tracking-wide text-slate-500">Unidades</dt>
              <dd className="text-base font-medium text-slate-900">{totalUnits}</dd>
            </div>
            <div>
              <dt className="text-xs uppercase tracking-wide text-slate-500">Importe estimado</dt>
              <dd className="text-xl font-semibold text-slate-900">
                {totalAmount.toLocaleString('es-ES', { style: 'currency', currency: form.currency || defaultCurrency })}
              </dd>
            </div>
          </dl>
        </Card>

        {formError && <div className="rounded-2xl border border-red-200 bg-red-50 p-4 text-sm text-red-600">{formError}</div>}

        <div className="flex justify-end gap-3">
          <Button type="button" variant="ghost" onClick={() => router.push('/dashboard/orders')}>
            Cancelar
          </Button>
          <Button type="submit" disabled={isSubmitting || isLoadingCatalog}>
            {isSubmitting ? 'Creando pedido…' : 'Registrar pedido'}
          </Button>
        </div>
      </form>
    </div>
  );
}
