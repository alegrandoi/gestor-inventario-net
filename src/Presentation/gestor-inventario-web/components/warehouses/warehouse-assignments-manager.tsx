'use client';

import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import { Card } from '../ui/card';
import { Select } from '../ui/select';
import { Input } from '../ui/input';
import { Button } from '../ui/button';
import { apiClient } from '../../src/lib/api-client';
import type {
  PagedResult,
  ProductDto,
  WarehouseDto,
  WarehouseProductVariantDto
} from '../../src/types/api';

interface FormState {
  variantId: string;
  minimumQuantity: string;
  targetQuantity: string;
}

const initialFormState: FormState = {
  variantId: '',
  minimumQuantity: '0',
  targetQuantity: '0'
};

interface WarehouseAssignmentsManagerProps {
  initialWarehouseId?: number;
  reloadToken?: number;
}

export function WarehouseAssignmentsManager({
  initialWarehouseId,
  reloadToken
}: WarehouseAssignmentsManagerProps) {
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([]);
  const [selectedWarehouseId, setSelectedWarehouseId] = useState<number | null>(
    initialWarehouseId ?? null
  );
  const [assignments, setAssignments] = useState<WarehouseProductVariantDto[]>([]);
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [form, setForm] = useState<FormState>(initialFormState);
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [editingAssignment, setEditingAssignment] = useState<WarehouseProductVariantDto | null>(
    null
  );

  const loadProducts = useCallback(async () => {
    try {
      const response = await apiClient.get<PagedResult<ProductDto>>('/products', {
        params: { isActive: true, pageSize: 200 }
      });
      setProducts(response.data.items ?? []);
    } catch (err) {
      console.error(err);
      setError('No se pudieron cargar los productos para asignar.');
    }
  }, []);

  const loadWarehouses = useCallback(async () => {
    setError(null);

    try {
      const response = await apiClient.get<WarehouseDto[]>('/warehouses');
      const fetched = response.data;
      setWarehouses(fetched);

      setSelectedWarehouseId((current) => {
        if (initialWarehouseId && fetched.some((warehouse) => warehouse.id === initialWarehouseId)) {
          return initialWarehouseId;
        }

        if (current && fetched.some((warehouse) => warehouse.id === current)) {
          return current;
        }

        return fetched.length > 0 ? fetched[0].id : null;
      });
    } catch (err) {
      console.error(err);
      setError('No se pudieron cargar los almacenes disponibles.');
      setWarehouses([]);
      setSelectedWarehouseId(null);
    }
  }, [initialWarehouseId]);

  const loadAssignments = useCallback(async (warehouseId: number) => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await apiClient.get<WarehouseProductVariantDto[]>(
        `/warehouses/${warehouseId}/product-variants`
      );
      setAssignments(response.data);
    } catch (err) {
      console.error(err);
      setError('No se pudieron cargar las asignaciones del almacén.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadProducts();
  }, [loadProducts]);

  useEffect(() => {
    void loadWarehouses();
  }, [loadWarehouses, reloadToken]);

  useEffect(() => {
    if (selectedWarehouseId === null) {
      setAssignments([]);
      return;
    }

    void loadAssignments(selectedWarehouseId);
  }, [loadAssignments, selectedWarehouseId]);

  useEffect(() => {
    if (!initialWarehouseId) {
      return;
    }

    if (warehouses.some((warehouse) => warehouse.id === initialWarehouseId)) {
      setSelectedWarehouseId(initialWarehouseId);
    }
  }, [initialWarehouseId, warehouses]);

  const variantOptions = useMemo(() => {
    return products.flatMap((product) =>
      product.variants.map((variant) => ({
        id: variant.id,
        label: `${variant.sku} · ${product.name}`
      }))
    );
  }, [products]);

  const selectedWarehouse = useMemo(
    () => warehouses.find((warehouse) => warehouse.id === selectedWarehouseId) ?? null,
    [warehouses, selectedWarehouseId]
  );

  function resetForm() {
    setForm(initialFormState);
    setEditingAssignment(null);
  }

  function handleFieldChange(key: keyof FormState, value: string) {
    setForm((previous) => ({
      ...previous,
      [key]: value
    }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!selectedWarehouseId) {
      setError('Selecciona un almacén antes de guardar.');
      return;
    }

    const variantId = Number.parseInt(form.variantId, 10);
    const minimumQuantity = Number.parseFloat(form.minimumQuantity);
    const targetQuantity = Number.parseFloat(form.targetQuantity);

    if (!Number.isFinite(variantId) || variantId <= 0) {
      setError('Selecciona un producto válido para asignar.');
      return;
    }

    if (!Number.isFinite(minimumQuantity) || minimumQuantity < 0) {
      setError('La cantidad mínima debe ser un número mayor o igual a cero.');
      return;
    }

    if (!Number.isFinite(targetQuantity) || targetQuantity < minimumQuantity) {
      setError('La cantidad objetivo no puede ser inferior a la mínima.');
      return;
    }

    setIsSaving(true);
    setError(null);
    setFeedback(null);

    try {
      if (editingAssignment) {
        await apiClient.put<WarehouseProductVariantDto>(
          `/warehouses/${selectedWarehouseId}/product-variants/${editingAssignment.id}`,
          {
            minimumQuantity,
            targetQuantity
          }
        );
        setFeedback('Asignación actualizada correctamente.');
      } else {
        await apiClient.post<WarehouseProductVariantDto>(
          `/warehouses/${selectedWarehouseId}/product-variants`,
          {
            variantId,
            minimumQuantity,
            targetQuantity
          }
        );
        setFeedback('Producto asignado al almacén.');
      }

      await loadAssignments(selectedWarehouseId);
      resetForm();
    } catch (err) {
      console.error(err);
      setError('No se pudo guardar la asignación. Verifica los datos e intenta nuevamente.');
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDelete(assignmentId: number) {
    if (!selectedWarehouseId) {
      return;
    }

    const shouldDelete = window.confirm('¿Eliminar la asignación seleccionada?');
    if (!shouldDelete) {
      return;
    }

    try {
      await apiClient.delete(`/warehouses/${selectedWarehouseId}/product-variants/${assignmentId}`);
      setFeedback('Asignación eliminada.');
      await loadAssignments(selectedWarehouseId);
    } catch (err) {
      console.error(err);
      setError('No se pudo eliminar la asignación.');
    }
  }

  function beginEdit(assignment: WarehouseProductVariantDto) {
    setEditingAssignment(assignment);
    setForm({
      variantId: assignment.variantId.toString(),
      minimumQuantity: assignment.minimumQuantity.toString(),
      targetQuantity: assignment.targetQuantity.toString()
    });
  }

  const warehouseDescription = selectedWarehouse?.description ?? selectedWarehouse?.address ?? '';
  const hasWarehouses = warehouses.length > 0;

  return (
    <div className="flex flex-col gap-6">
      <Card
        title="Asignaciones de productos por almacén"
        subtitle="Gestiona los mínimos y objetivos de inventario para cada combinación"
      >
        {hasWarehouses ? (
          <div className="flex flex-col gap-4">
            <Select
              label="Almacén"
              value={selectedWarehouseId?.toString() ?? ''}
              onChange={(event) =>
                setSelectedWarehouseId(Number.parseInt(event.target.value, 10) || null)
              }
            >
              <option value="" disabled>
                Selecciona un almacén
              </option>
              {warehouses.map((warehouse) => (
                <option key={warehouse.id} value={warehouse.id}>
                  {warehouse.name}
                </option>
              ))}
            </Select>

            {warehouseDescription && (
              <p className="text-xs text-slate-500">{warehouseDescription}</p>
            )}

            <form
              className="grid gap-4 md:grid-cols-[minmax(0,2fr),repeat(2,minmax(0,1fr)),auto] md:items-end"
              onSubmit={handleSubmit}
            >
              <Select
                label="Producto"
                value={form.variantId}
                onChange={(event) => handleFieldChange('variantId', event.target.value)}
                disabled={variantOptions.length === 0 || Boolean(editingAssignment)}
                required={!editingAssignment}
              >
                <option value="" disabled>
                  Selecciona una variante
                </option>
                {variantOptions.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.label}
                  </option>
                ))}
              </Select>

              <Input
                label="Cantidad mínima"
                type="number"
                min={0}
                step="0.01"
                value={form.minimumQuantity}
                onChange={(event) => handleFieldChange('minimumQuantity', event.target.value)}
                required
              />

              <Input
                label="Cantidad objetivo"
                type="number"
                min={form.minimumQuantity}
                step="0.01"
                value={form.targetQuantity}
                onChange={(event) => handleFieldChange('targetQuantity', event.target.value)}
                required
              />

              <div className="flex w-full flex-col gap-1 md:self-stretch">
                <span className="text-sm font-medium text-slate-700 md:invisible">Acciones</span>
                <div className="flex flex-col gap-2 md:flex-row md:justify-end">
                  <Button type="submit" className="w-full md:w-auto" disabled={isSaving}>
                    {editingAssignment ? 'Actualizar' : 'Asignar'}
                  </Button>
                  {editingAssignment && (
                    <Button
                      type="button"
                      variant="secondary"
                      className="w-full md:w-auto"
                      onClick={resetForm}
                    >
                      Cancelar
                    </Button>
                  )}
                </div>
                <div aria-hidden="true" className="min-h-[1.25rem]" />
              </div>
            </form>

            {feedback && <p className="text-xs text-emerald-600">{feedback}</p>}
            {error && <p className="text-xs text-red-500">{error}</p>}
          </div>
        ) : (
          <p className="text-sm text-slate-500">
            Crea un almacén para comenzar a asignar productos y definir niveles de inventario.
          </p>
        )}
      </Card>

      <Card title="Productos vinculados" subtitle="Visualiza los niveles mínimos y objetivo por SKU">
        {selectedWarehouseId === null ? (
          <p className="text-sm text-slate-500">Selecciona un almacén para ver sus asignaciones.</p>
        ) : isLoading ? (
          <p className="text-sm text-slate-500">Cargando asignaciones…</p>
        ) : assignments.length === 0 ? (
          <p className="text-sm text-slate-500">Este almacén no tiene productos asignados.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200 text-sm">
              <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                <tr>
                  <th className="px-4 py-3 text-left">SKU</th>
                  <th className="px-4 py-3 text-left">Producto</th>
                  <th className="px-4 py-3 text-right">Mínimo</th>
                  <th className="px-4 py-3 text-right">Objetivo</th>
                  <th className="px-4 py-3 text-right">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {assignments.map((assignment) => (
                  <tr key={assignment.id}>
                    <td className="px-4 py-2 font-mono text-xs text-slate-600">{assignment.variantSku}</td>
                    <td className="px-4 py-2 text-slate-700">{assignment.productName}</td>
                    <td className="px-4 py-2 text-right">
                      {assignment.minimumQuantity.toLocaleString('es-ES')}
                    </td>
                    <td className="px-4 py-2 text-right">
                      {assignment.targetQuantity.toLocaleString('es-ES')}
                    </td>
                    <td className="px-4 py-2 text-right">
                      <div className="flex justify-end gap-2">
                        <Button type="button" variant="secondary" onClick={() => beginEdit(assignment)}>
                          Editar
                        </Button>
                        <Button
                          type="button"
                          variant="outline"
                          className="border-red-200 text-red-600 hover:bg-red-50"
                          onClick={() => handleDelete(assignment.id)}
                        >
                          Eliminar
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
}
