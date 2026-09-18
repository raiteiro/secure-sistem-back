# Plan de Desarrollo — Punto de Venta (POS)

Primer sistema construido sobre el template base de `SecureSistem` (rama
`develop/punto-venta-back`). Objetivo: un POS práctico y entendible, que cubra
las necesidades de la mayoría de negocios pequeños/medianos (abarrotes,
boutique, ferretería, farmacia, etc.) sin atarse a un giro específico.

Reutiliza lo que ya existe en el template: `Company` (tenant), `User`, `Role`,
`NavigationRoute` y todo el sistema de auth/permisos — no se vuelve a construir
nada de eso aquí.

## Fase 1 — Núcleo (imprescindible, no negociable)

- [x] **Sucursales** — si una empresa tiene más de un punto físico, cada venta,
  caja e inventario debe distinguir de cuál sucursal viene. Cuelga de `Company`.
  CRUD completo en `BranchesController` (`/api/branches`), con bootstrap
  automático de una "Sucursal Principal" al crear una empresa nueva, y regla de
  que no se puede desactivar la última sucursal activa de una empresa. **Crear
  una sucursal también crea automáticamente su propio almacén** (`"Almacén
  {NombreSucursal}"`) — cubre el caso común de 1 almacén por sucursal sin
  perder la flexibilidad de tener almacenes independientes o varios por
  sucursal cuando se necesite.
- [x] **Catálogo de productos** — SKU/código de barras, nombre, categoría,
  precio de venta, costo, unidad de medida, imagen. Tres controllers:
  `TaxRatesController` (`/api/taxrates`), `CategoriesController`
  (`/api/categories`) y `ProductsController` (`/api/products`, con
  `POST /api/products/{id}/image` para subir foto, mismo patrón que el logo de
  empresa). SKU único por empresa pero opcional (no todo producto lo necesita).
  Categoría con productos activos no se puede desactivar.
- [x] **Ventas (el POS en sí)** — carrito (líneas por producto), cálculo de
  totales (subtotal, descuento, impuesto snapshot por línea, total), cobrar.
  `SalesController` (`POST /api/sales`, `GET /api/sales`,
  `GET /api/sales/{id}`). La venta solo puede registrarse contra un turno de
  caja abierto (`CashSession`) del propio cajero; valida sucursal/almacén/
  cliente/productos de la misma empresa, descuenta stock atómicamente vía
  `InventoryMovement` (bloquea si no hay stock suficiente) y genera un
  `FolioNumber` consecutivo por empresa. Todo en una sola transacción: si algo
  falla, no se descuenta stock ni se cobra nada.
- [x] **Pagos** — efectivo, tarjeta, "Other", uno o varios por venta (pago
  mixto). La suma de los pagos debe igualar exactamente el total de la venta
  (no hay cálculo de cambio: si el cliente paga con un billete más grande, el
  front debe mandar el monto exacto a cobrar, no lo entregado). Cubierto por
  `Payment`, expuesto anidado dentro de la respuesta de `Sale`.
- [x] **Almacenes** — entidad independiente de Sucursal (una sucursal puede
  tener 0, 1 o varios almacenes; también puede haber un almacén central sin
  sucursal). CRUD completo en `WarehousesController` (`/api/warehouses`), con
  bootstrap automático de un "Almacén Principal" ligado a la sucursal principal
  al crear una empresa, y la misma regla de no poder desactivar el último activo.
- [x] **Inventario** — stock por producto (por almacén), movimientos
  (entrada/salida/ajuste) como bitácora inmutable, alertas de stock bajo.
  `InventoryController` (`/api/inventory`, solo lectura + umbral de stock
  mínimo) e `InventoryMovementsController` (`/api/inventorymovements`, crear
  movimiento). Registrar un movimiento actualiza el stock atómicamente
  (transacción); nunca se edita `Inventory.Quantity` directo. Bloquea si el
  movimiento dejaría el stock en negativo.
- [x] **Corte de caja / turnos** — apertura y cierre de caja, conteo de
  efectivo, cuadre contra lo vendido, por cajero. `CashRegistersController`
  (`/api/cashregisters`, CRUD de cajas por sucursal) y `CashSessionsController`
  (`/api/cashsessions`: `open`, `{id}/close`, `current` para ver el turno
  propio abierto). Reglas: una caja no puede tener dos turnos abiertos a la
  vez; un cajero no puede tener dos turnos abiertos en cajas distintas; no se
  puede desactivar una caja con turno abierto. `ExpectedAmount` al cerrar suma
  el monto de apertura más los pagos en efectivo (`Payment.Method == "Cash"`)
  de las ventas hechas durante ese turno; pagos con tarjeta/otro método no
  cuentan para el efectivo esperado.
- [x] **Clientes** — registro básico, historial de compras (opcional, debe
  poder venderse a "público en general" sin registrar cliente).
  `CustomersController` (`/api/customers`). Email único por empresa pero
  opcional.
- [x] **Impuestos** — tasa de impuesto reutilizable. Cubierto por `TaxRate` /
  `TaxRatesController` (`/api/taxrates`), implementado como parte del catálogo
  de productos ya que `Product` lo referencia directamente.
- [x] **Devoluciones y cancelaciones** — ligadas a la venta original, con
  reingreso de stock. Dos flujos separados:
  - **Cancelación** (`POST /api/sales/{id}/cancel`): anula la venta completa.
    Solo mientras el turno de caja de esa venta sigue abierto (corrección del
    mismo turno) y solo si no tiene devoluciones ya aplicadas. Revierte todo
    el stock, marca `Sale.Status = "Cancelled"` y no se puede repetir ni
    revertir. `CashSession.Close` ya la excluye del efectivo esperado.
  - **Devolución** (`POST /api/returns`, `GET /api/returns`,
    `GET /api/returns/{id}`): parcial o total, por línea (`SaleItemId` +
    cantidad), contra el turno de caja **actual** del cajero que la procesa
    (puede ser distinto al turno original — puede pasar días después). Repone
    stock al almacén original de la venta, calcula el monto a reembolsar
    proporcional al descuento/impuesto ya snapshotteado en el `SaleItem`, y
    exige un `RefundMethod` (Cash/Card/Other) — solo el efectivo resta del
    efectivo esperado del turno que procesa la devolución
    (`CashSession.Close` ya lo descuenta). No se puede devolver más de lo que
    queda pendiente por línea (descuenta lo ya devuelto en devoluciones
    previas).
  - Nuevo tipo de movimiento de inventario `"Return"` (se comporta como
    `"In"`), usado por ambos flujos y disponible también para registrar
    movimientos manuales vía `InventoryMovementsController`.
  - Pantalla nueva **Devoluciones** (`/devoluciones`), ítem suelto junto a
    "Ventas", `IsDefaultForNewRoles = true`, con backfill para Empresa Demo y
    VetPet (empresas activas) y todos sus roles activos.
- [x] **Tickets/recibos** — folio consecutivo (ya cubierto por
  `Sale.FolioNumber`), más dos endpoints nuevos en `SalesController`:
  - `GET /api/sales/{id}/receipt`: el detalle completo de la venta más el
    encabezado de la empresa (nombre, RFC/TaxId, dirección, teléfono, logo)
    que `SaleResponse` no trae — pensado para que el frontend arme el ticket
    imprimible sin tener que cruzar `Company` por separado. No hay generación
    de PDF en el backend (no hay librería de PDF en el proyecto); esa parte es
    responsabilidad del frontend/driver de impresora.
  - `POST /api/sales/{id}/send-receipt`: envía el recibo por correo (texto
    plano) usando `IEmailService`. Si el body no trae `email`, usa el del
    cliente de la venta; si no hay ninguno de los dos, 400. Si el envío falla
    (típicamente porque SMTP no está configurado — ver `TECH_DEBT.md`),
    responde 502 en vez de reventar, igual que `forgot-password`.
- [x] **Reportes básicos** — ventas por día/periodo, productos más vendidos,
  corte por cajero. Todo de solo lectura en `ReportsController`
  (`/api/reports`), sin capa de repositorio ni `Result<T>` (esos patrones de
  "Patrones Obligatorios" en `CLAUDE.md` son aspiracionales y no se usan en
  ningún controller real del proyecto — se siguió la convención real:
  `ApplicationDbContext` inyectado directo, `{ message }` en español para
  errores). Tres endpoints:
  - `GET /api/reports/sales-by-period`: ventas (excluye canceladas) agrupadas
    por día o mes (`groupBy=day|month`) dentro de un rango de fechas. Cada
    periodo trae anidado `products` con **todos** los productos vendidos en
    ese periodo (mismo shape que `top-products`, sin límite), pedido por el
    frontend para no tener que cruzar ambos endpoints a mano.
  - `GET /api/reports/top-products`: productos más vendidos por cantidad
    dentro de un rango de fechas, con límite configurable.
  - `GET /api/reports/cashier-closeouts`: histórico de cortes de caja
    (turnos ya cerrados) con el desglose de ventas por método de pago detrás
    de cada uno, filtrable por cajero/sucursal/fecha.
  - Los tres por defecto cubren "el mes en curso a la fecha" si no se manda
    `from`/`to`, y respetan el filtro de empresa/`IsSystemAdmin()` como el
    resto de los endpoints.
  - Pantalla nueva **Reportes** (`/reportes`), ítem suelto junto a
    "Devoluciones", `IsDefaultForNewRoles = true`, con backfill para Empresa
    Demo y VetPet (empresas activas) y todos sus roles activos.
- [x] **Permisos aplicados al POS** — usando los `Role` que ya existen. Alcance
  decidido: control por ventana **y por botón/acción** (catálogo de 46
  acciones, ver `permissions.md` en la raíz para el árbol completo
  ventana → acciones), sin validación adicional en el backend por ahora —
  se considera suficiente con que el frontend oculte/deshabilite botones
  según el permiso, mismo criterio que ya se usaba para rutas.
  - `Permission` (catálogo global, no por empresa — a diferencia de
    `NavigationRoute`, una acción solo tiene sentido si el backend
    efectivamente la valida, así que no se crea libremente vía API) +
    `RolePermission` (asignación por rol, mismo patrón que
    `RoleNavigationRoute`).
  - `GET /api/permissions` (catálogo completo) y
    `GET /api/permissions/my-permissions` (claves efectivas del usuario
    actual, para que el frontend decida qué botones mostrar/habilitar sin
    tener que llamar la API con cada click).
  - `GET/POST /api/roles/{id}/permissions` en `RolesController` para
    asignar/revocar acciones por rol, calcado de `/routes`.
  - Todas arrancan con `IsDefaultForNewRoles = true` (permisivo por
    default, igual que las rutas de "Catálogo") — se le asignaron a los 4
    roles activos que ya existían (backfill vía la propia migración, no
    manual) y `RolesController.Create`/`CompaniesController.Create` ya
    asignan los permisos default a cualquier rol nuevo, presente o futuro.
  - Decisión consciente (no pendiente): el backend no valida estos permisos
    en cada endpoint — ver `TECH_DEBT.md` para el detalle de qué implica
    eso si se necesita reforzar más adelante.

## Fase 2 — Valor agregado (después del núcleo)

- [ ] Compras a proveedores (cierra el ciclo del inventario).
- [ ] Proveedores.
- [ ] Variantes de producto (talla/color) y combos/kits.
- [ ] Listas de precios (mayoreo vs. menudeo, precios por cliente).
- [ ] Apartados / ventas a crédito.
- [ ] Cotizaciones que se convierten en venta.
- [ ] Facturación electrónica (CFDI) — ya manejamos RFC en `Company`, es
  probable que sea requisito real en México más adelante.
- [ ] Programa de lealtad/puntos.
- [ ] Modo offline con sincronización.

## Específico por giro (opcional, fuera del core)

- **Restaurante**: mesas, comandas a cocina, propinas, dividir cuenta,
  modificadores de menú.
- **Servicios**: citas/agenda en vez de inventario.

## Entidades nuevas a diseñar (modelo de datos)

Punto de partida para la siguiente sesión de trabajo, antes de escribir código:

| Entidad | Notas |
|---|---|
| `Branch` (Sucursal) | ✅ Implementado. Cuelga de `Company`. |
| `Warehouse` (Almacén) | ✅ Implementado. Cuelga de `Company`; `BranchId` opcional (null = almacén central independiente de cualquier sucursal). |
| `Category` | ✅ Implementado. |
| `Product` | ✅ Implementado. SKU, precio, costo, categoría, impuesto aplicable, imagen. |
| `TaxRate` | ✅ Implementado (adelantado, ya lo usa `Product`). |
| `Inventory` | ✅ Implementado. Stock actual por producto + almacén. |
| `InventoryMovement` | ✅ Implementado. Bitácora inmutable de entradas/salidas/ajustes, con snapshot de `ResultingQuantity`. |
| `Sale` | ✅ Implementado. Encabezado de venta: sucursal, almacén, cajero, cliente opcional, turno de caja, folio consecutivo por empresa, totales. |
| `SaleItem` | ✅ Implementado. Líneas de la venta con snapshot de precio/tasa de impuesto al momento de vender (inmune a cambios posteriores del producto). |
| `Payment` | ✅ Implementado. Uno o más pagos por venta (efectivo/tarjeta/otro); la suma debe igualar el total. |
| `CashRegister` / `CashSession` | ✅ Implementado. Turno de caja: apertura, cierre, conteo. |
| `Customer` | ✅ Implementado. Cliente (opcional en la venta). |
| `Return` / `ReturnItem` | ✅ Implementado. Devolución parcial/total de una venta, procesada contra el turno de caja actual (no el original); repone stock y registra el reembolso. |
| `Supplier` (fase 2) | Proveedor. |
| `PurchaseOrder` (fase 2) | Orden de compra a proveedor. |

## Menú de navegación del módulo POS

Todas las pantallas de este módulo (Sucursales, Almacenes, Categorías,
Productos, Impuestos, Inventario) cuelgan de un grupo nuevo **"Catálogo"** (separado de
"Administración"), con rutas `/catalogo/*`. Se marcan con
`NavigationRoute.IsDefaultForNewRoles = true`, lo que significa:

- Se crean automáticamente al dar de alta una empresa nueva, asignadas al rol
  admin inicial.
- **Cualquier rol que se cree de aquí en adelante** (en cualquier empresa, sin
  importar cuándo) las recibe automáticamente activas — no nace en blanco como
  el resto de los permisos. La idea es que el control fino sea "desactivarlas
  si un rol no las debe tener", no "activarlas a mano cada vez".
- Se hizo backfill de esto para las empresas que ya existían (Empresa Demo,
  VetPet) y todos sus roles activos.

La pantalla de **Ventas** (`/ventas`, el checkout del POS en sí) es un ítem
suelto — no cuelga de "Catálogo" — con su propio icono, también marcado
`IsDefaultForNewRoles = true` y con el mismo backfill aplicado.

**Pendiente de coordinar con el frontend:** Empresa Demo ya tenía una ruta
"Sucursales" creada a mano bajo "Administración" (`/admin/sucursales`, antes
de que existiera el grupo "Catálogo"). Quedó viva sin tocar para no romper
nada — hay que avisarle al frontend que mueva esa referencia a la nueva
"Sucursales" del grupo "Catálogo" y, una vez migrado, desactivar/eliminar la
vieja.

## Cómo se prioriza

Primero se completa **toda la Fase 1** antes de tocar Fase 2 — es lo que hace
que ya sea "un POS funcional" de verdad, y es la parte que se reutiliza sin
cambios sin importar el giro del negocio. Lo específico por giro (restaurante,
servicios) se deja como módulo aparte, no se mezcla en el core.
