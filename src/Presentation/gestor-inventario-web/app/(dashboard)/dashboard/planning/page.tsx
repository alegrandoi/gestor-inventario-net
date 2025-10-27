'use client';

import { ChangeEvent, FormEvent, useEffect, useMemo, useState } from 'react';
import { apiClient } from '../../../../src/lib/api-client';
import type {
  DemandForecastDto,
  OptimizationRecommendationDto,
  OptimizationScenarioComparisonDto,
  PagedResult,
  ProductDto,
  PurchasePlanDto,
  ScenarioSimulationDto,
  SupplierDto
} from '../../../../src/types/api';
import { Card } from '../../../../components/ui/card';
import { InfoTooltip } from '../../../../components/ui/info-tooltip';
import { Input } from '../../../../components/ui/input';
import { Select } from '../../../../components/ui/select';
import { Textarea } from '../../../../components/ui/textarea';
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  Legend,
  BarChart,
  Bar,
  CartesianGrid
} from 'recharts';

type VariantOption = {
  id: number;
  label: string;
  sku: string;
  productName: string;
  unitPrice: number;
  currency: string;
};

const productPageSize = 200;

const formatDecimal = (value: number, digits = 2) =>
  value.toLocaleString('es-ES', { minimumFractionDigits: digits, maximumFractionDigits: digits });

const PLANNING_HELP = {
  configuration:
    'Configura el contexto del análisis eligiendo qué variantes estudiar y cómo calcular la demanda futura.',
  selectionSection:
    'Selecciona las variantes del catálogo que servirán de base para las previsiones y los planes.',
  forecastSection:
    'Ajusta cómo se calcula la previsión de demanda mediante suavizado exponencial y factores estacionales.',
  forecastSetup:
    'Configura la variante de referencia y los parámetros del modelo de demanda para este cálculo específico.',
  inventorySection:
    'Define los supuestos logísticos (lead time, revisión, nivel de servicio) que afectan a la política de inventario.',
  planSetup:
    'Selecciona las variantes, periodos y supuestos logísticos que alimentan el plan sugerido de compra.',
  variantPrincipal:
    'Variante principal para estimar la demanda: se usa en los pronósticos, simulaciones y comparativas what-if.',
  planVariants:
    'Variantes incluidas en el plan sugerido y en las recomendaciones de política. Puedes seleccionar varias a la vez.',
  periods: 'Número de periodos futuros (en meses) que se proyectarán en todos los cálculos.',
  alpha:
    'Parámetro alpha del suavizado exponencial: controla el peso de los datos más recientes (0 = histórico, 1 = actual).',
  beta:
    'Parámetro beta del suavizado: regula la sensibilidad a la tendencia detectada en la demanda.',
  seasonLength:
    'Duración del ciclo estacional en periodos. Útil para datos con picos repetitivos (por ejemplo, 12 para ciclos anuales).',
  includeSeasonality: 'Activa la corrección estacional en los modelos de previsión y simulaciones.',
  leadTimeDays:
    'Permite fijar manualmente un plazo de aprovisionamiento medio cuando no se dispone de datos históricos consistentes.',
  reviewPeriodDays:
    'Frecuencia, en días, con la que se revisarán los niveles de inventario dentro de las políticas sugeridas.',
  serviceLevel:
    'Probabilidad objetivo de satisfacer la demanda sin quiebres. Valores cercanos a 1 implican más stock.',
  safetyStockFactor:
    'Multiplicador adicional para calcular el stock de seguridad a partir de la variabilidad de la demanda.',
  leadTimeOptions:
    'Lista de plazos de proveedor (en días) separados por comas para evaluar diferentes escenarios en la simulación.',
  actions:
    'Ejecuta los distintos cálculos: previsión, plan sugerido, simulaciones y comparativas. Cada botón utiliza la configuración actual.',
  forecastCard:
    'Visualiza la serie histórica frente al pronóstico generado para la variante seleccionada.',
  forecastTable:
    'Detalle numérico por periodo que muestra cómo evoluciona la demanda real frente a la proyectada.',
  forecastPeriod: 'Identificador de periodo usado en el histórico (ej. AAAA-MM).',
  forecastHistorical: 'Demanda real registrada en el periodo.',
  forecastProjected: 'Demanda estimada por el modelo para el periodo indicado.',
  planCard:
    'Convierte los pronósticos en un plan de compra con cantidades recomendadas y costes estimados.',
  planTotalsUnits: 'Suma de unidades recomendadas en el plan de compra actual.',
  planTotalsCost: 'Inversión estimada necesaria para cubrir el plan sugerido.',
  planTotalsItems: 'Número de variantes incluidas en el plan recomendado.',
  planGenerated: 'Momento exacto en el que se generó el plan con los parámetros actuales.',
  planVariant: 'Producto y variante para los que aplica la recomendación.',
  planAvailable: 'Inventario disponible actualmente para la variante.',
  planForecast: 'Demanda prevista en el horizonte definido.',
  planSafetyStock: 'Stock adicional para cubrir la variabilidad y mantener el nivel de servicio objetivo.',
  planReorder: 'Punto en el que se recomienda lanzar un nuevo pedido considerando demanda y lead time.',
  planRecommended: 'Cantidad óptima sugerida para reabastecer la variante.',
  planServiceLevel: 'Nivel de servicio estimado tras aplicar la política recomendada.',
  planAbc: 'Clasificación ABC basada en el impacto económico de la variante.',
  planUnitPrice: 'Precio unitario utilizado para estimar el coste total.',
  planCost: 'Importe estimado del pedido recomendado para la variante.',
  coverageCard:
    'Calcula cuántos días de cobertura aporta el inventario disponible y el pedido sugerido para anticipar faltantes o exceso.',
  scenarioSetup:
    'Selecciona la variante y define los plazos a evaluar para estimar cobertura y riesgo bajo distintos escenarios.',
  coverageVariant: 'Producto y variante analizados dentro del estudio de cobertura.',
  coverageCoverage:
    'Días de cobertura estimados combinando el inventario disponible y la cantidad recomendada en el plan.',
  coverageOrderCoverage:
    'Días adicionales de cobertura aportados únicamente por la cantidad recomendada.',
  coverageLeadTime: 'Plazo de aprovisionamiento considerado al generar el plan.',
  coverageReview: 'Frecuencia de revisión de inventario utilizada en el cálculo.',
  recommendationsCard:
    'Resultados de la optimización de políticas min/max, EOQ y niveles de servicio basados en simulación Monte Carlo.',
  optimizationSetup:
    'Indica qué variantes optimizar y los supuestos logísticos que deben respetar las políticas sugeridas.',
  policyChart:
    'Comparativa visual entre los límites de inventario sugeridos para la variante seleccionada.',
  policyVariant: 'Variante optimizada con la política calculada.',
  policyMin: 'Stock mínimo recomendado para cubrir la demanda segura.',
  policyReorder: 'Nivel de inventario que dispara un nuevo pedido.',
  policyMax: 'Stock máximo sugerido para evitar sobreinventario.',
  policyEoq: 'Cantidad económica de pedido calculada (EOQ).',
  policyFillRate: 'Fill rate estimado para la política propuesta.',
  policyIndicators: 'Resumen de KPIs de servicio y costos obtenidos tras la simulación.',
  scenarioComparisonCard:
    'Evalúa escenarios what-if modificando nivel de servicio, lead time y estructura de costes para comparar KPIs.',
  comparisonSetup:
    'Prepara la variante base y los ajustes what-if que quieres contrastar frente a la línea base.',
  scenarioChart:
    'Simula distintos plazos de proveedor y su impacto en riesgos y cantidades recomendadas.',
  scenarioIndicators: 'KPIs resultantes del escenario seleccionado frente a la línea base.',
  scenarioLeadTime: 'Plazo de proveedor evaluado en días.',
  scenarioForecast: 'Demanda esperada durante el lead time.',
  scenarioSafetyStock: 'Stock de seguridad necesario para ese escenario.',
  scenarioReorder: 'Nivel de inventario que activa el pedido bajo el escenario simulado.',
  scenarioRisk: 'Probabilidad de quiebre de stock sin acciones adicionales.',
  scenarioResidualRisk: 'Riesgo restante tras aplicar la recomendación de pedido.',
  scenarioRecommended: 'Cantidad óptima sugerida para cubrir la demanda bajo ese lead time.',
  compareFillRate: 'Fill rate (nivel de servicio) esperado en cada escenario what-if comparado.',
  compareCost: 'Costo total asociado a cada política simulada.',
  compareChart: 'Visualización combinada de fill rate y costo total para cada escenario simulado.',
  whatIfService: 'Nivel de servicio hipotético que se utilizará en la comparación what-if.',
  whatIfLeadTime: 'Plazo de aprovisionamiento alternativo que deseas probar.',
  whatIfHoldingCost: 'Costo de mantenimiento por unidad utilizado en el escenario what-if.',
  whatIfOrderingCost: 'Costo fijo por pedido para el escenario alternativo.',
  whatIfStockoutCost: 'Penalización económica asociada a quiebres en el escenario what-if.',
  orderCard:
    'Genera una orden de compra real utilizando los resultados del plan sugerido y selecciona proveedor, moneda y notas.',
  orderSupplier: 'Proveedor al que se enviará la orden generada.',
  orderCurrency: 'Moneda en la que se emitirá la orden; puedes adaptarla antes de confirmar.',
  orderNotes: 'Notas internas o instrucciones adicionales que acompañarán la orden.',
  actionsInfo:
    'Ejecuta los cálculos con la configuración actual. Puedes repetirlos al ajustar parámetros para comparar resultados.'
} as const;

const ACTION_LABELS = {
  forecast: 'Pronóstico de demanda',
  plan: 'Plan sugerido de compra',
  scenario: 'Simulación de escenarios',
  optimization: 'Recomendaciones de política',
  comparison: 'Comparativa what-if'
} as const;

type ActionKey = keyof typeof ACTION_LABELS;

function formatUsageHint(actions: ActionKey[]) {
  if (actions.length === 0) {
    return '';
  }

  const labels = actions.map((action) => ACTION_LABELS[action]);
  return `Usado en: ${labels.join(', ')}`;
}

const ACTION_REQUIREMENTS: Record<ActionKey, string> = {
  forecast:
    'Requiere variante principal y parámetros de pronóstico (meses, alpha/beta opcionales, ciclos estacionales y uso de estacionalidad).',
  plan:
    'Utiliza variantes del plan, periodos proyectados, parámetros de pronóstico y supuestos logísticos (lead time, revisión, nivel de servicio y factor de seguridad).',
  scenario:
    'Necesita variante principal, periodos, parámetros de pronóstico, factor de seguridad y lista de plazos a simular.',
  optimization:
    'Considera variantes del plan, periodos, parámetros de pronóstico y supuestos logísticos (lead time, revisión y nivel de servicio).',
  comparison:
    'Parte de la variante principal con parámetros de pronóstico y supuestos logísticos. Puedes añadir ajustes what-if de servicio, lead time y costos.'
};

function LabelWithInfo({
  label,
  info,
  className
}: {
  label: string;
  info: string;
  className?: string;
}) {
  return (
    <span className={["inline-flex items-center gap-1", className].filter(Boolean).join(' ')}>
      <span>{label}</span>
      <InfoTooltip content={info} size="sm" />
    </span>
  );
}

function TableHeaderLabel({ label, info }: { label: string; info: string }) {
  return (
    <span className="inline-flex items-center gap-1">
      <span>{label}</span>
      <InfoTooltip content={info} size="sm" position="bottom" />
    </span>
  );
}

export default function PlanningPage() {
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [suppliers, setSuppliers] = useState<SupplierDto[]>([]);
  const [selectedVariantId, setSelectedVariantId] = useState('');
  const [selectedVariantIds, setSelectedVariantIds] = useState<string[]>([]);
  const [periods, setPeriods] = useState('3');
  const [alpha, setAlpha] = useState('0.3');
  const [beta, setBeta] = useState('0.1');
  const [seasonLength, setSeasonLength] = useState('12');
  const [includeSeasonality, setIncludeSeasonality] = useState(true);
  const [leadTimeDays, setLeadTimeDays] = useState('');
  const [reviewPeriodDays, setReviewPeriodDays] = useState('30');
  const [serviceLevel, setServiceLevel] = useState('0.9');
  const [safetyStockFactor, setSafetyStockFactor] = useState('1');
  const [leadTimeOptions, setLeadTimeOptions] = useState('7,14,21');
  const [forecast, setForecast] = useState<DemandForecastDto | null>(null);
  const [plan, setPlan] = useState<PurchasePlanDto | null>(null);
  const [scenario, setScenario] = useState<ScenarioSimulationDto | null>(null);
  const [recommendations, setRecommendations] = useState<OptimizationRecommendationDto | null>(null);
  const [scenarioComparison, setScenarioComparison] =
    useState<OptimizationScenarioComparisonDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [planError, setPlanError] = useState<string | null>(null);
  const [scenarioError, setScenarioError] = useState<string | null>(null);
  const [optimizationError, setOptimizationError] = useState<string | null>(null);
  const [scenarioComparisonError, setScenarioComparisonError] = useState<string | null>(null);
  const [orderError, setOrderError] = useState<string | null>(null);
  const [orderSuccess, setOrderSuccess] = useState<string | null>(null);
  const [selectedSupplierId, setSelectedSupplierId] = useState('');
  const [currency, setCurrency] = useState('EUR');
  const [orderNotes, setOrderNotes] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [isPlanLoading, setIsPlanLoading] = useState(false);
  const [isScenarioLoading, setIsScenarioLoading] = useState(false);
  const [isOptimizationLoading, setIsOptimizationLoading] = useState(false);
  const [isScenarioComparisonLoading, setIsScenarioComparisonLoading] = useState(false);
  const [isOrderSubmitting, setIsOrderSubmitting] = useState(false);
  const [selectedPolicyVariantId, setSelectedPolicyVariantId] = useState<number | null>(null);
  const [selectedScenarioName, setSelectedScenarioName] = useState('Base');
  const [whatIfServiceLevel, setWhatIfServiceLevel] = useState('0.95');
  const [whatIfLeadTime, setWhatIfLeadTime] = useState('');
  const [whatIfHoldingCost, setWhatIfHoldingCost] = useState('');
  const [whatIfOrderingCost, setWhatIfOrderingCost] = useState('');
  const [whatIfStockoutCost, setWhatIfStockoutCost] = useState('');

  useEffect(() => {
    async function loadMetadata() {
      try {
        const [productsResponse, suppliersResponse] = await Promise.all([
          apiClient.get<PagedResult<ProductDto>>('/products', {
            params: { pageSize: productPageSize, isActive: true }
          }),
          apiClient.get<SupplierDto[]>('/suppliers')
        ]);

        const fetchedProducts = productsResponse.data.items ?? [];
        const fetchedSuppliers = suppliersResponse.data ?? [];

        setProducts(fetchedProducts);
        setSuppliers(fetchedSuppliers);

        if (!selectedVariantId && fetchedProducts.length > 0) {
          const firstProduct = fetchedProducts[0];
          const firstVariant = firstProduct.variants[0];
          if (firstVariant) {
            const variantValue = String(firstVariant.id);
            setSelectedVariantId(variantValue);
            setSelectedVariantIds([variantValue]);
            setCurrency(firstProduct.currency ?? 'EUR');
          }
        }

        if (!selectedSupplierId && fetchedSuppliers.length > 0) {
          setSelectedSupplierId(String(fetchedSuppliers[0].id));
        }
      } catch (loadError) {
        console.error(loadError);
        setError('No se pudieron cargar los catálogos de productos y proveedores.');
      }
    }

    void loadMetadata();
  }, [selectedVariantId, selectedSupplierId]);

  const variantOptions = useMemo<VariantOption[]>(
    () =>
      products.flatMap((product) =>
        product.variants.map((variant) => ({
          id: variant.id,
          label: `${product.name} — ${variant.sku}`,
          sku: variant.sku,
          productName: product.name,
          unitPrice: variant.price ?? product.defaultPrice,
          currency: product.currency
        }))
      ),
    [products]
  );

  const variantMap = useMemo(() => {
    const map = new Map<number, VariantOption>();
    variantOptions.forEach((option) => {
      map.set(option.id, option);
    });
    return map;
  }, [variantOptions]);

  useEffect(() => {
    if (selectedVariantId) {
      const variant = variantMap.get(Number(selectedVariantId));
      if (variant) {
        setCurrency(variant.currency);
      }
    }
  }, [selectedVariantId, variantMap]);

  useEffect(() => {
    if (selectedVariantId && !selectedVariantIds.includes(selectedVariantId)) {
      setSelectedVariantIds((current) => [...current, selectedVariantId]);
    }
  }, [selectedVariantId, selectedVariantIds]);

  useEffect(() => {
    if (recommendations && recommendations.policies.length > 0) {
      setSelectedPolicyVariantId((current) => current ?? recommendations.policies[0].variantId);
    } else {
      setSelectedPolicyVariantId(null);
    }
  }, [recommendations]);

  useEffect(() => {
    if (scenarioComparison) {
      setSelectedScenarioName(scenarioComparison.baseline.scenarioName);
    }
  }, [scenarioComparison]);

  const selectedVariant = selectedVariantId ? variantMap.get(Number(selectedVariantId)) : undefined;

  const chartData = useMemo(() => {
    if (!forecast) {
      return [];
    }

    const historicalPeriods = new Set(forecast.historical.map((point) => point.period));
    const forecastPeriods = new Set(forecast.forecast.map((point) => point.period));
    const combined = [...forecast.historical, ...forecast.forecast];
    const seen = new Set<string>();

    return combined
      .filter((point) => {
        if (seen.has(point.period)) {
          return false;
        }
        seen.add(point.period);
        return true;
      })
      .map((point) => ({
        period: point.period,
        historical: historicalPeriods.has(point.period) ? point.quantity : null,
        forecast: forecastPeriods.has(point.period) ? point.quantity : null
      }));
  }, [forecast]);

  const scenarioVariant = scenario?.variants[0];

  const scenarioChartData = useMemo(() => {
    if (!scenarioVariant) {
      return [];
    }

    return scenarioVariant.scenarios.map((item) => ({
      leadTime: item.leadTimeDays,
      recommended: item.recommendedOrderQuantity,
      risk: Number((item.stockoutRisk * 100).toFixed(2)),
      residualRisk: Number((item.residualRisk * 100).toFixed(2))
    }));
  }, [scenarioVariant]);

  const selectedPolicy = useMemo(() => {
    if (!recommendations || recommendations.policies.length === 0) {
      return null;
    }

    if (selectedPolicyVariantId !== null) {
      return (
        recommendations.policies.find((policy) => policy.variantId === selectedPolicyVariantId) ??
        recommendations.policies[0]
      );
    }

    return recommendations.policies[0];
  }, [recommendations, selectedPolicyVariantId]);

  const policyChartData = useMemo(() => {
    if (!selectedPolicy) {
      return [];
    }

    return [
      { name: 'Stock mínimo', value: selectedPolicy.minStockLevel },
      { name: 'Punto de pedido', value: selectedPolicy.reorderPoint },
      { name: 'EOQ sugerido', value: selectedPolicy.economicOrderQuantity },
      { name: 'Stock máximo', value: selectedPolicy.maxStockLevel },
      { name: 'Disponible', value: selectedPolicy.available }
    ];
  }, [selectedPolicy]);

  const scenarioOptions = useMemo(() => {
    if (!scenarioComparison) {
      return [] as { value: string; label: string }[];
    }

    const options: { value: string; label: string }[] = [
      {
        value: scenarioComparison.baseline.scenarioName,
        label: `Base (${formatDecimal(scenarioComparison.baseline.kpis.fillRate * 100, 1)} %)`
      }
    ];

    scenarioComparison.alternatives.forEach((alternative) => {
      options.push({
        value: alternative.scenarioName,
        label: `${alternative.scenarioName} (${formatDecimal(alternative.kpis.fillRate * 100, 1)} %)`
      });
    });

    return options;
  }, [scenarioComparison]);

  const selectedScenarioOutcome = useMemo(() => {
    if (!scenarioComparison) {
      return null;
    }

    if (selectedScenarioName === scenarioComparison.baseline.scenarioName) {
      return scenarioComparison.baseline;
    }

    return (
      scenarioComparison.alternatives.find((alternative) => alternative.scenarioName === selectedScenarioName) ??
      scenarioComparison.baseline
    );
  }, [scenarioComparison, selectedScenarioName]);

  const scenarioKpiData = useMemo(() => {
    if (!scenarioComparison) {
      return [];
    }

    const data = [
      {
        name: scenarioComparison.baseline.scenarioName,
        fillRate: Number((scenarioComparison.baseline.kpis.fillRate * 100).toFixed(2)),
        totalCost: Number(scenarioComparison.baseline.kpis.totalCost.toFixed(2))
      }
    ];

    scenarioComparison.alternatives.forEach((alternative) => {
      data.push({
        name: alternative.scenarioName,
        fillRate: Number((alternative.kpis.fillRate * 100).toFixed(2)),
        totalCost: Number(alternative.kpis.totalCost.toFixed(2))
      });
    });

    return data;
  }, [scenarioComparison]);

  const planTotals = useMemo(
    () =>
      plan
        ? plan.items.reduce(
            (accumulator, item) => ({
              quantity: accumulator.quantity + item.recommendedOrderQuantity,
              cost: accumulator.cost + item.recommendedOrderQuantity * item.unitPrice
            }),
            { quantity: 0, cost: 0 }
          )
        : { quantity: 0, cost: 0 },
    [plan]
  );

  const planCurrency = plan?.items[0]?.currency ?? currency;

  const coverageRows = useMemo(() => {
    if (!plan) {
      return [] as {
        variantId: number;
        productName: string;
        sku: string;
        coverageDays: number;
        orderCoverageDays: number;
        leadTimeDays: number;
        reviewPeriodDays: number;
      }[];
    }

    return plan.items
      .map((item) => {
        const dailyDemand = item.averageDailyDemand;

        if (!dailyDemand || dailyDemand <= 0) {
          return null;
        }

        const totalCoverage = (item.available + item.recommendedOrderQuantity) / dailyDemand;
        const orderCoverage = item.recommendedOrderQuantity / dailyDemand;

        return {
          variantId: item.variantId,
          productName: item.productName,
          sku: item.variantSku,
          coverageDays: totalCoverage,
          orderCoverageDays: orderCoverage,
          leadTimeDays: item.leadTimeDays,
          reviewPeriodDays: item.reviewPeriodDays
        };
      })
      .filter((item): item is NonNullable<typeof item> => item !== null);
  }, [plan]);

  const coverageSummary = useMemo(() => {
    if (coverageRows.length === 0) {
      return null;
    }

    const totals = coverageRows.reduce(
      (accumulator, row) => ({
        coverageDays: accumulator.coverageDays + row.coverageDays,
        orderCoverageDays: accumulator.orderCoverageDays + row.orderCoverageDays
      }),
      { coverageDays: 0, orderCoverageDays: 0 }
    );

    return {
      averageCoverage: totals.coverageDays / coverageRows.length,
      averageOrderCoverage: totals.orderCoverageDays / coverageRows.length
    };
  }, [coverageRows]);

  function handlePlanVariantsChange(event: ChangeEvent<HTMLSelectElement>) {
    const values = Array.from(event.target.selectedOptions).map((option) => option.value);
    setSelectedVariantIds(values);
  }

  async function handleForecast(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsLoading(true);
    setError(null);

    if (!selectedVariantId) {
      setError('Selecciona una variante para generar la previsión.');
      setIsLoading(false);
      return;
    }

    const periodsValue = Number(periods);
    if (!Number.isFinite(periodsValue) || periodsValue <= 0) {
      setError('Indica un número de periodos válido.');
      setIsLoading(false);
      return;
    }

    const alphaValue = alpha.trim() ? Number(alpha) : undefined;
    const betaValue = beta.trim() ? Number(beta) : undefined;
    const seasonLengthValue = seasonLength.trim() ? Number(seasonLength) : undefined;

    try {
      const params: Record<string, unknown> = {
        periods: periodsValue,
        includeSeasonality
      };

      if (alphaValue !== undefined && Number.isFinite(alphaValue)) {
        params.alpha = alphaValue;
      }

      if (betaValue !== undefined && Number.isFinite(betaValue)) {
        params.beta = betaValue;
      }

      if (seasonLengthValue !== undefined && Number.isFinite(seasonLengthValue)) {
        params.seasonLength = seasonLengthValue;
      }

      const response = await apiClient.get<DemandForecastDto>(`/analytics/demand/${selectedVariantId}`, { params });
      setForecast(response.data);
    } catch (requestError) {
      console.error(requestError);
      setError('No se pudo calcular la previsión para la variante indicada.');
      setForecast(null);
    } finally {
      setIsLoading(false);
    }
  }

  async function handleGeneratePlan() {
    setPlanError(null);
    setOrderError(null);
    setOrderSuccess(null);

    const periodsValue = Number(periods);
    const variantIds = selectedVariantIds
      .map((value) => Number(value))
      .filter((value) => Number.isFinite(value) && value > 0);

    if (variantIds.length === 0) {
      setPlanError('Selecciona al menos una variante para generar el plan.');
      return;
    }

    if (!Number.isFinite(periodsValue) || periodsValue <= 0) {
      setPlanError('Indica un número de periodos válido.');
      return;
    }

    const alphaValue = alpha.trim() ? Number(alpha) : undefined;
    const betaValue = beta.trim() ? Number(beta) : undefined;
    const seasonLengthValue = seasonLength.trim() ? Number(seasonLength) : undefined;
    const leadTimeValue = leadTimeDays.trim() ? Number(leadTimeDays) : undefined;
    const reviewPeriodValue = reviewPeriodDays.trim() ? Number(reviewPeriodDays) : undefined;
    const serviceLevelValue = serviceLevel.trim() ? Number(serviceLevel) : undefined;
    const safetyFactorValue = safetyStockFactor.trim() ? Number(safetyStockFactor) : undefined;

    setIsPlanLoading(true);

    try {
      const payload: Record<string, unknown> = {
        variantIds,
        periods: periodsValue,
        includeSeasonality
      };

      if (alphaValue !== undefined && Number.isFinite(alphaValue)) {
        payload.alpha = alphaValue;
      }

      if (betaValue !== undefined && Number.isFinite(betaValue)) {
        payload.beta = betaValue;
      }

      if (seasonLengthValue !== undefined && Number.isFinite(seasonLengthValue)) {
        payload.seasonLength = seasonLengthValue;
      }

      if (leadTimeValue !== undefined && Number.isFinite(leadTimeValue)) {
        payload.leadTimeDays = leadTimeValue;
      }

      if (reviewPeriodValue !== undefined && Number.isFinite(reviewPeriodValue)) {
        payload.reviewPeriodDays = reviewPeriodValue;
      }

      if (serviceLevelValue !== undefined && Number.isFinite(serviceLevelValue)) {
        payload.serviceLevel = serviceLevelValue;
      }

      if (safetyFactorValue !== undefined && Number.isFinite(safetyFactorValue)) {
        payload.safetyStockFactor = safetyFactorValue;
      }

      const response = await apiClient.post<PurchasePlanDto>('/analytics/purchase-plan', payload);
      setPlan(response.data);
    } catch (requestError) {
      console.error(requestError);
      setPlanError('No se pudo generar el plan de compra sugerido.');
      setPlan(null);
    } finally {
      setIsPlanLoading(false);
    }
  }

  async function handleSimulateScenario() {
    setScenarioError(null);

    if (!selectedVariantId) {
      setScenarioError('Selecciona una variante para simular escenarios.');
      return;
    }

    const periodsValue = Number(periods);
    if (!Number.isFinite(periodsValue) || periodsValue <= 0) {
      setScenarioError('Indica un número de periodos válido.');
      return;
    }

    const alphaValue = alpha.trim() ? Number(alpha) : undefined;
    const betaValue = beta.trim() ? Number(beta) : undefined;
    const seasonLengthValue = seasonLength.trim() ? Number(seasonLength) : undefined;
    const safetyFactorValue = safetyStockFactor.trim() ? Number(safetyStockFactor) : undefined;

    const leadTimes = leadTimeOptions
      .split(',')
      .map((value) => Number(value.trim()))
      .filter((value) => Number.isFinite(value) && value > 0);

    if (leadTimes.length === 0) {
      setScenarioError('Indica al menos un plazo de proveedor válido.');
      return;
    }

    setIsScenarioLoading(true);

    try {
      const payload: Record<string, unknown> = {
        variantId: Number(selectedVariantId),
        periods: periodsValue,
        leadTimesDays: leadTimes,
        includeSeasonality
      };

      if (alphaValue !== undefined && Number.isFinite(alphaValue)) {
        payload.alpha = alphaValue;
      }

      if (betaValue !== undefined && Number.isFinite(betaValue)) {
        payload.beta = betaValue;
      }

      if (seasonLengthValue !== undefined && Number.isFinite(seasonLengthValue)) {
        payload.seasonLength = seasonLengthValue;
      }

      if (safetyFactorValue !== undefined && Number.isFinite(safetyFactorValue)) {
        payload.safetyStockFactor = safetyFactorValue;
      }

      const response = await apiClient.post<ScenarioSimulationDto>('/analytics/scenario-simulations', payload);
      setScenario(response.data);
    } catch (requestError) {
      console.error(requestError);
      setScenarioError('No se pudieron calcular los escenarios solicitados.');
      setScenario(null);
    } finally {
      setIsScenarioLoading(false);
    }
  }

  async function handleFetchRecommendations() {
    setOptimizationError(null);
    setRecommendations(null);

    const variantIds = selectedVariantIds
      .map((value) => Number(value))
      .filter((value) => Number.isFinite(value) && value > 0);

    if (variantIds.length === 0) {
      setOptimizationError('Selecciona al menos una variante para recomendar políticas.');
      return;
    }

    const periodsValue = Number(periods);
    if (!Number.isFinite(periodsValue) || periodsValue <= 0) {
      setOptimizationError('Indica un número de periodos válido.');
      return;
    }

    const alphaValue = alpha.trim() ? Number(alpha) : undefined;
    const betaValue = beta.trim() ? Number(beta) : undefined;
    const seasonLengthValue = seasonLength.trim() ? Number(seasonLength) : undefined;
    const leadTimeValue = leadTimeDays.trim() ? Number(leadTimeDays) : undefined;
    const reviewPeriodValue = reviewPeriodDays.trim() ? Number(reviewPeriodDays) : undefined;
    const serviceLevelValue = serviceLevel.trim() ? Number(serviceLevel) : undefined;

    setIsOptimizationLoading(true);

    try {
      const payload: Record<string, unknown> = {
        variantIds,
        periods: periodsValue,
        includeSeasonality,
        monteCarloIterations: 400
      };

      if (alphaValue !== undefined && Number.isFinite(alphaValue)) {
        payload.alpha = alphaValue;
      }

      if (betaValue !== undefined && Number.isFinite(betaValue)) {
        payload.beta = betaValue;
      }

      if (seasonLengthValue !== undefined && Number.isFinite(seasonLengthValue)) {
        payload.seasonLength = seasonLengthValue;
      }

      if (leadTimeValue !== undefined && Number.isFinite(leadTimeValue)) {
        payload.leadTimeDays = leadTimeValue;
      }

      if (reviewPeriodValue !== undefined && Number.isFinite(reviewPeriodValue)) {
        payload.reviewPeriodDays = reviewPeriodValue;
      }

      if (serviceLevelValue !== undefined && Number.isFinite(serviceLevelValue)) {
        payload.serviceLevel = serviceLevelValue;
      }

      const response = await apiClient.post<OptimizationRecommendationDto>(
        '/analytics/optimization/recommendations',
        payload
      );
      setRecommendations(response.data);
    } catch (requestError) {
      console.error(requestError);
      setOptimizationError('No se pudieron generar recomendaciones de política.');
    } finally {
      setIsOptimizationLoading(false);
    }
  }

  async function handleCompareOptimizationScenarios() {
    setScenarioComparisonError(null);
    setScenarioComparison(null);

    if (!selectedVariantId) {
      setScenarioComparisonError('Selecciona una variante para comparar escenarios.');
      return;
    }

    const periodsValue = Number(periods);
    if (!Number.isFinite(periodsValue) || periodsValue <= 0) {
      setScenarioComparisonError('Indica un número de periodos válido.');
      return;
    }

    const alphaValue = alpha.trim() ? Number(alpha) : undefined;
    const betaValue = beta.trim() ? Number(beta) : undefined;
    const seasonLengthValue = seasonLength.trim() ? Number(seasonLength) : undefined;
    const leadTimeValue = leadTimeDays.trim() ? Number(leadTimeDays) : undefined;
    const reviewPeriodValue = reviewPeriodDays.trim() ? Number(reviewPeriodDays) : undefined;
    const serviceLevelValue = serviceLevel.trim() ? Number(serviceLevel) : undefined;

    const scenarioPayload: Record<string, unknown> = {
      variantId: Number(selectedVariantId),
      periods: periodsValue,
      includeSeasonality,
      monteCarloIterations: 500
    };

    if (alphaValue !== undefined && Number.isFinite(alphaValue)) {
      scenarioPayload.alpha = alphaValue;
    }

    if (betaValue !== undefined && Number.isFinite(betaValue)) {
      scenarioPayload.beta = betaValue;
    }

    if (seasonLengthValue !== undefined && Number.isFinite(seasonLengthValue)) {
      scenarioPayload.seasonLength = seasonLengthValue;
    }

    if (leadTimeValue !== undefined && Number.isFinite(leadTimeValue)) {
      scenarioPayload.leadTimeDays = leadTimeValue;
    }

    if (reviewPeriodValue !== undefined && Number.isFinite(reviewPeriodValue)) {
      scenarioPayload.reviewPeriodDays = reviewPeriodValue;
    }

    if (serviceLevelValue !== undefined && Number.isFinite(serviceLevelValue)) {
      scenarioPayload.serviceLevel = serviceLevelValue;
    }

    const whatIfScenario: Record<string, unknown> = { name: 'What-if personalizado' };
    const whatIfServiceLevelValue = whatIfServiceLevel.trim() ? Number(whatIfServiceLevel) : undefined;
    const whatIfLeadTimeValue = whatIfLeadTime.trim() ? Number(whatIfLeadTime) : undefined;
    const whatIfHoldingCostValue = whatIfHoldingCost.trim() ? Number(whatIfHoldingCost) : undefined;
    const whatIfOrderingCostValue = whatIfOrderingCost.trim() ? Number(whatIfOrderingCost) : undefined;
    const whatIfStockoutCostValue = whatIfStockoutCost.trim() ? Number(whatIfStockoutCost) : undefined;

    if (
      (whatIfServiceLevelValue !== undefined && Number.isFinite(whatIfServiceLevelValue)) ||
      (whatIfLeadTimeValue !== undefined && Number.isFinite(whatIfLeadTimeValue)) ||
      (whatIfHoldingCostValue !== undefined && Number.isFinite(whatIfHoldingCostValue)) ||
      (whatIfOrderingCostValue !== undefined && Number.isFinite(whatIfOrderingCostValue)) ||
      (whatIfStockoutCostValue !== undefined && Number.isFinite(whatIfStockoutCostValue))
    ) {
      if (whatIfServiceLevelValue !== undefined && Number.isFinite(whatIfServiceLevelValue)) {
        whatIfScenario.serviceLevel = whatIfServiceLevelValue;
      }

      if (whatIfLeadTimeValue !== undefined && Number.isFinite(whatIfLeadTimeValue)) {
        whatIfScenario.leadTimeDays = whatIfLeadTimeValue;
      }

      if (whatIfHoldingCostValue !== undefined && Number.isFinite(whatIfHoldingCostValue)) {
        whatIfScenario.holdingCostRate = whatIfHoldingCostValue;
      }

      if (whatIfOrderingCostValue !== undefined && Number.isFinite(whatIfOrderingCostValue)) {
        whatIfScenario.orderingCost = whatIfOrderingCostValue;
      }

      if (whatIfStockoutCostValue !== undefined && Number.isFinite(whatIfStockoutCostValue)) {
        whatIfScenario.stockoutCost = whatIfStockoutCostValue;
      }

      scenarioPayload.scenarios = [whatIfScenario];
    }

    setIsScenarioComparisonLoading(true);

    try {
      const response = await apiClient.post<OptimizationScenarioComparisonDto>(
        '/analytics/optimization/scenarios',
        scenarioPayload
      );
      setScenarioComparison(response.data);
    } catch (requestError) {
      console.error(requestError);
      setScenarioComparisonError('No se pudieron comparar los escenarios indicados.');
    } finally {
      setIsScenarioComparisonLoading(false);
    }
  }

  async function handleCreatePurchaseOrder() {
    setOrderError(null);
    setOrderSuccess(null);

    if (!plan) {
      setOrderError('Genera primero un plan de compra.');
      return;
    }

    if (!selectedSupplierId) {
      setOrderError('Selecciona un proveedor para crear la orden.');
      return;
    }

    const lines = plan.items
      .filter((item) => item.recommendedOrderQuantity > 0)
      .map((item) => ({
        variantId: item.variantId,
        quantity: Math.round(item.recommendedOrderQuantity * 100) / 100,
        unitPrice: Math.round(item.unitPrice * 100) / 100,
        discount: null,
        taxRateId: null
      }));

    if (lines.length === 0) {
      setOrderError('No hay cantidades recomendadas para generar una orden.');
      return;
    }

    setIsOrderSubmitting(true);

    try {
      await apiClient.post('/purchaseorders', {
        supplierId: Number(selectedSupplierId),
        orderDate: new Date().toISOString(),
        status: 1,
        currency: currency || plan.items[0]?.currency || 'EUR',
        notes: orderNotes.trim() || null,
        lines
      });

      setOrderSuccess('Orden de compra generada correctamente.');
    } catch (requestError) {
      console.error(requestError);
      setOrderError('No se pudo generar la orden de compra.');
    } finally {
      setIsOrderSubmitting(false);
    }
  }

  const planVariantsSize = Math.min(Math.max(variantOptions.length, 4), 8);

  return (
    <div className="flex flex-col gap-8">
      <section className="flex flex-col gap-4">
        <Card
          title={
            <span className="flex items-center gap-2">
              Pronóstico de demanda
              <InfoTooltip content={PLANNING_HELP.forecastSetup} size="sm" />
            </span>
          }
          subtitle="Proyecta la demanda de una variante específica."
        >
          <form className="flex flex-col gap-4" onSubmit={handleForecast}>
            <div className="grid gap-4 md:grid-cols-2">
              <Select
                label={<LabelWithInfo label="Variante a pronosticar" info={PLANNING_HELP.variantPrincipal} />}
                value={selectedVariantId}
                onChange={(event) => setSelectedVariantId(event.target.value)}
                required
                hint={formatUsageHint(['forecast', 'scenario', 'comparison'])}
              >
                <option value="">Selecciona una variante</option>
                {variantOptions.map((variant) => (
                  <option key={variant.id} value={variant.id}>
                    {variant.label}
                  </option>
                ))}
              </Select>

              <Input
                label={<LabelWithInfo label="Meses a proyectar" info={PLANNING_HELP.periods} />}
                type="number"
                min={1}
                max={24}
                value={periods}
                onChange={(event) => setPeriods(event.target.value)}
                required
              />

              <Input
                label={<LabelWithInfo label="Alpha (0-1)" info={PLANNING_HELP.alpha} />}
                type="number"
                step="0.05"
                min={0}
                max={1}
                value={alpha}
                onChange={(event) => setAlpha(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Beta (0-1)" info={PLANNING_HELP.beta} />}
                type="number"
                step="0.05"
                min={0}
                max={1}
                value={beta}
                onChange={(event) => setBeta(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Ciclos estacionales" info={PLANNING_HELP.seasonLength} />}
                type="number"
                min={1}
                max={24}
                value={seasonLength}
                onChange={(event) => setSeasonLength(event.target.value)}
                hint="Opcional"
              />

              <label
                htmlFor="includeSeasonalityForecast"
                className="mt-1 flex cursor-pointer items-center gap-2 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600 shadow-sm md:col-span-2"
              >
                <input
                  id="includeSeasonalityForecast"
                  type="checkbox"
                  checked={includeSeasonality}
                  onChange={(event) => setIncludeSeasonality(event.target.checked)}
                  className="h-4 w-4 rounded border-slate-300 text-primary-600 focus:ring-primary-500"
                />
                <LabelWithInfo
                  label="Aplicar factores estacionales"
                  info={PLANNING_HELP.includeSeasonality}
                  className="text-slate-600"
                />
              </label>
            </div>
            <div className="flex flex-col gap-2">
              <button
                type="submit"
                className="inline-flex items-center justify-center rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-primary-700 disabled:opacity-60"
                disabled={isLoading}
              >
                {isLoading ? 'Calculando…' : 'Calcular previsión'}
              </button>
              {error && <p className="text-xs text-red-500">{error}</p>}
            </div>
          </form>
        </Card>
        {forecast && (
          <div className="grid gap-6 lg:grid-cols-2">
            <Card
              title={
                <span className="flex items-center gap-2">
                  {selectedVariant ? `Pronóstico de ${selectedVariant.productName}` : 'Pronóstico de demanda'}
                  <InfoTooltip content={PLANNING_HELP.forecastCard} size="sm" />
                </span>
              }
              subtitle={selectedVariant ? `Variante ${selectedVariant.sku}` : undefined}
            >
              {chartData.length === 0 ? (
                <p className="text-sm text-slate-500">Sin histórico para graficar.</p>
              ) : (
                <div className="h-72">
                  <ResponsiveContainer width="100%" height="100%">
                    <LineChart data={chartData}>
                      <XAxis dataKey="period" stroke="#94a3b8" fontSize={12} />
                      <YAxis stroke="#94a3b8" fontSize={12} />
                      <Tooltip />
                      <Legend />
                      <Line type="monotone" dataKey="historical" name="Histórico" stroke="#0f78ff" strokeWidth={2} dot={false} />
                      <Line type="monotone" dataKey="forecast" name="Pronóstico" stroke="#f97316" strokeWidth={2} strokeDasharray="6 3" dot />
                    </LineChart>
                  </ResponsiveContainer>
                </div>
              )}
            </Card>

            <Card
              title={
                <span className="flex items-center gap-2">
                  Detalle de periodos
                  <InfoTooltip content={PLANNING_HELP.forecastTable} size="sm" />
                </span>
              }
              subtitle="Comparativa entre histórico y proyección"
            >
              <div className="overflow-hidden rounded-xl border border-slate-200 bg-white">
                <table className="min-w-full divide-y divide-slate-200 text-sm">
                  <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                    <tr>
                      <th className="px-3 py-2 text-left">
                        <TableHeaderLabel label="Periodo" info={PLANNING_HELP.forecastPeriod} />
                      </th>
                      <th className="px-3 py-2 text-right">
                        <TableHeaderLabel label="Histórico" info={PLANNING_HELP.forecastHistorical} />
                      </th>
                      <th className="px-3 py-2 text-right">
                        <TableHeaderLabel label="Forecast" info={PLANNING_HELP.forecastProjected} />
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {chartData.map((item) => (
                      <tr key={item.period} className="text-slate-700">
                        <td className="px-3 py-2 text-xs text-slate-500">{item.period}</td>
                        <td className="px-3 py-2 text-right text-sm font-medium text-slate-900">
                          {item.historical !== null ? formatDecimal(item.historical) : '—'}
                        </td>
                        <td className="px-3 py-2 text-right text-sm font-medium text-primary-600">
                          {item.forecast !== null ? formatDecimal(item.forecast) : '—'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </Card>
          </div>
        )}
      </section>

      <section className="flex flex-col gap-4">
        <Card
          title={
            <span className="flex items-center gap-2">
              Plan sugerido de compra
              <InfoTooltip content={PLANNING_HELP.planSetup} size="sm" />
            </span>
          }
          subtitle="Convierte la previsión en cantidades recomendadas."
        >
          <form
            className="flex flex-col gap-4"
            onSubmit={(event) => {
              event.preventDefault();
              void handleGeneratePlan();
            }}
          >
            <Select
              label={<LabelWithInfo label="Variantes incluidas" info={PLANNING_HELP.planVariants} />}
              multiple
              value={selectedVariantIds}
              onChange={handlePlanVariantsChange}
              size={planVariantsSize}
            >
              {variantOptions.map((variant) => (
                <option key={variant.id} value={variant.id}>
                  {variant.label}
                </option>
              ))}
            </Select>
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              <Input
                label={<LabelWithInfo label="Meses a proyectar" info={PLANNING_HELP.periods} />}
                type="number"
                min={1}
                max={24}
                value={periods}
                onChange={(event) => setPeriods(event.target.value)}
                required
                hint={formatUsageHint(['forecast', 'plan', 'scenario', 'optimization', 'comparison'])}
              />

              <Input
                label={<LabelWithInfo label="Alpha (0-1)" info={PLANNING_HELP.alpha} />}
                type="number"
                step="0.05"
                min={0}
                max={1}
                value={alpha}
                onChange={(event) => setAlpha(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Beta (0-1)" info={PLANNING_HELP.beta} />}
                type="number"
                step="0.05"
                min={0}
                max={1}
                value={beta}
                onChange={(event) => setBeta(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Ciclos estacionales" info={PLANNING_HELP.seasonLength} />}
                type="number"
                min={1}
                max={24}
                value={seasonLength}
                onChange={(event) => setSeasonLength(event.target.value)}
                hint="Opcional"
              />

              <label
                htmlFor="includeSeasonalityPlan"
                className="mt-1 flex cursor-pointer items-center gap-2 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600 shadow-sm md:col-span-2 xl:col-span-3"
              >
                <input
                  id="includeSeasonalityPlan"
                  type="checkbox"
                  checked={includeSeasonality}
                  onChange={(event) => setIncludeSeasonality(event.target.checked)}
                  className="h-4 w-4 rounded border-slate-300 text-primary-600 focus:ring-primary-500"
                />
                <LabelWithInfo
                  label="Aplicar factores estacionales"
                  info={PLANNING_HELP.includeSeasonality}
                  className="text-slate-600"
                />
                <span className="ml-auto text-xs text-slate-400">
                  {formatUsageHint(['forecast', 'plan', 'scenario', 'optimization', 'comparison'])}
                </span>
              </label>

              <Input
                label={<LabelWithInfo label="Lead time manual (días)" info={PLANNING_HELP.leadTimeDays} />}
                type="number"
                min={1}
                value={leadTimeDays}
                onChange={(event) => setLeadTimeDays(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Revisión (días)" info={PLANNING_HELP.reviewPeriodDays} />}
                type="number"
                min={1}
                value={reviewPeriodDays}
                onChange={(event) => setReviewPeriodDays(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Nivel de servicio" info={PLANNING_HELP.serviceLevel} />}
                type="number"
                step="0.05"
                min={0}
                max={1}
                value={serviceLevel}
                onChange={(event) => setServiceLevel(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Factor de stock de seguridad" info={PLANNING_HELP.safetyStockFactor} />}
                type="number"
                step="0.1"
                min={0}
                value={safetyStockFactor}
                onChange={(event) => setSafetyStockFactor(event.target.value)}
                hint="Opcional"
              />
            </div>
            <div className="flex flex-col gap-2">
              <button
                type="submit"
                className="inline-flex items-center justify-center rounded-lg border border-primary-200 bg-primary-50 px-4 py-2 text-sm font-medium text-primary-700 transition hover:bg-primary-100 disabled:opacity-60"
                disabled={isPlanLoading}
              >
                {isPlanLoading ? 'Generando plan…' : 'Generar plan sugerido'}
              </button>
              {planError && <p className="text-xs text-red-500">{planError}</p>}
            </div>
          </form>
        </Card>
        {plan && (
          <Card
            title={
              <span className="flex items-center gap-2">
                Plan de compra sugerido
                <InfoTooltip content={PLANNING_HELP.planCard} size="sm" />
              </span>
            }
            subtitle="Recomendaciones basadas en la previsión y la política ABC"
          >
            <div className="mb-4 grid gap-3 text-sm text-slate-600 sm:grid-cols-3">
              <div className="rounded-lg border border-slate-200 bg-white px-4 py-3 shadow-sm">
                <span className="flex items-center justify-between text-xs uppercase text-slate-500">
                  Unidades totales
                  <InfoTooltip content={PLANNING_HELP.planTotalsUnits} size="sm" />
                </span>
                <p className="mt-1 text-lg font-semibold text-slate-900">{formatDecimal(planTotals.quantity)}</p>
              </div>
              <div className="rounded-lg border border-slate-200 bg-white px-4 py-3 shadow-sm">
                <span className="flex items-center justify-between text-xs uppercase text-slate-500">
                  Inversión estimada
                  <InfoTooltip content={PLANNING_HELP.planTotalsCost} size="sm" />
                </span>
                <p className="mt-1 text-lg font-semibold text-slate-900">
                  {formatDecimal(planTotals.cost)} {planCurrency}
                </p>
              </div>
              <div className="rounded-lg border border-slate-200 bg-white px-4 py-3 shadow-sm">
                <span className="flex items-center justify-between text-xs uppercase text-slate-500">
                  Variantes incluidas
                  <InfoTooltip content={PLANNING_HELP.planTotalsItems} size="sm" />
                </span>
                <p className="mt-1 text-lg font-semibold text-slate-900">{plan.items.length}</p>
              </div>
            </div>
            <p className="mb-4 flex items-center gap-2 text-xs text-slate-500">
              <span>Generado:</span>
              <InfoTooltip content={PLANNING_HELP.planGenerated} size="sm" />
              <span className="font-medium text-slate-700">{new Date(plan.generatedAt).toLocaleString('es-ES')}</span>
            </p>
            <div className="overflow-hidden rounded-xl border border-slate-200 bg-white">
              <table className="min-w-full divide-y divide-slate-200 text-sm">
                <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                  <tr>
                    <th className="px-3 py-2 text-left">
                      <TableHeaderLabel label="Variante" info={PLANNING_HELP.planVariant} />
                    </th>
                    <th className="px-3 py-2 text-right">
                      <TableHeaderLabel label="Disponible" info={PLANNING_HELP.planAvailable} />
                    </th>
                    <th className="px-3 py-2 text-right">
                      <TableHeaderLabel label="Forecast" info={PLANNING_HELP.planForecast} />
                    </th>
                    <th className="px-3 py-2 text-right">
                      <TableHeaderLabel label="Stock seg." info={PLANNING_HELP.planSafetyStock} />
                    </th>
                    <th className="px-3 py-2 text-right">
                      <TableHeaderLabel label="Reorden" info={PLANNING_HELP.planReorder} />
                    </th>
                    <th className="px-3 py-2 text-right">
                      <TableHeaderLabel label="Recomendado" info={PLANNING_HELP.planRecommended} />
                    </th>
                    <th className="px-3 py-2 text-right">
                      <TableHeaderLabel label="Nivel servicio" info={PLANNING_HELP.planServiceLevel} />
                    </th>
                    <th className="px-3 py-2 text-left">
                      <TableHeaderLabel label="ABC" info={PLANNING_HELP.planAbc} />
                    </th>
                    <th className="px-3 py-2 text-right">
                      <TableHeaderLabel label="Precio" info={PLANNING_HELP.planUnitPrice} />
                    </th>
                    <th className="px-3 py-2 text-right">
                      <TableHeaderLabel label="Coste estimado" info={PLANNING_HELP.planCost} />
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {plan.items.map((item) => (
                    <tr key={item.variantId} className="text-slate-700">
                      <td className="px-3 py-2 text-sm font-medium text-slate-900">
                        <div className="flex flex-col">
                          <span>{item.productName}</span>
                          <span className="text-xs text-slate-500">SKU: {item.variantSku}</span>
                        </div>
                      </td>
                      <td className="px-3 py-2 text-right">{formatDecimal(item.available)}</td>
                      <td className="px-3 py-2 text-right">{formatDecimal(item.forecastedDemand)}</td>
                      <td className="px-3 py-2 text-right">{formatDecimal(item.safetyStock)}</td>
                      <td className="px-3 py-2 text-right">{formatDecimal(item.reorderPoint)}</td>
                      <td className="px-3 py-2 text-right text-primary-600">{formatDecimal(item.recommendedOrderQuantity)}</td>
                      <td className="px-3 py-2 text-right">
                        {item.serviceLevel !== undefined ? `${formatDecimal(item.serviceLevel * 100, 1)} %` : '—'}
                      </td>
                      <td className="px-3 py-2 text-left">{item.abcClass ?? '—'}</td>
                      <td className="px-3 py-2 text-right">
                        {formatDecimal(item.unitPrice)} {item.currency}
                      </td>
                      <td className="px-3 py-2 text-right font-medium text-slate-900">
                        {formatDecimal(item.recommendedOrderQuantity * item.unitPrice)} {item.currency}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Card>
        )}
        {coverageRows.length > 0 && (
          <Card
            title={
              <span className="flex items-center gap-2">
                Cobertura de inventario
                <InfoTooltip content={PLANNING_HELP.coverageCard} size="sm" />
              </span>
            }
            subtitle="Traduce el plan en días de cobertura por variante"
          >
            {coverageSummary && (
              <div className="mb-4 grid gap-3 text-sm text-slate-600 sm:grid-cols-2">
                <div className="rounded-lg border border-slate-200 bg-white px-4 py-3 shadow-sm">
                  <span className="flex items-center justify-between text-xs uppercase text-slate-500">
                    Cobertura media total
                    <InfoTooltip content={PLANNING_HELP.coverageCoverage} size="sm" />
                  </span>
                  <p className="mt-1 text-lg font-semibold text-slate-900">
                    {formatDecimal(coverageSummary.averageCoverage, 1)} días
                  </p>
                </div>
                <div className="rounded-lg border border-slate-200 bg-white px-4 py-3 shadow-sm">
                  <span className="flex items-center justify-between text-xs uppercase text-slate-500">
                    Cobertura media del pedido
                    <InfoTooltip content={PLANNING_HELP.coverageOrderCoverage} size="sm" />
                  </span>
                  <p className="mt-1 text-lg font-semibold text-slate-900">
                    {formatDecimal(coverageSummary.averageOrderCoverage, 1)} días
                  </p>
                </div>
              </div>
            )}

            <div className="overflow-hidden rounded-xl border border-slate-200 bg-white">
              <table className="min-w-full divide-y divide-slate-200 text-sm">
                <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                  <tr>
                    <th className="px-3 py-2 text-left">
                      <TableHeaderLabel label="Variante" info={PLANNING_HELP.coverageVariant} />
                    </th>
                    <th className="px-3 py-2 text-right">
                      <TableHeaderLabel label="Cobertura total" info={PLANNING_HELP.coverageCoverage} />
                    </th>
                    <th className="px-3 py-2 text-right">
                      <TableHeaderLabel label="Cobertura pedido" info={PLANNING_HELP.coverageOrderCoverage} />
                    </th>
                    <th className="px-3 py-2 text-right">
                      <TableHeaderLabel label="Lead time" info={PLANNING_HELP.coverageLeadTime} />
                    </th>
                    <th className="px-3 py-2 text-right">
                      <TableHeaderLabel label="Revisión" info={PLANNING_HELP.coverageReview} />
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {coverageRows.map((row) => (
                    <tr key={row.variantId} className="text-slate-700">
                      <td className="px-3 py-2 text-sm font-medium text-slate-900">
                        <div className="flex flex-col">
                          <span>{row.productName}</span>
                          <span className="text-xs text-slate-500">SKU: {row.sku}</span>
                        </div>
                      </td>
                      <td className="px-3 py-2 text-right font-medium text-slate-900">
                        {formatDecimal(row.coverageDays, 1)} días
                      </td>
                      <td className="px-3 py-2 text-right text-primary-600">
                        {formatDecimal(row.orderCoverageDays, 1)} días
                      </td>
                      <td className="px-3 py-2 text-right">{row.leadTimeDays} días</td>
                      <td className="px-3 py-2 text-right">{row.reviewPeriodDays} días</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Card>
        )}
        {plan && (
          <Card
            title={
              <span className="flex items-center gap-2">
                Generar orden de compra
                <InfoTooltip content={PLANNING_HELP.orderCard} size="sm" />
              </span>
            }
            subtitle="Convierte las recomendaciones en una orden real"
          >
            <div className="grid gap-4 md:grid-cols-3">
              <Select
                label={<LabelWithInfo label="Proveedor" info={PLANNING_HELP.orderSupplier} />}
                value={selectedSupplierId}
                onChange={(event) => setSelectedSupplierId(event.target.value)}
              >
                {suppliers.map((supplier) => (
                  <option key={supplier.id} value={supplier.id}>
                    {supplier.name}
                  </option>
                ))}
              </Select>

              <Input
                label={<LabelWithInfo label="Moneda" info={PLANNING_HELP.orderCurrency} />}
                value={currency}
                onChange={(event) => setCurrency(event.target.value)}
              />

              <Textarea
                label={<LabelWithInfo label="Notas" info={PLANNING_HELP.orderNotes} />}
                value={orderNotes}
                onChange={(event) => setOrderNotes(event.target.value)}
                rows={3}
              />
            </div>
            <div className="mt-4 flex gap-3">
              <button
                type="button"
                className="inline-flex items-center justify-center rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition hover:bg-primary-700"
                onClick={handleCreatePurchaseOrder}
                disabled={isOrderSubmitting}
              >
                {isOrderSubmitting ? 'Generando…' : 'Generar orden de compra'}
              </button>
            </div>
            {orderError && <p className="mt-3 text-sm text-red-500">{orderError}</p>}
            {orderSuccess && <p className="mt-3 text-sm text-green-600">{orderSuccess}</p>}
          </Card>
        )}
      </section>

      <section className="flex flex-col gap-4">
        <Card
          title={
            <span className="flex items-center gap-2">
              Simulación de escenarios
              <InfoTooltip content={PLANNING_HELP.scenarioSetup} size="sm" />
            </span>
          }
          subtitle="Explora distintos plazos de proveedor y su impacto en el riesgo."
        >
          <form
            className="flex flex-col gap-4"
            onSubmit={(event) => {
              event.preventDefault();
              void handleSimulateScenario();
            }}
          >
            <div className="grid gap-4 md:grid-cols-2">
              <Select
                label={<LabelWithInfo label="Variante a simular" info={PLANNING_HELP.variantPrincipal} />}
                value={selectedVariantId}
                onChange={(event) => setSelectedVariantId(event.target.value)}
                required
              >
                <option value="">Selecciona una variante</option>
                {variantOptions.map((variant) => (
                  <option key={variant.id} value={variant.id}>
                    {variant.label}
                  </option>
                ))}
              </Select>

              <Input
                label={<LabelWithInfo label="Meses a proyectar" info={PLANNING_HELP.periods} />}
                type="number"
                min={1}
                max={24}
                value={periods}
                onChange={(event) => setPeriods(event.target.value)}
                required
              />

              <Input
                label={<LabelWithInfo label="Alpha (0-1)" info={PLANNING_HELP.alpha} />}
                type="number"
                step="0.05"
                min={0}
                max={1}
                value={alpha}
                onChange={(event) => setAlpha(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Beta (0-1)" info={PLANNING_HELP.beta} />}
                type="number"
                step="0.05"
                min={0}
                max={1}
                value={beta}
                onChange={(event) => setBeta(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Ciclos estacionales" info={PLANNING_HELP.seasonLength} />}
                type="number"
                min={1}
                max={24}
                value={seasonLength}
                onChange={(event) => setSeasonLength(event.target.value)}
                hint="Opcional"
              />

              <label
                htmlFor="includeSeasonalityScenario"
                className="mt-1 flex cursor-pointer items-center gap-2 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600 shadow-sm md:col-span-2"
              >
                <input
                  id="includeSeasonalityScenario"
                  type="checkbox"
                  checked={includeSeasonality}
                  onChange={(event) => setIncludeSeasonality(event.target.checked)}
                  className="h-4 w-4 rounded border-slate-300 text-primary-600 focus:ring-primary-500"
                />
                <LabelWithInfo
                  label="Aplicar factores estacionales"
                  info={PLANNING_HELP.includeSeasonality}
                  className="text-slate-600"
                />
              </label>

              <Input
                label={<LabelWithInfo label="Factor de stock de seguridad" info={PLANNING_HELP.safetyStockFactor} />}
                type="number"
                step="0.1"
                min={0}
                value={safetyStockFactor}
                onChange={(event) => setSafetyStockFactor(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Escenarios de lead time" info={PLANNING_HELP.leadTimeOptions} />}
                value={leadTimeOptions}
                onChange={(event) => setLeadTimeOptions(event.target.value)}
                hint="Separados por comas"
                className="md:col-span-2"
              />
            </div>
            <div className="flex flex-col gap-2">
              <button
                type="submit"
                className="inline-flex items-center justify-center rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-100 disabled:opacity-60"
                disabled={isScenarioLoading}
              >
                {isScenarioLoading ? 'Simulando…' : 'Simular escenarios'}
              </button>
              {scenarioError && <p className="text-xs text-red-500">{scenarioError}</p>}
            </div>
          </form>
        </Card>
        {scenarioVariant && (
          <Card
            title={
              <span className="flex items-center gap-2">
                Simulación de escenarios
                <InfoTooltip content={PLANNING_HELP.scenarioChart} size="sm" />
              </span>
            }
            subtitle={`Evaluación de riesgo para distintos plazos de ${scenarioVariant.productName}`}
          >
            <div className="grid gap-6 lg:grid-cols-2">
              <div className="flex h-72 flex-col gap-2">
                <div className="flex items-center justify-between text-sm font-medium text-slate-700">
                  <LabelWithInfo label="Riesgo vs. cantidad" info={PLANNING_HELP.scenarioChart} />
                </div>
                <div className="flex-1">
                  <ResponsiveContainer width="100%" height="100%">
                    <LineChart data={scenarioChartData}>
                      <XAxis dataKey="leadTime" stroke="#94a3b8" fontSize={12} />
                      <YAxis yAxisId="left" stroke="#94a3b8" fontSize={12} />
                      <YAxis
                        yAxisId="right"
                        orientation="right"
                        stroke="#94a3b8"
                        tickFormatter={(value) => `${value}%`}
                        fontSize={12}
                      />
                      <Tooltip
                        formatter={(value: number, name) =>
                          name === 'recommended'
                            ? [`${formatDecimal(value as number)}`, 'Cantidad recomendada']
                            : [`${formatDecimal(value as number, 1)} %`, name === 'risk' ? 'Riesgo' : 'Riesgo residual']
                        }
                      />
                      <Legend />
                      <Line
                        yAxisId="left"
                        type="monotone"
                        dataKey="recommended"
                        name="Cantidad recomendada"
                        stroke="#0f78ff"
                        strokeWidth={2}
                        dot
                      />
                      <Line
                        yAxisId="right"
                        type="monotone"
                        dataKey="risk"
                        name="Riesgo"
                        stroke="#f97316"
                        strokeWidth={2}
                        strokeDasharray="4 4"
                        dot
                      />
                      <Line
                        yAxisId="right"
                        type="monotone"
                        dataKey="residualRisk"
                        name="Riesgo residual"
                        stroke="#16a34a"
                        strokeWidth={2}
                        strokeDasharray="2 6"
                        dot
                      />
                    </LineChart>
                  </ResponsiveContainer>
                </div>
              </div>

              <div className="overflow-hidden rounded-xl border border-slate-200 bg-white">
                <table className="min-w-full divide-y divide-slate-200 text-sm">
                  <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                    <tr>
                      <th className="px-3 py-2 text-left">
                        <TableHeaderLabel label="Lead time" info={PLANNING_HELP.scenarioLeadTime} />
                      </th>
                      <th className="px-3 py-2 text-right">
                        <TableHeaderLabel label="Forecast" info={PLANNING_HELP.scenarioForecast} />
                      </th>
                      <th className="px-3 py-2 text-right">
                        <TableHeaderLabel label="Stock seg." info={PLANNING_HELP.scenarioSafetyStock} />
                      </th>
                      <th className="px-3 py-2 text-right">
                        <TableHeaderLabel label="Reorden" info={PLANNING_HELP.scenarioReorder} />
                      </th>
                      <th className="px-3 py-2 text-right">
                        <TableHeaderLabel label="Riesgo" info={PLANNING_HELP.scenarioRisk} />
                      </th>
                      <th className="px-3 py-2 text-right">
                        <TableHeaderLabel label="Riesgo residual" info={PLANNING_HELP.scenarioResidualRisk} />
                      </th>
                      <th className="px-3 py-2 text-right">
                        <TableHeaderLabel label="Recomendado" info={PLANNING_HELP.scenarioRecommended} />
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {scenarioVariant.scenarios.map((item) => (
                      <tr key={item.leadTimeDays} className="text-slate-700">
                        <td className="px-3 py-2 text-left">{item.leadTimeDays} días</td>
                        <td className="px-3 py-2 text-right">{formatDecimal(item.forecastedDemand)}</td>
                        <td className="px-3 py-2 text-right">{formatDecimal(item.safetyStock)}</td>
                        <td className="px-3 py-2 text-right">{formatDecimal(item.reorderPoint)}</td>
                        <td className="px-3 py-2 text-right">{formatDecimal(item.stockoutRisk * 100, 1)} %</td>
                        <td className="px-3 py-2 text-right">{formatDecimal(item.residualRisk * 100, 1)} %</td>
                        <td className="px-3 py-2 text-right text-primary-600">
                          {formatDecimal(item.recommendedOrderQuantity)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </Card>
        )}
      </section>

      <section className="flex flex-col gap-4">
        <Card
          title={
            <span className="flex items-center gap-2">
              Recomendaciones de política
              <InfoTooltip content={PLANNING_HELP.optimizationSetup} size="sm" />
            </span>
          }
          subtitle="Optimiza políticas min/max y EOQ para las variantes seleccionadas."
        >
          <form
            className="flex flex-col gap-4"
            onSubmit={(event) => {
              event.preventDefault();
              void handleFetchRecommendations();
            }}
          >
            <Select
              label={<LabelWithInfo label="Variantes a optimizar" info={PLANNING_HELP.planVariants} />}
              multiple
              value={selectedVariantIds}
              onChange={handlePlanVariantsChange}
              size={planVariantsSize}
              hint="Se comparte con el plan sugerido"
            >
              {variantOptions.map((variant) => (
                <option key={variant.id} value={variant.id}>
                  {variant.label}
                </option>
              ))}
            </Select>
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              <Input
                label={<LabelWithInfo label="Meses a proyectar" info={PLANNING_HELP.periods} />}
                type="number"
                min={1}
                max={24}
                value={periods}
                onChange={(event) => setPeriods(event.target.value)}
                required
              />

              <Input
                label={<LabelWithInfo label="Alpha (0-1)" info={PLANNING_HELP.alpha} />}
                type="number"
                step="0.05"
                min={0}
                max={1}
                value={alpha}
                onChange={(event) => setAlpha(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Beta (0-1)" info={PLANNING_HELP.beta} />}
                type="number"
                step="0.05"
                min={0}
                max={1}
                value={beta}
                onChange={(event) => setBeta(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Ciclos estacionales" info={PLANNING_HELP.seasonLength} />}
                type="number"
                min={1}
                max={24}
                value={seasonLength}
                onChange={(event) => setSeasonLength(event.target.value)}
                hint="Opcional"
              />

              <label
                htmlFor="includeSeasonalityOptimization"
                className="mt-1 flex cursor-pointer items-center gap-2 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600 shadow-sm md:col-span-2 xl:col-span-3"
              >
                <input
                  id="includeSeasonalityOptimization"
                  type="checkbox"
                  checked={includeSeasonality}
                  onChange={(event) => setIncludeSeasonality(event.target.checked)}
                  className="h-4 w-4 rounded border-slate-300 text-primary-600 focus:ring-primary-500"
                />
                <LabelWithInfo
                  label="Aplicar factores estacionales"
                  info={PLANNING_HELP.includeSeasonality}
                  className="text-slate-600"
                />
              </label>

              <Input
                label={<LabelWithInfo label="Lead time manual (días)" info={PLANNING_HELP.leadTimeDays} />}
                type="number"
                min={1}
                value={leadTimeDays}
                onChange={(event) => setLeadTimeDays(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Revisión (días)" info={PLANNING_HELP.reviewPeriodDays} />}
                type="number"
                min={1}
                value={reviewPeriodDays}
                onChange={(event) => setReviewPeriodDays(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Nivel de servicio" info={PLANNING_HELP.serviceLevel} />}
                type="number"
                step="0.05"
                min={0}
                max={1}
                value={serviceLevel}
                onChange={(event) => setServiceLevel(event.target.value)}
                hint="Opcional"
              />
            </div>
            <div className="flex flex-col gap-2">
              <button
                type="submit"
                className="inline-flex items-center justify-center rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-2 text-sm font-medium text-emerald-700 transition hover:bg-emerald-100 disabled:opacity-60"
                disabled={isOptimizationLoading}
              >
                {isOptimizationLoading ? 'Optimizando…' : 'Recomendaciones de política'}
              </button>
              {optimizationError && <p className="text-xs text-red-500">{optimizationError}</p>}
            </div>
          </form>
        </Card>
        {recommendations && recommendations.policies.length > 0 && (
          <Card
            title={
              <span className="flex items-center gap-2">
                Recomendaciones de política
                <InfoTooltip content={PLANNING_HELP.recommendationsCard} size="sm" />
              </span>
            }
            subtitle="Optimización de parámetros min/max, EOQ y niveles de servicio"
          >
            <div className="grid gap-6 lg:grid-cols-2">
              <div className="space-y-4">
                <Select
                  label={<LabelWithInfo label="Variante analizada" info={PLANNING_HELP.policyVariant} />}
                  value={selectedPolicy ? String(selectedPolicy.variantId) : ''}
                  onChange={(event) => {
                    const value = Number(event.target.value);
                    setSelectedPolicyVariantId(Number.isFinite(value) ? value : null);
                  }}
                >
                  {recommendations.policies.map((policy) => (
                    <option key={policy.variantId} value={policy.variantId}>
                      {policy.productName} — {policy.variantSku}
                    </option>
                  ))}
                </Select>
                <div className="overflow-hidden rounded-xl border border-slate-200 bg-white">
                  <table className="min-w-full divide-y divide-slate-200 text-sm">
                    <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                      <tr>
                        <th className="px-3 py-2 text-left">
                          <TableHeaderLabel label="Variante" info={PLANNING_HELP.policyVariant} />
                        </th>
                        <th className="px-3 py-2 text-right">
                          <TableHeaderLabel label="Min" info={PLANNING_HELP.policyMin} />
                        </th>
                        <th className="px-3 py-2 text-right">
                          <TableHeaderLabel label="Reorden" info={PLANNING_HELP.policyReorder} />
                        </th>
                        <th className="px-3 py-2 text-right">
                          <TableHeaderLabel label="Máx" info={PLANNING_HELP.policyMax} />
                        </th>
                        <th className="px-3 py-2 text-right">
                          <TableHeaderLabel label="EOQ" info={PLANNING_HELP.policyEoq} />
                        </th>
                        <th className="px-3 py-2 text-right">
                          <TableHeaderLabel label="Fill rate" info={PLANNING_HELP.policyFillRate} />
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {recommendations.policies.map((policy) => (
                        <tr
                          key={policy.variantId}
                          className={`cursor-pointer text-slate-700 transition hover:bg-primary-50 ${
                            selectedPolicy?.variantId === policy.variantId ? 'bg-primary-50' : ''
                          }`}
                          onClick={() => setSelectedPolicyVariantId(policy.variantId)}
                        >
                          <td className="px-3 py-2 text-sm font-medium text-slate-900">
                            <div className="flex flex-col">
                              <span>{policy.productName}</span>
                              <span className="text-xs text-slate-500">SKU: {policy.variantSku}</span>
                            </div>
                          </td>
                          <td className="px-3 py-2 text-right">{formatDecimal(policy.minStockLevel)}</td>
                          <td className="px-3 py-2 text-right">{formatDecimal(policy.reorderPoint)}</td>
                          <td className="px-3 py-2 text-right">{formatDecimal(policy.maxStockLevel)}</td>
                          <td className="px-3 py-2 text-right">{formatDecimal(policy.economicOrderQuantity)}</td>
                          <td className="px-3 py-2 text-right text-emerald-600">
                            {formatDecimal(policy.kpis.fillRate * 100, 1)} %
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>

              <div className="space-y-4">
                {selectedPolicy ? (
                  <>
                    <div className="rounded-xl border border-slate-200 bg-white p-4 text-sm text-slate-700">
                      <p className="flex items-center gap-2 text-base font-semibold text-slate-900">
                        Indicadores clave
                        <InfoTooltip content={PLANNING_HELP.policyIndicators} size="sm" />
                      </p>
                      <dl className="mt-2 grid grid-cols-2 gap-x-4 gap-y-2">
                        <div>
                          <dt className="text-xs uppercase text-slate-500">Fill rate esperado</dt>
                          <dd className="text-sm font-medium text-emerald-600">
                            {formatDecimal(selectedPolicy.kpis.fillRate * 100, 1)} %
                          </dd>
                        </div>
                        <div>
                          <dt className="text-xs uppercase text-slate-500">Costo total</dt>
                          <dd className="text-sm font-medium text-slate-900">
                            {formatDecimal(selectedPolicy.kpis.totalCost)} {selectedPolicy.currency}
                          </dd>
                        </div>
                        <div>
                          <dt className="text-xs uppercase text-slate-500">Costo de mantenimiento</dt>
                          <dd>{formatDecimal(selectedPolicy.kpis.holdingCost)} {selectedPolicy.currency}</dd>
                        </div>
                        <div>
                          <dt className="text-xs uppercase text-slate-500">Costo de pedido</dt>
                          <dd>{formatDecimal(selectedPolicy.kpis.orderingCost)} {selectedPolicy.currency}</dd>
                        </div>
                        <div>
                          <dt className="text-xs uppercase text-slate-500">Prob. de quiebre</dt>
                          <dd>{formatDecimal(selectedPolicy.kpis.stockoutRisk * 100, 2)} %</dd>
                        </div>
                        <div>
                          <dt className="text-xs uppercase text-slate-500">Inventario medio</dt>
                          <dd>{formatDecimal(selectedPolicy.kpis.averageInventory)}</dd>
                        </div>
                      </dl>
                      <div className="mt-4 rounded-lg bg-slate-50 p-3 text-xs text-slate-500">
                        <p>
                          Simulación Monte Carlo ({selectedPolicy.monteCarlo.iterations} iteraciones): fill rate medio de{' '}
                          {formatDecimal(selectedPolicy.monteCarlo.averageFillRate * 100, 1)} % y costo esperado de{' '}
                          {formatDecimal(selectedPolicy.monteCarlo.averageTotalCost)} {selectedPolicy.currency}.
                        </p>
                      </div>
                    </div>
                    <div className="h-72">
                      <ResponsiveContainer width="100%" height="100%">
                        <BarChart data={policyChartData}>
                          <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                          <XAxis dataKey="name" stroke="#94a3b8" fontSize={12} />
                          <YAxis stroke="#94a3b8" fontSize={12} />
                          <Tooltip formatter={(value: number) => formatDecimal(value as number)} />
                          <Legend />
                          <Bar dataKey="value" name="Unidades" fill="#0f78ff" radius={[6, 6, 0, 0]} />
                        </BarChart>
                      </ResponsiveContainer>
                    </div>
                  </>
                ) : (
                  <p className="text-sm text-slate-500">Selecciona una variante para visualizar el detalle optimizado.</p>
                )}
              </div>
            </div>
          </Card>
        )}
      </section>

      <section className="flex flex-col gap-4">
        <Card
          title={
            <span className="flex items-center gap-2">
              Comparativa what-if
              <InfoTooltip content={PLANNING_HELP.comparisonSetup} size="sm" />
            </span>
          }
          subtitle="Contrasta escenarios alternativos con la línea base."
        >
          <form
            className="flex flex-col gap-4"
            onSubmit={(event) => {
              event.preventDefault();
              void handleCompareOptimizationScenarios();
            }}
          >
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              <Select
                label={<LabelWithInfo label="Variante base" info={PLANNING_HELP.variantPrincipal} />}
                value={selectedVariantId}
                onChange={(event) => setSelectedVariantId(event.target.value)}
                required
              >
                <option value="">Selecciona una variante</option>
                {variantOptions.map((variant) => (
                  <option key={variant.id} value={variant.id}>
                    {variant.label}
                  </option>
                ))}
              </Select>

              <Input
                label={<LabelWithInfo label="Meses a proyectar" info={PLANNING_HELP.periods} />}
                type="number"
                min={1}
                max={24}
                value={periods}
                onChange={(event) => setPeriods(event.target.value)}
                required
              />

              <Input
                label={<LabelWithInfo label="Alpha (0-1)" info={PLANNING_HELP.alpha} />}
                type="number"
                step="0.05"
                min={0}
                max={1}
                value={alpha}
                onChange={(event) => setAlpha(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Beta (0-1)" info={PLANNING_HELP.beta} />}
                type="number"
                step="0.05"
                min={0}
                max={1}
                value={beta}
                onChange={(event) => setBeta(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Ciclos estacionales" info={PLANNING_HELP.seasonLength} />}
                type="number"
                min={1}
                max={24}
                value={seasonLength}
                onChange={(event) => setSeasonLength(event.target.value)}
                hint="Opcional"
              />

              <label
                htmlFor="includeSeasonalityComparison"
                className="mt-1 flex cursor-pointer items-center gap-2 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600 shadow-sm md:col-span-2 xl:col-span-3"
              >
                <input
                  id="includeSeasonalityComparison"
                  type="checkbox"
                  checked={includeSeasonality}
                  onChange={(event) => setIncludeSeasonality(event.target.checked)}
                  className="h-4 w-4 rounded border-slate-300 text-primary-600 focus:ring-primary-500"
                />
                <LabelWithInfo
                  label="Aplicar factores estacionales"
                  info={PLANNING_HELP.includeSeasonality}
                  className="text-slate-600"
                />
              </label>

              <Input
                label={<LabelWithInfo label="Lead time manual (días)" info={PLANNING_HELP.leadTimeDays} />}
                type="number"
                min={1}
                value={leadTimeDays}
                onChange={(event) => setLeadTimeDays(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Revisión (días)" info={PLANNING_HELP.reviewPeriodDays} />}
                type="number"
                min={1}
                value={reviewPeriodDays}
                onChange={(event) => setReviewPeriodDays(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Nivel de servicio" info={PLANNING_HELP.serviceLevel} />}
                type="number"
                step="0.05"
                min={0}
                max={1}
                value={serviceLevel}
                onChange={(event) => setServiceLevel(event.target.value)}
                hint="Opcional"
              />
            </div>
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              <Input
                label={<LabelWithInfo label="Nivel de servicio what-if" info={PLANNING_HELP.whatIfService} />}
                type="number"
                step="0.05"
                min={0}
                max={1}
                value={whatIfServiceLevel}
                onChange={(event) => setWhatIfServiceLevel(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Lead time what-if" info={PLANNING_HELP.whatIfLeadTime} />}
                type="number"
                min={0}
                value={whatIfLeadTime}
                onChange={(event) => setWhatIfLeadTime(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Costo de mantenimiento" info={PLANNING_HELP.whatIfHoldingCost} />}
                type="number"
                min={0}
                value={whatIfHoldingCost}
                onChange={(event) => setWhatIfHoldingCost(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Costo por pedido" info={PLANNING_HELP.whatIfOrderingCost} />}
                type="number"
                min={0}
                value={whatIfOrderingCost}
                onChange={(event) => setWhatIfOrderingCost(event.target.value)}
                hint="Opcional"
              />

              <Input
                label={<LabelWithInfo label="Costo por quiebre" info={PLANNING_HELP.whatIfStockoutCost} />}
                type="number"
                min={0}
                value={whatIfStockoutCost}
                onChange={(event) => setWhatIfStockoutCost(event.target.value)}
                hint="Opcional"
              />
            </div>
            <div className="flex flex-col gap-2">
              <button
                type="submit"
                className="inline-flex items-center justify-center rounded-lg border border-indigo-200 bg-indigo-50 px-4 py-2 text-sm font-medium text-indigo-700 transition hover:bg-indigo-100 disabled:opacity-60"
                disabled={isScenarioComparisonLoading}
              >
                {isScenarioComparisonLoading ? 'Comparando…' : 'Comparar what-if'}
              </button>
              {scenarioComparisonError && <p className="text-xs text-red-500">{scenarioComparisonError}</p>}
            </div>
          </form>
        </Card>
        {scenarioComparison && (
          <Card
            title={
              <span className="flex items-center gap-2">
                Comparativa what-if
                <InfoTooltip content={PLANNING_HELP.scenarioComparisonCard} size="sm" />
              </span>
            }
            subtitle="Evalúa el impacto de ajustes en servicio, lead time y costos"
          >
            <div className="grid gap-6 lg:grid-cols-2">
              <div className="space-y-4">
                {scenarioOptions.length > 1 && (
                  <Select
                    label={<LabelWithInfo label="Escenario" info={PLANNING_HELP.scenarioComparisonCard} />}
                    value={selectedScenarioName}
                    onChange={(event) => setSelectedScenarioName(event.target.value)}
                  >
                    {scenarioOptions.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </Select>
                )}

                {selectedScenarioOutcome ? (
                  <div className="rounded-xl border border-slate-200 bg-white p-4 text-sm text-slate-700">
                    <p className="flex items-center gap-2 text-base font-semibold text-slate-900">
                      Indicadores del escenario
                      <InfoTooltip content={PLANNING_HELP.scenarioIndicators} size="sm" />
                    </p>
                    <dl className="mt-2 grid grid-cols-2 gap-x-4 gap-y-2">
                      <div>
                        <dt className="text-xs uppercase text-slate-500">Fill rate proyectado</dt>
                        <dd className="text-sm font-medium text-emerald-600">
                          {formatDecimal(selectedScenarioOutcome.kpis.fillRate * 100, 1)} %
                        </dd>
                      </div>
                      <div>
                        <dt className="text-xs uppercase text-slate-500">Costo total</dt>
                        <dd className="text-sm font-medium text-slate-900">
                          {formatDecimal(selectedScenarioOutcome.kpis.totalCost)} {selectedScenarioOutcome.policy.currency}
                        </dd>
                      </div>
                      <div>
                        <dt className="text-xs uppercase text-slate-500">Costo de mantenimiento</dt>
                        <dd>{formatDecimal(selectedScenarioOutcome.kpis.holdingCost)}</dd>
                      </div>
                      <div>
                        <dt className="text-xs uppercase text-slate-500">Costo de pedido</dt>
                        <dd>{formatDecimal(selectedScenarioOutcome.kpis.orderingCost)}</dd>
                      </div>
                      <div>
                        <dt className="text-xs uppercase text-slate-500">Prob. de quiebre</dt>
                        <dd>{formatDecimal(selectedScenarioOutcome.kpis.stockoutRisk * 100, 2)} %</dd>
                      </div>
                      <div>
                        <dt className="text-xs uppercase text-slate-500">Inventario medio</dt>
                        <dd>{formatDecimal(selectedScenarioOutcome.kpis.averageInventory)}</dd>
                      </div>
                    </dl>
                    <div className="mt-4 rounded-lg bg-slate-50 p-3 text-xs text-slate-500">
                        <p>
                          Simulación Monte Carlo ({selectedScenarioOutcome.monteCarlo.iterations} iteraciones): fill rate medio de{' '}
                          {formatDecimal(selectedScenarioOutcome.monteCarlo.averageFillRate * 100, 1)} % y costo esperado de{' '}
                          {formatDecimal(selectedScenarioOutcome.monteCarlo.averageTotalCost)} {selectedScenarioOutcome.policy.currency}.
                          Probabilidad de quiebre: {formatDecimal(selectedScenarioOutcome.monteCarlo.stockoutProbability * 100, 2)} %.
                        </p>
                    </div>
                  </div>
                ) : (
                  <p className="text-sm text-slate-500">Ejecuta una comparación para visualizar los KPIs.</p>
                )}

                <div className="grid gap-4 lg:grid-cols-2">
                  <Input
                    label={<LabelWithInfo label="What-if: nivel de servicio" info={PLANNING_HELP.whatIfService} />}
                    type="number"
                    step="0.01"
                    min={0.5}
                    max={0.999}
                    value={whatIfServiceLevel}
                    onChange={(event) => setWhatIfServiceLevel(event.target.value)}
                    hint={formatUsageHint(['comparison'])}
                  />
                  <Input
                    label={<LabelWithInfo label="What-if: lead time (días)" info={PLANNING_HELP.whatIfLeadTime} />}
                    type="number"
                    min={1}
                    value={whatIfLeadTime}
                    onChange={(event) => setWhatIfLeadTime(event.target.value)}
                    hint={formatUsageHint(['comparison'])}
                  />
                  <Input
                    label={<LabelWithInfo label="What-if: coste mantenimiento" info={PLANNING_HELP.whatIfHoldingCost} />}
                    type="number"
                    step="0.01"
                    min={0}
                    value={whatIfHoldingCost}
                    onChange={(event) => setWhatIfHoldingCost(event.target.value)}
                    hint={formatUsageHint(['comparison'])}
                  />
                  <Input
                    label={<LabelWithInfo label="What-if: coste pedido" info={PLANNING_HELP.whatIfOrderingCost} />}
                    type="number"
                    step="0.01"
                    min={0}
                    value={whatIfOrderingCost}
                    onChange={(event) => setWhatIfOrderingCost(event.target.value)}
                    hint={formatUsageHint(['comparison'])}
                  />
                  <Input
                    label={<LabelWithInfo label="What-if: coste quiebre" info={PLANNING_HELP.whatIfStockoutCost} />}
                    type="number"
                    step="0.01"
                    min={0}
                    value={whatIfStockoutCost}
                    onChange={(event) => setWhatIfStockoutCost(event.target.value)}
                    hint={formatUsageHint(['comparison'])}
                  />
                </div>
              </div>

              <div className="flex h-72 flex-col gap-2">
                <div className="flex items-center justify-between text-sm font-medium text-slate-700">
                  <LabelWithInfo label="Comparativa de KPIs" info={PLANNING_HELP.compareChart} />
                </div>
                <div className="flex-1">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={scenarioKpiData}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                      <XAxis dataKey="name" stroke="#94a3b8" fontSize={12} />
                      <YAxis yAxisId="left" stroke="#94a3b8" fontSize={12} tickFormatter={(value) => `${value}%`} />
                      <YAxis yAxisId="right" orientation="right" stroke="#94a3b8" fontSize={12} />
                      <Tooltip
                        formatter={(value: number, name) =>
                          name === 'fillRate'
                            ? [`${formatDecimal(value as number, 1)} %`, 'Fill rate']
                            : [`${formatDecimal(value as number)}`, 'Costo total']
                        }
                      />
                      <Legend formatter={(value) => (value === 'fillRate' ? 'Fill rate (%)' : 'Costo total')} />
                      <Bar yAxisId="left" dataKey="fillRate" name="fillRate" fill="#0f78ff" radius={[6, 6, 0, 0]} />
                      <Bar yAxisId="right" dataKey="totalCost" name="totalCost" fill="#f97316" radius={[6, 6, 0, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              </div>
            </div>
          </Card>
        )}
      </section>
    </div>
  );
}
