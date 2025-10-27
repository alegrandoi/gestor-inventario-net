'use client';

import { useCallback, useEffect, useState } from 'react';
import type { ChangeEvent, FormEvent } from 'react';
import { apiClient } from '../../../../src/lib/api-client';
import type { AuditLogDto, PagedResult, RoleDto, TenantDto, UserSummaryDto } from '../../../../src/types/api';
import { Card } from '../../../../components/ui/card';
import { Button } from '../../../../components/ui/button';
import { Input } from '../../../../components/ui/input';
import { Select } from '../../../../components/ui/select';

type AuditFilterState = {
  entityName: string;
  action: string;
  userId: string;
  from: string;
  to: string;
};

const initialAuditFilters: AuditFilterState = {
  entityName: '',
  action: '',
  userId: 'all',
  from: '',
  to: ''
};

type AuditPayload = {
  description?: string;
  changes?: Record<string, { old: unknown; new: unknown }>;
  metadata?: { performedBy?: string; performedById?: number; timestamp?: string };
};

type AuditPaginationState = {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

type CreateUserFormState = {
  username: string;
  email: string;
  password: string;
  role: string;
};

const emptyUserForm: CreateUserFormState = {
  username: '',
  email: '',
  password: '',
  role: ''
};

const DEFAULT_PAGE_NUMBER = 1;
const AUDIT_PAGE_SIZE = 25;
const AUDIT_EXPORT_PAGE_SIZE = 200;

const createInitialAuditPagination = (): AuditPaginationState => ({
  pageNumber: DEFAULT_PAGE_NUMBER,
  pageSize: AUDIT_PAGE_SIZE,
  totalCount: 0,
  totalPages: 1
});

function buildAuditQueryParams(filters: AuditFilterState, pageNumber: number, pageSize: number) {
  const params = new URLSearchParams();

  const trimmedEntity = filters.entityName.trim();
  const trimmedAction = filters.action.trim();

  if (trimmedEntity) {
    params.set('entityName', trimmedEntity);
  }

  if (trimmedAction) {
    params.set('action', trimmedAction);
  }

  if (filters.userId !== 'all') {
    const parsedUserId = Number(filters.userId);
    if (!Number.isNaN(parsedUserId)) {
      params.set('userId', parsedUserId.toString());
    }
  }

  if (filters.from) {
    const fromDate = new Date(filters.from);
    if (!Number.isNaN(fromDate.getTime())) {
      params.set('from', fromDate.toISOString());
    }
  }

  if (filters.to) {
    const toDate = new Date(filters.to);
    if (!Number.isNaN(toDate.getTime())) {
      params.set('to', toDate.toISOString());
    }
  }

  params.set('pageNumber', Math.max(1, pageNumber).toString());
  params.set('pageSize', Math.max(1, pageSize).toString());

  return params;
}

export default function AdminPage() {
  const [users, setUsers] = useState<UserSummaryDto[]>([]);
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [userForm, setUserForm] = useState<CreateUserFormState>(emptyUserForm);
  const [userSuccess, setUserSuccess] = useState<string | null>(null);
  const [userError, setUserError] = useState<string | null>(null);
  const [isCreatingUser, setIsCreatingUser] = useState(false);

  const [auditLogs, setAuditLogs] = useState<AuditLogDto[]>([]);
  const [auditError, setAuditError] = useState<string | null>(null);
  const [auditSuccess, setAuditSuccess] = useState<string | null>(null);
  const [isAuditLoading, setIsAuditLoading] = useState(false);
  const [auditFilters, setAuditFilters] = useState<AuditFilterState>(initialAuditFilters);
  const [auditPagination, setAuditPagination] = useState<AuditPaginationState>(() => createInitialAuditPagination());
  const [tenants, setTenants] = useState<TenantDto[]>([]);
  const [selectedTenant, setSelectedTenant] = useState<string>('');
  const [deletingAuditLogId, setDeletingAuditLogId] = useState<number | null>(null);

  const fetchAuditLogs = useCallback(async (
    filters: AuditFilterState,
    pageNumber = DEFAULT_PAGE_NUMBER,
    pageSize = AUDIT_PAGE_SIZE
  ) => {
    setIsAuditLoading(true);
    setAuditError(null);
    setAuditSuccess(null);

    try {
      const params = buildAuditQueryParams(filters, pageNumber, pageSize);
      const queryString = params.toString();
      const endpoint = queryString ? `/auditlogs?${queryString}` : '/auditlogs';
      const response = await apiClient.get<PagedResult<AuditLogDto>>(endpoint);
      setAuditLogs(response.data.items);
      setAuditPagination({
        pageNumber: response.data.pageNumber,
        pageSize: response.data.pageSize,
        totalCount: response.data.totalCount,
        totalPages: response.data.totalPages
      });
    } catch (err) {
      console.error(err);
      setAuditError('No se pudo cargar el historial de auditoría.');
      setAuditLogs([]);
      setAuditPagination(createInitialAuditPagination());
    } finally {
      setIsAuditLoading(false);
    }
  }, []);

  const refreshDataForTenant = useCallback(
    async (tenantId: string, filters: AuditFilterState, pageNumber = DEFAULT_PAGE_NUMBER) => {
      setIsLoading(true);
      setError(null);
      setUserError(null);
      setUserSuccess(null);
      setAuditSuccess(null);
      setAuditPagination(createInitialAuditPagination());

      try {
        if (tenantId) {
          apiClient.defaults.headers.common['X-Tenant-Id'] = tenantId;
        } else {
        delete apiClient.defaults.headers.common['X-Tenant-Id'];
      }

      const [usersResponse, rolesResponse] = await Promise.all([
        apiClient.get<UserSummaryDto[]>('/users'),
        apiClient.get<RoleDto[]>('/roles')
      ]);

      setUsers(usersResponse.data);
      setRoles(rolesResponse.data);

      await fetchAuditLogs(filters, pageNumber);
    } catch (err) {
      console.error(err);
      setError('No se pudieron cargar los datos administrativos.');
    } finally {
      setIsLoading(false);
    }
  }, [fetchAuditLogs]);

  const initializeTenants = useCallback(async () => {
    try {
      const tenantResponse = await apiClient.get<TenantDto[]>('/tenants');
      const availableTenants = tenantResponse.data;
      setTenants(availableTenants);

      if (availableTenants.length === 0) {
        setSelectedTenant('');
        setUsers([]);
        setRoles([]);
        setAuditLogs([]);
        setAuditPagination(createInitialAuditPagination());
        setIsLoading(false);
        return;
      }

      const storedTenantId = typeof window !== 'undefined' ? window.localStorage.getItem('tenantId') : null;
      let tenantIdToUse = storedTenantId ?? '';

      if (!tenantIdToUse || !availableTenants.some((tenant) => tenant.id.toString() === tenantIdToUse)) {
        tenantIdToUse = availableTenants[0].id.toString();
      }

      setSelectedTenant(tenantIdToUse);

      if (typeof window !== 'undefined') {
        window.localStorage.setItem('tenantId', tenantIdToUse);
      }

      setAuditFilters(initialAuditFilters);
      await refreshDataForTenant(tenantIdToUse, initialAuditFilters, DEFAULT_PAGE_NUMBER);
    } catch (err) {
      console.error(err);
      setError('No se pudieron cargar los inquilinos.');
      setIsLoading(false);
    }
  }, [refreshDataForTenant]);

  useEffect(() => {
    initializeTenants().catch((err) => console.error(err));
  }, [initializeTenants]);

  useEffect(() => {
    if (roles.length === 0) {
      setUserForm((prev) => ({ ...prev, role: '' }));
      return;
    }

    setUserForm((prev) => {
      if (prev.role && roles.some((role) => role.name === prev.role)) {
        return prev;
      }

      return { ...prev, role: roles[0].name };
    });
  }, [roles]);

  async function updateRole(userId: number, role: string) {
    try {
      const response = await apiClient.put<UserSummaryDto>(`/users/${userId}/role`, { userId, role });
      setUsers((prev) => prev.map((user) => (user.id === userId ? response.data : user)));
    } catch (err) {
      console.error(err);
      setError('No se pudo actualizar el rol.');
    }
  }

  async function toggleStatus(userId: number, isActive: boolean) {
    try {
      const response = await apiClient.put<UserSummaryDto>(`/users/${userId}/status`, { userId, isActive });
      setUsers((prev) => prev.map((user) => (user.id === userId ? response.data : user)));
    } catch (err) {
      console.error(err);
      setError('No se pudo actualizar el estado del usuario.');
    }
  }

  function updateUserFormField<Field extends keyof CreateUserFormState>(field: Field, value: CreateUserFormState[Field]) {
    setUserForm((prev) => ({ ...prev, [field]: value }));
  }

  function handleFilterChange(event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) {
    const { name, value } = event.target;
    setAuditFilters((prev) => ({ ...prev, [name]: value }));
  }

  async function handleFilterSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await fetchAuditLogs(auditFilters, DEFAULT_PAGE_NUMBER);
  }

  async function handleResetFilters() {
    setAuditFilters(initialAuditFilters);
    await fetchAuditLogs(initialAuditFilters, DEFAULT_PAGE_NUMBER);
  }

  async function handleAuditPageChange(nextPage: number) {
    if (
      nextPage === auditPagination.pageNumber ||
      nextPage < 1 ||
      nextPage > auditPagination.totalPages ||
      auditPagination.totalCount === 0
    ) {
      return;
    }

    await fetchAuditLogs(auditFilters, nextPage, auditPagination.pageSize);
  }

  async function handleCreateUser(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setUserSuccess(null);
    setUserError(null);

    if (!selectedTenant) {
      setUserError('Selecciona un inquilino antes de crear usuarios.');
      return;
    }

    if (roles.length === 0) {
      setUserError('No hay roles disponibles para asignar.');
      return;
    }

    const username = userForm.username.trim();
    const email = userForm.email.trim();
    const password = userForm.password.trim();
    const role = userForm.role;

    if (!username || !email || !password || !role) {
      setUserError('Completa todos los campos del nuevo usuario.');
      return;
    }

    setIsCreatingUser(true);

    try {
      await apiClient.post('/auth/register', {
        username,
        email,
        password,
        role
      });

      const nextRole = roles.some((item) => item.name === role) ? role : roles[0].name;

      setUserForm({
        username: '',
        email: '',
        password: '',
        role: nextRole
      });

      await refreshDataForTenant(selectedTenant, auditFilters, DEFAULT_PAGE_NUMBER);
      setUserSuccess(`Usuario ${username} creado correctamente.`);
    } catch (err) {
      console.error(err);
      setUserError('No se pudo crear el usuario. Revisa los datos proporcionados.');
    } finally {
      setIsCreatingUser(false);
    }
  }

  async function handleTenantChange(event: ChangeEvent<HTMLSelectElement>) {
    const tenantId = event.target.value;
    setSelectedTenant(tenantId);

    if (typeof window !== 'undefined') {
      window.localStorage.setItem('tenantId', tenantId);
    }

    await refreshDataForTenant(tenantId, auditFilters, DEFAULT_PAGE_NUMBER);
  }

  function parsePayload(log: AuditLogDto): AuditPayload | null {
    if (!log.changes) {
      return null;
    }

    try {
      return JSON.parse(log.changes) as AuditPayload;
    } catch (error) {
      console.error('No se pudo parsear el detalle de auditoría', error);
      return null;
    }
  }

  function formatChangeValue(value: unknown): string {
    if (value === null || value === undefined) {
      return '—';
    }

    if (typeof value === 'object') {
      try {
        return JSON.stringify(value);
      } catch (error) {
        console.error('No se pudo serializar el valor de cambio', error);
        return String(value);
      }
    }

    return String(value);
  }

  function buildCsvRow(values: unknown[]): string {
    return values
      .map((value) => {
        if (value === null || value === undefined) {
          return '""';
        }

        const normalized = String(value).replace(/"/g, '""');
        return `"${normalized}"`;
      })
      .join(',');
  }

  async function fetchAllAuditLogsForExport(filters: AuditFilterState) {
    const aggregatedLogs: AuditLogDto[] = [];
    let currentPage = DEFAULT_PAGE_NUMBER;
    let totalPages = 1;

    while (true) {
      const params = buildAuditQueryParams(filters, currentPage, AUDIT_EXPORT_PAGE_SIZE);
      const queryString = params.toString();
      const endpoint = queryString ? `/auditlogs?${queryString}` : '/auditlogs';
      const response = await apiClient.get<PagedResult<AuditLogDto>>(endpoint);
      aggregatedLogs.push(...response.data.items);
      totalPages = Math.max(response.data.totalPages, 1);

      if (currentPage >= totalPages) {
        break;
      }

      currentPage += 1;
    }

    return aggregatedLogs;
  }

  async function handleExport() {
    if (auditPagination.totalCount === 0) {
      return;
    }

    setAuditError(null);
    setAuditSuccess(null);

    let logsForExport: AuditLogDto[] = [];

    try {
      logsForExport = await fetchAllAuditLogsForExport(auditFilters);
    } catch (error) {
      console.error(error);
      setAuditError('No se pudieron obtener las auditorías para exportar.');
      return;
    }

    if (logsForExport.length === 0) {
      return;
    }

    const header = buildCsvRow(['ID', 'Fecha', 'Entidad', 'Acción', 'Usuario', 'Descripción', 'Cambios']);

    const rows = logsForExport.map((log) => {
      const payload = parsePayload(log);
      const metadataUser = payload?.metadata?.performedBy ?? log.username ?? '';
      const description = payload?.description ?? '';

      const changesSummary = payload?.changes
        ? Object.entries(payload.changes)
            .map(([key, value]) => {
              const oldValue = formatChangeValue(value.old);
              const newValue = formatChangeValue(value.new);
              return `${key}: ${oldValue} -> ${newValue}`;
            })
            .join(' | ')
        : '';

      const entityLabel = log.entityId ? `${log.entityName} #${log.entityId}` : log.entityName;

      return buildCsvRow([
        log.id,
        new Date(log.createdAt).toISOString(),
        entityLabel,
        log.action,
        metadataUser,
        description,
        changesSummary
      ]);
    });

    const csvContent = [header, ...rows].join('\r\n');
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);

    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `audit-logs-${new Date().toISOString()}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  async function handleDeleteAuditLog(auditLogId: number) {
    const confirmed = window.confirm(
      '¿Seguro que deseas eliminar este registro de auditoría? Esta acción es irreversible.'
    );

    if (!confirmed) {
      return;
    }

    const currentPage = auditPagination.pageNumber;
    const currentPageSize = auditPagination.pageSize;

    setDeletingAuditLogId(auditLogId);
    setAuditError(null);
    setAuditSuccess(null);

    try {
      await apiClient.delete(`/auditlogs/${auditLogId}`);
      await fetchAuditLogs(auditFilters, currentPage, currentPageSize);
      setAuditSuccess(`Registro de auditoría #${auditLogId} eliminado correctamente.`);
    } catch (err) {
      console.error(err);
      setAuditError('No se pudo eliminar el registro de auditoría seleccionado.');
    } finally {
      setDeletingAuditLogId(null);
    }
  }

  const hasAuditEntries = auditPagination.totalCount > 0;
  const auditRangeStart = hasAuditEntries
    ? (auditPagination.pageNumber - 1) * auditPagination.pageSize + 1
    : 0;
  const auditRangeEnd = hasAuditEntries
    ? Math.min(auditPagination.pageNumber * auditPagination.pageSize, auditPagination.totalCount)
    : 0;
  const auditCurrentPage = hasAuditEntries ? auditPagination.pageNumber : 0;
  const auditTotalPages = hasAuditEntries ? auditPagination.totalPages : 0;

  return (
    <div className="flex flex-col gap-6">
      <Card
        title="Inquilinos"
        subtitle="Selecciona el inquilino activo para administrar usuarios, roles y auditoría"
        action={
          <Select
            name="tenantId"
            value={selectedTenant}
            onChange={(event) => {
              handleTenantChange(event).catch((err) => console.error(err));
            }}
            disabled={tenants.length === 0 || isLoading}
          >
            <option value="" disabled>
              {tenants.length === 0 ? 'Sin inquilinos disponibles' : 'Selecciona un inquilino'}
            </option>
            {tenants.map((tenant) => (
              <option key={tenant.id} value={tenant.id}>
                {tenant.name}
              </option>
            ))}
          </Select>
        }
      >
        <p className="text-sm text-slate-600">
          Las operaciones que realices se aplicarán exclusivamente al inquilino seleccionado. Cambiar el inquilino recarga
          usuarios, roles y registros de auditoría asociados.
        </p>
      </Card>

      <Card title="Gestión de usuarios" subtitle="Controla roles, estados y accesos al sistema">
        {error && (
          <div className="mb-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">{error}</div>
        )}
        {userError && (
          <div className="mb-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">{userError}</div>
        )}
        {userSuccess && (
          <div className="mb-4 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
            {userSuccess}
          </div>
        )}
        <form
          onSubmit={handleCreateUser}
          className="mb-6 grid gap-3 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm md:grid-cols-5 md:items-end"
        >
          <Input
            label="Usuario"
            value={userForm.username}
            onChange={(event) => updateUserFormField('username', event.target.value)}
            placeholder="Nombre de usuario"
            required
            disabled={isLoading || isCreatingUser}
          />
          <Input
            label="Correo"
            type="email"
            value={userForm.email}
            onChange={(event) => updateUserFormField('email', event.target.value)}
            placeholder="usuario@empresa.com"
            required
            disabled={isLoading || isCreatingUser}
          />
          <Input
            label="Contraseña temporal"
            type="password"
            value={userForm.password}
            onChange={(event) => updateUserFormField('password', event.target.value)}
            placeholder="Contraseña inicial"
            required
            disabled={isLoading || isCreatingUser}
          />
          <Select
            label="Rol"
            value={userForm.role}
            onChange={(event) => updateUserFormField('role', event.target.value)}
            disabled={isLoading || isCreatingUser || roles.length === 0}
            hint={roles.length === 0 ? 'Crea un rol antes de asignarlo a un usuario.' : undefined}
          >
            {roles.length === 0 ? (
              <option value="">Sin roles disponibles</option>
            ) : (
              roles.map((role) => (
                <option key={role.id} value={role.name}>
                  {role.name}
                </option>
              ))
            )}
          </Select>
          <div className="flex w-full flex-col gap-1 md:self-stretch">
            <span className="text-sm font-medium text-slate-700 md:invisible">Acciones</span>
            <div className="flex justify-end">
              <Button
                type="submit"
                size="sm"
                className="w-full md:w-auto"
                disabled={
                  isCreatingUser ||
                  isLoading ||
                  roles.length === 0 ||
                  selectedTenant.length === 0
                }
              >
                {isCreatingUser ? 'Creando…' : 'Crear usuario'}
              </Button>
            </div>
            <div aria-hidden="true" className="min-h-[1.25rem]" />
          </div>
        </form>
        <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          <table className="min-w-full divide-y divide-slate-200 text-sm">
            <thead className="bg-slate-50 text-xs uppercase text-slate-500">
              <tr>
                <th className="px-4 py-3 text-left">Usuario</th>
                <th className="px-4 py-3 text-left">Correo</th>
                <th className="px-4 py-3 text-left">Rol</th>
                <th className="px-4 py-3 text-left">Estado</th>
                <th className="px-4 py-3 text-right">Acciones</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {isLoading ? (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center text-sm text-slate-500">
                    Cargando usuarios…
                  </td>
                </tr>
              ) : users.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center text-sm text-slate-500">
                    No hay usuarios registrados.
                  </td>
                </tr>
              ) : (
                users.map((user) => (
                  <tr key={user.id} className="text-slate-700">
                    <td className="px-4 py-3 text-sm font-semibold text-slate-900">{user.username}</td>
                    <td className="px-4 py-3 text-xs text-slate-500">{user.email}</td>
                    <td className="px-4 py-3">
                      <select
                        className="w-44 rounded-lg border border-slate-200 bg-white px-2 py-1 text-xs text-slate-700 focus:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-100"
                        value={user.role}
                        onChange={(event) => updateRole(user.id, event.target.value)}
                      >
                        {roles.map((role) => (
                          <option key={role.id} value={role.name}>
                            {role.name}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td className="px-4 py-3 text-xs text-slate-500">{user.isActive ? 'Activo' : 'Suspendido'}</td>
                    <td className="px-4 py-3 text-right">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => toggleStatus(user.id, !user.isActive)}
                      >
                        {user.isActive ? 'Suspender' : 'Reactivar'}
                      </Button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </Card>

      <Card title="Auditoría del sistema" subtitle="Visualiza operaciones sensibles y exporta los registros">
        {auditError && (
          <div className="mb-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
            {auditError}
          </div>
        )}
        {auditSuccess && (
          <div className="mb-4 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
            {auditSuccess}
          </div>
        )}
        <form
          onSubmit={handleFilterSubmit}
          className="mb-4 grid gap-4 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm md:grid-cols-5"
        >
          <Input
            name="entityName"
            label="Entidad"
            placeholder="Producto, InventoryStock..."
            value={auditFilters.entityName}
            onChange={handleFilterChange}
          />
          <Input
            name="action"
            label="Acción"
            placeholder="ProductCreated"
            value={auditFilters.action}
            onChange={handleFilterChange}
          />
          <Select
            name="userId"
            label="Usuario"
            value={auditFilters.userId}
            onChange={handleFilterChange}
          >
            <option value="all">Todos</option>
            {users.map((user) => (
              <option key={user.id} value={user.id}>
                {user.username}
              </option>
            ))}
          </Select>
          <Input
            name="from"
            type="date"
            label="Desde"
            value={auditFilters.from}
            onChange={handleFilterChange}
          />
          <Input
            name="to"
            type="date"
            label="Hasta"
            value={auditFilters.to}
            onChange={handleFilterChange}
          />
          <div className="flex items-end gap-2 md:col-span-5">
            <Button type="submit" size="sm">
              Aplicar filtros
            </Button>
            <Button type="button" variant="outline" size="sm" onClick={handleResetFilters}>
              Restablecer
            </Button>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => {
                void handleExport();
              }}
              disabled={auditPagination.totalCount === 0 || isAuditLoading}
            >
              Exportar CSV
            </Button>
          </div>
        </form>

        <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          <table className="min-w-full divide-y divide-slate-200 text-sm">
            <thead className="bg-slate-50 text-xs uppercase text-slate-500">
              <tr>
                <th className="px-4 py-3 text-left">Fecha</th>
                <th className="px-4 py-3 text-left">Entidad</th>
                <th className="px-4 py-3 text-left">Acción</th>
                <th className="px-4 py-3 text-left">Usuario</th>
                <th className="px-4 py-3 text-left">Detalles</th>
                <th className="px-4 py-3 text-left">Acciones</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {isAuditLoading ? (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-sm text-slate-500">
                    Cargando auditorías…
                  </td>
                </tr>
              ) : auditLogs.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-sm text-slate-500">
                    No se encontraron registros de auditoría.
                  </td>
                </tr>
              ) : (
                auditLogs.map((log) => {
                  const payload = parsePayload(log);
                  const metadata = payload?.metadata;
                  const changeEntries = payload?.changes ? Object.entries(payload.changes) : [];
                  const isDeletingAuditLog = deletingAuditLogId === log.id;

                  return (
                    <tr key={log.id} className="align-top text-slate-700">
                      <td className="px-4 py-3 text-xs text-slate-500">
                        {new Date(log.createdAt).toLocaleString()}
                      </td>
                      <td className="px-4 py-3 text-sm font-semibold text-slate-900">
                        {log.entityName}
                        {log.entityId ? <span className="ml-1 text-xs text-slate-500">#{log.entityId}</span> : null}
                      </td>
                      <td className="px-4 py-3 text-xs text-slate-500">{log.action}</td>
                      <td className="px-4 py-3 text-xs text-slate-500">
                        {metadata?.performedBy ?? log.username ?? '—'}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex flex-col gap-2 text-xs text-slate-600">
                          <span className="text-slate-500">
                            {payload?.description ?? 'Sin descripción disponible.'}
                          </span>
                          {changeEntries.length > 0 && (
                            <ul className="space-y-1">
                              {changeEntries.map(([key, value]) => (
                                <li key={key}>
                                  <span className="font-medium text-slate-700">{key}</span>:{' '}
                                  <span>
                                    {formatChangeValue(value.old)} → {formatChangeValue(value.new)}
                                  </span>
                                </li>
                              ))}
                            </ul>
                          )}
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => {
                            void handleDeleteAuditLog(log.id);
                          }}
                          disabled={isDeletingAuditLog}
                        >
                          {isDeletingAuditLog ? 'Eliminando…' : 'Eliminar registro'}
                        </Button>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
          <div className="flex flex-col gap-3 border-t border-slate-100 bg-white p-4 text-xs text-slate-500 md:flex-row md:items-center md:justify-between">
            <div>
              {hasAuditEntries
                ? `Mostrando ${auditRangeStart}–${auditRangeEnd} de ${auditPagination.totalCount} registros`
                : 'Mostrando 0 registros'}
            </div>
            <div className="flex items-center gap-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => {
                  void handleAuditPageChange(auditPagination.pageNumber - 1);
                }}
                disabled={!hasAuditEntries || isAuditLoading || auditPagination.pageNumber <= 1}
              >
                Anterior
              </Button>
              <span>Página {auditCurrentPage} de {auditTotalPages}</span>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => {
                  void handleAuditPageChange(auditPagination.pageNumber + 1);
                }}
                disabled={!hasAuditEntries || isAuditLoading || auditPagination.pageNumber >= auditPagination.totalPages}
              >
                Siguiente
              </Button>
            </div>
          </div>
        </div>
      </Card>
    </div>
  );
}
