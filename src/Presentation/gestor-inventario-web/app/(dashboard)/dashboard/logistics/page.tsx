'use client';

import { FormEvent, useEffect, useState } from 'react';
import { apiClient } from '../../../../src/lib/api-client';
import type { LogisticsDashboardDto, ShipmentSummaryDto } from '../../../../src/types/api';
import { Card } from '../../../../components/ui/card';
import { Input } from '../../../../components/ui/input';
import { Button } from '../../../../components/ui/button';
import { Badge } from '../../../../components/ui/badge';
import { InfoTooltip } from '../../../../components/ui/info-tooltip';
import { Area, AreaChart, CartesianGrid, Legend, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';

const defaultPlanningWindow = 90;

const shipmentStatusMap: Record<string, { label: string; tone: 'success' | 'warning' | 'neutral' }> = {
  Created: { label: 'Creado', tone: 'neutral' },
  InTransit: { label: 'En tránsito', tone: 'warning' },
  Delivered: { label: 'Entregado', tone: 'success' },
  Cancelled: { label: 'Cancelado', tone: 'warning' }
};

const shipmentStatusCodes: Record<string, keyof typeof shipmentStatusMap> = {
  '1': 'Created',
  '2': 'InTransit',
  '3': 'Delivered',
  '4': 'Cancelled'
};

export default function LogisticsPage() {
  const [planningWindowInput, setPlanningWindowInput] = useState(defaultPlanningWindow.toString());
  const [appliedPlanningWindow, setAppliedPlanningWindow] = useState(defaultPlanningWindow);
  const [data, setData] = useState<LogisticsDashboardDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchDashboard(defaultPlanningWindow).catch((err) => console.error(err));
  }, []);

  async function fetchDashboard(windowDays: number) {
    setIsLoading(true);
    setError(null);

    try {
      const response = await apiClient.get<LogisticsDashboardDto>('/analytics/logistics-dashboard', {
        params: { planningWindowDays: windowDays }
      });
      setData(response.data);
      setAppliedPlanningWindow(windowDays);
    } catch (err) {
      console.error(err);
      setError('No se pudo obtener el panel logístico. Intenta nuevamente.');
    } finally {
      setIsLoading(false);
    }
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const sanitized = sanitizePlanningWindow(planningWindowInput);
    setPlanningWindowInput(sanitized.toString());
    fetchDashboard(sanitized).catch((err) => console.error(err));
  }

  const delayedShipments = data?.topDelayedShipments ?? [];
  const upcomingShipments = data?.upcomingShipments ?? [];
  const warehousePerformance = data?.warehousePerformance ?? [];
  const carrierPerformance = data?.carrierPerformance ?? [];
  const shipmentTrend = data?.shipmentVolumeTrend ?? [];
  const totalShipments = data?.totalShipments ?? 0;
  const inTransitPercentage = data && totalShipments > 0 ? Math.round((data.inTransitShipments / totalShipments) * 100) : 0;
  const deliveredPercentage = data && totalShipments > 0 ? Math.round((data.deliveredShipments / totalShipments) * 100) : 0;
  const fulfillmentPercent = data ? Math.round(data.averageFulfillmentRate * 1000) / 10 : 0;
  const onTimePercentage = data ? Math.round((data.onTimeDeliveryRate ?? 0) * 1000) / 10 : 0;
  const generatedAt = data ? new Date(data.generatedAt).toLocaleString('es-ES') : null;
  const showInitialLoader = isLoading && !data;

  return (
    <div className="flex flex-col gap-6">
      <Card
        title={
          <span className="flex items-center gap-2">
            Planificación logística
            <InfoTooltip content="Configura el horizonte temporal que se utiliza para todas las métricas y recomendaciones del panel." />
          </span>
        }
        subtitle="Ajusta el horizonte temporal para analizar envíos, entregas y reposición"
      >
        <form className="flex flex-col gap-4 md:flex-row md:items-end" onSubmit={handleSubmit}>
          <Input
            label={
              <span className="flex items-center gap-2">
                Ventana de planificación (días)
                <InfoTooltip
                  content="Cantidad de días que se usan para proyectar demanda, reposición y seguimiento de envíos."
                  size="sm"
                />
              </span>
            }
            type="number"
            min={7}
            max={180}
            value={planningWindowInput}
            onChange={(event) => setPlanningWindowInput(event.target.value)}
            hint="Entre 7 y 180 días"
            className="md:w-44"
          />
          <Button type="submit" className="md:self-end">
            Actualizar panel
          </Button>
        </form>
        {data && (
          <p className="mt-3 text-xs text-slate-500">
            Mostrando recomendaciones para un horizonte de{' '}
            <span className="font-semibold text-slate-900">{appliedPlanningWindow} días</span>. Última actualización:
            {' '}
            <span className="font-semibold text-slate-900">{generatedAt}</span>
          </p>
        )}
        {error && <p className="mt-3 text-xs text-red-500">{error}</p>}
      </Card>

      {showInitialLoader && <p className="text-sm text-slate-500">Cargando indicadores logísticos…</p>}

      {data && (
        <>
          {isLoading && !showInitialLoader && (
            <p className="text-xs text-slate-400">Actualizando métricas con los últimos datos…</p>
          )}

          <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
            <StatTile
              label="Envíos totales"
              value={data.totalShipments.toLocaleString('es-ES')}
              helper={`Generados en los últimos ${appliedPlanningWindow} días`}
              info="Total de envíos registrados por el sistema durante la ventana analizada."
            />
            <StatTile
              label="En tránsito"
              value={data.inTransitShipments.toLocaleString('es-ES')}
              helper={`${inTransitPercentage}% del total analizado`}
              info="Envíos que ya salieron del almacén pero aún no se entregaron al cliente."
            />
            <StatTile
              label="Entregados"
              value={data.deliveredShipments.toLocaleString('es-ES')}
              helper={`${deliveredPercentage}% completados`}
              info="Pedidos completados con entrega confirmada dentro del periodo seleccionado."
            />
            <StatTile
              label="Órdenes abiertas"
              value={data.openSalesOrders.toLocaleString('es-ES')}
              helper="Pendientes de preparación o envío"
              info="Órdenes de venta que siguen pendientes de preparación o despacho."
            />
            <StatTile
              label="Entregas a tiempo"
              value={`${onTimePercentage.toLocaleString('es-ES', {
                minimumFractionDigits: 0,
                maximumFractionDigits: 1
              })}%`}
              helper="Porcentaje de entregas dentro del compromiso"
              info="Proporción de entregas realizadas antes o en la fecha comprometida."
            />
          </section>

          <Card
            title={
              <span className="flex items-center gap-2">
                Tendencia de envíos
                <InfoTooltip content="Evolución diaria del volumen de envíos para identificar picos de demanda y comportamientos estacionales." />
              </span>
            }
            subtitle={`Volumen diario en los últimos ${appliedPlanningWindow} días`}
          >
            {shipmentTrend.length === 0 ? (
              <p className="text-sm text-slate-500">No se registran envíos en el periodo analizado.</p>
            ) : (
              <div className="h-72">
                <ResponsiveContainer width="100%" height="100%">
                  <AreaChart data={shipmentTrend}>
                    <defs>
                      <linearGradient id="totalShipmentsGradient" x1="0" x2="0" y1="0" y2="1">
                        <stop offset="0%" stopColor="#2563eb" stopOpacity={0.45} />
                        <stop offset="100%" stopColor="#2563eb" stopOpacity={0} />
                      </linearGradient>
                      <linearGradient id="deliveredShipmentsGradient" x1="0" x2="0" y1="0" y2="1">
                        <stop offset="0%" stopColor="#059669" stopOpacity={0.4} />
                        <stop offset="100%" stopColor="#059669" stopOpacity={0} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                    <XAxis
                      dataKey="date"
                      stroke="#94a3b8"
                      fontSize={12}
                      tickFormatter={formatChartDate}
                    />
                    <YAxis stroke="#94a3b8" fontSize={12} allowDecimals={false} />
                    <Tooltip
                      formatter={(value: number) => value.toLocaleString('es-ES')}
                      labelFormatter={(value) => formatChartTooltipLabel(value as string)}
                    />
                    <Legend />
                    <Area
                      type="monotone"
                      dataKey="total"
                      name="Total"
                      stroke="#2563eb"
                      fill="url(#totalShipmentsGradient)"
                      strokeWidth={2}
                    />
                    <Area
                      type="monotone"
                      dataKey="delivered"
                      name="Entregados"
                      stroke="#059669"
                      fill="url(#deliveredShipmentsGradient)"
                      strokeWidth={2}
                    />
                    <Area
                      type="monotone"
                      dataKey="inTransit"
                      name="En tránsito"
                      stroke="#f59e0b"
                      fill="#f59e0b33"
                      strokeWidth={2}
                    />
                  </AreaChart>
                </ResponsiveContainer>
              </div>
            )}
          </Card>

          <div className="grid gap-6 lg:grid-cols-2">
            <Card
              title={
                <span className="flex items-center gap-2">
                  Desempeño operativo
                  <InfoTooltip content="Resumen de la experiencia de entrega combinando tiempos promedio y nivel de cumplimiento." />
                </span>
              }
              subtitle="Calidad de entrega y nivel de servicio"
            >
              <div className="grid gap-4 sm:grid-cols-2">
                <MetricHighlight
                  label="Tiempo de tránsito"
                  value={`${data.averageTransitDays.toLocaleString('es-ES', {
                    minimumFractionDigits: 0,
                    maximumFractionDigits: 2
                  })} días`}
                  helper="Promedio desde la salida hasta la entrega"
                  info="Promedio de días entre la expedición del pedido y la confirmación de entrega."
                />
                <MetricHighlight
                  label="Nivel de cumplimiento"
                  value={`${fulfillmentPercent.toLocaleString('es-ES', {
                    minimumFractionDigits: 0,
                    maximumFractionDigits: 1
                  })}%`}
                  helper="Porcentaje medio de unidades completadas"
                  accent="primary"
                  info="Porcentaje medio de unidades entregadas respecto al total solicitado por los clientes."
                />
              </div>
            </Card>

            <Card
              title={
                <span className="flex items-center gap-2">
                  Reposición sugerida
                  <InfoTooltip content="Cantidad total recomendada para reponer considerando ventas, reservas y niveles mínimos." />
                </span>
              }
              subtitle={`Volumen recomendado para ${appliedPlanningWindow} días`}
            >
              <div className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-5 text-emerald-700">
                <p className="text-3xl font-semibold">
                  {data.totalReplenishmentRecommendation.toLocaleString('es-ES', {
                    minimumFractionDigits: 0,
                    maximumFractionDigits: 0
                  })}
                </p>
                <p className="mt-1 text-xs font-medium uppercase">unidades a reponer</p>
              </div>
              <p className="mt-4 text-xs text-slate-500">
                El plan tiene en cuenta pedidos abiertos, reservas y niveles mínimos por almacén para evitar roturas de stock.
              </p>
            </Card>
          </div>

          <div className="grid gap-6 xl:grid-cols-[3fr,2fr]">
            <Card
              title={
                <span className="flex items-center gap-2">
                  Desempeño por almacén
                  <InfoTooltip content="Comparativa por centro logístico para detectar cuellos de botella y variaciones de servicio." />
                </span>
              }
              subtitle="Comparativa de puntualidad y tiempos de tránsito"
            >
              {warehousePerformance.length === 0 ? (
                <p className="text-sm text-slate-500">Sin envíos asociados a almacenes en el periodo.</p>
              ) : (
                <div className="overflow-hidden rounded-xl border border-slate-200 bg-white">
                  <table className="min-w-full divide-y divide-slate-200 text-sm">
                    <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                      <tr>
                        <th className="px-4 py-3 text-left">
                          <span className="inline-flex items-center gap-1">
                            Almacén
                            <InfoTooltip
                              content="Centro logístico desde el que se despacha cada envío analizado."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                        <th className="px-4 py-3 text-right">
                          <span className="inline-flex items-center justify-end gap-1">
                            Envíos
                            <InfoTooltip
                              content="Número total de envíos completados por el almacén en la ventana seleccionada."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                        <th className="px-4 py-3 text-right">
                          <span className="inline-flex items-center justify-end gap-1">
                            A tiempo
                            <InfoTooltip
                              content="Envíos entregados antes o en la fecha comprometida para cada almacén."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                        <th className="px-4 py-3 text-right">
                          <span className="inline-flex items-center justify-end gap-1">
                            Retrasos
                            <InfoTooltip
                              content="Cantidad de envíos que llegaron después de la fecha prometida."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                        <th className="px-4 py-3 text-right">
                          <span className="inline-flex items-center justify-end gap-1">
                            Tránsito medio
                            <InfoTooltip
                              content="Promedio de días transcurridos desde la salida del almacén hasta la entrega."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {warehousePerformance.map((warehouse) => (
                        <tr key={warehouse.warehouseId} className="text-slate-700">
                          <td className="px-4 py-3">
                            <p className="text-sm font-semibold text-slate-900">{warehouse.warehouseName}</p>
                          </td>
                          <td className="px-4 py-3 text-right font-semibold text-slate-900">
                            {warehouse.totalShipments.toLocaleString('es-ES')}
                          </td>
                          <td className="px-4 py-3 text-right text-emerald-600">
                            {warehouse.onTimeShipments.toLocaleString('es-ES')}
                          </td>
                          <td className="px-4 py-3 text-right text-amber-600">
                            {warehouse.delayedShipments.toLocaleString('es-ES')}
                          </td>
                          <td className="px-4 py-3 text-right text-xs text-slate-500">
                            {warehouse.averageTransitDays.toLocaleString('es-ES', {
                              minimumFractionDigits: 0,
                              maximumFractionDigits: 2
                            })}{' '}
                            días
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </Card>

            <Card
              title={
                <span className="flex items-center gap-2">
                  Rendimiento por transportista
                  <InfoTooltip content="Analiza el desempeño de cada operador logístico en volumen, servicio y puntualidad." />
                </span>
              }
              subtitle="Envíos gestionados y nivel de servicio"
            >
              {carrierPerformance.length === 0 ? (
                <p className="text-sm text-slate-500">Sin envíos asociados a transportistas en el periodo.</p>
              ) : (
                <div className="overflow-hidden rounded-xl border border-slate-200 bg-white">
                  <table className="min-w-full divide-y divide-slate-200 text-sm">
                    <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                      <tr>
                        <th className="px-4 py-3 text-left">
                          <span className="inline-flex items-center gap-1">
                            Transportista
                            <InfoTooltip
                              content="Proveedor logístico o carrier responsable de mover el envío."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                        <th className="px-4 py-3 text-right">
                          <span className="inline-flex items-center justify-end gap-1">
                            Envíos
                            <InfoTooltip
                              content="Total de envíos gestionados por el transportista durante la ventana analizada."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                        <th className="px-4 py-3 text-right">
                          <span className="inline-flex items-center justify-end gap-1">
                            En tránsito
                            <InfoTooltip
                              content="Envíos que continúan en ruta bajo responsabilidad del transportista."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                        <th className="px-4 py-3 text-right">
                          <span className="inline-flex items-center justify-end gap-1">
                            Entregados
                            <InfoTooltip
                              content="Envíos completados por el transportista con confirmación de entrega."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                        <th className="px-4 py-3 text-right">
                          <span className="inline-flex items-center justify-end gap-1">
                            A tiempo
                            <InfoTooltip
                              content="Porcentaje de envíos entregados antes o en la fecha objetivo."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                        <th className="px-4 py-3 text-right">
                          <span className="inline-flex items-center justify-end gap-1">
                            Retraso medio
                            <InfoTooltip
                              content="Días promedio de retraso respecto a la fecha prometida para cada transportista."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {carrierPerformance.map((carrier) => (
                        <tr key={`${carrier.carrierId ?? carrier.carrierName}`} className="text-slate-700">
                          <td className="px-4 py-3">
                            <p className="text-sm font-semibold text-slate-900">{carrier.carrierName}</p>
                          </td>
                          <td className="px-4 py-3 text-right font-semibold text-slate-900">
                            {carrier.totalShipments.toLocaleString('es-ES')}
                          </td>
                          <td className="px-4 py-3 text-right text-slate-500">
                            {carrier.inTransitShipments.toLocaleString('es-ES')}
                          </td>
                          <td className="px-4 py-3 text-right text-slate-500">
                            {carrier.deliveredShipments.toLocaleString('es-ES')}
                          </td>
                          <td className="px-4 py-3 text-right text-emerald-600">
                            {(carrier.onTimeRate * 100).toLocaleString('es-ES', {
                              minimumFractionDigits: 0,
                              maximumFractionDigits: 1
                            })}
                            %
                          </td>
                          <td className="px-4 py-3 text-right text-xs text-slate-500">
                            {carrier.averageDelayDays.toLocaleString('es-ES', {
                              minimumFractionDigits: 0,
                              maximumFractionDigits: 2
                            })}{' '}
                            días
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </Card>
          </div>

          <div className="grid gap-6 xl:grid-cols-2">
            <Card
              title={
                <span className="flex items-center gap-2">
                  Próximos envíos comprometidos
                  <InfoTooltip content="Monitoriza los envíos que deben entregarse pronto para anticipar retrasos y coordinar acciones." />
                </span>
              }
              subtitle="Seguimiento de entregas pendientes y su nivel de riesgo"
            >
              {upcomingShipments.length === 0 ? (
                <p className="text-sm text-slate-500">No hay envíos pendientes de entrega en la ventana seleccionada.</p>
              ) : (
                <div className="overflow-hidden rounded-xl border border-slate-200 bg-white">
                  <table className="min-w-full divide-y divide-slate-200 text-sm">
                    <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                      <tr>
                        <th className="px-4 py-3 text-left">
                          <span className="inline-flex items-center gap-1">
                            Envío
                            <InfoTooltip
                              content="Identificador del envío junto a los datos clave del pedido y seguimiento."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                        <th className="px-4 py-3 text-left">
                          <span className="inline-flex items-center gap-1">
                            Almacén
                            <InfoTooltip
                              content="Centro logístico responsable de preparar la orden."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                        <th className="px-4 py-3 text-left">
                          <span className="inline-flex items-center gap-1">
                            Estado
                            <InfoTooltip
                              content="Situación reportada por el WMS o el transportista para el envío."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                        <th className="px-4 py-3 text-left">
                          <span className="inline-flex items-center gap-1">
                            Entrega estimada
                            <InfoTooltip
                              content="Fecha objetivo y días restantes para completar la entrega."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                        <th className="px-4 py-3 text-left">
                          <span className="inline-flex items-center gap-1">
                            Riesgo
                            <InfoTooltip
                              content="Nivel de riesgo calculado según el tiempo restante y los retrasos detectados."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {upcomingShipments.map((shipment) => {
                        const normalizedStatusKey = shipmentStatusCodes[String(shipment.status)] ?? String(shipment.status);
                        const statusInfo = shipmentStatusMap[normalizedStatusKey] ?? {
                          label: normalizedStatusKey,
                          tone: 'neutral' as const
                        };
                        const remainingDays = calculateRemainingDays(shipment);
                        const riskInfo = getDeliveryRisk(shipment);

                        return (
                          <tr key={shipment.id} className="text-slate-700">
                            <td className="px-4 py-3">
                              <p className="text-sm font-semibold text-slate-900">
                                #{shipment.id.toString().padStart(5, '0')}
                              </p>
                              <p className="text-xs text-slate-500">Creado: {formatDate(shipment.createdAt)}</p>
                              {shipment.carrierName && (
                                <p className="text-xs text-slate-500">Transportista: {shipment.carrierName}</p>
                              )}
                              {shipment.trackingNumber && (
                                <p className="text-xs text-slate-400">Tracking: {shipment.trackingNumber}</p>
                              )}
                            </td>
                            <td className="px-4 py-3 text-xs text-slate-500">
                              <p className="text-sm font-medium text-slate-900">{shipment.warehouseName}</p>
                              <p className="text-xs text-slate-400">ID almacén {shipment.warehouseId}</p>
                            </td>
                            <td className="px-4 py-3">
                              <Badge tone={statusInfo.tone}>{statusInfo.label}</Badge>
                            </td>
                            <td className="px-4 py-3 text-xs text-slate-500">
                              <p>{formatDate(shipment.estimatedDeliveryDate)}</p>
                              {remainingDays !== null && (
                                <p className="mt-1 text-[11px] text-slate-400">
                                  {remainingDays < 0
                                    ? `Retraso de ${Math.abs(remainingDays)} días`
                                    : `Restan ${remainingDays} días`}
                                </p>
                              )}
                            </td>
                            <td className="px-4 py-3 text-xs text-slate-500">
                              <Badge tone={riskInfo.tone}>{riskInfo.label}</Badge>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </Card>

            <Card
              title={
                <span className="flex items-center gap-2">
                  Envíos con mayor retraso
                  <InfoTooltip content="Casos críticos que superaron la fecha estimada para priorizar análisis de causa raíz." />
                </span>
              }
              subtitle="Top 5 casos entregados después de la fecha estimada"
            >
              {delayedShipments.length === 0 ? (
                <p className="text-sm text-slate-500">
                  No se registran retrasos en el periodo analizado.
                </p>
              ) : (
                <div className="overflow-hidden rounded-xl border border-slate-200 bg-white">
                  <table className="min-w-full divide-y divide-slate-200 text-sm">
                    <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                      <tr>
                        <th className="px-4 py-3 text-left">
                          <span className="inline-flex items-center gap-1">
                            Envío
                            <InfoTooltip
                              content="Identificador del envío con su detalle logístico para investigar el retraso."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                        <th className="px-4 py-3 text-left">
                          <span className="inline-flex items-center gap-1">
                            Almacén
                            <InfoTooltip
                              content="Centro desde el que partió el envío retrasado."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                        <th className="px-4 py-3 text-left">
                          <span className="inline-flex items-center gap-1">
                            Estado
                            <InfoTooltip
                              content="Situación final o actual reportada para el envío retrasado."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                        <th className="px-4 py-3 text-left">
                          <span className="inline-flex items-center gap-1">
                            Fechas clave
                            <InfoTooltip
                              content="Cronología de salida, entrega estimada y fecha real para contextualizar el retraso."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                        <th className="px-4 py-3 text-left">
                          <span className="inline-flex items-center gap-1">
                            Retraso
                            <InfoTooltip
                              content="Días de atraso calculados entre la fecha prometida y la entrega real."
                              size="sm"
                              position="bottom"
                            />
                          </span>
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {delayedShipments.map((shipment) => {
                        const normalizedStatusKey = shipmentStatusCodes[String(shipment.status)] ?? String(shipment.status);
                        const statusInfo = shipmentStatusMap[normalizedStatusKey] ?? {
                          label: normalizedStatusKey,
                          tone: 'neutral' as const
                        };
                        const delayDays = calculateDelayDays(shipment);

                        return (
                          <tr key={shipment.id} className="text-slate-700">
                            <td className="px-4 py-3">
                              <p className="text-sm font-semibold text-slate-900">
                                #{shipment.id.toString().padStart(5, '0')}
                              </p>
                              <p className="text-xs text-slate-500">Creado: {formatDate(shipment.createdAt)}</p>
                              {shipment.carrierName && (
                                <p className="text-xs text-slate-500">Transportista: {shipment.carrierName}</p>
                              )}
                              {shipment.trackingNumber && (
                                <p className="text-xs text-slate-400">Tracking: {shipment.trackingNumber}</p>
                              )}
                            </td>
                            <td className="px-4 py-3 text-xs text-slate-500">
                              <p className="text-sm font-medium text-slate-900">{shipment.warehouseName}</p>
                              <p className="text-xs text-slate-400">ID almacén {shipment.warehouseId}</p>
                            </td>
                            <td className="px-4 py-3">
                              <Badge tone={statusInfo.tone}>{statusInfo.label}</Badge>
                              {typeof shipment.totalWeight === 'number' && (
                                <p className="mt-2 text-xs text-slate-500">
                                  Peso: {shipment.totalWeight.toLocaleString('es-ES', {
                                    minimumFractionDigits: 0,
                                    maximumFractionDigits: 2
                                  })}{' '}
                                  kg
                                </p>
                              )}
                            </td>
                            <td className="px-4 py-3 text-xs text-slate-500">
                              <p>Salida: {formatDate(shipment.shippedAt)}</p>
                              <p>Estimada: {formatDate(shipment.estimatedDeliveryDate)}</p>
                              <p>Entrega: {formatDate(shipment.deliveredAt)}</p>
                            </td>
                            <td className="px-4 py-3 text-xs text-slate-500">
                              {delayDays !== null ? (
                                <span className="font-semibold text-red-600">{delayDays} días</span>
                              ) : (
                                '—'
                              )}
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </Card>
          </div>
        </>
      )}
    </div>
  );
}

function sanitizePlanningWindow(value: string): number {
  const parsed = Number.parseInt(value, 10);
  if (Number.isNaN(parsed)) {
    return defaultPlanningWindow;
  }

  return Math.min(180, Math.max(7, parsed));
}

function formatDate(value?: string | null) {
  if (!value) {
    return '—';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '—';
  }

  return date.toLocaleDateString('es-ES');
}

function formatChartDate(value: string) {
  const date = new Date(`${value}T00:00:00`);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleDateString('es-ES', { day: '2-digit', month: '2-digit' });
}

function formatChartTooltipLabel(value: string) {
  const date = new Date(`${value}T00:00:00`);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleDateString('es-ES');
}

function calculateDelayDays(shipment: ShipmentSummaryDto): number | null {
  if (!shipment.deliveredAt || !shipment.estimatedDeliveryDate) {
    return null;
  }

  const delivered = new Date(shipment.deliveredAt).getTime();
  const estimated = new Date(shipment.estimatedDeliveryDate).getTime();

  if (Number.isNaN(delivered) || Number.isNaN(estimated)) {
    return null;
  }

  const diff = delivered - estimated;
  if (diff <= 0) {
    return 0;
  }

  return Math.ceil(diff / (1000 * 60 * 60 * 24));
}

function calculateRemainingDays(shipment: ShipmentSummaryDto): number | null {
  if (!shipment.estimatedDeliveryDate) {
    return null;
  }

  const estimated = new Date(shipment.estimatedDeliveryDate).getTime();
  if (Number.isNaN(estimated)) {
    return null;
  }

  const now = Date.now();
  const diff = estimated - now;

  return Math.ceil(diff / (1000 * 60 * 60 * 24));
}

function getDeliveryRisk(
  shipment: ShipmentSummaryDto
): { label: string; tone: 'success' | 'warning' | 'neutral' } {
  const remaining = calculateRemainingDays(shipment);

  if (remaining === null) {
    return { label: 'Sin estimar', tone: 'neutral' };
  }

  if (remaining < 0) {
    return { label: 'Retrasado', tone: 'warning' };
  }

  if (remaining <= 2) {
    return { label: 'Atención', tone: 'warning' };
  }

  return { label: 'En plazo', tone: 'success' };
}

function StatTile({
  label,
  value,
  helper,
  info
}: {
  label: string;
  value: string;
  helper?: string;
  info?: string;
}) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white/80 p-5 shadow-sm">
      <p className="flex items-center gap-1 text-xs font-medium uppercase text-slate-500">
        {label}
        {info && <InfoTooltip content={info} size="sm" />}
      </p>
      <p className="mt-2 text-2xl font-semibold text-slate-900">{value}</p>
      {helper && <p className="mt-1 text-xs text-slate-400">{helper}</p>}
    </div>
  );
}

function MetricHighlight({
  label,
  value,
  helper,
  accent = 'default',
  info
}: {
  label: string;
  value: string;
  helper: string;
  accent?: 'default' | 'primary' | 'emerald';
  info?: string;
}) {
  const accentClasses: Record<string, string> = {
    default: 'text-slate-900',
    primary: 'text-primary-600',
    emerald: 'text-emerald-600'
  };

  return (
    <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3">
      <p className="flex items-center gap-1 text-xs font-medium uppercase text-slate-500">
        {label}
        {info && <InfoTooltip content={info} size="sm" />}
      </p>
      <p className={`mt-2 text-2xl font-semibold ${accentClasses[accent]}`}>{value}</p>
      <p className="mt-1 text-xs text-slate-500">{helper}</p>
    </div>
  );
}
