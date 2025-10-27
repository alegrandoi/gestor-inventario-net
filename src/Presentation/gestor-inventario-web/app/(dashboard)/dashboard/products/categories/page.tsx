'use client';

import { FormEvent, ReactNode, useEffect, useMemo, useState, useId } from 'react';
import {
  ArrowPathIcon,
  CheckIcon,
  ExclamationTriangleIcon,
  PlusIcon,
  TrashIcon
} from '@heroicons/react/24/outline';
import clsx from 'clsx';
import { Card } from '../../../../../components/ui/card';
import { Input } from '../../../../../components/ui/input';
import { Select } from '../../../../../components/ui/select';
import { Textarea } from '../../../../../components/ui/textarea';
import { Button } from '../../../../../components/ui/button';
import { apiClient } from '../../../../../src/lib/api-client';
import type { CategoryDto } from '../../../../../src/types/api';

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

interface CategoryFormState {
  id?: number;
  name: string;
  description: string;
  parentId: string;
}

interface CategoryOption {
  id: number;
  label: string;
}

function buildCategoryOptions(categories: CategoryDto[]): CategoryOption[] {
  const options: CategoryOption[] = [];

  function visit(nodes: CategoryDto[], depth: number) {
    nodes.forEach((node) => {
      const prefix = depth > 0 ? `${'— '.repeat(depth)}` : '';
      options.push({ id: node.id, label: `${prefix}${node.name}` });
      if (node.children && node.children.length > 0) {
        visit(node.children, depth + 1);
      }
    });
  }

  visit(categories, 0);
  return options;
}

function findCategoryById(categories: CategoryDto[], id: number): CategoryDto | undefined {
  for (const category of categories) {
    if (category.id === id) {
      return category;
    }

    if (category.children && category.children.length > 0) {
      const match = findCategoryById(category.children, id);
      if (match) {
        return match;
      }
    }
  }

  return undefined;
}

function collectDescendantIds(category: CategoryDto | undefined, accumulator: Set<number>) {
  if (!category) {
    return;
  }

  if (category.children) {
    for (const child of category.children) {
      accumulator.add(child.id);
      collectDescendantIds(child, accumulator);
    }
  }
}

function resetForm(): CategoryFormState {
  return {
    name: '',
    description: '',
    parentId: ''
  };
}

function removeCategoryById(nodes: CategoryDto[], id: number): CategoryDto[] {
  return nodes
    .filter((node) => node.id !== id)
    .map((node) => {
      const updatedChildren = node.children ? removeCategoryById(node.children, id) : undefined;
      const result: CategoryDto = { ...node };
      if (updatedChildren && updatedChildren.length > 0) {
        result.children = updatedChildren;
      } else {
        delete result.children;
      }
      return result;
    });
}

export default function CategoriesPage() {
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [form, setForm] = useState<CategoryFormState>(() => resetForm());
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [isFormModalOpen, setIsFormModalOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const categoryOptions = useMemo(() => buildCategoryOptions(categories), [categories]);

  const selectedCategory = useMemo(
    () => (selectedId != null ? findCategoryById(categories, selectedId) : undefined),
    [categories, selectedId]
  );

  const invalidParentIds = useMemo(() => {
    if (!selectedCategory) {
      return new Set<number>();
    }

    const ids = new Set<number>([selectedCategory.id]);
    collectDescendantIds(selectedCategory, ids);
    return ids;
  }, [selectedCategory]);

  function resetFormState() {
    setForm(resetForm());
  }

  function closeFormModal() {
    setIsFormModalOpen(false);
  }

  async function loadCategories(preserveSelection?: number | null, options?: { silent?: boolean }) {
    setError(null);

    try {
      const response = await apiClient.get<CategoryDto[]>('/categories');
      const fetched = response.data ?? [];
      setCategories(fetched);

      const targetId = preserveSelection ?? selectedId;
      if (targetId != null) {
        const match = findCategoryById(fetched, targetId);
        if (match) {
          setSelectedId(match.id);
          setForm({
            id: match.id,
            name: match.name,
            description: match.description ?? '',
            parentId: match.parentId != null ? match.parentId.toString() : ''
          });
        } else {
          setSelectedId(null);
          resetFormState();
          closeFormModal();
        }
      }
    } catch (loadError) {
      console.error(loadError);
      if (!options?.silent) {
        setError('No se pudieron cargar las categorías.');
      }
    }
  }

  useEffect(() => {
    loadCategories()
      .catch((loadError) => console.error(loadError))
      .finally(() => setIsLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function startCreate() {
    setSelectedId(null);
    setSuccess(null);
    setError(null);
    resetFormState();
    setIsFormModalOpen(true);
  }

  function handleSelectCategory(category: CategoryDto) {
    setSelectedId(category.id);
    setForm({
      id: category.id,
      name: category.name,
      description: category.description ?? '',
      parentId: category.parentId != null ? category.parentId.toString() : ''
    });
    setIsFormModalOpen(true);
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSuccess(null);
    setIsSaving(true);

    const payload = {
      name: form.name.trim(),
      description: form.description.trim() || undefined,
      parentId: form.parentId ? Number(form.parentId) : null
    };

    try {
      if (form.id) {
        await apiClient.put(`/categories/${form.id}`, payload);
        setSuccess('Categoría actualizada correctamente.');
        await loadCategories(form.id);
        closeFormModal();
      } else {
        await apiClient.post<CategoryDto>('/categories', payload);
        setSuccess('Categoría creada correctamente.');
        await loadCategories();
        resetFormState();
        closeFormModal();
      }
    } catch (submitError) {
      console.error(submitError);
      setError('No se pudo guardar la categoría.');
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDelete(category: CategoryDto) {
    if (isDeleting) {
      return;
    }

    const confirmed = window.confirm(
      `¿Seguro que deseas eliminar la categoría "${category.name}"? Los elementos descendientes no podrán volver a asignarse automáticamente.`
    );

    if (!confirmed) {
      return;
    }

    setError(null);
    setSuccess(null);
    setIsDeleting(true);

    try {
      await apiClient.delete(`/categories/${category.id}`);
      setCategories((prev) => removeCategoryById(prev, category.id));
      setSuccess('Categoría eliminada correctamente.');
      if (selectedId === category.id) {
        setSelectedId(null);
        resetFormState();
        closeFormModal();
      }
      await loadCategories(undefined, { silent: true });
    } catch (deleteError) {
      console.error(deleteError);
      setError('No se pudo eliminar la categoría seleccionada. Verifica que no tenga elementos dependientes.');
    } finally {
      setIsDeleting(false);
    }
  }

  async function handleRefresh() {
    setIsRefreshing(true);
    setSuccess(null);
    await loadCategories();
    setIsRefreshing(false);
  }

  function updateForm<K extends keyof CategoryFormState>(key: K, value: CategoryFormState[K]) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  function renderNodes(nodes: CategoryDto[], depth = 0): JSX.Element {
    if (nodes.length === 0) {
      return (
        <p className="px-3 py-6 text-sm text-slate-500">No hay categorías registradas. Usa el formulario para crear la primera.</p>
      );
    }

    return (
      <ul className={clsx('space-y-1', depth > 0 && 'pl-4')}>
        {nodes.map((node) => (
          <li key={node.id}>
            <div
              className={clsx(
                'flex items-center justify-between rounded-lg px-3 py-2 text-sm transition',
                selectedId === node.id ? 'bg-primary-50 text-primary-700 shadow-sm' : 'hover:bg-slate-50'
              )}
            >
              <button
                type="button"
                onClick={() => handleSelectCategory(node)}
                className="flex-1 text-left font-medium"
              >
                {node.name}
              </button>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => handleDelete(node)}
                disabled={isSaving || isDeleting}
                aria-label={`Eliminar ${node.name}`}
              >
                <TrashIcon className="h-4 w-4" />
              </Button>
            </div>
            {node.children && node.children.length > 0 && renderNodes(node.children, depth + 1)}
          </li>
        ))}
      </ul>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">Gestión de categorías</h1>
          <p className="text-sm text-slate-500">
            Organiza la jerarquía de productos para facilitar la clasificación y la búsqueda.
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Button type="button" variant="secondary" onClick={startCreate}>
            <PlusIcon className="mr-2 h-4 w-4" /> Nueva categoría
          </Button>
          <Button type="button" variant="ghost" onClick={handleRefresh} disabled={isRefreshing}>
            <ArrowPathIcon className={clsx('mr-2 h-4 w-4', isRefreshing && 'animate-spin')} /> Actualizar
          </Button>
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

      <Card title="Árbol de categorías" subtitle="Consulta la estructura y selecciona los nodos para editarlos">
        {isLoading ? (
          <p className="px-3 py-10 text-center text-sm text-slate-500">Cargando categorías…</p>
        ) : categories.length === 0 ? (
          <p className="px-3 py-10 text-center text-sm text-slate-500">
            No se han creado categorías todavía. Agrega la primera mediante el formulario.
          </p>
        ) : (
          renderNodes(categories)
        )}
      </Card>

      {isFormModalOpen && (
        <Modal
          title={form.id ? 'Editar categoría' : 'Nueva categoría'}
          description={
            form.id
              ? 'Modifica el nombre, la descripción o la posición jerárquica de la categoría seleccionada.'
              : 'Define una nueva categoría para organizar el catálogo de productos.'
          }
          onClose={closeFormModal}
          disableClose={isSaving}
        >
          <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
            <Input
              label="Nombre"
              required
              value={form.name}
              onChange={(event) => updateForm('name', event.target.value)}
              placeholder="Ej. Electrónica"
              disabled={isSaving}
            />
            <Textarea
              label="Descripción"
              value={form.description}
              onChange={(event) => updateForm('description', event.target.value)}
              placeholder="Describe el alcance de la categoría"
              rows={4}
              disabled={isSaving}
            />
            <Select
              label="Categoría padre"
              value={form.parentId}
              onChange={(event) => updateForm('parentId', event.target.value)}
              disabled={isSaving}
            >
              <option value="">Sin categoría padre</option>
              {categoryOptions.map((option) => (
                <option key={option.id} value={option.id.toString()} disabled={invalidParentIds.has(option.id)}>
                  {option.label}
                </option>
              ))}
            </Select>
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
