'use client';

import { FormEvent, ReactNode, useEffect, useMemo, useState, useId } from 'react';
import { CheckIcon, ExclamationTriangleIcon, PencilSquareIcon, PlusIcon, TrashIcon } from '@heroicons/react/24/outline';
import clsx from 'clsx';
import { Card } from '../../../../../components/ui/card';
import { Input } from '../../../../../components/ui/input';
import { Textarea } from '../../../../../components/ui/textarea';
import { Button } from '../../../../../components/ui/button';
import { apiClient } from '../../../../../src/lib/api-client';
import type { ProductAttributeGroupDto, ProductAttributeValueDto } from '../../../../../src/types/api';

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
        className="w-full max-w-4xl overflow-hidden rounded-2xl bg-white p-6 shadow-xl"
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

        <div className="mt-6 max-h-[75vh] overflow-y-auto pr-1">{children}</div>
      </div>
    </div>
  );
}

interface GroupFormState {
  id?: number;
  name: string;
  description: string;
  allowCustomValues: boolean;
}

interface ValueFormState {
  id?: number;
  name: string;
  description: string;
  hexColor: string;
  displayOrder: string;
  isActive: boolean;
}

const emptyGroup: GroupFormState = {
  name: '',
  description: '',
  allowCustomValues: false
};

const emptyValue: ValueFormState = {
  name: '',
  description: '',
  hexColor: '',
  displayOrder: '',
  isActive: true
};

export default function ProductAttributesPage() {
  const [groups, setGroups] = useState<ProductAttributeGroupDto[]>([]);
  const [groupForm, setGroupForm] = useState<GroupFormState>(emptyGroup);
  const [valueForm, setValueForm] = useState<ValueFormState>(emptyValue);
  const [selectedGroupId, setSelectedGroupId] = useState<number | null>(null);
  const [isGroupModalOpen, setIsGroupModalOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [isSavingGroup, setIsSavingGroup] = useState(false);
  const [isSavingValue, setIsSavingValue] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const colorPickerValue = useMemo(() => {
    const trimmed = valueForm.hexColor.trim();

    if (/^#[0-9a-fA-F]{6}$/.test(trimmed)) {
      return trimmed.toUpperCase();
    }

    if (/^[0-9a-fA-F]{6}$/.test(trimmed)) {
      return `#${trimmed.toUpperCase()}`;
    }

    return '#000000';
  }, [valueForm.hexColor]);

  useEffect(() => {
    async function fetchGroups() {
      setError(null);
      try {
        const response = await apiClient.get<ProductAttributeGroupDto[]>('/product-attributes');
        setGroups(response.data ?? []);
      } catch (err) {
        console.error(err);
        setError('No se pudieron obtener los atributos de producto.');
      } finally {
        setIsLoading(false);
      }
    }

    fetchGroups().catch((err) => console.error(err));
  }, []);

  const selectedGroup = useMemo(() => {
    if (selectedGroupId == null) {
      return undefined;
    }

    return groups.find((group) => group.id === selectedGroupId);
  }, [groups, selectedGroupId]);

  function resetForms() {
    setGroupForm(emptyGroup);
    setValueForm(emptyValue);
  }

  function closeGroupModal() {
    setIsGroupModalOpen(false);
    setValueForm(emptyValue);
    if (selectedGroupId == null) {
      setGroupForm(emptyGroup);
    }
  }

  function startCreateGroup() {
    setSelectedGroupId(null);
    resetForms();
    setSuccess(null);
    setError(null);
    setIsGroupModalOpen(true);
  }

  function updateGroupForm<Key extends keyof GroupFormState>(field: Key, value: GroupFormState[Key]) {
    setGroupForm((prev) => ({ ...prev, [field]: value }));
  }

  function updateValueForm<Key extends keyof ValueFormState>(field: Key, value: ValueFormState[Key]) {
    setValueForm((prev) => ({ ...prev, [field]: value }));
  }

  function handleHexColorChange(value: string) {
    updateValueForm('hexColor', value.toUpperCase());
  }

  function selectGroup(group: ProductAttributeGroupDto, options?: { preserveFeedback?: boolean }) {
    setSelectedGroupId(group.id);
    setGroupForm({
      id: group.id,
      name: group.name,
      description: group.description ?? '',
      allowCustomValues: group.allowCustomValues
    });
    setValueForm(emptyValue);
    setError(null);
    if (!options?.preserveFeedback) {
      setSuccess(null);
    }
    setIsGroupModalOpen(true);
  }

  async function reloadGroups(preserveSelection?: number | null, options?: { silent?: boolean }) {
    try {
      const response = await apiClient.get<ProductAttributeGroupDto[]>('/product-attributes');
      const fetchedGroups = response.data ?? [];
      setGroups(fetchedGroups);
      if (preserveSelection) {
        const match = fetchedGroups.find((group) => group.id === preserveSelection);
        if (match) {
          selectGroup(match, { preserveFeedback: true });
        } else {
          setSelectedGroupId(null);
          resetForms();
          closeGroupModal();
        }
      } else if (selectedGroupId != null) {
        const match = fetchedGroups.find((group) => group.id === selectedGroupId);
        if (!match) {
          setSelectedGroupId(null);
          resetForms();
          closeGroupModal();
        }
      }
    } catch (err) {
      console.error(err);
      if (!options?.silent) {
        setError('No se pudieron actualizar los atributos.');
      }
    }
  }

  async function handleGroupSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    if (groupForm.name.trim().length === 0) {
      setError('El nombre del grupo es obligatorio.');
      return;
    }

    const payload = {
      name: groupForm.name.trim(),
      description: groupForm.description.trim() || undefined,
      allowCustomValues: groupForm.allowCustomValues
    };

    setIsSavingGroup(true);
    try {
      if (groupForm.id) {
        await apiClient.put(`/product-attributes/${groupForm.id}`, payload);
        setSuccess('Grupo actualizado correctamente.');
        await reloadGroups(groupForm.id);
      } else {
        await apiClient.post<ProductAttributeGroupDto>('/product-attributes', payload);
        setSuccess('Grupo creado correctamente.');
        await reloadGroups();
        setSelectedGroupId(null);
        resetForms();
        closeGroupModal();
      }
    } catch (err) {
      console.error(err);
      setError('No se pudo guardar el grupo.');
    } finally {
      setIsSavingGroup(false);
    }
  }

  async function handleGroupDelete(group: ProductAttributeGroupDto) {
    if (isDeleting) {
      return;
    }

    const confirmed = window.confirm(`¿Eliminar el grupo "${group.name}" y todos sus valores asociados?`);
    if (!confirmed) {
      return;
    }

    setIsDeleting(true);
    setError(null);
    setSuccess(null);

    try {
      await apiClient.delete(`/product-attributes/${group.id}`);
      setGroups((prev) => prev.filter((item) => item.id !== group.id));
      setSuccess('Grupo eliminado.');
      if (selectedGroupId === group.id) {
        setSelectedGroupId(null);
        resetForms();
        closeGroupModal();
      }
      await reloadGroups(undefined, { silent: true });
    } catch (err) {
      console.error(err);
      setError('No se pudo eliminar el grupo seleccionado.');
    } finally {
      setIsDeleting(false);
    }
  }

  async function handleValueSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedGroupId) {
      setError('Selecciona un grupo para gestionar sus valores.');
      return;
    }

    const trimmedName = valueForm.name.trim();
    if (!trimmedName) {
      setError('El nombre del valor es obligatorio.');
      return;
    }

    const payload = {
      name: trimmedName,
      description: valueForm.description.trim() || undefined,
      hexColor: valueForm.hexColor.trim() || undefined,
      displayOrder: valueForm.displayOrder.trim().length > 0 ? Number.parseInt(valueForm.displayOrder, 10) : undefined,
      isActive: valueForm.isActive
    };

    if (payload.displayOrder != null && Number.isNaN(payload.displayOrder)) {
      setError('El orden de visualización debe ser un número entero.');
      return;
    }

    if (payload.hexColor && !/^#?[0-9a-fA-F]{6}$/.test(payload.hexColor)) {
      setError('El color debe tener el formato hexadecimal RRGGBB.');
      return;
    }

    setIsSavingValue(true);
    setError(null);
    setSuccess(null);

    try {
      if (valueForm.id) {
        await apiClient.put(`/product-attributes/${selectedGroupId}/values/${valueForm.id}`, payload);
        setSuccess('Valor actualizado correctamente.');
      } else {
        await apiClient.post(`/product-attributes/${selectedGroupId}/values`, {
          ...payload,
          displayOrder: payload.displayOrder
        });
        setSuccess('Valor agregado correctamente.');
      }

      setValueForm(emptyValue);
      await reloadGroups(selectedGroupId);
    } catch (err) {
      console.error(err);
      setError('No se pudo guardar el valor.');
    } finally {
      setIsSavingValue(false);
    }
  }

  async function handleValueEdit(value: ProductAttributeValueDto) {
    setValueForm({
      id: value.id,
      name: value.name,
      description: value.description ?? '',
      hexColor: value.hexColor ?? '',
      displayOrder: value.displayOrder.toString(),
      isActive: value.isActive
    });
  }

  async function handleValueDelete(value: ProductAttributeValueDto) {
    if (!selectedGroupId || isDeleting) {
      return;
    }

    const confirmed = window.confirm(`¿Eliminar el valor "${value.name}"?`);
    if (!confirmed) {
      return;
    }

    setIsDeleting(true);
    setError(null);
    setSuccess(null);

    try {
      await apiClient.delete(`/product-attributes/${selectedGroupId}/values/${value.id}`);
      setGroups((prev) =>
        prev.map((group) =>
          group.id === selectedGroupId
            ? { ...group, values: group.values.filter((item) => item.id !== value.id) }
            : group
        )
      );
      if (valueForm.id === value.id) {
        setValueForm(emptyValue);
      }
      setSuccess('Valor eliminado.');
      await reloadGroups(selectedGroupId, { silent: true });
    } catch (err) {
      console.error(err);
      setError('No se pudo eliminar el valor seleccionado.');
    } finally {
      setIsDeleting(false);
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">Atributos de producto</h1>
          <p className="text-sm text-slate-500">
            Gestiona opciones recurrentes como tallas, colores o materiales y reutilízalas al crear variantes.
          </p>
        </div>
        <Button type="button" variant="secondary" onClick={startCreateGroup}>
          <PlusIcon className="mr-2 h-4 w-4" /> Nuevo grupo
        </Button>
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

      <Card title="Grupos disponibles" subtitle="Selecciona un grupo para gestionar sus valores">
        {isLoading ? (
          <p className="px-3 py-8 text-center text-sm text-slate-500">Cargando atributos…</p>
        ) : groups.length === 0 ? (
          <p className="px-3 py-8 text-center text-sm text-slate-500">
            No has creado atributos todavía. Empieza añadiendo un grupo.
          </p>
        ) : (
          <ul className="divide-y divide-slate-100">
            {groups.map((group) => {
              const isActive = selectedGroupId === group.id;
              return (
                <li key={group.id} className="flex items-center justify-between gap-3 px-3 py-3">
                  <button
                    type="button"
                    onClick={() => selectGroup(group)}
                    className={clsx(
                      'flex-1 text-left text-sm transition',
                      isActive ? 'font-semibold text-primary-700' : 'text-slate-700 hover:text-primary-600'
                    )}
                  >
                    <span className="block">{group.name}</span>
                    <span className="text-xs text-slate-500">
                      {group.values.length} valores · {group.allowCustomValues ? 'admite valores libres' : 'solo valores definidos'}
                    </span>
                  </button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => handleGroupDelete(group)}
                    disabled={isDeleting}
                    aria-label={`Eliminar ${group.name}`}
                  >
                    <TrashIcon className="h-4 w-4" />
                  </Button>
                </li>
              );
            })}
          </ul>
        )}
      </Card>

      {isGroupModalOpen && (
        <Modal
          title={groupForm.id ? 'Editar grupo' : 'Nuevo grupo'}
          description={
            groupForm.id
              ? 'Modifica el nombre, la descripción o las opciones disponibles para el grupo seleccionado.'
              : 'Crea un grupo de atributos para clasificar tus variantes.'
          }
          onClose={closeGroupModal}
          disableClose={isSavingGroup || isSavingValue || isDeleting}
        >
          <div className="space-y-6">
            <form className="flex flex-col gap-4" onSubmit={handleGroupSubmit}>
              <Input
                label="Nombre"
                required
                value={groupForm.name}
                onChange={(event) => updateGroupForm('name', event.target.value)}
                placeholder="Ej. Talla"
                disabled={isSavingGroup}
              />
              <Textarea
                label="Descripción"
                value={groupForm.description}
                onChange={(event) => updateGroupForm('description', event.target.value)}
                placeholder="Describe cuándo se usa este atributo"
                rows={3}
                disabled={isSavingGroup}
              />
              <label className="flex items-center gap-2 text-sm text-slate-600">
                <input
                  type="checkbox"
                  checked={groupForm.allowCustomValues}
                  onChange={(event) => updateGroupForm('allowCustomValues', event.target.checked)}
                  className="h-4 w-4 rounded border border-slate-300 text-primary-600 focus:ring-primary-500"
                  disabled={isSavingGroup}
                />
                Permitir que los usuarios introduzcan valores personalizados
              </label>
              <div className="flex flex-wrap items-center gap-3">
                <Button type="submit" disabled={isSavingGroup}>
                  {isSavingGroup ? 'Guardando…' : 'Guardar grupo'}
                </Button>
                <Button type="button" variant="ghost" onClick={closeGroupModal} disabled={isSavingGroup}>
                  Cancelar
                </Button>
              </div>
            </form>

            {selectedGroup ? (
              <div className="space-y-5">
                {selectedGroup.values.length === 0 ? (
                  <p className="rounded-lg border border-dashed border-slate-200 bg-slate-50 px-4 py-6 text-center text-sm text-slate-500">
                    Aún no hay valores definidos. Agrega el primero con el formulario inferior.
                  </p>
                ) : (
                  <div className="overflow-hidden rounded-xl border border-slate-200">
                    <table className="min-w-full divide-y divide-slate-200 text-sm">
                      <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                        <tr>
                          <th className="px-3 py-2 text-left">Nombre</th>
                          <th className="px-3 py-2 text-left">Descripción</th>
                          <th className="px-3 py-2 text-left">Color</th>
                          <th className="px-3 py-2 text-right">Orden</th>
                          <th className="px-3 py-2 text-right">Estado</th>
                          <th className="px-3 py-2 text-right">Acciones</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100">
                        {selectedGroup.values.map((value) => (
                          <tr key={value.id} className="text-slate-700">
                            <td className="px-3 py-2 text-sm font-medium text-slate-900">{value.name}</td>
                            <td className="px-3 py-2 text-xs text-slate-500">{value.description || '—'}</td>
                            <td className="px-3 py-2 text-xs">
                              {value.hexColor ? (
                                <span className="inline-flex items-center gap-2">
                                  <span
                                    className="inline-flex h-4 w-4 rounded-full border border-slate-200"
                                    style={{ backgroundColor: value.hexColor.startsWith('#') ? value.hexColor : `#${value.hexColor}` }}
                                  />
                                  <span className="text-slate-500">{value.hexColor}</span>
                                </span>
                              ) : (
                                <span className="text-slate-400">—</span>
                              )}
                            </td>
                            <td className="px-3 py-2 text-right text-xs text-slate-500">{value.displayOrder}</td>
                            <td className="px-3 py-2 text-right">
                              <span
                                className={clsx(
                                  'inline-flex rounded-full px-2 py-1 text-xs font-medium',
                                  value.isActive
                                    ? 'bg-emerald-100 text-emerald-700'
                                    : 'bg-slate-200 text-slate-600'
                                )}
                              >
                                {value.isActive ? 'Activo' : 'Inactivo'}
                              </span>
                            </td>
                            <td className="px-3 py-2 text-right">
                              <div className="flex justify-end gap-2">
                                <Button
                                  type="button"
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => handleValueEdit(value)}
                                  aria-label={`Editar ${value.name}`}
                                >
                                  <PencilSquareIcon className="h-4 w-4" />
                                </Button>
                                <Button
                                  type="button"
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => handleValueDelete(value)}
                                  disabled={isDeleting}
                                  aria-label={`Eliminar ${value.name}`}
                                >
                                  <TrashIcon className="h-4 w-4" />
                                </Button>
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}

                <form className="space-y-4 rounded-xl border border-slate-200 bg-white p-4 shadow-sm" onSubmit={handleValueSubmit}>
                  <div className="grid gap-4 md:grid-cols-2">
                    <Input
                      label="Nombre"
                      required
                      value={valueForm.name}
                      onChange={(event) => updateValueForm('name', event.target.value)}
                      placeholder="Ej. Azul"
                      disabled={isSavingValue}
                    />
                    <Input
                      label="Orden de visualización"
                      value={valueForm.displayOrder}
                      onChange={(event) => updateValueForm('displayOrder', event.target.value)}
                      placeholder="Ej. 10"
                      disabled={isSavingValue}
                    />
                  </div>
                  <div className="grid gap-4 md:grid-cols-2">
                    <Textarea
                      label="Descripción"
                      value={valueForm.description}
                      onChange={(event) => updateValueForm('description', event.target.value)}
                      placeholder="Información adicional opcional"
                      rows={3}
                      disabled={isSavingValue}
                    />
                    <div className="flex flex-col gap-2">
                      <span className="text-sm font-medium text-slate-700">Color hexadecimal</span>
                      <div className="flex items-center gap-3">
                        <input
                          type="color"
                          value={colorPickerValue}
                          onChange={(event) => handleHexColorChange(event.target.value)}
                          className="h-10 w-16 cursor-pointer rounded border border-slate-200 bg-white p-0.5"
                          disabled={isSavingValue}
                          aria-label="Seleccionar color"
                        />
                        <Input
                          label="Valor"
                          value={valueForm.hexColor}
                          onChange={(event) => handleHexColorChange(event.target.value)}
                          placeholder="Ej. #FF5733"
                          disabled={isSavingValue}
                          className="flex-1"
                        />
                      </div>
                      <span className="text-xs text-slate-500">
                        Selecciona un color de la paleta o introduce un código hexadecimal válido.
                      </span>
                    </div>
                  </div>
                  <label className="flex items-center gap-2 text-sm text-slate-600">
                    <input
                      type="checkbox"
                      checked={valueForm.isActive}
                      onChange={(event) => updateValueForm('isActive', event.target.checked)}
                      className="h-4 w-4 rounded border border-slate-300 text-primary-600 focus:ring-primary-500"
                      disabled={isSavingValue}
                    />
                    Habilitar valor
                  </label>
                  <div className="flex flex-wrap items-center gap-3">
                    <Button type="submit" disabled={isSavingValue}>
                      {isSavingValue ? 'Guardando…' : valueForm.id ? 'Actualizar valor' : 'Agregar valor'}
                    </Button>
                    <Button type="button" variant="ghost" onClick={() => setValueForm(emptyValue)} disabled={isSavingValue}>
                      Limpiar formulario
                    </Button>
                  </div>
                </form>
              </div>
            ) : (
              <p className="rounded-lg border border-dashed border-slate-200 bg-slate-50 px-4 py-6 text-center text-sm text-slate-500">
                Selecciona un grupo para añadir o editar valores.
              </p>
            )}
          </div>
        </Modal>
      )}
    </div>
  );
}
