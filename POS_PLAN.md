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
  que no se puede desactivar la última sucursal activa de una empresa.
- [ ] **Catálogo de productos** — SKU/código de barras, nombre, categoría,
  precio de venta, costo, unidad de medida, imagen.
- [ ] **Ventas (el POS en sí)** — carrito, búsqueda rápida por código/nombre,
  cálculo de totales, aplicar descuento, cobrar.
- [ ] **Pagos** — efectivo, tarjeta, mixto (dividido entre métodos), cálculo de
  cambio.
- [ ] **Inventario** — stock por producto (por sucursal), movimientos
  (entrada/salida/ajuste) como bitácora inmutable, alertas de stock bajo.
- [ ] **Corte de caja / turnos** — apertura y cierre de caja, conteo de
  efectivo, cuadre contra lo vendido, por cajero.
- [ ] **Clientes** — registro básico, historial de compras (opcional, debe
  poder venderse a "público en general" sin registrar cliente).
- [ ] **Impuestos** — tasa de impuesto por producto/categoría, IVA incluido o
  no incluido en el precio.
- [ ] **Devoluciones y cancelaciones** — ligadas a la venta original, con
  reingreso de stock.
- [ ] **Tickets/recibos** — folio consecutivo, impresión, opcionalmente envío
  por correo.
- [ ] **Reportes básicos** — ventas por día/periodo, productos más vendidos,
  corte por cajero.
- [ ] **Permisos aplicados al POS** — usando los `Role` que ya existen (ej. un
  cajero no puede cancelar una venta sin autorización de un supervisor).

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
| `Category` | Categoría de producto. |
| `Product` | SKU, precio, costo, categoría, impuesto aplicable. |
| `Inventory` | Stock actual por producto + sucursal. |
| `InventoryMovement` | Bitácora inmutable de entradas/salidas/ajustes. |
| `Sale` | Encabezado de venta: sucursal, cajero, cliente, totales, fecha. |
| `SaleItem` | Líneas de la venta (producto, cantidad, precio, descuento). |
| `Payment` | Uno o más pagos por venta (efectivo/tarjeta/mixto). |
| `CashRegister` / `CashSession` | Turno de caja: apertura, cierre, conteo. |
| `Customer` | Cliente (opcional en la venta). |
| `TaxRate` | Tasa de impuesto reutilizable por producto/categoría. |
| `Supplier` (fase 2) | Proveedor. |
| `PurchaseOrder` (fase 2) | Orden de compra a proveedor. |

## Cómo se prioriza

Primero se completa **toda la Fase 1** antes de tocar Fase 2 — es lo que hace
que ya sea "un POS funcional" de verdad, y es la parte que se reutiliza sin
cambios sin importar el giro del negocio. Lo específico por giro (restaurante,
servicios) se deja como módulo aparte, no se mezcla en el core.
