# Gestor de inventario

Este repositorio recopila la documentación funcional y técnica necesaria para implementar una solución integral de gestión de inventario sobre .NET 8 y un frontend moderno con Next.js 14.

## Contenido

- [Documentación detallada de la aplicación y del agente](docs/documentacion.md)
- [Esquema relacional en formato JSON](docs/database-schema.json)
- Código fuente organizado por capas (`Domain`, `Application`, `Infrastructure`, `Presentation`).

## Estado de la hoja de ruta

| Paso | Descripción | Estado |
|------|-------------|--------|
| 1 | Revisión de requisitos y esquema de base de datos | ✅ Completado en `docs/documentacion.md` y `docs/database-schema.json` |
| 2 | Generación del backend .NET 8 siguiendo Clean Architecture | ✅ Bases creadas en la solución `GestorInventario.sln` |
| 3 | Selección de frontend y alineación DevOps | ✅ Next.js 14 elegido y orquestación con Docker configurada |

## Paso 3 · Frontend seleccionado y alineación DevOps

### Stack elegido

- **Framework:** Next.js 14 (App Router) + React 18.
- **Estilos:** Tailwind CSS con diseño responsivo.
- **Tipado:** TypeScript estricto y soporte para importación del esquema de base de datos.
- **Contenedores:** Dockerfile multi-stage con runtime Node 20 Alpine y `docker-compose.yml` para API, frontend y SQL Server.

### Arquitectura y principios de diseño

- **Capas**: la solución sigue Clean Architecture con proyectos `GestorInventario.Domain`, `GestorInventario.Application`, `GestorInventario.Infrastructure` y `GestorInventario.Api`. El frontend vive en `src/Presentation/gestor-inventario-web`.
- **Inyección de dependencias**: cada capa expone un método `DependencyInjection` que registra servicios, repositorios y clientes externos.
- **Asincronía**: los manejadores y controladores usan `async/await` de extremo a extremo, evitando bloqueos del thread pool.
- **Validación**: se usa FluentValidation en los comandos y consultas y se aplican `Behaviors` de MediatR para validaciones transversales.
- **Caching distribuido**: la consulta de catálogo de productos utiliza `IDistributedCache` con claves registradas por región para invalidar cambios de catálogo.

### Convenciones de código

- **C#**: PascalCase para clases, métodos y propiedades; camelCase para parámetros y variables locales. `nullable` habilitado y análisis de advertencias activo.
- **TypeScript/React**: componentes funcionales, hooks personalizados para la lógica compartida y `zustand` para estado global. Imports ordenados y `eslint`/`tsc` garantizan calidad.
- **Tests**: `xUnit` para la capa de aplicación/API y `vitest`/`@testing-library` para el frontend. Las pruebas de dominio se agrupan por módulo bajo `tests/`.

### Requisitos previos

- .NET SDK 8.0
- Node.js 20.x y npm 10+
- Docker Desktop o motor compatible para ejecutar contenedores

### Ejecución local

1. **API**
   ```bash
   dotnet build
   dotnet run --project src/Presentation/GestorInventario.Api/GestorInventario.Api.csproj
   ```
2. **Frontend**
   Desde la raíz del repositorio puedes preparar y levantar la aplicación Next.js sin cambiar de carpeta:
   ```bash
   cp src/Presentation/gestor-inventario-web/.env.example src/Presentation/gestor-inventario-web/.env.local
   npm install
   npm run dev
   ```
   Si prefieres trabajar dentro del directorio del frontend, los comandos clásicos (`cd src/Presentation/gestor-inventario-web` y `npm run dev`) siguen siendo válidos.
   El panel estará disponible en `http://localhost:3000` y consumirá la API en `http://localhost:5000`.

### Ejecución con Docker Compose

```bash
docker compose up --build
```

- **api:** ASP.NET Core expuesto en `http://localhost:5000` con conexión a SQL Server.
- **web:** Next.js en `http://localhost:3000` utilizando la variable `NEXT_PUBLIC_API_BASE_URL`.
- **sqlserver:** Motor SQL Server 2022 con volumen persistente `sqlserver-data`.

### Pruebas automatizadas

```bash
dotnet test
npm --prefix src/Presentation/gestor-inventario-web run test
```

Las suites cubren casos críticos de catálogo de productos, pedidos y dashboards. Añade pruebas al modificar la lógica de negocio o la interfaz.

### Referencia rápida de la API

- `GET /api/products`: devuelve un `PagedResult<ProductDto>` con filtros opcionales `searchTerm`, `categoryId`, `isActive`, `pageNumber` y `pageSize` (máximo 200).
- `POST /api/products`: crea un producto con variantes, dispara invalidación de caché y eventos de integración.
- `GET /api/warehouses/{id}/product-variants`: recupera asignaciones de stock por almacén.

Ejemplo de consumo paginado:

```http
GET /api/products?searchTerm=sku&pageNumber=2&pageSize=100
X-Tenant-Id: <tenant>
Authorization: Bearer <token>
```

## Referencia del esquema de base de datos

El fichero [`docs/database-schema.json`](docs/database-schema.json) centraliza nombres de tablas, columnas, claves primarias y relaciones para garantizar que cualquier cambio en backend o frontend mantenga la coherencia con el dominio. La aplicación de Next.js importa este esquema (ver `src/Presentation/gestor-inventario-web/src/data/database-schema.ts`) para mostrar resúmenes en la interfaz y guiar la construcción de peticiones.

## Próximos pasos sugeridos

1. Extender la capa de infraestructura con modelos de Entity Framework Core basados en el esquema provisto.
2. Implementar endpoints específicos (productos, stock, pedidos) y pruebas automatizadas asociadas.
3. Desarrollar vistas y formularios en Next.js para CRUDs críticos, dashboards y workflows de reposición.

## Mantenimiento recomendado

- Revisa y expira entradas de caché mediante los eventos de dominio `ProductCatalogChangedDomainEvent` cuando se modifiquen productos, precios o impuestos.
- Centraliza nuevas consultas que devuelvan colecciones grandes utilizando `PagedResult<T>` para evitar respuestas masivas.
- Ejecuta `dotnet format` y `npm run lint` antes de enviar PRs para garantizar estilos consistentes.
