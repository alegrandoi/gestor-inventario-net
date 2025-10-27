'use client';

import { FormEvent, useEffect, useState } from 'react';
import { Card } from '../../../../components/ui/card';
import { Input } from '../../../../components/ui/input';
import { Textarea } from '../../../../components/ui/textarea';
import { Button } from '../../../../components/ui/button';
import { WarehouseAssignmentsManager } from '../../../../components/warehouses/warehouse-assignments-manager';
import { apiClient } from '../../../../src/lib/api-client';
import type { WarehouseDto } from '../../../../src/types/api';

interface FormState {
  name: string;
  address: string;
  description: string;
}

const initialFormState: FormState = {
  name: '',
  address: '',
  description: ''
};

export default function WarehousesPage() {
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([]);
  const [form, setForm] = useState<FormState>(initialFormState);
  const [editingWarehouseId, setEditingWarehouseId] = useState<number | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [formFeedback, setFormFeedback] = useState<string | null>(null);
  const [assignmentsFocusId, setAssignmentsFocusId] = useState<number | undefined>(undefined);
  const [assignmentsReloadToken, setAssignmentsReloadToken] = useState(0);

  useEffect(() => {
    void loadWarehousesList();
  }, []);

  async function loadWarehousesList(preferredWarehouseId?: number) {
    setIsLoading(true);
    setLoadError(null);

    try {
      const response = await apiClient.get<WarehouseDto[]>('/warehouses');
      const fetched = response.data;
      setWarehouses(fetched);

      setAssignmentsFocusId((current) => {
        if (preferredWarehouseId && fetched.some((warehouse) => warehouse.id === preferredWarehouseId)) {
          return preferredWarehouseId;
        }

        if (current && fetched.some((warehouse) => warehouse.id === current)) {
          return current;
        }

        return fetched.length > 0 ? fetched[0].id : undefined;
      });
    } catch (err) {
      console.error(err);
      setLoadError('No se pudieron cargar los almacenes.');
      setWarehouses([]);
      setAssignmentsFocusId(undefined);
    } finally {
      setIsLoading(false);
    }
  }

  function resetForm() {
    setForm(initialFormState);
    setEditingWarehouseId(null);
    setFormError(null);
  }

  function beginEdit(warehouse: WarehouseDto) {
    setEditingWarehouseId(warehouse.id);
    setForm({
      name: warehouse.name,
      address: warehouse.address ?? '',
      description: warehouse.description ?? ''
    });
    setFormFeedback(null);
    setFormError(null);
  }

  function handleFieldChange(key: keyof FormState, value: string) {
    setForm((previous) => ({
      ...previous,
      [key]: value
    }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const trimmedName = form.name.trim();
    if (trimmedName.length === 0) {
      setFormError('El nombre del almacén es obligatorio.');
      return;
    }

    if (trimmedName.length > 100) {
      setFormError('El nombre del almacén no puede superar los 100 caracteres.');
      return;
    }

    if (form.address.length > 200) {
      setFormError('La dirección no puede superar los 200 caracteres.');
      return;
    }

    if (form.description.length > 200) {
      setFormError('La descripción no puede superar los 200 caracteres.');
      return;
    }

    setIsSubmitting(true);
    setFormError(null);
    setFormFeedback(null);

    try {
      if (editingWarehouseId) {
        await apiClient.put<WarehouseDto>(`/warehouses/${editingWarehouseId}`, {
          name: trimmedName,
          address: form.address.trim() || null,
          description: form.description.trim() || null
        });
        setFormFeedback('Almacén actualizado correctamente.');
        await loadWarehousesList(editingWarehouseId);
        setAssignmentsReloadToken((value) => value + 1);
        setAssignmentsFocusId(editingWarehouseId);
      } else {
        const response = await apiClient.post<WarehouseDto>('/warehouses', {
          name: trimmedName,
          address: form.address.trim() || null,
          description: form.description.trim() || null
        });
        const createdWarehouse = response.data;
        setFormFeedback('Almacén creado correctamente.');
        await loadWarehousesList(createdWarehouse.id);
        setAssignmentsReloadToken((value) => value + 1);
        setAssignmentsFocusId(createdWarehouse.id);
      }

      resetForm();
    } catch (err) {
      console.error(err);
      setFormError('No se pudo guardar el almacén. Inténtalo nuevamente.');
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleDelete(warehouseId: number) {
    const shouldDelete = window.confirm('¿Eliminar el almacén seleccionado?');
    if (!shouldDelete) {
      return;
    }

    setIsSubmitting(true);
    setFormFeedback(null);
    setFormError(null);

    try {
      await apiClient.delete(`/warehouses/${warehouseId}`);
      setFormFeedback('Almacén eliminado correctamente.');
      await loadWarehousesList();
      setAssignmentsReloadToken((value) => value + 1);
    } catch (err) {
      console.error(err);
      setFormError('No se pudo eliminar el almacén.');
    } finally {
      setIsSubmitting(false);
      if (editingWarehouseId === warehouseId) {
        resetForm();
      }
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="grid gap-6 xl:grid-cols-[1.1fr,1.2fr]">
        <Card
          title={editingWarehouseId ? 'Editar almacén' : 'Crear nuevo almacén'}
          subtitle="Gestiona la red de almacenes y mantén actualizada su información de contacto"
        >
          <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
            <Input
              label="Nombre"
              value={form.name}
              onChange={(event) => handleFieldChange('name', event.target.value)}
              placeholder="Almacén Central"
              required
            />
            <Input
              label="Dirección"
              value={form.address}
              onChange={(event) => handleFieldChange('address', event.target.value)}
              placeholder="Calle y número"
            />
            <Textarea
              label="Descripción"
              value={form.description}
              onChange={(event) => handleFieldChange('description', event.target.value)}
              placeholder="Notas u horarios de operación"
              rows={3}
              className="min-h-[120px] resize-y leading-relaxed"
            />
            <div className="flex flex-wrap gap-2">
              <Button type="submit" disabled={isSubmitting}>
                {editingWarehouseId ? 'Actualizar' : 'Guardar'}
              </Button>
              {editingWarehouseId && (
                <Button type="button" variant="secondary" onClick={resetForm} disabled={isSubmitting}>
                  Cancelar
                </Button>
              )}
            </div>
            {formFeedback && <p className="text-xs text-emerald-600">{formFeedback}</p>}
            {formError && <p className="text-xs text-red-500">{formError}</p>}
          </form>
        </Card>

        <Card
          title="Almacenes registrados"
          subtitle="Consulta y gestiona las ubicaciones disponibles"
        >
          {isLoading ? (
            <p className="text-sm text-slate-500">Cargando almacenes…</p>
          ) : warehouses.length === 0 ? (
            <p className="text-sm text-slate-500">Todavía no hay almacenes registrados.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-slate-200 text-sm">
                <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                  <tr>
                    <th className="px-4 py-3 text-left align-top">Nombre</th>
                    <th className="px-4 py-3 text-left align-top">Dirección</th>
                    <th className="px-4 py-3 text-left align-top">Descripción</th>
                    <th className="px-4 py-3 text-right align-top">Productos</th>
                    <th className="px-4 py-3 text-right align-top">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {warehouses.map((warehouse) => (
                    <tr key={warehouse.id}>
                      <td className="px-4 py-2 align-top font-medium text-slate-900 break-words">{warehouse.name}</td>
                      <td className="px-4 py-2 align-top text-slate-600 break-words">{warehouse.address ?? '—'}</td>
                      <td className="px-4 py-2 align-top text-slate-600 break-words whitespace-pre-line hyphens-auto">
                        {warehouse.description ?? '—'}
                      </td>
                      <td className="px-4 py-2 align-top text-right">
                        {warehouse.productVariants?.length ?? 0}
                      </td>
                      <td className="px-4 py-2 align-top">
                        <div className="flex flex-wrap items-start justify-end gap-2">
                          <Button
                            type="button"
                            variant="secondary"
                            onClick={() => beginEdit(warehouse)}
                            disabled={isSubmitting}
                          >
                            Editar
                          </Button>
                          <Button
                            type="button"
                            variant="outline"
                            className="border-slate-200 text-primary-600 hover:bg-primary-50"
                            onClick={() => {
                              setAssignmentsFocusId(warehouse.id);
                            }}
                          >
                            Gestionar productos
                          </Button>
                          <Button
                            type="button"
                            variant="outline"
                            className="border-red-200 text-red-600 hover:bg-red-50"
                            onClick={() => handleDelete(warehouse.id)}
                            disabled={isSubmitting}
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
          {loadError && <p className="mt-3 text-xs text-red-500">{loadError}</p>}
        </Card>
      </div>

      <WarehouseAssignmentsManager
        initialWarehouseId={assignmentsFocusId}
        reloadToken={assignmentsReloadToken}
      />
    </div>
  );
}
