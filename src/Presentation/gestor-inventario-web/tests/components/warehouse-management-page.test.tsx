import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import WarehousesPage from '../../app/(dashboard)/dashboard/warehouses/page';
import { apiClient } from '../../src/lib/api-client';

vi.mock('next/navigation', () => {
  return {
    useSearchParams: () => new URLSearchParams()
  };
});

vi.mock('../../components/warehouses/warehouse-assignments-manager', () => {
  return {
    WarehouseAssignmentsManager: () => <div data-testid="assignments-manager" />
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

describe('WarehousesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('allows creating, updating and deleting warehouses', async () => {
    const initialWarehouses = [
      {
        id: 1,
        name: 'Central',
        address: 'Calle 123',
        description: 'Principal',
        productVariants: []
      }
    ];

    const createdWarehouse = {
      id: 2,
      name: 'Almacén Norte',
      address: 'Avenida Norte 10',
      description: 'Secundario',
      productVariants: []
    };

    const updatedWarehouse = {
      ...createdWarehouse,
      name: 'Almacén Norte Expandido',
      address: 'Avenida Norte 12'
    };

    mockedApi.get
      .mockResolvedValueOnce({ data: initialWarehouses })
      .mockResolvedValueOnce({ data: [...initialWarehouses, createdWarehouse] })
      .mockResolvedValueOnce({ data: [initialWarehouses[0], updatedWarehouse] })
      .mockResolvedValueOnce({ data: initialWarehouses });

    mockedApi.post.mockResolvedValueOnce({ data: createdWarehouse });
    mockedApi.put.mockResolvedValueOnce({ data: updatedWarehouse });
    mockedApi.delete.mockResolvedValueOnce({});

    const user = userEvent.setup();
    render(<WarehousesPage />);

    await waitFor(() => {
      expect(mockedApi.get).toHaveBeenCalledTimes(1);
    });

    await user.type(screen.getByLabelText('Nombre'), 'Almacén Norte');
    await user.type(screen.getByLabelText('Dirección'), 'Avenida Norte 10');
    await user.type(screen.getByLabelText('Descripción'), 'Secundario');

    await user.click(screen.getByRole('button', { name: 'Guardar' }));

    await waitFor(() => {
      expect(mockedApi.post).toHaveBeenCalledWith('/warehouses', {
        name: 'Almacén Norte',
        address: 'Avenida Norte 10',
        description: 'Secundario'
      });
    });

    await screen.findByText('Almacén Norte');

    const newWarehouseRow = screen.getByText('Almacén Norte').closest('tr');
    if (!newWarehouseRow) {
      throw new Error('No se encontró la fila del nuevo almacén.');
    }

    await user.click(within(newWarehouseRow).getByRole('button', { name: 'Editar' }));

    const nameInput = screen.getByLabelText('Nombre');
    await user.clear(nameInput);
    await user.type(nameInput, 'Almacén Norte Expandido');
    const addressInput = screen.getByLabelText('Dirección');
    await user.clear(addressInput);
    await user.type(addressInput, 'Avenida Norte 12');

    await user.click(screen.getByRole('button', { name: 'Actualizar' }));

    await waitFor(() => {
      expect(mockedApi.put).toHaveBeenCalledWith('/warehouses/2', {
        name: 'Almacén Norte Expandido',
        address: 'Avenida Norte 12',
        description: 'Secundario'
      });
    });

    await screen.findByText('Almacén Norte Expandido');

    const updatedRow = screen.getByText('Almacén Norte Expandido').closest('tr');
    if (!updatedRow) {
      throw new Error('No se encontró la fila actualizada del almacén.');
    }

    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);
    await user.click(within(updatedRow).getByRole('button', { name: 'Eliminar' }));

    await waitFor(() => {
      expect(mockedApi.delete).toHaveBeenCalledWith('/warehouses/2');
    });

    confirmSpy.mockRestore();
  });
});
