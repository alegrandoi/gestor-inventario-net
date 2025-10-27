'use client';

import { useSearchParams } from 'next/navigation';
import { WarehouseAssignmentsManager } from '../../../../../components/warehouses/warehouse-assignments-manager';

export default function WarehouseAssignmentsPage() {
  const searchParams = useSearchParams();
  const warehouseIdParam = searchParams?.get('warehouseId');
  const parsedWarehouseId = warehouseIdParam ? Number.parseInt(warehouseIdParam, 10) : Number.NaN;
  const initialWarehouseId = Number.isNaN(parsedWarehouseId) ? undefined : parsedWarehouseId;

  return <WarehouseAssignmentsManager initialWarehouseId={initialWarehouseId} />;
}
