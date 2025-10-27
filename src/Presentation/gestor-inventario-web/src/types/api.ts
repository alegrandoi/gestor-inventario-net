export interface UserSummaryDto {
  id: number;
  username: string;
  email: string;
  role: string;
  isActive: boolean;
}

export interface AuditLogDto {
  id: number;
  entityName: string;
  entityId?: number;
  action: string;
  changes?: string;
  userId?: number;
  username?: string;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AuthResponseDto {
  token?: string;
  expiresAt?: string;
  user?: UserSummaryDto;
  requiresTwoFactor: boolean;
  twoFactorSessionId?: string;
  sessionExpiresAt?: string;
}

export interface PasswordResetRequestDto {
  token: string;
  expiresAt: string;
  deliveryChannel: string;
}

export interface TotpSetupDto {
  secret: string;
  qrCodeUri: string;
}

export interface TotpActivationResultDto {
  recoveryCodes: string[];
}

export interface CategoryDto {
  id: number;
  name: string;
  description?: string | null;
  parentId?: number;
  children?: CategoryDto[];
}

export interface TaxRateDto {
  id: number;
  name: string;
  rate: number;
  region?: string;
  description?: string | null;
}

export interface ProductAttributeValueDto {
  id: number;
  name: string;
  description?: string;
  hexColor?: string;
  displayOrder: number;
  isActive: boolean;
}

export interface ProductAttributeGroupDto {
  id: number;
  name: string;
  slug: string;
  description?: string;
  allowCustomValues: boolean;
  values: ProductAttributeValueDto[];
}

export interface ProductVariantDto {
  id: number;
  sku: string;
  attributes: string;
  price?: number;
  barcode?: string;
}

export interface ProductImageDto {
  id: number;
  imageUrl: string;
  altText?: string;
}

export interface ProductDto {
  id: number;
  code: string;
  name: string;
  description?: string | null;
  categoryId?: number | null;
  defaultPrice: number;
  currency: string;
  taxRateId?: number | null;
  appliedTaxRate?: number | null;
  finalPrice?: number | null;
  isActive: boolean;
  requiresSerialTracking: boolean;
  weightKg: number;
  heightCm?: number | null;
  widthCm?: number | null;
  lengthCm?: number | null;
  leadTimeDays?: number | null;
  safetyStock?: number | null;
  reorderPoint?: number | null;
  reorderQuantity?: number | null;
  variants: ProductVariantDto[];
  images: ProductImageDto[];
}

export interface SupplierDto {
  id: number;
  name: string;
  contactName?: string;
  email?: string;
  phone?: string;
  address?: string;
  notes?: string;
}

export interface CustomerDto {
  id: number;
  name: string;
  email?: string;
  phone?: string;
  address?: string;
  notes?: string;
}

export interface CarrierDto {
  id: number;
  name: string;
  contactName?: string;
  email?: string;
  phone?: string;
  trackingUrl?: string;
  notes?: string;
}

export interface InventoryDashboardDto {
  totalProducts: number;
  activeProducts: number;
  totalInventoryValue: number;
  lowStockVariants: number;
  reorderAlerts: ReorderAlertDto[];
  topSellingProducts: TopSellingProductDto[];
  monthlySales: SalesTrendPointDto[];
}

export interface ReorderAlertDto {
  variantId: number;
  productName: string;
  variantSku: string;
  quantity: number;
  minStockLevel: number;
  warehouse: string;
}

export interface TopSellingProductDto {
  productId: number;
  productName: string;
  quantity: number;
  revenue: number;
}

export interface SalesTrendPointDto {
  year: number;
  month: number;
  totalAmount: number;
}

export interface DemandForecastDto {
  variantId: number;
  variantSku: string;
  productName: string;
  historical: DemandPointDto[];
  forecast: DemandPointDto[];
}

export interface DemandPointDto {
  period: string;
  quantity: number;
}

export interface PurchasePlanDto {
  generatedAt: string;
  items: PurchasePlanItemDto[];
}

export interface PurchasePlanItemDto {
  variantId: number;
  variantSku: string;
  productName: string;
  onHand: number;
  reserved: number;
  available: number;
  minStockLevel: number;
  forecastedDemand: number;
  safetyStock: number;
  reorderPoint: number;
  recommendedOrderQuantity: number;
  averageDailyDemand?: number;
  leadTimeDays: number;
  reviewPeriodDays: number;
  serviceLevel?: number;
  abcClass?: string;
  unitPrice: number;
  currency: string;
}

export interface ScenarioSimulationDto {
  generatedAt: string;
  variants: ScenarioSimulationVariantDto[];
}

export interface ScenarioSimulationVariantDto {
  variantId: number;
  variantSku: string;
  productName: string;
  onHand: number;
  reserved: number;
  forecastedDemand: number;
  serviceLevel?: number;
  abcClass?: string;
  scenarios: ScenarioSimulationResultDto[];
}

export interface ScenarioSimulationResultDto {
  leadTimeDays: number;
  forecastedDemand: number;
  safetyStock: number;
  reorderPoint: number;
  stockoutRisk: number;
  residualRisk: number;
  recommendedOrderQuantity: number;
}

export interface OptimizationRecommendationDto {
  generatedAt: string;
  policies: OptimizationPolicyDto[];
}

export interface OptimizationPolicyDto {
  variantId: number;
  variantSku: string;
  productName: string;
  onHand: number;
  reserved: number;
  available: number;
  minStockLevel: number;
  maxStockLevel: number;
  safetyStock: number;
  reorderPoint: number;
  economicOrderQuantity: number;
  targetServiceLevel: number;
  averageDailyDemand: number;
  reviewPeriodDays: number;
  holdingCostRate: number;
  orderingCost: number;
  unitPrice: number;
  currency: string;
  kpis: OptimizationKpiDto;
  monteCarlo: MonteCarloSummaryDto;
}

export interface OptimizationScenarioComparisonDto {
  generatedAt: string;
  variant: OptimizationScenarioVariantDto;
  baseline: OptimizationScenarioOutcomeDto;
  alternatives: OptimizationScenarioOutcomeDto[];
}

export interface OptimizationScenarioVariantDto {
  variantId: number;
  variantSku: string;
  productName: string;
}

export interface OptimizationScenarioOutcomeDto {
  scenarioName: string;
  policy: OptimizationPolicyDto;
  kpis: OptimizationKpiDto;
  monteCarlo: MonteCarloSummaryDto;
}

export interface OptimizationKpiDto {
  fillRate: number;
  totalCost: number;
  holdingCost: number;
  orderingCost: number;
  stockoutRisk: number;
  averageInventory: number;
}

export interface MonteCarloSummaryDto {
  iterations: number;
  averageFillRate: number;
  averageTotalCost: number;
  stockoutProbability: number;
}

export interface InventoryStockDto {
  id: number;
  variantId: number;
  warehouseId: number;
  variantSku: string;
  productName: string;
  warehouseName: string;
  quantity: number;
  reservedQuantity: number;
  minStockLevel: number;
}

export interface ReplenishmentPlanDto {
  generatedAt: string;
  suggestions: ReplenishmentSuggestionDto[];
}

export interface ReplenishmentSuggestionDto {
  variantId: number;
  variantSku: string;
  productName: string;
  warehouseId: number;
  warehouseName: string;
  onHand: number;
  reserved: number;
  recommendedQuantity: number;
  safetyStock?: number;
  reorderPoint?: number;
  leadTimeDemand?: number;
  averageDailyDemand: number;
}

export interface SalesOrderDto {
  id: number;
  customerId: number;
  orderDate: string;
  status: string | number;
  shippingAddress?: string;
  totalAmount: number;
  currency: string;
  notes?: string;
  carrierId?: number;
  carrierName?: string;
  estimatedDeliveryDate?: string;
  customerName: string;
  lines: SalesOrderLineDto[];
  fulfillmentRate: number;
  shipments: ShipmentSummaryDto[];
}

export interface SalesOrderLineDto {
  id: number;
  variantId: number;
  quantity: number;
  unitPrice: number;
  discount?: number;
  totalLine: number;
  variantSku: string;
  productName: string;
  allocations: SalesOrderAllocationDto[];
}

export interface SalesOrderAllocationDto {
  id: number;
  warehouseId: number;
  warehouseName: string;
  quantity: number;
  fulfilledQuantity: number;
  status: string | number;
  createdAt: string;
  shippedAt?: string;
  releasedAt?: string;
}

export interface ShipmentSummaryDto {
  id: number;
  warehouseId: number;
  warehouseName: string;
  status: string;
  createdAt: string;
  shippedAt?: string;
  deliveredAt?: string;
  carrierId?: number;
  carrierName?: string;
  trackingNumber?: string;
  totalWeight?: number;
  estimatedDeliveryDate?: string;
}

export interface ShipmentTrendPointDto {
  date: string;
  total: number;
  delivered: number;
  inTransit: number;
}

export interface WarehousePerformanceDto {
  warehouseId: number;
  warehouseName: string;
  totalShipments: number;
  onTimeShipments: number;
  delayedShipments: number;
  averageTransitDays: number;
}

export interface CarrierPerformanceDto {
  carrierId?: number;
  carrierName: string;
  totalShipments: number;
  inTransitShipments: number;
  deliveredShipments: number;
  onTimeRate: number;
  averageDelayDays: number;
}

export interface LogisticsDashboardDto {
  generatedAt: string;
  totalShipments: number;
  inTransitShipments: number;
  deliveredShipments: number;
  averageTransitDays: number;
  openSalesOrders: number;
  averageFulfillmentRate: number;
  topDelayedShipments: ShipmentSummaryDto[];
  totalReplenishmentRecommendation: number;
  onTimeDeliveryRate: number;
  shipmentVolumeTrend: ShipmentTrendPointDto[];
  warehousePerformance: WarehousePerformanceDto[];
  carrierPerformance: CarrierPerformanceDto[];
  upcomingShipments: ShipmentSummaryDto[];
}

export interface PurchaseOrderDto {
  id: number;
  supplierId: number;
  orderDate: string;
  status: string;
  totalAmount: number;
  currency: string;
  supplierName: string;
  notes?: string;
  lines: PurchaseOrderLineDto[];
}

export interface PurchaseOrderLineDto {
  id: number;
  variantId: number;
  quantity: number;
  unitPrice: number;
  discount?: number;
  totalLine: number;
  variantSku: string;
  productName: string;
}

export interface WarehouseProductVariantDto {
  id: number;
  warehouseId: number;
  variantId: number;
  minimumQuantity: number;
  targetQuantity: number;
  variantSku: string;
  productName: string;
  warehouseName: string;
}

export interface WarehouseDto {
  id: number;
  name: string;
  address?: string;
  description?: string;
  productVariants: WarehouseProductVariantDto[];
}

export interface RoleDto {
  id: number;
  name: string;
  description?: string;
}

export interface TenantBranchDto {
  id: number;
  name: string;
  code: string;
  locale?: string;
  timeZone?: string;
  currency?: string;
  isDefault: boolean;
  isActive: boolean;
}

export interface TenantDto {
  id: number;
  name: string;
  code: string;
  defaultCulture?: string;
  defaultCurrency?: string;
  isActive: boolean;
  branches: TenantBranchDto[];
}
