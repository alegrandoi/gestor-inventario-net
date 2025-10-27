import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import TaxRatesPage from '../../app/(dashboard)/dashboard/products/tax-rates/page';
import { apiClient } from '../../src/lib/api-client';

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

describe('TaxRatesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('creates a new tax rate and refreshes the list', async () => {
    mockedApi.get
      .mockResolvedValueOnce({ data: [] })
      .mockResolvedValueOnce({
        data: [
          {
            id: 1,
            name: 'IVA general',
            rate: 21,
            region: 'España',
            description: 'Tarifa estándar'
          }
        ]
      });

    mockedApi.post.mockResolvedValueOnce({
      data: {
        id: 1,
        name: 'IVA general',
        rate: 21,
        region: 'España',
        description: 'Tarifa estándar'
      }
    });

    const user = userEvent.setup();
    render(<TaxRatesPage />);

    await screen.findByText('No se encontraron tarifas.');

    await user.click(screen.getByRole('button', { name: 'Nueva tarifa' }));

    await user.type(screen.getByLabelText('Nombre'), 'IVA general');
    await user.type(screen.getByLabelText('Porcentaje'), '21');
    await user.type(screen.getByLabelText('Región'), 'España');
    await user.type(screen.getByLabelText('Descripción'), 'Tarifa estándar');

    await user.click(screen.getByRole('button', { name: 'Guardar cambios' }));

    await waitFor(() => {
      expect(mockedApi.post).toHaveBeenCalledWith('/taxrates', {
        name: 'IVA general',
        rate: 21,
        region: 'España',
        description: 'Tarifa estándar'
      });
    });

    await waitFor(() => {
      expect(mockedApi.get).toHaveBeenCalledTimes(2);
    });

    await screen.findByText('IVA general');
  });
});
