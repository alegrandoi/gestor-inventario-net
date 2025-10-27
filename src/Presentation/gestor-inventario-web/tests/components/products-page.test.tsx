import { render, screen, within } from '@testing-library/react';
import ProductsPage from '../../app/(dashboard)/dashboard/products/page';
import { apiClient } from '../../src/lib/api-client';
import type { ProductDto } from '../../src/types/api';

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

const mockedApiClient = apiClient as unknown as {
  get: ReturnType<typeof vi.fn>;
  post: ReturnType<typeof vi.fn>;
  put: ReturnType<typeof vi.fn>;
  delete: ReturnType<typeof vi.fn>;
};

const baseProductPayload: ProductDto = {
  id: 1,
  code: 'SKU-001',
  name: 'Producto con IVA',
  description: 'Descripción',
  categoryId: null,
  defaultPrice: 100,
  currency: 'EUR',
  taxRateId: 1,
  appliedTaxRate: 21,
  finalPrice: 121,
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
  variants: [],
  images: []
};

const pagedProductsResponse = (items: typeof baseProductPayload[]) => ({
  data: {
    items,
    pageNumber: 1,
    pageSize: 200,
    totalCount: items.length,
    totalPages: 1
  }
});

describe('ProductsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the final price column with the precalculated value', async () => {
    mockedApiClient.get
      .mockResolvedValueOnce(pagedProductsResponse([baseProductPayload]))
      .mockResolvedValueOnce({ data: [] })
      .mockResolvedValueOnce({ data: [] })
      .mockResolvedValueOnce({ data: [] });

    render(<ProductsPage />);

    await screen.findByText('Producto con IVA');

    expect(screen.getByRole('columnheader', { name: 'Precio final' })).toBeInTheDocument();

    const row = screen.getByText('Producto con IVA').closest('tr');
    expect(row).not.toBeNull();

    const cells = within(row as HTMLTableRowElement).getAllByRole('cell');
    expect(cells[3]).toHaveTextContent(/121,00/);
  });

  it('computes the final price when it is missing from the payload', async () => {
    mockedApiClient.get
      .mockResolvedValueOnce(
        pagedProductsResponse([
          {
            ...baseProductPayload,
            finalPrice: null
          }
        ])
      )
      .mockResolvedValueOnce({ data: [] })
      .mockResolvedValueOnce({
        data: [
          {
            id: 1,
            name: 'IVA general',
            rate: 21,
            region: 'ES',
            description: null
          }
        ]
      })
      .mockResolvedValueOnce({ data: [] });

    render(<ProductsPage />);

    await screen.findByText('Producto con IVA');

    const row = screen.getByText('Producto con IVA').closest('tr');
    expect(row).not.toBeNull();

    const cells = within(row as HTMLTableRowElement).getAllByRole('cell');
    expect(cells[3]).toHaveTextContent(/121,00/);
  });
});
