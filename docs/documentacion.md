# Documentación de la aplicación y del agente de gestión de inventario

## Visión general

Este documento resume los requisitos funcionales y técnicos para una aplicación de gestión de inventario desarrollada con .NET 8 y describe el esquema de base de datos propuesto. El objetivo es que cualquier agente o desarrollador disponga de toda la información necesaria para generar la aplicación, desde las tecnologías a emplear hasta las tablas de la base de datos.

## Objetivos principales

- **Control integral de productos:** cada producto debe almacenar información detallada (código único, nombre, descripción, categoría, variantes, precios, tarifas/impuestos, imágenes, etc.).
- **Gestión de stock:** seguimiento en tiempo real de cantidades disponibles, reservadas y mínimas por almacén y variante, con alertas de reposición automática.
- **Procesamiento de pedidos y compras:** registro de pedidos de clientes y pedidos a proveedores, con estados de pedido, cálculo automático de impuestos y envío, y generación de órdenes de compra cuando el stock sea insuficiente.
- **Gestión de proveedores y clientes:** mantener datos de contacto, acuerdos de suministro, listas de precios específicas y condiciones de pago.
- **Planificación de la demanda:** permitir el análisis de histórico de ventas y la predicción de demanda para optimizar el inventario.
- **Informes y analítica:** ofrecer informes sobre rotación de inventario, niveles de stock, costes de almacenamiento, ventas y KPIs clave.
- **Panel de administración:** configurar parámetros globales (impuestos, moneda, unidades), gestionar usuarios y roles, aplicar control de acceso (RBAC) y realizar tareas de mantenimiento.

## Arquitectura y tecnologías

### Backend

- **Plataforma:** .NET 8 (LTS) para aprovechar el soporte a largo plazo, el rendimiento mejorado y características como Native AOT y Blazor.
- **API:** ASP.NET Core 8 utilizando controladores RESTful y, opcionalmente, minimal APIs.
- **Persistencia:** Entity Framework Core 8 (Code-First) con SQL Server como motor relacional.
- **Patrones de diseño:** Clean Architecture y Domain-Driven Design con repositorios y Unit of Work para desacoplar el acceso a datos.
- **Seguridad:** ASP.NET Identity con autenticación basada en JWT o IdentityServer. Roles principales: Administrador, Planificador de demanda y Gestor de inventario.
- **Servicios complementarios:** SignalR para notificaciones en tiempo real, colas de mensajería (RabbitMQ/Azure Service Bus) para redistribución de eventos, caché con Redis y documentación de API con Swagger/OpenAPI.

### Frontend

- **Framework principal:** React 18 con Next.js para renderizado del lado del servidor (SSR) y mejor rendimiento inicial.
- **Gestión de estado:** Redux o Zustand para manejar flujos de datos complejos.
- **Estilos:** Tailwind CSS, Bootstrap o Material UI para una interfaz profesional y responsiva.
- **Alternativa:** Blazor Server/WebAssembly en .NET 8 como opción full-stack basada en C#.

#### Flujo de pedidos en la aplicación web

La pantalla de **Pedidos** dentro del panel (`/dashboard/orders`) unifica los pedidos de venta y de compra. Los estados se representan con las siguientes etiquetas:

- Ventas: Pendiente → Confirmado → Enviado → Entregado (Cancelado disponible en cualquier punto).
- Compras: Pendiente → Solicitado → Recibido (o Cancelado).

El flujo esperado es:

1. Al cargar la vista se consultan `/salesorders` (alias disponible en `/orders`) y `/purchaseorders`. Mientras tanto se muestra el mensaje “Cargando pedidos…”.
2. El botón **Enviar** abre un modal con las asignaciones pendientes del pedido de venta (`/salesorders/{id}`, también accesible como `/orders/{id}`). Al confirmar se realiza `PUT /salesorders/{id}/status` (alias `/orders/{id}/status`) indicando los lotes pendientes.
3. El botón **Registrar recepción** abre el modal para seleccionar el almacén de destino (`/purchaseorders/{id}`). Al confirmar se llama a `PUT /purchaseorders/{id}/status` con el almacén elegido.
4. Cada actualización vuelve a consultar los listados para refrescar las tablas y mostrar los mensajes de éxito o error devueltos por la API.

##### Cómo probar el flujo

- **Unitarias:** `npm run test --workspace gestor-inventario-web` ejecuta pruebas de utilidades como el cálculo de unidades pendientes por enviar.
- **E2E/Integración:** `npm run test:e2e --workspace gestor-inventario-web` dispara Playwright con mocks para `/salesorders`, `/purchaseorders` y `/warehouses`, validando la carga inicial, la apertura de modales y los envíos/recepciones.
- **Manual:** levantar la app (`npm run dev --workspace gestor-inventario-web`) y validar que los mensajes de éxito/error y los estados de la tabla se actualizan después de confirmar un envío o recepción.

### Despliegue y DevOps

- **Contenedores:** Docker para empaquetar la API, el front-end y la base de datos.
- **CI/CD:** pipelines en GitHub Actions, Azure DevOps u otra herramienta que compile, ejecute pruebas y despliegue a los entornos objetivo.
- **Cumplimiento y DR:** las políticas documentadas en `docs/compliance-dr.md` cubren ISO 27001, GDPR/LOPD y el plan de recuperación ante desastres.

## Esquema de base de datos

El siguiente esquema describe la base de datos relacional propuesta. Los tipos de datos se basan en SQL Server.

### Tabla `Users`

| Columna      | Tipo           | PK | FK | Nulo | Comentarios                              |
|--------------|----------------|----|----|------|------------------------------------------|
| UserId       | INT IDENTITY   | ✓  | -  | NO   | Identificador único del usuario.         |
| Username     | NVARCHAR(100)  | -  | -  | NO   | Nombre de usuario único.                 |
| Email        | NVARCHAR(200)  | -  | -  | NO   | Correo electrónico único.                |
| PasswordHash | NVARCHAR(500)  | -  | -  | NO   | Hash de la contraseña.                   |
| RoleId       | INT            | -  | ✓  | NO   | Relación con `Roles`.                    |
| CreatedAt    | DATETIME       | -  | -  | NO   | Fecha de creación.                       |
| IsActive     | BIT            | -  | -  | NO   | Indica si el usuario está activo.        |

### Tabla `Roles`

| Columna     | Tipo           | PK | Nulo | Comentarios                                        |
|-------------|----------------|----|------|----------------------------------------------------|
| RoleId      | INT IDENTITY   | ✓  | NO   | Identificador del rol.                             |
| Name        | NVARCHAR(50)   | -  | NO   | Nombre del rol (Administrador, Planificador, etc.). |
| Description | NVARCHAR(200)  | -  | SÍ   | Descripción opcional del rol.                      |

### Tabla `Categories`

| Columna     | Tipo           | PK | FK | Nulo | Comentarios                                              |
|-------------|----------------|----|----|------|----------------------------------------------------------|
| CategoryId  | INT IDENTITY   | ✓  | -  | NO   | Identificador de la categoría.                           |
| Name        | NVARCHAR(100)  | -  | -  | NO   | Nombre de la categoría.                                 |
| Description | NVARCHAR(200)  | -  | -  | SÍ   | Descripción de la categoría.                            |
| ParentId    | INT            | -  | ✓  | SÍ   | Autorelación para categorías jerárquicas.                |

### Tabla `Products`

| Columna     | Tipo           | PK | FK | Nulo | Comentarios                                             |
|-------------|----------------|----|----|------|---------------------------------------------------------|
| ProductId   | INT IDENTITY   | ✓  | -  | NO   | Identificador del producto.                             |
| Code        | NVARCHAR(50)   | -  | -  | NO   | Código único/SKU.                                       |
| Name        | NVARCHAR(150)  | -  | -  | NO   | Nombre del producto.                                   |
| Description | NVARCHAR(MAX)  | -  | -  | SÍ   | Descripción detallada.                                 |
| CategoryId  | INT            | -  | ✓  | SÍ   | Relación con `Categories`.                             |
| DefaultPrice| DECIMAL(18,4)  | -  | -  | NO   | Precio base.                                           |
| Currency    | NVARCHAR(10)   | -  | -  | NO   | Moneda (EUR, USD).                                     |
| TaxRateId   | INT            | -  | ✓  | SÍ   | Relación con `TaxRates`.                               |
| IsActive    | BIT            | -  | -  | NO   | Indica si el producto está disponible.                 |
| CreatedAt   | DATETIME       | -  | -  | NO   | Fecha de creación.                                     |

### Tabla `ProductImages`

| Columna  | Tipo           | PK | FK | Nulo | Comentarios                            |
|----------|----------------|----|----|------|----------------------------------------|
| ImageId  | INT IDENTITY   | ✓  | -  | NO   | Identificador de la imagen.            |
| ProductId| INT            | -  | ✓  | NO   | Relación con `Products`.               |
| ImageUrl | NVARCHAR(300)  | -  | -  | NO   | URL o ruta de la imagen.               |
| AltText  | NVARCHAR(200)  | -  | -  | SÍ   | Texto alternativo.                     |

### Tabla `Variants`

| Columna    | Tipo           | PK | FK | Nulo | Comentarios                                                                 |
|------------|----------------|----|----|------|-----------------------------------------------------------------------------|
| VariantId  | INT IDENTITY   | ✓  | -  | NO   | Identificador de la variante.                                               |
| ProductId  | INT            | -  | ✓  | NO   | Relación con `Products`.                                                    |
| Sku        | NVARCHAR(50)   | -  | -  | NO   | Código único por variante.                                                  |
| Attributes | NVARCHAR(200)  | -  | -  | NO   | JSON o texto con pares atributo=valor (ej. "talla=M;color=azul").         |
| Price      | DECIMAL(18,4)  | -  | -  | SÍ   | Precio específico si difiere del producto.                                   |
| Barcode    | NVARCHAR(50)   | -  | -  | SÍ   | Código de barras.                                                            |

### Tabla `Warehouses`

| Columna     | Tipo           | PK | Nulo | Comentarios                     |
|-------------|----------------|----|------|---------------------------------|
| WarehouseId | INT IDENTITY   | ✓  | NO   | Identificador del almacén.      |
| Name        | NVARCHAR(100)  | -  | NO   | Nombre del almacén.             |
| Address     | NVARCHAR(200)  | -  | SÍ   | Dirección.                       |
| Description | NVARCHAR(200)  | -  | SÍ   | Comentarios adicionales.        |

### Tabla `InventoryStock`

| Columna        | Tipo           | PK | FK | Nulo | Comentarios                                                     |
|----------------|----------------|----|----|------|-----------------------------------------------------------------|
| StockId        | INT IDENTITY   | ✓  | -  | NO   | Identificador del registro.                                     |
| VariantId      | INT            | -  | ✓  | NO   | Relación con `Variants`.                                        |
| WarehouseId    | INT            | -  | ✓  | NO   | Relación con `Warehouses`.                                      |
| Quantity       | DECIMAL(18,4)  | -  | -  | NO   | Cantidad disponible.                                            |
| ReservedQty    | DECIMAL(18,4)  | -  | -  | NO   | Cantidad reservada para pedidos.                               |
| MinStockLevel  | DECIMAL(18,4)  | -  | -  | NO   | Nivel mínimo antes de reabastecer.                             |

### Tabla `InventoryTransactions`

| Columna         | Tipo           | PK | FK | Nulo | Comentarios                                                                     |
|-----------------|----------------|----|----|------|---------------------------------------------------------------------------------|
| TransactionId   | INT IDENTITY   | ✓  | -  | NO   | Identificador del movimiento.                                                   |
| VariantId       | INT            | -  | ✓  | NO   | Referencia a `Variants`.                                                        |
| WarehouseId     | INT            | -  | ✓  | NO   | Referencia a `Warehouses`.                                                      |
| TransactionType | NVARCHAR(20)   | -  | -  | NO   | Tipo de movimiento: IN, OUT, MOVE, ADJUST.                                     |
| Quantity        | DECIMAL(18,4)  | -  | -  | NO   | Cantidad del movimiento.                                                        |
| TransactionDate | DATETIME       | -  | -  | NO   | Fecha y hora.                                                                   |
| ReferenceType   | NVARCHAR(20)   | -  | -  | SÍ   | Origen (por ejemplo, "PurchaseOrder", "SalesOrder").                          |
| ReferenceId     | INT            | -  | -  | SÍ   | Identificador de la entidad referenciada.                                       |
| UserId          | INT            | -  | ✓  | SÍ   | Usuario que realizó el movimiento.                                              |
| Notes           | NVARCHAR(200)  | -  | -  | SÍ   | Comentarios adicionales.                                                        |

### Tabla `Suppliers`

| Columna     | Tipo           | PK | Nulo | Comentarios                                       |
|-------------|----------------|----|------|---------------------------------------------------|
| SupplierId  | INT IDENTITY   | ✓  | NO   | Identificador del proveedor.                      |
| Name        | NVARCHAR(150)  | -  | NO   | Nombre o razón social.                            |
| ContactName | NVARCHAR(150)  | -  | SÍ   | Persona de contacto.                              |
| Email       | NVARCHAR(200)  | -  | SÍ   | Correo electrónico.                               |
| Phone       | NVARCHAR(50)   | -  | SÍ   | Teléfono.                                         |
| Address     | NVARCHAR(200)  | -  | SÍ   | Dirección.                                        |
| Notes       | NVARCHAR(200)  | -  | SÍ   | Notas o acuerdos de suministro.                   |

### Tabla `Customers`

| Columna    | Tipo           | PK | Nulo | Comentarios                     |
|------------|----------------|----|------|---------------------------------|
| CustomerId | INT IDENTITY   | ✓  | NO   | Identificador del cliente.      |
| Name       | NVARCHAR(150)  | -  | NO   | Nombre o razón social.          |
| Email      | NVARCHAR(200)  | -  | SÍ   | Correo electrónico.             |
| Phone      | NVARCHAR(50)   | -  | SÍ   | Teléfono.                       |
| Address    | NVARCHAR(200)  | -  | SÍ   | Dirección.                      |
| Notes      | NVARCHAR(200)  | -  | SÍ   | Observaciones.                  |

### Tabla `PurchaseOrders`

| Columna         | Tipo           | PK | FK | Nulo | Comentarios                                     |
|-----------------|----------------|----|----|------|-------------------------------------------------|
| PurchaseOrderId | INT IDENTITY   | ✓  | -  | NO   | Identificador del pedido de compra.             |
| SupplierId      | INT            | -  | ✓  | NO   | Proveedor asociado.                              |
| OrderDate       | DATETIME       | -  | -  | NO   | Fecha del pedido.                               |
| Status          | NVARCHAR(20)   | -  | -  | NO   | Estado (Pending, Ordered, Received, Cancelled). |
| TotalAmount     | DECIMAL(18,4)  | -  | -  | NO   | Importe total calculado.                        |
| Currency        | NVARCHAR(10)   | -  | -  | NO   | Moneda.                                         |
| Notes           | NVARCHAR(200)  | -  | -  | SÍ   | Comentarios.                                    |

### Tabla `PurchaseOrderLines`

| Columna         | Tipo           | PK | FK | Nulo | Comentarios                                      |
|-----------------|----------------|----|----|------|--------------------------------------------------|
| LineId          | INT IDENTITY   | ✓  | -  | NO   | Identificador de la línea.                       |
| PurchaseOrderId | INT            | -  | ✓  | NO   | Referencia a `PurchaseOrders`.                   |
| VariantId       | INT            | -  | ✓  | NO   | Producto/variante solicitada.                    |
| Quantity        | DECIMAL(18,4)  | -  | -  | NO   | Cantidad solicitada.                             |
| UnitPrice       | DECIMAL(18,4)  | -  | -  | NO   | Precio unitario.                                 |
| Discount        | DECIMAL(18,4)  | -  | -  | SÍ   | Descuento aplicado.                              |
| TaxRateId       | INT            | -  | ✓  | SÍ   | Relación con `TaxRates` para calcular impuestos. |
| TotalLine       | DECIMAL(18,4)  | -  | -  | NO   | Importe total de la línea.                       |

### Tabla `SalesOrders`

| Columna       | Tipo           | PK | FK | Nulo | Comentarios                                              |
|---------------|----------------|----|----|------|----------------------------------------------------------|
| SalesOrderId  | INT IDENTITY   | ✓  | -  | NO   | Identificador del pedido de venta.                      |
| CustomerId    | INT            | -  | ✓  | NO   | Cliente asociado.                                       |
| OrderDate     | DATETIME       | -  | -  | NO   | Fecha del pedido.                                       |
| Status        | NVARCHAR(20)   | -  | -  | NO   | Estado (Pending, Confirmed, Shipped, Delivered, Cancelled). |
| ShippingAddress| NVARCHAR(200) | -  | -  | SÍ   | Dirección de envío si difiere del cliente.               |
| TotalAmount   | DECIMAL(18,4)  | -  | -  | NO   | Importe total.                                          |
| Currency      | NVARCHAR(10)   | -  | -  | NO   | Moneda.                                                 |
| Notes         | NVARCHAR(200)  | -  | -  | SÍ   | Comentarios.                                            |

### Tabla `SalesOrderLines`

| Columna      | Tipo           | PK | FK | Nulo | Comentarios                                      |
|--------------|----------------|----|----|------|--------------------------------------------------|
| LineId       | INT IDENTITY   | ✓  | -  | NO   | Identificador de la línea.                       |
| SalesOrderId | INT            | -  | ✓  | NO   | Referencia a `SalesOrders`.                      |
| VariantId    | INT            | -  | ✓  | NO   | Producto/variante vendido.                       |
| Quantity     | DECIMAL(18,4)  | -  | -  | NO   | Cantidad.                                        |
| UnitPrice    | DECIMAL(18,4)  | -  | -  | NO   | Precio de venta.                                 |
| Discount     | DECIMAL(18,4)  | -  | -  | SÍ   | Descuento.                                       |
| TaxRateId    | INT            | -  | ✓  | SÍ   | Relación con `TaxRates`.                         |
| TotalLine    | DECIMAL(18,4)  | -  | -  | NO   | Importe total de la línea.                       |

### Tabla `PriceLists`

| Columna    | Tipo           | PK | Nulo | Comentarios                               |
|------------|----------------|----|------|-------------------------------------------|
| PriceListId| INT IDENTITY   | ✓  | NO   | Identificador de la lista.                |
| Name       | NVARCHAR(100)  | -  | NO   | Nombre (p. ej. "Tarifa mayorista").      |
| Description| NVARCHAR(200)  | -  | SÍ   | Descripción.                              |
| Currency   | NVARCHAR(10)   | -  | NO   | Moneda.                                   |

### Tabla `ProductPrices`

| Columna    | Tipo           | PK | FK | Nulo | Comentarios                                           |
|------------|----------------|----|----|------|-------------------------------------------------------|
| PriceId    | INT IDENTITY   | ✓  | -  | NO   | Identificador de la fila.                             |
| PriceListId| INT            | -  | ✓  | NO   | Lista de precios asociada.                            |
| VariantId  | INT            | -  | ✓  | NO   | Producto/variante al que se aplica el precio.         |
| Price      | DECIMAL(18,4)  | -  | -  | NO   | Precio definido en esa lista.                         |

### Tabla `TaxRates`

| Columna    | Tipo           | PK | Nulo | Comentarios                             |
|------------|----------------|----|------|-----------------------------------------|
| TaxRateId  | INT IDENTITY   | ✓  | NO   | Identificador del impuesto.             |
| Name       | NVARCHAR(100)  | -  | NO   | Nombre (IVA 21 %, IGIC 7 %, etc.).      |
| Rate       | DECIMAL(5,2)   | -  | NO   | Porcentaje (%).                         |
| Region     | NVARCHAR(50)   | -  | SÍ   | Región o país.                          |
| Description| NVARCHAR(200)  | -  | SÍ   | Notas.                                  |

### Tabla `ShippingRates`

| Columna        | Tipo           | PK | Nulo | Comentarios                                          |
|----------------|----------------|----|------|------------------------------------------------------|
| ShippingRateId | INT IDENTITY   | ✓  | NO   | Identificador de la tarifa de envío.                 |
| Name           | NVARCHAR(100)  | -  | NO   | Nombre del método (Estándar, Express).               |
| BaseCost       | DECIMAL(18,4)  | -  | NO   | Coste base de envío.                                 |
| CostPerWeight  | DECIMAL(18,4)  | -  | SÍ   | Coste adicional por unidad de peso.                  |
| CostPerDistance| DECIMAL(18,4)  | -  | SÍ   | Coste por distancia si se emplea.                    |
| Currency       | NVARCHAR(10)   | -  | NO   | Moneda.                                              |
| Description    | NVARCHAR(200)  | -  | SÍ   | Observaciones.                                       |

## Relaciones clave

- `Users` contiene una clave foránea a `Roles` para definir los permisos de cada usuario.
- `Categories` incorpora `ParentId` para soportar jerarquías de categorías.
- `Products` puede pertenecer a una categoría y relacionarse con tasas de impuestos mediante `TaxRateId`.
- `Variants` referencia a `Products`, permitiendo múltiples variantes por producto.
- `InventoryStock` y `InventoryTransactions` vinculan `Variants` con `Warehouses` para registrar existencias y movimientos.
- `PurchaseOrders` y `PurchaseOrderLines` gestionan pedidos a proveedores y los artículos solicitados.
- `SalesOrders` y `SalesOrderLines` registran pedidos de clientes y sus líneas de detalle.
- `PriceLists` y `ProductPrices` permiten crear tarifas específicas por segmento o proveedor.
- Tanto `PurchaseOrderLines` como `SalesOrderLines` pueden referenciar un `TaxRateId` para aplicar impuestos.

## Consideraciones adicionales

- Añadir índices en campos de búsqueda frecuente como `Products.Code`, `Variants.Sku` y `OrderDate` en pedidos.
- Definir reglas de integridad referencial con `ON DELETE RESTRICT` u `ON DELETE CASCADE` según cada relación.
- Incluir una tabla `AuditLogs` para auditar operaciones sensibles (quién modificó qué y cuándo).
- Guardar el histórico de ventas en una tabla `DemandHistory` para alimentar algoritmos de previsión de demanda.

## Notas sobre funcionalidades y roles

- **Planificador de demanda:** debe poder analizar datos históricos, pronosticar demanda y gestionar proveedores estratégicos.
- **Gestor de inventario:** requiere seguimiento de stock, generación de órdenes automáticas de reposición y organización de almacenes.
- **Administrador:** administra la configuración del sistema, usuarios, permisos, seguridad y copias de seguridad.

## Referencias

- Tendencias y funcionalidades clave para gestores de inventario y planificación de demanda: [cleveroad.com](https://www.cleveroad.com/).
- Ventajas de .NET 8 y Blazor para desarrollo full-stack: [eluminoustechnologies.com](https://eluminoustechnologies.com/).
- Integración de SQL Server con aplicaciones .NET: [intelegain.com](https://www.intelegain.com/).
- Ventajas de React 18 y Next.js en escenarios .NET: [moldstud.com](https://moldstud.com/).
