import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import WarehouseAssignmentsPage from '../../app/(dashboard)/dashboard/logistics/warehouses/page';
import { apiClient } from '../../src/lib/api-client';

vi.mock('next/navigation', () => {
  return {
    useSearchParams: () => new URLSearchParams()
  };
});

vi.mock('../../src/lib/api-client', () => {
  return {
    apiClient: {
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(),
      delete: vi.fn()
    }
  };
});

const mockedApi = apiClient as unknown as {
  get: ReturnType<typeof vi.fn>;
  post: ReturnType<typeof vi.fn>;
  put: ReturnType<typeof vi.fn>;
  delete: ReturnType<typeof vi.fn>;
};

const pagedProductsResponse = (items: Array<Record<string, unknown>>) => ({
  data: {
    items,
    pageNumber: 1,
    pageSize: 200,
    totalCount: items.length,
    totalPages: 1
  }
});

describe('WarehouseAssignmentsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('allows creating, updating and removing assignments', async () => {
    mockedApi.get
      .mockResolvedValueOnce(
        pagedProductsResponse([
          {
            id: 10,
            code: 'PRD-10',
            name: 'Producto 10',
            description: 'Demo',
            categoryId: null,
            defaultPrice: 10,
            currency: 'EUR',
            taxRateId: null,
            isActive: true,
            requiresSerialTracking: false,
            weightKg: 1,
            heightCm: null,
            widthCm: null,
            lengthCm: null,
            leadTimeDays: null,
            safetyStock: null,
            reorderPoint: null,
            reorderQuantity: null,
            variants: [
              {
                id: 100,
                sku: 'SKU-100',
                attributes: 'color=rojo',
                price: 12,
                barcode: '1234567890'
              }
            ],
            images: []
          }
        ])
      )
      .mockResolvedValueOnce({
        data: [
          {
            id: 1,
            name: 'Central',
            address: 'Calle 123',
            description: 'Principal',
            productVariants: []
          }
        ]
      })
      .mockResolvedValueOnce({ data: [] })
      .mockResolvedValueOnce({
        data: [
          {
            id: 5,
            warehouseId: 1,
            variantId: 100,
            minimumQuantity: 50,
            targetQuantity: 60,
            variantSku: 'SKU-100',
            productName: 'Producto 10',
            warehouseName: 'Central'
          }
        ]
      })
      .mockResolvedValueOnce({
        data: [
          {
            id: 5,
            warehouseId: 1,
            variantId: 100,
            minimumQuantity: 50,
            targetQuantity: 75,
            variantSku: 'SKU-100',
            productName: 'Producto 10',
            warehouseName: 'Central'
          }
        ]
      })
      .mockResolvedValueOnce({ data: [] });

    mockedApi.post.mockResolvedValueOnce({
      data: {
        id: 5,
        warehouseId: 1,
        variantId: 100,
        minimumQuantity: 50,
        targetQuantity: 60,
        variantSku: 'SKU-100',
        productName: 'Producto 10',
        warehouseName: 'Central'
      }
    });

    mockedApi.put.mockResolvedValueOnce({
      data: {
        id: 5,
        warehouseId: 1,
        variantId: 100,
        minimumQuantity: 50,
        targetQuantity: 75,
        variantSku: 'SKU-100',
        productName: 'Producto 10',
        warehouseName: 'Central'
      }
    });

    mockedApi.delete.mockResolvedValueOnce({});

    const user = userEvent.setup();
    render(<WarehouseAssignmentsPage />);

    await waitFor(() => {
      expect(mockedApi.get).toHaveBeenCalledTimes(3);
    });

    await user.selectOptions(await screen.findByLabelText('Producto'), '100');
    await user.clear(screen.getByLabelText('Cantidad mínima'));
    await user.type(screen.getByLabelText('Cantidad mínima'), '50');
    await user.clear(screen.getByLabelText('Cantidad objetivo'));
    await user.type(screen.getByLabelText('Cantidad objetivo'), '60');

    await user.click(screen.getByRole('button', { name: 'Asignar' }));

    await waitFor(() => {
      expect(mockedApi.post).toHaveBeenCalledWith('/warehouses/1/product-variants', {
        variantId: 100,
        minimumQuantity: 50,
        targetQuantity: 60
      });
    });

    await screen.findByText('Producto 10');
    await user.click(screen.getByRole('button', { name: 'Editar' }));

    await user.clear(screen.getByLabelText('Cantidad objetivo'));
    await user.type(screen.getByLabelText('Cantidad objetivo'), '75');
    await user.click(screen.getByRole('button', { name: 'Actualizar' }));

    await waitFor(() => {
      expect(mockedApi.put).toHaveBeenCalledWith('/warehouses/1/product-variants/5', {
        minimumQuantity: 50,
        targetQuantity: 75
      });
    });

    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);
    await user.click(screen.getByRole('button', { name: 'Eliminar' }));

    await waitFor(() => {
      expect(mockedApi.delete).toHaveBeenCalledWith('/warehouses/1/product-variants/5');
    });

    confirmSpy.mockRestore();
  });
});
