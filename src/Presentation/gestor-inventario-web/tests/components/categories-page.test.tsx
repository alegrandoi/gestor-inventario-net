import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import CategoriesPage from '../../app/(dashboard)/dashboard/products/categories/page';
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

const mockedApiClient = apiClient as unknown as {
  get: ReturnType<typeof vi.fn>;
  post: ReturnType<typeof vi.fn>;
  put: ReturnType<typeof vi.fn>;
  delete: ReturnType<typeof vi.fn>;
};

describe('CategoriesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('allows creating a new category and refreshes the tree', async () => {
    mockedApiClient.get
      .mockResolvedValueOnce({ data: [] })
      .mockResolvedValueOnce({
        data: [
          {
            id: 1,
            name: 'Electrónica',
            description: 'Catálogo principal',
            parentId: null,
            children: []
          }
        ]
      });

    mockedApiClient.post.mockResolvedValueOnce({
      data: {
        id: 1,
        name: 'Electrónica',
        description: 'Catálogo principal',
        parentId: null,
        children: []
      }
    });

    const user = userEvent.setup();
    render(<CategoriesPage />);

    await screen.findByText('No se han creado categorías todavía. Agrega la primera mediante el formulario.');

    await user.click(screen.getByRole('button', { name: 'Nueva categoría' }));

    await user.type(screen.getByLabelText('Nombre'), 'Electrónica');
    await user.type(screen.getByLabelText('Descripción'), 'Catálogo principal');

    await user.click(screen.getByRole('button', { name: 'Guardar cambios' }));

    await waitFor(() => {
      expect(mockedApiClient.post).toHaveBeenCalledWith('/categories', {
        name: 'Electrónica',
        description: 'Catálogo principal',
        parentId: null
      });
    });

    await screen.findByText('Categoría creada correctamente.');
    await screen.findByRole('button', { name: 'Electrónica' });
  });

  it('allows editing an existing category and keeps the selection', async () => {
    mockedApiClient.get
      .mockResolvedValueOnce({
        data: [
          {
            id: 1,
            name: 'Electrónica',
            description: null,
            parentId: null,
            children: [
              {
                id: 2,
                name: 'Smartphones',
                description: '',
                parentId: 1,
                children: []
              }
            ]
          }
        ]
      })
      .mockResolvedValueOnce({
        data: [
          {
            id: 1,
            name: 'Electrónica',
            description: null,
            parentId: null,
            children: [
              {
                id: 2,
                name: 'Teléfonos',
                description: 'Dispositivos móviles',
                parentId: 1,
                children: []
              }
            ]
          }
        ]
      });

    mockedApiClient.put.mockResolvedValueOnce({
      data: {
        id: 2,
        name: 'Teléfonos',
        description: 'Dispositivos móviles',
        parentId: 1,
        children: []
      }
    });

    const user = userEvent.setup();
    render(<CategoriesPage />);

    await screen.findByRole('button', { name: 'Smartphones' });

    await user.click(screen.getByRole('button', { name: 'Smartphones' }));

    const nameInput = screen.getByLabelText('Nombre') as HTMLInputElement;
    const descriptionTextarea = screen.getByLabelText('Descripción') as HTMLTextAreaElement;
    const parentSelect = screen.getByLabelText('Categoría padre') as HTMLSelectElement;

    await waitFor(() => {
      expect(nameInput.value).toBe('Smartphones');
      expect(parentSelect.value).toBe('1');
    });

    await user.clear(nameInput);
    await user.type(nameInput, 'Teléfonos');
    await user.clear(descriptionTextarea);
    await user.type(descriptionTextarea, 'Dispositivos móviles');

    await user.click(screen.getByRole('button', { name: 'Guardar cambios' }));

    await waitFor(() => {
      expect(mockedApiClient.put).toHaveBeenCalledWith('/categories/2', {
        name: 'Teléfonos',
        description: 'Dispositivos móviles',
        parentId: 1
      });
    });

    await screen.findByText('Categoría actualizada correctamente.');
    await screen.findByRole('button', { name: 'Teléfonos' });
  });
});
