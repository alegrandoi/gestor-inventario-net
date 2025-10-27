import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ProductAttributesPage from '../../app/(dashboard)/dashboard/products/attributes/page';
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

describe('ProductAttributesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('allows creating a group and adding a value', async () => {
    mockedApi.get
      .mockResolvedValueOnce({ data: [] })
      .mockResolvedValueOnce({
        data: [
          {
            id: 1,
            name: 'Talla',
            slug: 'talla',
            description: 'Guías de tallas',
            allowCustomValues: false,
            values: []
          }
        ]
      })
      .mockResolvedValueOnce({
        data: [
          {
            id: 1,
            name: 'Talla',
            slug: 'talla',
            description: 'Guías de tallas',
            allowCustomValues: false,
            values: [
              {
                id: 10,
                name: 'M',
                description: 'Talla mediana',
                hexColor: null,
                displayOrder: 0,
                isActive: true
              }
            ]
          }
        ]
      });

    mockedApi.post
      .mockResolvedValueOnce({ data: { id: 1 } })
      .mockResolvedValueOnce({ data: { id: 10 } });

    const user = userEvent.setup();
    render(<ProductAttributesPage />);

    await screen.findByText('No has creado atributos todavía. Empieza añadiendo un grupo.');

    await user.click(screen.getByRole('button', { name: 'Nuevo grupo' }));

    await user.type(screen.getByLabelText('Nombre'), 'Talla');
    await user.type(screen.getByLabelText('Descripción'), 'Guías de tallas');

    await user.click(screen.getByRole('button', { name: 'Guardar grupo' }));

    await waitFor(() => {
      expect(mockedApi.post).toHaveBeenNthCalledWith(1, '/product-attributes', {
        name: 'Talla',
        description: 'Guías de tallas',
        allowCustomValues: false
      });
    });

    await waitFor(() => {
      expect(mockedApi.get).toHaveBeenCalledTimes(2);
    });

    const groupButton = screen.getByText('Talla').closest('button');
    expect(groupButton).not.toBeNull();
    await user.click(groupButton!);

    await waitFor(() => {
      expect(screen.getAllByLabelText('Nombre')).toHaveLength(2);
    });

    const nameInputs = screen.getAllByLabelText('Nombre');
    await user.type(nameInputs[1], 'M');

    const descriptionFields = screen.getAllByLabelText('Descripción');
    await user.type(descriptionFields[1], 'Talla mediana');

    await user.click(screen.getByRole('button', { name: 'Agregar valor' }));

    await waitFor(() => {
      expect(mockedApi.post).toHaveBeenNthCalledWith(2, '/product-attributes/1/values', {
        name: 'M',
        description: 'Talla mediana',
        hexColor: undefined,
        displayOrder: undefined,
        isActive: true
      });
    });

    await waitFor(() => {
      expect(mockedApi.get).toHaveBeenCalledTimes(3);
    });

    await screen.findByText('Talla mediana');
  });
});
