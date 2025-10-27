'use client';

import { useEffect, useState } from 'react';
import { apiClient } from '../../../src/lib/api-client';
import type { InventoryDashboardDto } from '../../../src/types/api';
import { Card } from '../../../components/ui/card';
import { Badge } from '../../../components/ui/badge';
import { InfoTooltip } from '../../../components/ui/info-tooltip';
import { ResponsiveContainer, AreaChart, Area, XAxis, YAxis, Tooltip } from 'recharts';

export default function DashboardHomePage() {
  const [data, setData] = useState<InventoryDashboardDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchDashboard() {
      try {
        const response = await apiClient.get<InventoryDashboardDto>('/analytics/dashboard');
        setData(response.data);
      } catch (err) {
        console.error(err);
        setError('No se pudo recuperar el panel de métricas.');
      } finally {
        setIsLoading(false);
      }
    }

    fetchDashboard().catch((err) => console.error(err));
  }, []);

  if (isLoading) {
    return <div className="text-sm text-slate-500">Cargando indicadores…</div>;
  }

  if (error || !data) {
    return <div className="text-sm text-red-500">{error ?? 'Sin datos disponibles en este momento.'}</div>;
  }

  const { totalProducts, activeProducts, totalInventoryValue, lowStockVariants, reorderAlerts, topSellingProducts, monthlySales } =
    data;

  const activeCatalogRate = totalProducts > 0 ? Math.round((activeProducts / totalProducts) * 100) : 0;
  const latestMonthlySales = monthlySales.slice(-1)[0]?.totalAmount ?? 0;
  const reorderAlertCount = reorderAlerts.length;

  return (
    <div className="flex flex-col gap-6">
      <section className="relative overflow-hidden rounded-3xl border border-slate-200 bg-gradient-to-br from-primary-50 via-white to-slate-50 p-6 shadow-sm">
        <div className="absolute -top-10 -right-12 h-40 w-40 rounded-full bg-primary-200/40 blur-3xl" aria-hidden="true" />
        <div className="absolute -bottom-16 left-8 h-32 w-32 rounded-full bg-primary-100/30 blur-3xl" aria-hidden="true" />
        <div className="relative flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
          <div className="space-y-3">
            <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-primary-700">
              Resumen ejecutivo
              <InfoTooltip
                content="El panel concentra los indicadores clave para planificar compras, supervisar ventas y detectar incidencias de stock sin salir de la vista principal."
                size="sm"
                position="bottom"
              />
            </div>
            <h1 className="text-2xl font-semibold text-slate-900">Panel de control de inventario</h1>
            <p className="max-w-2xl text-sm text-slate-600">
              Visualiza cómo evoluciona el catálogo, qué tan saludable está la disponibilidad y qué alertas requieren atención inmediata.
              Utiliza esta vista para anticipar roturas de stock, validar el rendimiento de ventas mensual y coordinar acciones con el
              equipo logístico.
            </p>
          </div>
          <dl className="grid w-full gap-4 rounded-2xl bg-white/70 p-4 shadow-sm backdrop-blur lg:w-auto lg:grid-cols-2">
            <div className="rounded-xl border border-white/60 bg-white/80 p-4">
              <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">Catálogo activo</dt>
              <dd className="mt-2 text-lg font-semibold text-slate-900">{activeCatalogRate}%</dd>
              <dd className="text-xs text-slate-500">{activeProducts.toLocaleString('es-ES')} de {totalProducts.toLocaleString('es-ES')} productos disponibles.</dd>
            </div>
            <div className="rounded-xl border border-white/60 bg-white/80 p-4">
              <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">Alertas activas</dt>
              <dd className="mt-2 text-lg font-semibold text-slate-900">{reorderAlertCount}</dd>
              <dd className="text-xs text-slate-500">Variantes pendientes de reposición prioritaria.</dd>
            </div>
            <div className="rounded-xl border border-white/60 bg-white/80 p-4">
              <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">Ventas del último mes</dt>
              <dd className="mt-2 text-lg font-semibold text-slate-900">
                {latestMonthlySales
                  ? latestMonthlySales.toLocaleString('es-ES', { style: 'currency', currency: 'EUR' })
                  : 'Sin registros'}
              </dd>
              <dd className="text-xs text-slate-500">Base para proyectar previsiones de corto plazo.</dd>
            </div>
            <div className="rounded-xl border border-white/60 bg-white/80 p-4">
              <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">Valor de inventario</dt>
              <dd className="mt-2 text-lg font-semibold text-slate-900">
                {totalInventoryValue.toLocaleString('es-ES', { style: 'currency', currency: 'EUR' })}
              </dd>
              <dd className="text-xs text-slate-500">Importe total comprometido en stock disponible.</dd>
            </div>
          </dl>
        </div>
      </section>

      <div className="grid gap-6 md:grid-cols-2 xl:grid-cols-4">
        <Card title="Productos totales" subtitle="Catálogo registrado">
          <p className="text-3xl font-semibold text-slate-900">{totalProducts}</p>
          <p className="text-xs text-slate-500">{activeProducts} activos actualmente.</p>
        </Card>
        <Card title="Valor de inventario" subtitle="Inventario disponible">
          <p className="text-3xl font-semibold text-slate-900">{totalInventoryValue.toLocaleString('es-ES', { style: 'currency', currency: 'EUR' })}</p>
          <p className="text-xs text-slate-500">Incluye todas las variantes en almacén.</p>
        </Card>
        <Card title="Alertas de reposición" subtitle="Variantes por debajo del mínimo">
          <p className="text-3xl font-semibold text-slate-900">{lowStockVariants}</p>
          <p className="text-xs text-slate-500">Planifica compras para evitar roturas de stock.</p>
        </Card>
        <Card title="Previsión mensual" subtitle="Ventas proyectadas">
          <p className="text-3xl font-semibold text-slate-900">
            {monthlySales.slice(-1)[0]?.totalAmount?.toLocaleString('es-ES', { style: 'currency', currency: 'EUR' }) ?? '—'}
          </p>
          <p className="text-xs text-slate-500">Último periodo registrado.</p>
        </Card>
      </div>

      <div className="grid gap-6 lg:grid-cols-[2fr,1fr]">
        <Card title="Evolución de ventas" subtitle="Ingresos por mes">
          {monthlySales.length === 0 ? (
            <p className="text-sm text-slate-500">Todavía no hay ventas registradas.</p>
          ) : (
            <div className="h-64">
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={monthlySales}>
                  <defs>
                    <linearGradient id="colorSales" x1="0" x2="0" y1="0" y2="1">
                      <stop offset="0%" stopColor="#0f78ff" stopOpacity={0.4} />
                      <stop offset="100%" stopColor="#0f78ff" stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <XAxis
                    dataKey="month"
                    tickFormatter={(month, index) => {
                      const entry = monthlySales[index];
                      if (!entry) {
                        return month?.toString?.() ?? '';
                      }

                      return `${entry.month.toString().padStart(2, '0')}/${entry.year}`;
                    }}
                    stroke="#94a3b8"
                    fontSize={12}
                  />
                  <YAxis stroke="#94a3b8" fontSize={12} tickFormatter={(value) => `${value / 1000}k`} />
                  <Tooltip
                    formatter={(value: number) => value.toLocaleString('es-ES', { style: 'currency', currency: 'EUR' })}
                    labelFormatter={(_, payload) => {
                      const entry = payload?.[0]?.payload as (typeof monthlySales)[number] | undefined;
                      if (!entry) {
                        return '';
                      }

                      return `${entry.month.toString().padStart(2, '0')}/${entry.year}`;
                    }}
                  />
                  <Area dataKey="totalAmount" stroke="#0f78ff" fill="url(#colorSales)" strokeWidth={2} />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          )}
        </Card>

        <Card title="Top ventas" subtitle="Variantes con mayor rotación">
          <ul className="space-y-3">
            {topSellingProducts.slice(0, 5).map((product) => (
              <li key={product.productId} className="flex items-center justify-between text-sm text-slate-700">
                <div>
                  <p className="font-medium text-slate-900">{product.productName}</p>
                  <p className="text-xs text-slate-500">{product.quantity.toLocaleString('es-ES')} unidades</p>
                </div>
                <span className="text-xs font-semibold text-primary-600">
                  {product.revenue.toLocaleString('es-ES', { style: 'currency', currency: 'EUR' })}
                </span>
              </li>
            ))}
          </ul>
        </Card>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card title="Alertas de stock" subtitle="Prioriza estas variantes para reposición">
          {reorderAlerts.length === 0 ? (
            <p className="text-sm text-slate-500">No hay variantes bajo el nivel mínimo.</p>
          ) : (
            <div className="overflow-hidden rounded-xl border border-slate-200 bg-white">
              <table className="min-w-full divide-y divide-slate-200 text-sm">
                <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                  <tr>
                    <th className="px-3 py-2 text-left">Variante</th>
                    <th className="px-3 py-2 text-left">Almacén</th>
                    <th className="px-3 py-2 text-right">Disponible</th>
                    <th className="px-3 py-2 text-right">Mínimo</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {reorderAlerts.map((alert) => (
                    <tr key={`${alert.variantId}-${alert.warehouse}`} className="text-slate-700">
                      <td className="px-3 py-2">
                        <p className="font-medium text-slate-900">{alert.productName}</p>
                        <p className="text-xs text-slate-500">SKU {alert.variantSku}</p>
                      </td>
                      <td className="px-3 py-2 text-xs text-slate-500">{alert.warehouse}</td>
                      <td className="px-3 py-2 text-right text-sm font-semibold text-red-600">{alert.quantity}</td>
                      <td className="px-3 py-2 text-right text-xs text-slate-500">{alert.minStockLevel}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>

        <Card title="Indicadores rápidos" subtitle="Estado general">
          <div className="grid gap-3">
            <div className="flex items-center justify-between rounded-xl border border-slate-200 bg-white px-4 py-3">
              <div>
                <p className="text-sm font-medium text-slate-900">Disponibilidad de catálogo</p>
                <p className="text-xs text-slate-500">Productos activos frente al total registrado.</p>
              </div>
              <Badge tone="success">{Math.round((activeProducts / totalProducts) * 100) || 0}% activo</Badge>
            </div>
            <div className="flex items-center justify-between rounded-xl border border-slate-200 bg-white px-4 py-3">
              <div>
                <p className="text-sm font-medium text-slate-900">Alertas críticas</p>
                <p className="text-xs text-slate-500">Variantes que requieren reposición inmediata.</p>
              </div>
              <Badge tone={lowStockVariants > 0 ? 'warning' : 'success'}>
                {lowStockVariants > 0 ? `${lowStockVariants} pendientes` : 'Sin alertas'}
              </Badge>
            </div>
          </div>
        </Card>
      </div>
    </div>
  );
}
