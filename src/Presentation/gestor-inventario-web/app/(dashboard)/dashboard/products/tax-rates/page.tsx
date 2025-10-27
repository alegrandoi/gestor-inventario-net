'use client';

import { FormEvent, ReactNode, useEffect, useMemo, useState, useId } from 'react';
import { CheckIcon, ExclamationTriangleIcon, PlusIcon, TrashIcon } from '@heroicons/react/24/outline';
import clsx from 'clsx';
import { Card } from '../../../../../components/ui/card';
import { Input } from '../../../../../components/ui/input';
import { Textarea } from '../../../../../components/ui/textarea';
import { Button } from '../../../../../components/ui/button';
import { apiClient } from '../../../../../src/lib/api-client';
import type { TaxRateDto } from '../../../../../src/types/api';

interface ModalProps {
  title: string;
  description?: string;
  onClose: () => void;
  children: ReactNode;
  disableClose?: boolean;
}

function Modal({ title, description, onClose, children, disableClose = false }: ModalProps) {
  const titleId = useId();
  const descriptionId = useId();

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 px-4 py-6 backdrop-blur-sm"
      role="dialog"
      aria-modal="true"
      aria-labelledby={titleId}
      aria-describedby={description ? descriptionId : undefined}
      onClick={() => {
        if (!disableClose) {
          onClose();
        }
      }}
    >
      <div
        className="w-full max-w-3xl overflow-hidden rounded-2xl bg-white p-6 shadow-xl"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 id={titleId} className="text-lg font-semibold text-slate-900">
              {title}
            </h2>
            {description && (
              <p id={descriptionId} className="mt-1 text-sm text-slate-500">
                {description}
              </p>
            )}
          </div>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => {
              if (!disableClose) {
                onClose();
              }
            }}
            aria-label="Cerrar"
            disabled={disableClose}
          >
            Cerrar
          </Button>
        </div>

        <div className="mt-6 max-h-[70vh] overflow-y-auto pr-1">{children}</div>
      </div>
    </div>
  );
}

interface TaxRateFormState {
  id?: number;
  name: string;
  rate: string;
  region: string;
  description: string;
}

const initialFormState: TaxRateFormState = {
  name: '',
  rate: '',
  region: '',
  description: ''
};

export default function TaxRatesPage() {
  const [taxRates, setTaxRates] = useState<TaxRateDto[]>([]);
  const [form, setForm] = useState<TaxRateFormState>(initialFormState);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [isFormModalOpen, setIsFormModalOpen] = useState(false);
  const [search, setSearch] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    async function fetchTaxRates() {
      setError(null);
      try {
        const response = await apiClient.get<TaxRateDto[]>('/taxrates');
        setTaxRates(response.data ?? []);
      } catch (err) {
        console.error(err);
        setError('No se pudieron obtener las tarifas de impuestos.');
      } finally {
        setIsLoading(false);
      }
    }

    fetchTaxRates().catch((err) => console.error(err));
  }, []);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) {
      return taxRates;
    }

    return taxRates.filter((rate) =>
      rate.name.toLowerCase().includes(term) || rate.region?.toLowerCase().includes(term)
    );
  }, [search, taxRates]);

  function resetFormState() {
    setForm(initialFormState);
  }

  function closeFormModal() {
    setIsFormModalOpen(false);
    if (selectedId == null) {
      resetFormState();
    }
  }

  function startCreate() {
    setSelectedId(null);
    resetFormState();
    setSuccess(null);
    setError(null);
    setIsFormModalOpen(true);
  }

  function selectRate(rate: TaxRateDto, options?: { preserveFeedback?: boolean }) {
    setSelectedId(rate.id);
    setForm({
      id: rate.id,
      name: rate.name,
      rate: rate.rate.toString(),
      region: rate.region ?? '',
      description: rate.description ?? ''
    });
    setError(null);
    if (!options?.preserveFeedback) {
      setSuccess(null);
    }
    setIsFormModalOpen(true);
  }

  function updateFormField<Key extends keyof TaxRateFormState>(field: Key, value: TaxRateFormState[Key]) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  async function reload(preserveId?: number | null, options?: { silent?: boolean }) {
    try {
      const response = await apiClient.get<TaxRateDto[]>('/taxrates');
      const fetchedRates = response.data ?? [];
      setTaxRates(fetchedRates);

      if (preserveId) {
        const match = fetchedRates.find((item) => item.id === preserveId);
        if (match) {
          selectRate(match, { preserveFeedback: true });
        } else {
          setSelectedId(null);
          resetFormState();
          closeFormModal();
        }
      } else if (selectedId != null) {
        const match = fetchedRates.find((item) => item.id === selectedId);
        if (!match) {
          setSelectedId(null);
          resetFormState();
          closeFormModal();
        }
      }
    } catch (err) {
      console.error(err);
      if (!options?.silent) {
        setError('No se pudieron actualizar las tarifas.');
      }
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    const normalizedRate = form.rate.replace(',', '.');
    const parsedRate = Number.parseFloat(normalizedRate);
    if (Number.isNaN(parsedRate) || parsedRate < 0) {
      setError('Introduce un porcentaje válido.');
      return;
    }

    const payload = {
      name: form.name.trim(),
      rate: parsedRate,
      region: form.region.trim() || undefined,
      description: form.description.trim() || undefined
    };

    setIsSaving(true);
    try {
      if (form.id) {
        await apiClient.put(`/taxrates/${form.id}`, payload);
        setSuccess('Tarifa actualizada correctamente.');
        await reload(form.id);
        closeFormModal();
      } else {
        await apiClient.post<TaxRateDto>('/taxrates', payload);
        setSuccess('Tarifa creada correctamente.');
        await reload();
        setSelectedId(null);
        resetFormState();
        closeFormModal();
      }
    } catch (err) {
      console.error(err);
      setError('No se pudo guardar la tarifa.');
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDelete(rate: TaxRateDto) {
    if (isDeleting) {
      return;
    }

    const confirmed = window.confirm(`¿Eliminar la tarifa "${rate.name}"?`);
    if (!confirmed) {
      return;
    }

    setIsDeleting(true);
    setError(null);
    setSuccess(null);

    try {
      await apiClient.delete(`/taxrates/${rate.id}`);
      setTaxRates((prev) => prev.filter((item) => item.id !== rate.id));
      setSuccess('Tarifa eliminada.');
      if (selectedId === rate.id) {
        setSelectedId(null);
        resetFormState();
        closeFormModal();
      }
      await reload(undefined, { silent: true });
    } catch (err) {
      console.error(err);
      setError('No se pudo eliminar la tarifa seleccionada.');
    } finally {
      setIsDeleting(false);
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">Gestión de impuestos</h1>
          <p className="text-sm text-slate-500">
            Define tarifas por región para aplicar automáticamente los impuestos en ventas y compras.
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Button type="button" variant="secondary" onClick={startCreate}>
            <PlusIcon className="mr-2 h-4 w-4" /> Nueva tarifa
          </Button>
          <Input
            type="search"
            placeholder="Buscar por nombre o región"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            className="w-56"
          />
        </div>
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
          <ExclamationTriangleIcon className="h-5 w-5" />
          <span>{error}</span>
        </div>
      )}

      {success && (
        <div className="flex items-center gap-2 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
          <CheckIcon className="h-5 w-5" />
          <span>{success}</span>
        </div>
      )}

      <Card title="Tarifas registradas" subtitle="Selecciona una tarifa para editar sus valores">
        {isLoading ? (
          <p className="px-3 py-8 text-center text-sm text-slate-500">Cargando tarifas…</p>
        ) : filtered.length === 0 ? (
          <p className="px-3 py-8 text-center text-sm text-slate-500">No se encontraron tarifas.</p>
        ) : (
          <ul className="divide-y divide-slate-100">
            {filtered.map((rate) => {
              const isActive = selectedId === rate.id;
              return (
                <li key={rate.id} className="flex items-center justify-between gap-4 px-3 py-3">
                  <button
                    type="button"
                    className={clsx(
                      'flex-1 text-left text-sm transition',
                      isActive ? 'font-semibold text-primary-700' : 'text-slate-700 hover:text-primary-600'
                    )}
                    onClick={() => selectRate(rate)}
                  >
                    <span className="block">{rate.name}</span>
                    <span className="text-xs text-slate-500">
                      {rate.rate.toLocaleString('es-ES', { minimumFractionDigits: 0, maximumFractionDigits: 2 })}%
                      {rate.region ? ` · ${rate.region}` : ''}
                    </span>
                  </button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => handleDelete(rate)}
                    disabled={isDeleting || isSaving}
                    aria-label={`Eliminar ${rate.name}`}
                  >
                    <TrashIcon className="h-4 w-4" />
                  </Button>
                </li>
              );
            })}
          </ul>
        )}
      </Card>

      {isFormModalOpen && (
        <Modal
          title={form.id ? 'Editar tarifa' : 'Nueva tarifa'}
          description={
            form.id
              ? 'Actualiza el porcentaje o la región asignada a la tarifa seleccionada.'
              : 'Crea una tarifa para aplicar impuestos en pedidos y facturas.'
          }
          onClose={closeFormModal}
          disableClose={isSaving}
        >
          <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
            <Input
              label="Nombre"
              required
              value={form.name}
              onChange={(event) => updateFormField('name', event.target.value)}
              placeholder="Ej. IVA general"
              disabled={isSaving}
            />
            <Input
              label="Porcentaje"
              required
              value={form.rate}
              onChange={(event) => updateFormField('rate', event.target.value)}
              placeholder="Ej. 21"
              disabled={isSaving}
            />
            <Input
              label="Región"
              value={form.region}
              onChange={(event) => updateFormField('region', event.target.value)}
              placeholder="Ej. España"
              disabled={isSaving}
            />
            <Textarea
              label="Descripción"
              value={form.description}
              onChange={(event) => updateFormField('description', event.target.value)}
              placeholder="Detalles adicionales sobre cuándo aplicar la tarifa"
              rows={3}
              disabled={isSaving}
            />
            <div className="flex flex-wrap items-center gap-3">
              <Button type="submit" disabled={isSaving}>
                {isSaving ? 'Guardando…' : 'Guardar cambios'}
              </Button>
              <Button type="button" variant="ghost" onClick={closeFormModal} disabled={isSaving}>
                Cancelar
              </Button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
