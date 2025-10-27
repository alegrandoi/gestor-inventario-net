'use client';

import { FormEvent, ReactNode, useEffect, useMemo, useRef, useState, useId } from 'react';
import {
  ArrowPathIcon,
  CheckIcon,
  ExclamationTriangleIcon,
  PlusIcon,
  TrashIcon,
  XMarkIcon
} from '@heroicons/react/24/outline';
import clsx from 'clsx';
import { Card } from '../../../../components/ui/card';
import { Input } from '../../../../components/ui/input';
import { Button } from '../../../../components/ui/button';
import { Badge } from '../../../../components/ui/badge';
import { Select } from '../../../../components/ui/select';
import { Textarea } from '../../../../components/ui/textarea';
import { apiClient } from '../../../../src/lib/api-client';
import type {
  CategoryDto,
  PagedResult,
  ProductAttributeGroupDto,
  ProductDto,
  TaxRateDto
} from '../../../../src/types/api';

interface VariantAttributeEntry {
  groupId?: number;
  groupKey: string;
  valueId?: number;
  valueKey: string;
}

interface ProductVariantFormState {
  id?: number;
  sku: string;
  attributeEntries: VariantAttributeEntry[];
  draftGroupId: string;
  draftValueId: string;
  price: string;
  barcode: string;
}

interface ProductImageFormState {
  id?: number;
  imageUrl: string;
  altText: string;
}

interface ProductFormState {
  id?: number;
  code: string;
  name: string;
  description: string;
  categoryId: string;
  defaultPrice: string;
  currency: string;
  taxRateId: string;
  isActive: boolean;
  weightKg: string;
  heightCm: string;
  widthCm: string;
  lengthCm: string;
  leadTimeDays: string;
  safetyStock: string;
  reorderPoint: string;
  reorderQuantity: string;
  requiresSerialTracking: boolean;
  variants: ProductVariantFormState[];
  images: ProductImageFormState[];
}

const catalogPageSize = 200;

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
        className="w-full max-w-5xl overflow-hidden rounded-2xl bg-white p-6 shadow-xl"
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

function createEmptyVariant(): ProductVariantFormState {
  return {
    sku: '',
    attributeEntries: [],
    draftGroupId: '',
    draftValueId: '',
    price: '',
    barcode: ''
  };
}

function createEmptyImage(): ProductImageFormState {
  return {
    imageUrl: '',
    altText: ''
  };
}

function createEmptyForm(): ProductFormState {
  return {
    code: '',
    name: '',
    description: '',
    categoryId: '',
    defaultPrice: '0',
    currency: 'EUR',
    taxRateId: '',
    isActive: true,
    weightKg: '0',
    heightCm: '',
    widthCm: '',
    lengthCm: '',
    leadTimeDays: '',
    safetyStock: '',
    reorderPoint: '',
    reorderQuantity: '',
    requiresSerialTracking: false,
    variants: [createEmptyVariant()],
    images: []
  };
}

function mapProductToForm(product: ProductDto): ProductFormState {
  return {
    id: product.id,
    code: product.code,
    name: product.name,
    description: product.description ?? '',
    categoryId: product.categoryId?.toString() ?? '',
    defaultPrice: product.defaultPrice.toString(),
    currency: product.currency,
    taxRateId: product.taxRateId?.toString() ?? '',
    isActive: product.isActive,
    weightKg: product.weightKg.toString(),
    heightCm: product.heightCm != null ? product.heightCm.toString() : '',
    widthCm: product.widthCm != null ? product.widthCm.toString() : '',
    lengthCm: product.lengthCm != null ? product.lengthCm.toString() : '',
    leadTimeDays: product.leadTimeDays != null ? product.leadTimeDays.toString() : '',
    safetyStock: product.safetyStock != null ? product.safetyStock.toString() : '',
    reorderPoint: product.reorderPoint != null ? product.reorderPoint.toString() : '',
    reorderQuantity: product.reorderQuantity != null ? product.reorderQuantity.toString() : '',
    requiresSerialTracking: product.requiresSerialTracking,
    variants:
      product.variants.length > 0
        ? product.variants.map((variant) => ({
            id: variant.id,
            sku: variant.sku,
            attributeEntries: parseVariantAttributesString(variant.attributes),
            draftGroupId: '',
            draftValueId: '',
            price: variant.price != null ? variant.price.toString() : '',
            barcode: variant.barcode ?? ''
          }))
        : [createEmptyVariant()],
    images: product.images.map((image) => ({
      id: image.id,
      imageUrl: image.imageUrl,
      altText: image.altText ?? ''
    }))
  };
}

function parseVariantAttributesString(rawValue: string): VariantAttributeEntry[] {
  return rawValue
    .split(';')
    .map((pair) => pair.trim())
    .filter((pair) => pair.length > 0)
    .map((pair) => {
      const [groupKey, ...valueParts] = pair.split('=');
      const valueKey = valueParts.join('=');
      const trimmedGroup = groupKey?.trim() ?? '';
      const trimmedValue = valueKey.trim();

      if (trimmedGroup.length === 0 || trimmedValue.length === 0) {
        return null;
      }

      return { groupKey: trimmedGroup, valueKey: trimmedValue } satisfies VariantAttributeEntry;
    })
    .filter((entry): entry is VariantAttributeEntry => entry !== null);
}

function stringifyVariantAttributes(entries: VariantAttributeEntry[]): string {
  return entries
    .map((entry) => ({
      groupKey: entry.groupKey.trim(),
      valueKey: entry.valueKey.trim()
    }))
    .filter((entry) => entry.groupKey.length > 0 && entry.valueKey.length > 0)
    .map((entry) => `${entry.groupKey}=${entry.valueKey}`)
    .join(';');
}

function enrichVariantAttributeEntry(
  entry: VariantAttributeEntry,
  groups: ProductAttributeGroupDto[]
): VariantAttributeEntry {
  const normalizedGroupKey = entry.groupKey.trim().toLowerCase();
  const matchingGroup = groups.find((group) => {
    const groupName = group.name.trim().toLowerCase();
    const groupSlug = group.slug?.trim().toLowerCase() ?? '';
    return groupName === normalizedGroupKey || (groupSlug.length > 0 && groupSlug === normalizedGroupKey);
  });

  if (!matchingGroup) {
    return entry;
  }

  const normalizedValueKey = entry.valueKey.trim().toLowerCase();
  const matchingValue = matchingGroup.values.find((value) => value.name.trim().toLowerCase() === normalizedValueKey);

  if (!matchingValue) {
    if (entry.groupId === matchingGroup.id && entry.groupKey === matchingGroup.name) {
      return entry;
    }

    return {
      ...entry,
      groupId: matchingGroup.id,
      groupKey: matchingGroup.name
    };
  }

  if (
    entry.groupId === matchingGroup.id &&
    entry.valueId === matchingValue.id &&
    entry.groupKey === matchingGroup.name &&
    entry.valueKey === matchingValue.name
  ) {
    return entry;
  }

  return {
    ...entry,
    groupId: matchingGroup.id,
    groupKey: matchingGroup.name,
    valueId: matchingValue.id,
    valueKey: matchingValue.name
  };
}

interface CategoryOption {
  id: number;
  label: string;
}

function flattenCategoryNodes(categories: CategoryDto[]): CategoryDto[] {
  const result: CategoryDto[] = [];

  function visit(nodes: CategoryDto[]) {
    nodes.forEach((node) => {
      result.push(node);
      if (node.children && node.children.length > 0) {
        visit(node.children);
      }
    });
  }

  visit(categories);
  return result;
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

const PRODUCTS_PENDING_REMOVALS_STORAGE_KEY = 'dashboard:products:pending-removals';

export default function ProductsPage() {
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [taxRates, setTaxRates] = useState<TaxRateDto[]>([]);
  const [attributeGroups, setAttributeGroups] = useState<ProductAttributeGroupDto[]>([]);
  const [form, setForm] = useState<ProductFormState>(() => createEmptyForm());
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [isFormModalOpen, setIsFormModalOpen] = useState(false);
  const [filters, setFilters] = useState({ search: '', category: 'all', status: 'all' });
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const optimisticRemovalIdsRef = useRef<number[]>([]);

  function persistPendingRemovalIds(ids: number[]) {
    if (typeof window === 'undefined') {
      return;
    }

    if (ids.length === 0) {
      window.sessionStorage.removeItem(PRODUCTS_PENDING_REMOVALS_STORAGE_KEY);
      return;
    }

    window.sessionStorage.setItem(PRODUCTS_PENDING_REMOVALS_STORAGE_KEY, JSON.stringify(ids));
  }

  useEffect(() => {
    if (typeof window === 'undefined') {
      return;
    }

    const rawValue = window.sessionStorage.getItem(PRODUCTS_PENDING_REMOVALS_STORAGE_KEY);
    if (!rawValue) {
      return;
    }

    try {
      const storedIds = JSON.parse(rawValue) as number[];
      if (Array.isArray(storedIds) && storedIds.length > 0) {
        optimisticRemovalIdsRef.current = storedIds.filter((value) => Number.isInteger(value));
        if (optimisticRemovalIdsRef.current.length !== storedIds.length) {
          persistPendingRemovalIds(optimisticRemovalIdsRef.current);
        }
      }
    } catch (parseError) {
      console.error(parseError);
      window.sessionStorage.removeItem(PRODUCTS_PENDING_REMOVALS_STORAGE_KEY);
    }
  }, []);

  async function loadAllResources(
    preserveSelection?: number | null | false,
    options?: { silent?: boolean }
  ) {
    setError(null);
    try {
      const [productsResponse, categoriesResponse, taxRatesResponse, attributesResponse] = await Promise.all([
        apiClient.get<PagedResult<ProductDto>>('/products', {
          params: { pageSize: catalogPageSize }
        }),
        apiClient.get<CategoryDto[]>('/categories'),
        apiClient.get<TaxRateDto[]>('/taxrates'),
        apiClient.get<ProductAttributeGroupDto[]>('/product-attributes')
      ]);

      const fetchedProducts = productsResponse.data.items ?? [];
      const fetchedCategories = categoriesResponse.data ?? [];
      const fetchedTaxRates = taxRatesResponse.data ?? [];
      const fetchedAttributeGroups = attributesResponse.data ?? [];

      let sanitizedProducts = fetchedProducts;
      if (optimisticRemovalIdsRef.current.length > 0) {
        const pendingRemovalIds = optimisticRemovalIdsRef.current;
        const pendingRemovalSet = new Set(pendingRemovalIds);
        const stillPresentIds: number[] = [];

        sanitizedProducts = fetchedProducts.filter((product) => {
          if (pendingRemovalSet.has(product.id)) {
            if (!stillPresentIds.includes(product.id)) {
              stillPresentIds.push(product.id);
            }
            return false;
          }

          return true;
        });

        optimisticRemovalIdsRef.current = stillPresentIds;
        persistPendingRemovalIds(optimisticRemovalIdsRef.current);
      }

      setProducts(sanitizedProducts);
      setCategories(fetchedCategories);
      setTaxRates(fetchedTaxRates);
      setAttributeGroups(fetchedAttributeGroups);

      const targetId = preserveSelection === false ? null : preserveSelection ?? selectedId;
      if (targetId) {
        const product = sanitizedProducts.find((item) => item.id === targetId);
        if (product) {
          setSelectedId(product.id);
          setForm(mapProductToForm(product));
        } else {
          setSelectedId(null);
          setForm(createEmptyForm());
        }
      }
    } catch (loadError) {
      console.error(loadError);
      if (!options?.silent) {
        setError('No se pudieron cargar los datos de productos.');
      }
    }
  }

  useEffect(() => {
    setIsLoading(true);
    loadAllResources()
      .catch((loadError) => console.error(loadError))
      .finally(() => setIsLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (attributeGroups.length === 0) {
      return;
    }

    setForm((prev) => {
      let hasChanges = false;

      const updatedVariants = prev.variants.map((variant) => {
        if (variant.attributeEntries.length === 0) {
          return variant;
        }

        const hydratedEntries = variant.attributeEntries.map((entry) =>
          enrichVariantAttributeEntry(entry, attributeGroups)
        );

        const changed = hydratedEntries.some((entry, index) => {
          const previous = variant.attributeEntries[index];
          return (
            entry.groupId !== previous.groupId ||
            entry.valueId !== previous.valueId ||
            entry.groupKey !== previous.groupKey ||
            entry.valueKey !== previous.valueKey
          );
        });

        if (!changed) {
          return variant;
        }

        hasChanges = true;
        return { ...variant, attributeEntries: hydratedEntries };
      });

      if (!hasChanges) {
        return prev;
      }

      return { ...prev, variants: updatedVariants };
    });
  }, [attributeGroups]);

  const flattenedCategories = useMemo(() => flattenCategoryNodes(categories), [categories]);

  const categoryOptions = useMemo(() => buildCategoryOptions(categories), [categories]);

  const categoryLookup = useMemo(() => {
    const lookup = new Map<number, string>();
    flattenedCategories.forEach((category) => lookup.set(category.id, category.name));
    return lookup;
  }, [flattenedCategories]);

  const taxRateLookup = useMemo(() => {
    const lookup = new Map<number, number>();
    taxRates.forEach((rate) => lookup.set(rate.id, rate.rate));
    return lookup;
  }, [taxRates]);

  const selectableAttributeGroups = useMemo(() => {
    return attributeGroups
      .map((group) => ({
        ...group,
        values: group.values.filter((value) => value.isActive)
      }))
      .filter((group) => group.values.length > 0);
  }, [attributeGroups]);

  const filteredProducts = useMemo(() => {
    return products.filter((product) => {
      const term = filters.search.trim().toLowerCase();
      const matchesSearch = term.length === 0
        || product.name.toLowerCase().includes(term)
        || product.code.toLowerCase().includes(term);
      const matchesCategory = filters.category === 'all' || product.categoryId?.toString() === filters.category;
      const matchesStatus =
        filters.status === 'all' || (filters.status === 'active' ? product.isActive : !product.isActive);
      return matchesSearch && matchesCategory && matchesStatus;
    });
  }, [filters, products]);

  function resolveFinalPrice(product: ProductDto): number {
    if (typeof product.finalPrice === 'number' && Number.isFinite(product.finalPrice)) {
      return product.finalPrice;
    }

    const rate =
      typeof product.appliedTaxRate === 'number' && Number.isFinite(product.appliedTaxRate)
        ? product.appliedTaxRate
        : product.taxRateId != null
          ? taxRateLookup.get(product.taxRateId) ?? 0
          : 0;

    const computed = product.defaultPrice * (1 + rate / 100);
    return Math.round((computed + Number.EPSILON) * 100) / 100;
  }

  function selectProduct(product: ProductDto) {
    setSelectedId(product.id);
    setForm(mapProductToForm(product));
    setSuccess(null);
    setError(null);
    setIsFormModalOpen(true);
  }

  function startCreate() {
    setSelectedId(null);
    setForm(createEmptyForm());
    setSuccess(null);
    setError(null);
    setIsFormModalOpen(true);
  }

  function closeFormModal() {
    if (isSaving) {
      return;
    }

    setIsFormModalOpen(false);
    setSelectedId(null);
    setForm(createEmptyForm());
  }

  function updateFormField<Field extends keyof ProductFormState>(field: Field, value: ProductFormState[Field]) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  function updateVariant(index: number, changes: Partial<ProductVariantFormState>) {
    setForm((prev) => ({
      ...prev,
      variants: prev.variants.map((variant, variantIndex) =>
        variantIndex === index ? { ...variant, ...changes } : variant
      )
    }));
  }

  function addVariant() {
    setForm((prev) => ({
      ...prev,
      variants: [...prev.variants, createEmptyVariant()]
    }));
  }

  function removeVariant(index: number) {
    setForm((prev) => ({
      ...prev,
      variants: prev.variants.filter((_, variantIndex) => variantIndex !== index)
    }));
  }

  function updateVariantDraftGroup(index: number, value: string) {
    updateVariant(index, { draftGroupId: value, draftValueId: '' });
  }

  function updateVariantDraftValue(index: number, value: string) {
    updateVariant(index, { draftValueId: value });
  }

  function handleAddVariantAttribute(index: number) {
    const variant = form.variants[index];
    if (!variant) {
      return;
    }

    const parsedGroupId = Number.parseInt(variant.draftGroupId, 10);
    if (Number.isNaN(parsedGroupId)) {
      setSuccess(null);
      setError('Selecciona un atributo para la variante.');
      return;
    }

    const parsedValueId = Number.parseInt(variant.draftValueId, 10);
    if (Number.isNaN(parsedValueId)) {
      setSuccess(null);
      setError('Selecciona un valor de atributo para la variante.');
      return;
    }

    const group = attributeGroups.find((item) => item.id === parsedGroupId);
    const value = group?.values.find((item) => item.id === parsedValueId && item.isActive);

    if (!group || !value) {
      setSuccess(null);
      setError('El atributo seleccionado ya no está disponible.');
      return;
    }

    const withoutDuplicates = variant.attributeEntries.filter((entry) => {
      if (entry.groupId != null) {
        return entry.groupId !== group.id;
      }

      return entry.groupKey.trim().toLowerCase() !== group.name.trim().toLowerCase();
    });

    const updatedEntries = [
      ...withoutDuplicates,
      {
        groupId: group.id,
        groupKey: group.name,
        valueId: value.id,
        valueKey: value.name
      }
    ];

    updateVariant(index, {
      attributeEntries: updatedEntries,
      draftGroupId: '',
      draftValueId: ''
    });
    setError(null);
  }

  function handleRemoveVariantAttribute(index: number, attributeIndex: number) {
    const variant = form.variants[index];
    if (!variant) {
      return;
    }

    const updatedEntries = variant.attributeEntries.filter((_, entryIndex) => entryIndex !== attributeIndex);
    updateVariant(index, { attributeEntries: updatedEntries });
  }

  function updateImage(index: number, changes: Partial<ProductImageFormState>) {
    setForm((prev) => ({
      ...prev,
      images: prev.images.map((image, imageIndex) =>
        imageIndex === index ? { ...image, ...changes } : image
      )
    }));
  }

  function addImage() {
    setForm((prev) => ({
      ...prev,
      images: [...prev.images, createEmptyImage()]
    }));
  }

  function removeImage(index: number) {
    setForm((prev) => ({
      ...prev,
      images: prev.images.filter((_, imageIndex) => imageIndex !== index)
    }));
  }

  async function handleRefresh() {
    setIsRefreshing(true);
    await loadAllResources(selectedId);
    setIsRefreshing(false);
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    if (form.code.trim().length === 0 || form.name.trim().length === 0) {
      setError('El código y el nombre del producto son obligatorios.');
      return;
    }

    const validVariants = form.variants.filter((variant) => {
      if (variant.sku.trim().length === 0) {
        return false;
      }

      const serializedAttributes = stringifyVariantAttributes(variant.attributeEntries);
      return serializedAttributes.length > 0;
    });

    if (validVariants.length === 0) {
      setError('Añade al menos una variante con SKU y atributos.');
      return;
    }

    const parsedDefaultPrice = Number.parseFloat(form.defaultPrice);
    if (Number.isNaN(parsedDefaultPrice) || parsedDefaultPrice < 0) {
      setError('El precio base debe ser un número válido.');
      return;
    }

    const parsedWeightKg = Number.parseFloat(form.weightKg);
    if (Number.isNaN(parsedWeightKg) || parsedWeightKg < 0) {
      setError('El peso debe ser un número mayor o igual que 0.');
      return;
    }

    const parseOptionalDecimal = (value: string, fieldLabel: string): number | null | undefined => {
      const trimmed = value.trim();
      if (trimmed.length === 0) {
        return null;
      }

      const parsed = Number.parseFloat(trimmed);
      if (Number.isNaN(parsed) || parsed < 0) {
        setError(`El campo ${fieldLabel} debe ser un número mayor o igual que 0.`);
        return undefined;
      }

      return parsed;
    };

    const parseOptionalInteger = (value: string, fieldLabel: string): number | null | undefined => {
      const trimmed = value.trim();
      if (trimmed.length === 0) {
        return null;
      }

      const parsed = Number.parseInt(trimmed, 10);
      if (Number.isNaN(parsed) || parsed < 0) {
        setError(`El campo ${fieldLabel} debe ser un número entero mayor o igual que 0.`);
        return undefined;
      }

      return parsed;
    };

    const parsedHeightCm = parseOptionalDecimal(form.heightCm, 'alto');
    if (parsedHeightCm === undefined) {
      return;
    }

    const parsedWidthCm = parseOptionalDecimal(form.widthCm, 'ancho');
    if (parsedWidthCm === undefined) {
      return;
    }

    const parsedLengthCm = parseOptionalDecimal(form.lengthCm, 'largo');
    if (parsedLengthCm === undefined) {
      return;
    }

    const parsedLeadTimeDays = parseOptionalInteger(form.leadTimeDays, 'tiempo de entrega');
    if (parsedLeadTimeDays === undefined) {
      return;
    }

    const parsedSafetyStock = parseOptionalDecimal(form.safetyStock, 'stock de seguridad');
    if (parsedSafetyStock === undefined) {
      return;
    }

    const parsedReorderPoint = parseOptionalDecimal(form.reorderPoint, 'punto de pedido');
    if (parsedReorderPoint === undefined) {
      return;
    }

    const parsedReorderQuantity = parseOptionalDecimal(form.reorderQuantity, 'cantidad de reaprovisionamiento');
    if (parsedReorderQuantity === undefined) {
      return;
    }

    const sanitizedVariants: Array<{
      id?: number;
      sku: string;
      attributes: string;
      price: number | null;
      barcode: string | null;
    }> = [];

    for (const variant of validVariants) {
      const sku = variant.sku.trim();
      const attributes = stringifyVariantAttributes(variant.attributeEntries);
      const barcodeValue = variant.barcode.trim();
      const priceValue = variant.price.trim();

      if (attributes.length === 0) {
        setError(`La variante ${sku || 'sin SKU'} debe tener al menos un atributo válido.`);
        return;
      }

      let price: number | null = null;
      if (priceValue.length > 0) {
        const parsedVariantPrice = Number.parseFloat(priceValue);
        if (Number.isNaN(parsedVariantPrice) || parsedVariantPrice < 0) {
          setError(`El precio de la variante ${sku || 'sin SKU'} debe ser un número mayor o igual que 0.`);
          return;
        }

        price = parsedVariantPrice;
      }

      sanitizedVariants.push({
        id: variant.id,
        sku,
        attributes,
        price,
        barcode: barcodeValue.length > 0 ? barcodeValue : null
      });
    }

    const normalizedSkuValues = sanitizedVariants.map((variant) => variant.sku.toLowerCase());
    if (new Set(normalizedSkuValues).size !== sanitizedVariants.length) {
      setError('Los SKU de las variantes no pueden repetirse.');
      return;
    }

    const sanitizedImages = form.images
      .filter((image) => image.imageUrl.trim().length > 0)
      .map((image) => {
        const imageUrl = image.imageUrl.trim();
        const altTextValue = image.altText?.trim() ?? '';
        return {
          id: image.id,
          imageUrl,
          altText: altTextValue.length > 0 ? altTextValue : null
        };
      });

    const normalizedImageUrls = sanitizedImages.map((image) => image.imageUrl.toLowerCase());
    if (new Set(normalizedImageUrls).size !== sanitizedImages.length) {
      setError('Las imágenes del producto no pueden repetirse.');
      return;
    }

    const variantsForCreate = sanitizedVariants.map(({ id, ...variant }) => variant);
    const variantsForUpdate = sanitizedVariants.map((variant) => ({
      id: variant.id ?? 0,
      sku: variant.sku,
      attributes: variant.attributes,
      price: variant.price,
      barcode: variant.barcode
    }));

    const imagesForCreate = sanitizedImages.map(({ id, ...image }) => image);
    const imagesForUpdate = sanitizedImages.map((image) => ({
      id: image.id ?? 0,
      imageUrl: image.imageUrl,
      altText: image.altText
    }));

    const payloadBase = {
      code: form.code.trim(),
      name: form.name.trim(),
      description: form.description.trim().length > 0 ? form.description.trim() : null,
      categoryId: form.categoryId ? Number.parseInt(form.categoryId, 10) : null,
      defaultPrice: parsedDefaultPrice,
      currency: form.currency.trim().toUpperCase(),
      taxRateId: form.taxRateId ? Number.parseInt(form.taxRateId, 10) : null,
      isActive: form.isActive,
      weightKg: parsedWeightKg,
      heightCm: parsedHeightCm,
      widthCm: parsedWidthCm,
      lengthCm: parsedLengthCm,
      leadTimeDays: parsedLeadTimeDays,
      safetyStock: parsedSafetyStock,
      reorderPoint: parsedReorderPoint,
      reorderQuantity: parsedReorderQuantity,
      requiresSerialTracking: form.requiresSerialTracking
    };

    setIsSaving(true);

    try {
      if (form.id) {
        await apiClient.put<ProductDto>(`/products/${form.id}`, {
          ...payloadBase,
          variants: variantsForUpdate,
          images: imagesForUpdate
        });
        setSuccess('Producto actualizado correctamente.');
        setIsFormModalOpen(false);
        setSelectedId(null);
        setForm(createEmptyForm());
        await loadAllResources(false);
      } else {
        await apiClient.post<ProductDto>('/products', {
          ...payloadBase,
          variants: variantsForCreate,
          images: imagesForCreate
        });

        setSuccess('Producto creado correctamente.');
        setIsFormModalOpen(false);
        setSelectedId(null);
        setForm(createEmptyForm());
        await loadAllResources(false);
      }
    } catch (submitError) {
      console.error(submitError);
      setError('No se pudo guardar el producto. Revisa los datos e inténtalo de nuevo.');
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDelete() {
    if (!form.id) {
      return;
    }

    const confirmed = window.confirm('¿Seguro que deseas eliminar este producto? Esta acción es irreversible.');
    if (!confirmed) {
      return;
    }

    setIsSaving(true);
    setError(null);
    setSuccess(null);

    try {
      const productId = form.id;
      await apiClient.delete(`/products/${productId}`);
      setProducts((prev) => prev.filter((product) => product.id !== productId));
      optimisticRemovalIdsRef.current = optimisticRemovalIdsRef.current.includes(productId)
        ? optimisticRemovalIdsRef.current
        : [...optimisticRemovalIdsRef.current, productId];
      persistPendingRemovalIds(optimisticRemovalIdsRef.current);
      setSuccess('Producto eliminado correctamente.');
      setSelectedId(null);
      setForm(createEmptyForm());
      setIsFormModalOpen(false);
      await loadAllResources(false, { silent: true });
    } catch (deleteError) {
      console.error(deleteError);
      setError('No se pudo eliminar el producto.');
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col justify-between gap-4 md:flex-row md:items-center">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">Catálogo de productos</h1>
          <p className="text-sm text-slate-500">
            Da de alta artículos, gestiona variantes y controla la información comercial de tu inventario.
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Button type="button" variant="secondary" onClick={startCreate}>
            <PlusIcon className="mr-2 h-4 w-4" /> Nuevo producto
          </Button>
          <Button type="button" variant="ghost" onClick={handleRefresh} disabled={isRefreshing}>
            <ArrowPathIcon className={clsx('mr-2 h-4 w-4', isRefreshing && 'animate-spin')} />
            Actualizar
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

      <Card title="Listado de productos" subtitle="Consulta y filtra el catálogo disponible">
        <div className="mb-4 grid gap-4 lg:grid-cols-3">
          <Input
            label="Buscar"
            placeholder="Nombre o código"
            value={filters.search}
            onChange={(event) => setFilters((prev) => ({ ...prev, search: event.target.value }))}
          />
          <Select
            label="Categoría"
            value={filters.category}
            onChange={(event) => setFilters((prev) => ({ ...prev, category: event.target.value }))}
          >
            <option value="all">Todas</option>
            {categoryOptions.map((category) => (
              <option key={category.id} value={category.id.toString()}>
                {category.label}
              </option>
            ))}
          </Select>
          <Select
            label="Estado"
            value={filters.status}
            onChange={(event) => setFilters((prev) => ({ ...prev, status: event.target.value }))}
          >
            <option value="all">Todos</option>
            <option value="active">Activos</option>
            <option value="inactive">Inactivos</option>
          </Select>
        </div>

        <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          <table className="min-w-full divide-y divide-slate-200 text-sm">
            <thead className="bg-slate-50 text-xs uppercase text-slate-500">
              <tr>
                <th className="px-4 py-3 text-left">Producto</th>
                <th className="px-4 py-3 text-left">Categoría</th>
                <th className="px-4 py-3 text-right">Precio base</th>
                <th className="px-4 py-3 text-right">Precio final</th>
                <th className="px-4 py-3 text-right">Variantes</th>
                <th className="px-4 py-3 text-left">Estado</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {isLoading ? (
                <tr>
                  <td colSpan={6} className="px-4 py-10 text-center text-sm text-slate-500">
                    Cargando productos…
                  </td>
                </tr>
              ) : filteredProducts.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-10 text-center text-sm text-slate-500">
                    No hay productos que coincidan con los filtros seleccionados.
                  </td>
                </tr>
              ) : (
                filteredProducts.map((product) => {
                  const finalPrice = resolveFinalPrice(product);
                  return (
                    <tr
                      key={product.id}
                      onClick={() => selectProduct(product)}
                      className={clsx(
                        'cursor-pointer bg-white text-slate-700 transition hover:bg-slate-50',
                        selectedId === product.id && 'bg-primary-50/60'
                      )}
                    >
                      <td className="px-4 py-3">
                        <p className="font-semibold text-slate-900">{product.name}</p>
                        <p className="text-xs text-slate-500">Código {product.code}</p>
                      </td>
                      <td className="px-4 py-3 text-xs text-slate-500">
                        {product.categoryId ? categoryLookup.get(product.categoryId) ?? 'Sin categoría' : 'Sin categoría'}
                      </td>
                      <td className="px-4 py-3 text-right text-sm font-semibold text-slate-900">
                        {product.defaultPrice.toLocaleString('es-ES', {
                          style: 'currency',
                          currency: product.currency || 'EUR'
                        })}
                      </td>
                      <td className="px-4 py-3 text-right text-sm font-semibold text-slate-900">
                        {finalPrice.toLocaleString('es-ES', {
                          style: 'currency',
                          currency: product.currency || 'EUR'
                        })}
                      </td>
                      <td className="px-4 py-3 text-right text-xs text-slate-500">{product.variants.length}</td>
                      <td className="px-4 py-3">
                        <Badge tone={product.isActive ? 'success' : 'warning'}>
                          {product.isActive ? 'Activo' : 'Inactivo'}
                        </Badge>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </Card>

      {isFormModalOpen && (
        <Modal
          title={form.id ? 'Editar producto' : 'Nuevo producto'}
          description={
            form.id
              ? 'Actualiza la información disponible para las ventas y la planificación.'
              : 'Completa los datos necesarios para poner en catálogo un nuevo producto.'
          }
          onClose={closeFormModal}
          disableClose={isSaving}
        >
          <form className="flex flex-col gap-6" onSubmit={handleSubmit}>
            <div className="grid gap-4 md:grid-cols-2">
              <Input
                label="Código SKU"
                required
                value={form.code}
                onChange={(event) => updateFormField('code', event.target.value)}
              />
              <Input
                label="Nombre"
                required
                value={form.name}
                onChange={(event) => updateFormField('name', event.target.value)}
              />
            </div>
            <Textarea
              label="Descripción"
              rows={3}
              value={form.description}
              onChange={(event) => updateFormField('description', event.target.value)}
              placeholder="Descripción corta del producto, características principales y usos recomendados."
            />
            <div className="grid gap-4 md:grid-cols-2">
              <Select
                label="Categoría"
                value={form.categoryId}
                onChange={(event) => updateFormField('categoryId', event.target.value)}
              >
                <option value="">Sin categoría</option>
                {categoryOptions.map((category) => (
                  <option key={category.id} value={category.id.toString()}>
                    {category.label}
                  </option>
                ))}
              </Select>
              <Select
                label="Impuesto"
                value={form.taxRateId}
                onChange={(event) => updateFormField('taxRateId', event.target.value)}
              >
                <option value="">Sin impuesto</option>
                {taxRates.map((tax) => (
                  <option key={tax.id} value={tax.id.toString()}>
                    {tax.name} ({tax.rate.toFixed(2)}%)
                  </option>
                ))}
              </Select>
            </div>
            <div className="grid gap-4 md:grid-cols-3">
              <Input
                label="Precio base"
                type="number"
                min="0"
                step="0.01"
                required
                value={form.defaultPrice}
                onChange={(event) => updateFormField('defaultPrice', event.target.value)}
              />
              <Input
                label="Moneda"
                value={form.currency}
                maxLength={10}
                onChange={(event) => updateFormField('currency', event.target.value.toUpperCase())}
              />
              <Input
                label="Peso (kg)"
                type="number"
                min="0"
                step="0.001"
                required
                value={form.weightKg}
                onChange={(event) => updateFormField('weightKg', event.target.value)}
              />
            </div>
            <div className="grid gap-4 md:grid-cols-4 md:[&>div>label>span]:min-h-[2.5rem]">
              <div>
                <Input
                  label="Ancho (cm)"
                  type="number"
                  min="0"
                  step="0.01"
                  value={form.widthCm}
                  onChange={(event) => updateFormField('widthCm', event.target.value)}
                />
              </div>
              <div>
                <Input
                  label="Alto (cm)"
                  type="number"
                  min="0"
                  step="0.01"
                  value={form.heightCm}
                  onChange={(event) => updateFormField('heightCm', event.target.value)}
                />
              </div>
              <div>
                <Input
                  label="Largo (cm)"
                  type="number"
                  min="0"
                  step="0.01"
                  value={form.lengthCm}
                  onChange={(event) => updateFormField('lengthCm', event.target.value)}
                />
              </div>
              <div>
                <Input
                  label="Tiempo de entrega (días)"
                  type="number"
                  min="0"
                  step="1"
                  value={form.leadTimeDays}
                  onChange={(event) => updateFormField('leadTimeDays', event.target.value)}
                />
              </div>
              <div>
                <Input
                  label="Stock de seguridad"
                  type="number"
                  min="0"
                  step="0.01"
                  value={form.safetyStock}
                  onChange={(event) => updateFormField('safetyStock', event.target.value)}
                />
              </div>
              <div>
                <Input
                  label="Punto de pedido"
                  type="number"
                  min="0"
                  step="0.01"
                  value={form.reorderPoint}
                  onChange={(event) => updateFormField('reorderPoint', event.target.value)}
                />
              </div>
              <div>
                <Input
                  label="Cantidad de reaprovisionamiento"
                  type="number"
                  min="0"
                  step="0.01"
                  value={form.reorderQuantity}
                  onChange={(event) => updateFormField('reorderQuantity', event.target.value)}
                />
              </div>
            </div>
            <div className="flex flex-col gap-2 text-xs text-slate-600 md:flex-row md:items-center">
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  className="h-4 w-4 rounded border-slate-300 text-primary-600 focus:ring-primary-500"
                  checked={form.isActive}
                  onChange={(event) => updateFormField('isActive', event.target.checked)}
                />
                Producto activo en catálogo
              </label>
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  className="h-4 w-4 rounded border-slate-300 text-primary-600 focus:ring-primary-500"
                  checked={form.requiresSerialTracking}
                  onChange={(event) => updateFormField('requiresSerialTracking', event.target.checked)}
                />
                Requiere trazabilidad por número de serie
              </label>
            </div>

            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <h2 className="text-sm font-semibold text-slate-900">Variantes</h2>
                <Button type="button" variant="secondary" size="sm" onClick={addVariant}>
                  <PlusIcon className="mr-2 h-4 w-4" /> Añadir variante
                </Button>
              </div>
              {form.variants.map((variant, index) => {
                const selectedGroupId = Number.parseInt(variant.draftGroupId, 10);
                const availableValues = Number.isNaN(selectedGroupId)
                  ? []
                  : selectableAttributeGroups.find((group) => group.id === selectedGroupId)?.values ?? [];

                return (
                  <div key={variant.id ?? index} className="space-y-4 rounded-2xl border border-slate-200 p-4">
                    <div className="space-y-3">
                      <div className="grid gap-3 md:grid-cols-4 md:items-end">
                        <Input
                          label="SKU"
                          required
                          value={variant.sku}
                          onChange={(event) => updateVariant(index, { sku: event.target.value })}
                        />
                        <Select
                          label="Atributo"
                          value={variant.draftGroupId}
                          onChange={(event) => updateVariantDraftGroup(index, event.target.value)}
                        >
                          <option value="">Selecciona un atributo</option>
                          {selectableAttributeGroups.map((group) => (
                            <option key={group.id} value={group.id.toString()}>
                              {group.name}
                            </option>
                          ))}
                        </Select>
                        <Select
                          label="Valor"
                          value={variant.draftValueId}
                          onChange={(event) => updateVariantDraftValue(index, event.target.value)}
                          disabled={Number.isNaN(selectedGroupId) || availableValues.length === 0}
                        >
                          <option value="">Selecciona un valor</option>
                          {availableValues.map((value) => (
                            <option key={value.id} value={value.id.toString()}>
                              {value.name}
                            </option>
                          ))}
                        </Select>
                        <div className="flex w-full flex-col gap-1 md:self-stretch">
                          <span className="text-sm font-medium text-slate-700 md:invisible">Acciones</span>
                          <div className="flex justify-end">
                            <Button
                              type="button"
                              variant="secondary"
                              size="sm"
                              className="w-full md:w-auto"
                              onClick={() => handleAddVariantAttribute(index)}
                              disabled={selectableAttributeGroups.length === 0}
                            >
                              Añadir
                            </Button>
                          </div>
                          <div aria-hidden="true" className="min-h-[1.25rem]" />
                        </div>
                      </div>
                      <div className="space-y-2 text-left">
                        <span className="block text-sm font-medium text-slate-700">Atributos seleccionados</span>
                        {variant.attributeEntries.length === 0 ? (
                          <p className="text-xs text-slate-500">Añade atributos existentes para definir la variante.</p>
                        ) : (
                          <div className="flex flex-wrap gap-2">
                            {variant.attributeEntries.map((entry, attributeIndex) => (
                              <span
                                key={`${entry.groupKey}-${entry.valueKey}-${attributeIndex}`}
                                className="inline-flex items-center gap-2 rounded-full border border-slate-200 bg-slate-50 px-3 py-1 text-xs text-slate-700"
                              >
                                <span className="font-medium text-slate-800">{entry.groupKey}</span>
                                <span className="text-slate-500">{entry.valueKey}</span>
                                <button
                                  type="button"
                                  onClick={() => handleRemoveVariantAttribute(index, attributeIndex)}
                                  className="rounded-full bg-transparent p-0.5 text-slate-400 transition hover:text-slate-600"
                                >
                                  <XMarkIcon className="h-3.5 w-3.5" aria-hidden="true" />
                                  <span className="sr-only">Quitar atributo</span>
                                </button>
                              </span>
                            ))}
                          </div>
                        )}
                      </div>
                    </div>
                    <div className="grid gap-4 md:grid-cols-3">
                      <Input
                        label="Precio específico"
                        type="number"
                        min="0"
                        step="0.01"
                        value={variant.price}
                        onChange={(event) => updateVariant(index, { price: event.target.value })}
                      />
                      <Input
                        label="Código de barras"
                        value={variant.barcode}
                        onChange={(event) => updateVariant(index, { barcode: event.target.value })}
                        placeholder="Opcional"
                        className="md:col-span-2"
                      />
                    </div>
                    <div className="flex flex-wrap items-center justify-between gap-3">
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() => removeVariant(index)}
                        disabled={form.variants.length === 1}
                      >
                        <TrashIcon className="mr-2 h-4 w-4" /> Eliminar variante
                      </Button>
                      <span className="text-xs text-slate-500">
                        Cada variante debe contener atributos previamente creados.
                      </span>
                    </div>
                  </div>
                );
              })}
            </div>

            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <h2 className="text-sm font-semibold text-slate-900">Imágenes</h2>
                <Button type="button" variant="secondary" size="sm" onClick={addImage}>
                  <PlusIcon className="mr-2 h-4 w-4" /> Añadir imagen
                </Button>
              </div>
              {form.images.length === 0 ? (
                <p className="text-xs text-slate-500">Añade URLs o rutas de imágenes para mejorar la ficha del producto.</p>
              ) : (
                form.images.map((image, index) => (
                  <div key={image.id ?? index} className="grid gap-3 rounded-2xl border border-slate-200 p-4 md:grid-cols-3">
                    <Input
                      label="URL de la imagen"
                      value={image.imageUrl}
                      onChange={(event) => updateImage(index, { imageUrl: event.target.value })}
                      placeholder="https://"
                    />
                    <Input
                      label="Texto alternativo"
                      value={image.altText}
                      onChange={(event) => updateImage(index, { altText: event.target.value })}
                    />
                    <div className="flex w-full flex-col gap-1 md:self-stretch">
                      <span className="text-sm font-medium text-slate-700 md:invisible">Acciones</span>
                      <div className="flex justify-end">
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          className="w-full md:w-auto"
                          onClick={() => removeImage(index)}
                        >
                          <TrashIcon className="mr-2 h-4 w-4" /> Quitar
                        </Button>
                      </div>
                      <div aria-hidden="true" className="min-h-[1.25rem]" />
                    </div>
                  </div>
                ))
              )}
            </div>

            <div className="flex flex-col gap-2 pt-2 md:flex-row md:justify-between">
              <div className="flex gap-2">
                <Button type="submit" disabled={isSaving}>
                  {isSaving ? 'Guardando…' : form.id ? 'Actualizar producto' : 'Crear producto'}
                </Button>
                {form.id && (
                  <Button type="button" variant="ghost" onClick={handleDelete} disabled={isSaving}>
                    <TrashIcon className="mr-2 h-4 w-4" /> Eliminar
                  </Button>
                )}
              </div>
              <Button type="button" variant="secondary" onClick={startCreate} disabled={isSaving}>
                Limpiar formulario
              </Button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
