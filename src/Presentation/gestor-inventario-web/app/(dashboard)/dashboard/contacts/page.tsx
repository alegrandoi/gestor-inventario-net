'use client';

import {
  ChangeEvent,
  FormEvent,
  ReactNode,
  useCallback,
  useEffect,
  useId,
  useMemo,
  useState
} from 'react';
import { AxiosError } from 'axios';
import { Card } from '../../../../components/ui/card';
import { Button } from '../../../../components/ui/button';
import { Input } from '../../../../components/ui/input';
import { Textarea } from '../../../../components/ui/textarea';
import { apiClient } from '../../../../src/lib/api-client';
import { useConfigureApiClient } from '../../../../src/hooks/use-configure-api-client';
import type { CarrierDto, CustomerDto, SupplierDto } from '../../../../src/types/api';

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
        className="w-full max-w-2xl rounded-2xl bg-white p-6 shadow-xl"
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

        <div className="mt-6">{children}</div>
      </div>
    </div>
  );
}

interface CustomerFormFields {
  name: string;
  email: string;
  phone: string;
  address: string;
  notes: string;
}

interface CustomerFormState extends CustomerFormFields {
  mode: 'create' | 'edit';
  id?: number;
}

interface SupplierFormFields {
  name: string;
  contactName: string;
  email: string;
  phone: string;
  address: string;
  notes: string;
}

interface SupplierFormState extends SupplierFormFields {
  mode: 'create' | 'edit';
  id?: number;
}

interface CarrierFormFields {
  name: string;
  contactName: string;
  email: string;
  phone: string;
  trackingUrl: string;
  notes: string;
}

interface CarrierFormState extends CarrierFormFields {
  mode: 'create' | 'edit';
  id?: number;
}

export default function ContactsPage() {
  const { isConfigured: isApiClientConfigured, tenantId: activeTenantId } = useConfigureApiClient();
  const [customers, setCustomers] = useState<CustomerDto[]>([]);
  const [suppliers, setSuppliers] = useState<SupplierDto[]>([]);
  const [carriers, setCarriers] = useState<CarrierDto[]>([]);
  const [isLoadingCustomers, setIsLoadingCustomers] = useState(true);
  const [isLoadingSuppliers, setIsLoadingSuppliers] = useState(true);
  const [isLoadingCarriers, setIsLoadingCarriers] = useState(true);
  const [customersError, setCustomersError] = useState<string | null>(null);
  const [suppliersError, setSuppliersError] = useState<string | null>(null);
  const [carriersError, setCarriersError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [customerForm, setCustomerForm] = useState<CustomerFormState | null>(null);
  const [supplierForm, setSupplierForm] = useState<SupplierFormState | null>(null);
  const [carrierForm, setCarrierForm] = useState<CarrierFormState | null>(null);
  const [customerFormError, setCustomerFormError] = useState<string | null>(null);
  const [supplierFormError, setSupplierFormError] = useState<string | null>(null);
  const [carrierFormError, setCarrierFormError] = useState<string | null>(null);
  const [isCustomerSubmitting, setIsCustomerSubmitting] = useState(false);
  const [isSupplierSubmitting, setIsSupplierSubmitting] = useState(false);
  const [isCarrierSubmitting, setIsCarrierSubmitting] = useState(false);
  const [feedbackMessage, setFeedbackMessage] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [deletingCustomerId, setDeletingCustomerId] = useState<number | null>(null);
  const [deletingSupplierId, setDeletingSupplierId] = useState<number | null>(null);
  const [deletingCarrierId, setDeletingCarrierId] = useState<number | null>(null);

  const fetchCustomers = useCallback(async () => {
    if (!isApiClientConfigured) {
      return;
    }

    setCustomersError(null);
    setIsLoadingCustomers(true);

    try {
      const response = await apiClient.get<CustomerDto[]>('/customers');
      setCustomers(response.data);
    } catch (error) {
      console.error(error);
      setCustomersError(extractErrorMessage(error, 'No se pudieron cargar los clientes.'));
    } finally {
      setIsLoadingCustomers(false);
    }
  }, [isApiClientConfigured]);

  const fetchSuppliers = useCallback(async () => {
    if (!isApiClientConfigured) {
      return;
    }

    setSuppliersError(null);
    setIsLoadingSuppliers(true);

    try {
      const response = await apiClient.get<SupplierDto[]>('/suppliers');
      setSuppliers(response.data);
    } catch (error) {
      console.error(error);
      setSuppliersError(extractErrorMessage(error, 'No se pudieron cargar los proveedores.'));
    } finally {
      setIsLoadingSuppliers(false);
    }
  }, [isApiClientConfigured]);

  const fetchCarriers = useCallback(async () => {
    if (!isApiClientConfigured) {
      return;
    }

    setCarriersError(null);
    setIsLoadingCarriers(true);

    try {
      const response = await apiClient.get<CarrierDto[]>('/carriers');
      setCarriers(response.data);
    } catch (error) {
      console.error(error);
      setCarriersError(extractErrorMessage(error, 'No se pudieron cargar los transportistas.'));
    } finally {
      setIsLoadingCarriers(false);
    }
  }, [isApiClientConfigured]);

  useEffect(() => {
    if (!isApiClientConfigured) {
      setCustomers([]);
      setSuppliers([]);
      setCarriers([]);
      return;
    }

    fetchCustomers().catch((error) => console.error(error));
    fetchSuppliers().catch((error) => console.error(error));
    fetchCarriers().catch((error) => console.error(error));
  }, [activeTenantId, fetchCarriers, fetchCustomers, fetchSuppliers, isApiClientConfigured]);

  const normalizedSearch = useMemo(() => searchTerm.trim().toLowerCase(), [searchTerm]);

  const filteredCustomers = useMemo(() => {
    if (!normalizedSearch) {
      return customers;
    }

    return customers.filter((customer) =>
      [customer.name, customer.email, customer.phone, customer.address, customer.notes].some((value) =>
        value?.toLowerCase().includes(normalizedSearch) ?? false
      )
    );
  }, [customers, normalizedSearch]);

  const filteredSuppliers = useMemo(() => {
    if (!normalizedSearch) {
      return suppliers;
    }

    return suppliers.filter((supplier) =>
      [
        supplier.name,
        supplier.contactName,
        supplier.email,
        supplier.phone,
        supplier.address,
        supplier.notes
      ].some((value) => value?.toLowerCase().includes(normalizedSearch) ?? false)
    );
  }, [suppliers, normalizedSearch]);

  const filteredCarriers = useMemo(() => {
    if (!normalizedSearch) {
      return carriers;
    }

    return carriers.filter((carrier) =>
      [carrier.name, carrier.contactName, carrier.email, carrier.phone, carrier.trackingUrl, carrier.notes].some(
        (value) => value?.toLowerCase().includes(normalizedSearch) ?? false
      )
    );
  }, [carriers, normalizedSearch]);

  const handleCustomerFieldChange = (field: keyof CustomerFormFields) =>
    (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      const value = event.target.value;
      setCustomerForm((previous) => (previous ? { ...previous, [field]: value } : previous));
    };

  const handleSupplierFieldChange = (field: keyof SupplierFormFields) =>
    (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      const value = event.target.value;
      setSupplierForm((previous) => (previous ? { ...previous, [field]: value } : previous));
    };

  const handleCarrierFieldChange = (field: keyof CarrierFormFields) =>
    (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      const value = event.target.value;
      setCarrierForm((previous) => (previous ? { ...previous, [field]: value } : previous));
    };

  function openCustomerForm(customer?: CustomerDto) {
    setFeedbackMessage(null);
    setActionError(null);
    setCustomerFormError(null);

    if (customer) {
      setCustomerForm({
        mode: 'edit',
        id: customer.id,
        name: customer.name,
        email: customer.email ?? '',
        phone: customer.phone ?? '',
        address: customer.address ?? '',
        notes: customer.notes ?? ''
      });
      return;
    }

    setCustomerForm({ mode: 'create', name: '', email: '', phone: '', address: '', notes: '' });
  }

  function openSupplierForm(supplier?: SupplierDto) {
    setFeedbackMessage(null);
    setActionError(null);
    setSupplierFormError(null);

    if (supplier) {
      setSupplierForm({
        mode: 'edit',
        id: supplier.id,
        name: supplier.name,
        contactName: supplier.contactName ?? '',
        email: supplier.email ?? '',
        phone: supplier.phone ?? '',
        address: supplier.address ?? '',
        notes: supplier.notes ?? ''
      });
      return;
    }

    setSupplierForm({ mode: 'create', name: '', contactName: '', email: '', phone: '', address: '', notes: '' });
  }

  function openCarrierForm(carrier?: CarrierDto) {
    setFeedbackMessage(null);
    setActionError(null);
    setCarrierFormError(null);

    if (carrier) {
      setCarrierForm({
        mode: 'edit',
        id: carrier.id,
        name: carrier.name,
        contactName: carrier.contactName ?? '',
        email: carrier.email ?? '',
        phone: carrier.phone ?? '',
        trackingUrl: carrier.trackingUrl ?? '',
        notes: carrier.notes ?? ''
      });
      return;
    }

    setCarrierForm({
      mode: 'create',
      name: '',
      contactName: '',
      email: '',
      phone: '',
      trackingUrl: '',
      notes: ''
    });
  }

  function closeCustomerForm() {
    if (isCustomerSubmitting) {
      return;
    }

    setCustomerForm(null);
    setCustomerFormError(null);
  }

  function closeSupplierForm() {
    if (isSupplierSubmitting) {
      return;
    }

    setSupplierForm(null);
    setSupplierFormError(null);
  }

  function closeCarrierForm() {
    if (isCarrierSubmitting) {
      return;
    }

    setCarrierForm(null);
    setCarrierFormError(null);
  }

  async function submitCustomerForm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!customerForm) {
      return;
    }

    if (!customerForm.name.trim()) {
      setCustomerFormError('El nombre es obligatorio.');
      return;
    }

    setCustomerFormError(null);
    setFeedbackMessage(null);
    setActionError(null);
    setIsCustomerSubmitting(true);

    try {
      if (customerForm.mode === 'create') {
        await apiClient.post<CustomerDto>('/customers', {
          name: customerForm.name,
          email: customerForm.email || null,
          phone: customerForm.phone || null,
          address: customerForm.address || null,
          notes: customerForm.notes || null
        });
        setFeedbackMessage('Cliente creado correctamente.');
      } else if (customerForm.id !== undefined) {
        await apiClient.put<CustomerDto>(`/customers/${customerForm.id}`, {
          name: customerForm.name,
          email: customerForm.email || null,
          phone: customerForm.phone || null,
          address: customerForm.address || null,
          notes: customerForm.notes || null
        });
        setFeedbackMessage('Cliente actualizado correctamente.');
      }

      setCustomerForm(null);
      await fetchCustomers();
    } catch (error) {
      console.error(error);
      setCustomerFormError(extractErrorMessage(error, 'No se pudo guardar el cliente.'));
    } finally {
      setIsCustomerSubmitting(false);
    }
  }

  async function submitSupplierForm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!supplierForm) {
      return;
    }

    if (!supplierForm.name.trim()) {
      setSupplierFormError('El nombre es obligatorio.');
      return;
    }

    setSupplierFormError(null);
    setFeedbackMessage(null);
    setActionError(null);
    setIsSupplierSubmitting(true);

    try {
      if (supplierForm.mode === 'create') {
        await apiClient.post<SupplierDto>('/suppliers', {
          name: supplierForm.name,
          contactName: supplierForm.contactName || null,
          email: supplierForm.email || null,
          phone: supplierForm.phone || null,
          address: supplierForm.address || null,
          notes: supplierForm.notes || null
        });
        setFeedbackMessage('Proveedor creado correctamente.');
      } else if (supplierForm.id !== undefined) {
        await apiClient.put<SupplierDto>(`/suppliers/${supplierForm.id}`, {
          name: supplierForm.name,
          contactName: supplierForm.contactName || null,
          email: supplierForm.email || null,
          phone: supplierForm.phone || null,
          address: supplierForm.address || null,
          notes: supplierForm.notes || null
        });
        setFeedbackMessage('Proveedor actualizado correctamente.');
      }

      setSupplierForm(null);
      await fetchSuppliers();
    } catch (error) {
      console.error(error);
      setSupplierFormError(extractErrorMessage(error, 'No se pudo guardar el proveedor.'));
    } finally {
      setIsSupplierSubmitting(false);
    }
  }

  async function submitCarrierForm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!carrierForm) {
      return;
    }

    if (!carrierForm.name.trim()) {
      setCarrierFormError('El nombre es obligatorio.');
      return;
    }

    setCarrierFormError(null);
    setFeedbackMessage(null);
    setActionError(null);
    setIsCarrierSubmitting(true);

    try {
      if (carrierForm.mode === 'create') {
        await apiClient.post<CarrierDto>('/carriers', {
          name: carrierForm.name,
          contactName: carrierForm.contactName || null,
          email: carrierForm.email || null,
          phone: carrierForm.phone || null,
          trackingUrl: carrierForm.trackingUrl || null,
          notes: carrierForm.notes || null
        });
        setFeedbackMessage('Transportista creado correctamente.');
      } else if (carrierForm.id !== undefined) {
        await apiClient.put<CarrierDto>(`/carriers/${carrierForm.id}`, {
          name: carrierForm.name,
          contactName: carrierForm.contactName || null,
          email: carrierForm.email || null,
          phone: carrierForm.phone || null,
          trackingUrl: carrierForm.trackingUrl || null,
          notes: carrierForm.notes || null
        });
        setFeedbackMessage('Transportista actualizado correctamente.');
      }

      setCarrierForm(null);
      await fetchCarriers();
    } catch (error) {
      console.error(error);
      setCarrierFormError(extractErrorMessage(error, 'No se pudo guardar el transportista.'));
    } finally {
      setIsCarrierSubmitting(false);
    }
  }

  async function deleteCustomer(id: number) {
    if (typeof window !== 'undefined' && !window.confirm('¿Eliminar este cliente?')) {
      return;
    }

    setFeedbackMessage(null);
    setActionError(null);
    setDeletingCustomerId(id);

    try {
      await apiClient.delete(`/customers/${id}`);
      setFeedbackMessage('Cliente eliminado correctamente.');
      await fetchCustomers();
    } catch (error) {
      console.error(error);
      setActionError(extractErrorMessage(error, 'No se pudo eliminar el cliente.'));
    } finally {
      setDeletingCustomerId(null);
    }
  }

  async function deleteSupplier(id: number) {
    if (typeof window !== 'undefined' && !window.confirm('¿Eliminar este proveedor?')) {
      return;
    }

    setFeedbackMessage(null);
    setActionError(null);
    setDeletingSupplierId(id);

    try {
      await apiClient.delete(`/suppliers/${id}`);
      setFeedbackMessage('Proveedor eliminado correctamente.');
      await fetchSuppliers();
    } catch (error) {
      console.error(error);
      setActionError(extractErrorMessage(error, 'No se pudo eliminar el proveedor.'));
    } finally {
      setDeletingSupplierId(null);
    }
  }

  async function deleteCarrier(id: number) {
    if (typeof window !== 'undefined' && !window.confirm('¿Eliminar este transportista?')) {
      return;
    }

    setFeedbackMessage(null);
    setActionError(null);
    setDeletingCarrierId(id);

    try {
      await apiClient.delete(`/carriers/${id}`);
      setFeedbackMessage('Transportista eliminado correctamente.');
      await fetchCarriers();
    } catch (error) {
      console.error(error);
      setActionError(extractErrorMessage(error, 'No se pudo eliminar el transportista.'));
    } finally {
      setDeletingCarrierId(null);
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold text-slate-900">Contactos comerciales</h1>
        <p className="text-sm text-slate-500">
          Gestiona los clientes, proveedores y transportistas asociados a tus pedidos para mantener la información siempre
          actualizada.
        </p>
      </div>

      {feedbackMessage && (
        <div className="rounded-xl border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700" role="status">
          {feedbackMessage}
        </div>
      )}

      {actionError && (
        <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600" role="alert">
          {actionError}
        </div>
      )}

      <div className="max-w-xl">
        <Input
          label="Buscar contactos"
          placeholder="Nombre, correo, teléfono o dirección"
          value={searchTerm}
          onChange={(event: ChangeEvent<HTMLInputElement>) => setSearchTerm(event.target.value)}
          name="search"
        />
      </div>

      <div className="grid grid-cols-1 gap-6">
        <Card
          title="Clientes"
          subtitle="Personas y empresas a las que vendes"
          action={
            <Button type="button" onClick={() => openCustomerForm()}>
              Nuevo cliente
            </Button>
          }
        >
          <div className="rounded-xl border border-slate-200 bg-white">
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-slate-200 text-sm">
                <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                  <tr>
                    <th className="px-4 py-3 text-left">Cliente</th>
                    <th className="px-4 py-3 text-left">Correo</th>
                    <th className="px-4 py-3 text-left">Teléfono</th>
                    <th className="px-4 py-3 text-right">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {isLoadingCustomers ? (
                    <tr>
                      <td colSpan={4} className="px-4 py-6 text-center text-sm text-slate-500">
                        Cargando clientes…
                      </td>
                    </tr>
                  ) : customersError ? (
                    <tr>
                      <td colSpan={4} className="px-4 py-6 text-center text-sm text-red-500">
                        {customersError}
                      </td>
                    </tr>
                  ) : filteredCustomers.length === 0 ? (
                    <tr>
                      <td colSpan={4} className="px-4 py-6 text-center text-sm text-slate-500">
                        {customers.length === 0
                          ? 'Todavía no has registrado clientes.'
                          : 'No hay clientes que coincidan con la búsqueda.'}
                      </td>
                    </tr>
                  ) : (
                    filteredCustomers.map((customer) => (
                      <tr key={customer.id} className="text-slate-700">
                        <td className="px-4 py-3">
                          <p className="text-sm font-semibold text-slate-900">{customer.name}</p>
                          {customer.address && (
                            <p className="text-xs text-slate-500">{customer.address}</p>
                          )}
                          {customer.notes && (
                            <p className="mt-1 text-xs text-slate-400">{customer.notes}</p>
                          )}
                        </td>
                        <td className="px-4 py-3 text-xs text-slate-500">{customer.email ?? '—'}</td>
                        <td className="px-4 py-3 text-xs text-slate-500">{customer.phone ?? '—'}</td>
                        <td className="px-4 py-3 text-right">
                          <div className="flex justify-end gap-2">
                            <Button type="button" variant="ghost" size="sm" onClick={() => openCustomerForm(customer)}>
                              Editar
                            </Button>
                            <Button
                              type="button"
                              variant="ghost"
                              size="sm"
                              onClick={() => deleteCustomer(customer.id)}
                              disabled={deletingCustomerId === customer.id}
                            >
                              {deletingCustomerId === customer.id ? 'Eliminando…' : 'Eliminar'}
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </Card>

        <Card
          title="Proveedores"
          subtitle="Socios que abastecen tus compras"
          action={
            <Button type="button" onClick={() => openSupplierForm()}>
              Nuevo proveedor
            </Button>
          }
        >
          <div className="rounded-xl border border-slate-200 bg-white">
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-slate-200 text-sm">
                <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                  <tr>
                    <th className="px-4 py-3 text-left">Proveedor</th>
                    <th className="px-4 py-3 text-left">Correo</th>
                    <th className="px-4 py-3 text-left">Teléfono</th>
                    <th className="px-4 py-3 text-right">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {isLoadingSuppliers ? (
                    <tr>
                      <td colSpan={4} className="px-4 py-6 text-center text-sm text-slate-500">
                        Cargando proveedores…
                      </td>
                    </tr>
                  ) : suppliersError ? (
                    <tr>
                      <td colSpan={4} className="px-4 py-6 text-center text-sm text-red-500">
                        {suppliersError}
                      </td>
                    </tr>
                  ) : filteredSuppliers.length === 0 ? (
                    <tr>
                      <td colSpan={4} className="px-4 py-6 text-center text-sm text-slate-500">
                        {suppliers.length === 0
                          ? 'Todavía no has registrado proveedores.'
                          : 'No hay proveedores que coincidan con la búsqueda.'}
                      </td>
                    </tr>
                  ) : (
                    filteredSuppliers.map((supplier) => (
                      <tr key={supplier.id} className="text-slate-700">
                        <td className="px-4 py-3">
                          <p className="text-sm font-semibold text-slate-900">{supplier.name}</p>
                          {supplier.contactName && (
                            <p className="text-xs text-slate-500">Contacto: {supplier.contactName}</p>
                          )}
                          {supplier.address && (
                            <p className="text-xs text-slate-500">{supplier.address}</p>
                          )}
                          {supplier.notes && (
                            <p className="mt-1 text-xs text-slate-400">{supplier.notes}</p>
                          )}
                        </td>
                        <td className="px-4 py-3 text-xs text-slate-500">{supplier.email ?? '—'}</td>
                        <td className="px-4 py-3 text-xs text-slate-500">{supplier.phone ?? '—'}</td>
                        <td className="px-4 py-3 text-right">
                          <div className="flex justify-end gap-2">
                            <Button type="button" variant="ghost" size="sm" onClick={() => openSupplierForm(supplier)}>
                              Editar
                            </Button>
                            <Button
                              type="button"
                              variant="ghost"
                              size="sm"
                              onClick={() => deleteSupplier(supplier.id)}
                              disabled={deletingSupplierId === supplier.id}
                            >
                              {deletingSupplierId === supplier.id ? 'Eliminando…' : 'Eliminar'}
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </Card>

        <Card
          title="Transportistas"
          subtitle="Operadores logísticos encargados de tus envíos"
          action={
            <Button type="button" onClick={() => openCarrierForm()}>
              Nuevo transportista
            </Button>
          }
        >
          <div className="rounded-xl border border-slate-200 bg-white">
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-slate-200 text-sm">
                <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                  <tr>
                    <th className="px-4 py-3 text-left">Transportista</th>
                    <th className="px-4 py-3 text-left">Correo</th>
                    <th className="px-4 py-3 text-left">Teléfono</th>
                    <th className="px-4 py-3 text-right">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {isLoadingCarriers ? (
                    <tr>
                      <td colSpan={4} className="px-4 py-6 text-center text-sm text-slate-500">
                        Cargando transportistas…
                      </td>
                    </tr>
                  ) : carriersError ? (
                    <tr>
                      <td colSpan={4} className="px-4 py-6 text-center text-sm text-red-500">
                        {carriersError}
                      </td>
                    </tr>
                  ) : filteredCarriers.length === 0 ? (
                    <tr>
                      <td colSpan={4} className="px-4 py-6 text-center text-sm text-slate-500">
                        {carriers.length === 0
                          ? 'Todavía no has registrado transportistas.'
                          : 'No hay transportistas que coincidan con la búsqueda.'}
                      </td>
                    </tr>
                  ) : (
                    filteredCarriers.map((carrier) => (
                      <tr key={carrier.id} className="text-slate-700">
                        <td className="px-4 py-3">
                          <p className="text-sm font-semibold text-slate-900">{carrier.name}</p>
                          {carrier.contactName && (
                            <p className="text-xs text-slate-500">Contacto: {carrier.contactName}</p>
                          )}
                          {carrier.trackingUrl && (
                            <p className="text-xs text-slate-500">
                              Seguimiento:{' '}
                              <a
                                href={carrier.trackingUrl}
                                className="text-primary-600 underline"
                                target="_blank"
                                rel="noopener noreferrer"
                              >
                                {carrier.trackingUrl}
                              </a>
                            </p>
                          )}
                          {carrier.notes && (
                            <p className="mt-1 text-xs text-slate-400">{carrier.notes}</p>
                          )}
                        </td>
                        <td className="px-4 py-3 text-xs text-slate-500">{carrier.email ?? '—'}</td>
                        <td className="px-4 py-3 text-xs text-slate-500">{carrier.phone ?? '—'}</td>
                        <td className="px-4 py-3 text-right">
                          <div className="flex justify-end gap-2">
                            <Button type="button" variant="ghost" size="sm" onClick={() => openCarrierForm(carrier)}>
                              Editar
                            </Button>
                            <Button
                              type="button"
                              variant="ghost"
                              size="sm"
                              onClick={() => deleteCarrier(carrier.id)}
                              disabled={deletingCarrierId === carrier.id}
                            >
                              {deletingCarrierId === carrier.id ? 'Eliminando…' : 'Eliminar'}
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </Card>
      </div>

      {customerForm && (
        <Modal
          title={customerForm.mode === 'create' ? 'Nuevo cliente' : 'Editar cliente'}
          description="Completa la información del cliente para asociarla a tus pedidos."
          onClose={closeCustomerForm}
          disableClose={isCustomerSubmitting}
        >
          <form className="space-y-4" onSubmit={submitCustomerForm}>
            <Input
              label="Nombre"
              name="customer-name"
              value={customerForm.name}
              onChange={handleCustomerFieldChange('name')}
              required
            />
            <Input
              label="Correo electrónico"
              name="customer-email"
              type="email"
              value={customerForm.email}
              onChange={handleCustomerFieldChange('email')}
            />
            <Input
              label="Teléfono"
              name="customer-phone"
              value={customerForm.phone}
              onChange={handleCustomerFieldChange('phone')}
            />
            <Input
              label="Dirección"
              name="customer-address"
              value={customerForm.address}
              onChange={handleCustomerFieldChange('address')}
            />
            <Textarea
              label="Notas"
              name="customer-notes"
              value={customerForm.notes}
              onChange={handleCustomerFieldChange('notes')}
              rows={3}
            />

            {customerFormError && (
              <div className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-600" role="alert">
                {customerFormError}
              </div>
            )}

            <div className="flex justify-end gap-2">
              <Button type="button" variant="ghost" onClick={closeCustomerForm} disabled={isCustomerSubmitting}>
                Cancelar
              </Button>
              <Button type="submit" disabled={isCustomerSubmitting}>
                {isCustomerSubmitting
                  ? 'Guardando…'
                  : customerForm.mode === 'create'
                  ? 'Crear cliente'
                  : 'Actualizar cliente'}
              </Button>
            </div>
          </form>
        </Modal>
      )}

      {supplierForm && (
        <Modal
          title={supplierForm.mode === 'create' ? 'Nuevo proveedor' : 'Editar proveedor'}
          description="Registra a tus proveedores para agilizar la generación de pedidos de compra."
          onClose={closeSupplierForm}
          disableClose={isSupplierSubmitting}
        >
          <form className="space-y-4" onSubmit={submitSupplierForm}>
            <Input
              label="Nombre"
              name="supplier-name"
              value={supplierForm.name}
              onChange={handleSupplierFieldChange('name')}
              required
            />
            <Input
              label="Persona de contacto"
              name="supplier-contact"
              value={supplierForm.contactName}
              onChange={handleSupplierFieldChange('contactName')}
            />
            <Input
              label="Correo electrónico"
              name="supplier-email"
              type="email"
              value={supplierForm.email}
              onChange={handleSupplierFieldChange('email')}
            />
            <Input
              label="Teléfono"
              name="supplier-phone"
              value={supplierForm.phone}
              onChange={handleSupplierFieldChange('phone')}
            />
            <Input
              label="Dirección"
              name="supplier-address"
              value={supplierForm.address}
              onChange={handleSupplierFieldChange('address')}
            />
            <Textarea
              label="Notas"
              name="supplier-notes"
              value={supplierForm.notes}
              onChange={handleSupplierFieldChange('notes')}
              rows={3}
            />

            {supplierFormError && (
              <div className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-600" role="alert">
                {supplierFormError}
              </div>
            )}

            <div className="flex justify-end gap-2">
              <Button type="button" variant="ghost" onClick={closeSupplierForm} disabled={isSupplierSubmitting}>
                Cancelar
              </Button>
              <Button type="submit" disabled={isSupplierSubmitting}>
                {isSupplierSubmitting
                  ? 'Guardando…'
                  : supplierForm.mode === 'create'
                  ? 'Crear proveedor'
                  : 'Actualizar proveedor'}
              </Button>
            </div>
          </form>
        </Modal>
      )}

      {carrierForm && (
        <Modal
          title={carrierForm.mode === 'create' ? 'Nuevo transportista' : 'Editar transportista'}
          description="Registra los operadores logísticos con los que trabajas para vincularlos a tus pedidos."
          onClose={closeCarrierForm}
          disableClose={isCarrierSubmitting}
        >
          <form className="space-y-4" onSubmit={submitCarrierForm}>
            <Input
              label="Nombre"
              name="carrier-name"
              value={carrierForm.name}
              onChange={handleCarrierFieldChange('name')}
              required
            />
            <Input
              label="Persona de contacto"
              name="carrier-contact"
              value={carrierForm.contactName}
              onChange={handleCarrierFieldChange('contactName')}
            />
            <Input
              label="Correo electrónico"
              name="carrier-email"
              type="email"
              value={carrierForm.email}
              onChange={handleCarrierFieldChange('email')}
            />
            <Input
              label="Teléfono"
              name="carrier-phone"
              value={carrierForm.phone}
              onChange={handleCarrierFieldChange('phone')}
            />
            <Input
              label="URL de seguimiento"
              name="carrier-tracking"
              value={carrierForm.trackingUrl}
              onChange={handleCarrierFieldChange('trackingUrl')}
              placeholder="https://"
            />
            <Textarea
              label="Notas"
              name="carrier-notes"
              value={carrierForm.notes}
              onChange={handleCarrierFieldChange('notes')}
              rows={3}
            />

            {carrierFormError && (
              <div className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-600" role="alert">
                {carrierFormError}
              </div>
            )}

            <div className="flex justify-end gap-2">
              <Button type="button" variant="ghost" onClick={closeCarrierForm} disabled={isCarrierSubmitting}>
                Cancelar
              </Button>
              <Button type="submit" disabled={isCarrierSubmitting}>
                {isCarrierSubmitting
                  ? 'Guardando…'
                  : carrierForm.mode === 'create'
                  ? 'Crear transportista'
                  : 'Actualizar transportista'}
              </Button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
