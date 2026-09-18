# Árbol de permisos por acción (botones)

Inventario de todas las ventanas del sistema y las acciones de negocio (botones) que exponen,
con un id alfanumérico por acción para que el backend pueda asignarlas a roles de forma granular
(hoy la única granularidad que existe es a nivel de ventana completa, vía `NavigationRoute` /
`RoleListComponent.onManageRoutes`).

Generado a partir del código en `src/app/features` el 2026-09-16. Si se agregan pantallas o botones
nuevos, este archivo hay que actualizarlo a mano — no se genera automáticamente.

## Convención de ID

`MODULO.ACCION`, en mayúsculas, con `_` para separar palabras dentro de cada parte. El módulo es
estable por ventana (no cambia aunque cambie el texto del botón); la acción describe el efecto de
negocio, no el ícono ni la etiqueta.

## Qué cuenta como "acción" aquí

Solo botones que disparan una operación de negocio real contra el backend (crear, editar,
desactivar, exportar, abrir/cerrar turno, enviar correo, etc.). **No** se listan como acciones:

- Cerrar un modal (`rm-close`, la `x`) o su botón "Cancelar" — es navegación de UI, no de negocio.
- Cambiar de tab (ej. "Nueva venta" / "Historial" en Ventas) — es navegación, misma ventana.
- Paginación, ordenar tabla, expandir una fila (ej. el desglose de productos en Reportes).
- Filtros de solo lectura (selects de sucursal/usuario/fecha, el toggle "solo stock bajo").
- Botones de solo lectura tipo "Ver detalle" / "Ver historial" — ya cubiertos por el acceso a la
  ventana en sí (si no tienes acceso a la ventana, ni siquiera ves el botón).
- Las pantallas públicas/de sesión (`/login`, `/forgot-password`, `/reset-password`,
  `/change-password`) — no son parte del árbol de módulos de negocio, están disponibles para
  cualquier usuario autenticado (o sin autenticar) independientemente de su rol.
- `/dashboard` (`DashboardComponent`) y la ruta comodín `**` (`NotFoundComponent`) — no tienen
  ningún botón, solo texto.

## Notas de diseño para el frontend (cuando se conecte a permisos reales)

- **Crear y Editar comparten el mismo modal y el mismo botón "Guardar"** en casi todas las
  pantallas (`onSave()` decide internamente si crea o actualiza según `isEdit()`). Aun así son dos
  permisos distintos: el frontend debe ocultar el botón de entrada correspondiente por separado
  — el `+` "Nueva X" / `group-add-btn` para `CREATE`, el ícono de lápiz por fila para `EDIT` — y
  dejar que "Guardar" ejecute lo que corresponda según por cuál entró.
- `COMPANIES.UPLOAD_LOGO` no es un `<button>`, es un `<label>` que envuelve un `<input type="file"
  hidden>` (`onLogoFileSelected`). Cuenta igual como acción disparadora de un POST.
- El componente `src/app/features/nav-routes/components/route-form/` existe en el código pero no
  está enrutado ni se usa desde ningún otro componente (código muerto) — no aparece en el árbol.
- `ROLES.ASSIGN_ROUTES` es la función que hoy asigna ventanas completas a un rol
  (`RolesService.assignRoutes`). Es, en sí misma, la pantalla que probablemente vaya a evolucionar
  para asignar también estos ids de acción, no solo rutas.

---

## Árbol

```
Administración
├── Usuarios                         /admin/users            UserListComponent
│   ├── USERS.CREATE                 Nuevo usuario (+ variante "nuevo en esta empresa")
│   ├── USERS.EDIT                   Editar (ícono lápiz por fila)
│   ├── USERS.DEACTIVATE             Desactivar (ícono basura por fila)
│   ├── USERS.RESET_PASSWORD         Restablecer contraseña (ícono por fila)
│   └── USERS.REASSIGN_COMPANY       Cambiar de empresa (ícono por fila)
│
├── Rutas de navegación              /admin/routes           RouteListComponent
│   ├── NAV_ROUTES.CREATE            Nueva ruta (+ variante "nueva en esta empresa")
│   ├── NAV_ROUTES.EDIT              Editar (ícono lápiz por fila)
│   └── NAV_ROUTES.DEACTIVATE        Desactivar (ícono basura por fila)
│
├── Roles                            /admin/roles             RoleListComponent
│   ├── ROLES.CREATE                 Nuevo rol (+ variante "nuevo en esta empresa")
│   ├── ROLES.EDIT                   Editar (ícono lápiz por fila)
│   ├── ROLES.DEACTIVATE             Desactivar (ícono basura por fila)
│   └── ROLES.ASSIGN_ROUTES          Asignar rutas (ícono sitemap por fila)
│
└── Empresas                         /admin/empresas          CompanyListComponent
    ├── COMPANIES.CREATE             Nueva empresa
    ├── COMPANIES.EDIT               Editar (ícono lápiz por fila)
    ├── COMPANIES.DEACTIVATE         Desactivar (ícono basura por fila)
    └── COMPANIES.UPLOAD_LOGO        Subir/cambiar logo (dentro del modal de edición)

Catálogo
├── Sucursales                       /catalogo/sucursales     BranchListComponent
│   ├── BRANCHES.CREATE              Nueva sucursal (+ variante "nueva en esta empresa")
│   ├── BRANCHES.EDIT                Editar (ícono lápiz por fila)
│   └── BRANCHES.DEACTIVATE          Desactivar (ícono basura por fila)
│
├── Almacenes                        /catalogo/almacenes      WarehouseListComponent
│   ├── WAREHOUSES.CREATE            Nuevo almacén (+ variante "nuevo en esta empresa")
│   ├── WAREHOUSES.EDIT              Editar (ícono lápiz por fila)
│   ├── WAREHOUSES.DEACTIVATE        Desactivar (ícono basura por fila)
│   │
│   └── Detalle de almacén           /catalogo/almacenes/:id  WarehouseDetailComponent
│       ├── WAREHOUSES.ADD_PRODUCT       Agregar producto (alta con entrada inicial)
│       ├── WAREHOUSES.EDIT_MIN_STOCK    Editar stock mínimo (ícono campana por fila)
│       └── WAREHOUSES.REGISTER_MOVEMENT Registrar movimiento (ícono flechas por fila)
│
├── Inventario                       /catalogo/inventario     InventoryListComponent
│   └── (sin acciones de negocio — solo lectura: ver historial, filtro de stock bajo)
│
├── Impuestos                        /catalogo/impuestos      TaxRateListComponent
│   ├── TAX_RATES.CREATE             Nuevo impuesto (+ variante "nuevo en esta empresa")
│   ├── TAX_RATES.EDIT               Editar (ícono lápiz por fila)
│   └── TAX_RATES.DEACTIVATE         Desactivar (ícono basura por fila)
│
├── Categorías                       /catalogo/categorias     CategoryListComponent
│   ├── CATEGORIES.CREATE            Nueva categoría (+ variante "nueva en esta empresa")
│   ├── CATEGORIES.EDIT              Editar (ícono lápiz por fila)
│   └── CATEGORIES.DEACTIVATE        Desactivar (ícono basura por fila)
│
├── Productos                        /catalogo/productos      ProductListComponent
│   ├── PRODUCTS.CREATE              Nuevo producto (+ variante "nuevo en esta empresa")
│   ├── PRODUCTS.EDIT                Editar (ícono lápiz por fila)
│   └── PRODUCTS.DEACTIVATE          Desactivar (ícono basura por fila)
│
└── Clientes                         /catalogo/clientes       CustomerListComponent
    ├── CUSTOMERS.CREATE             Nuevo cliente (+ variante "nuevo en esta empresa")
    ├── CUSTOMERS.EDIT               Editar (ícono lápiz por fila)
    └── CUSTOMERS.DEACTIVATE         Desactivar (ícono basura por fila)

Caja
├── Cajas                            /caja/cajas              CashRegisterListComponent
│   ├── CASH_REGISTERS.CREATE        Nueva caja (+ variante "nueva en esta empresa")
│   ├── CASH_REGISTERS.EDIT          Editar (ícono lápiz por fila)
│   └── CASH_REGISTERS.DEACTIVATE    Desactivar (ícono basura por fila)
│
└── Turnos                           /caja/turnos             CashSessionListComponent
    ├── CASH_SESSIONS.OPEN           Abrir caja / abrir turno
    └── CASH_SESSIONS.CLOSE          Cerrar turno

Ventas                                /ventas                 SalePosComponent
├── SALES.REGISTER                   Registrar venta (tab "Nueva venta")
├── SALES.CANCEL                     Cancelar venta (ícono por fila, tab "Historial")
└── SALES.SEND_RECEIPT               Enviar recibo por correo (modal de recibo)

Devoluciones                          /devoluciones           ReturnListComponent
└── RETURNS.REGISTER                 Registrar devolución

Reportes                              /reportes               ReportListComponent
└── (sin acciones de negocio — las 3 pestañas son de solo lectura: ventas por periodo,
    productos más vendidos, cortes de caja)
```

---

## Tabla plana (para seed del backend)

| ID | Módulo (ventana) | Ruta | Componente | Acción | Disparador en UI |
|---|---|---|---|---|---|
| `USERS.CREATE` | Usuarios | `/admin/users` | `UserListComponent` | Crear usuario | Botón "Nuevo usuario" / `group-add-btn` |
| `USERS.EDIT` | Usuarios | `/admin/users` | `UserListComponent` | Editar usuario | Ícono lápiz por fila |
| `USERS.DEACTIVATE` | Usuarios | `/admin/users` | `UserListComponent` | Desactivar usuario | Ícono basura por fila |
| `USERS.RESET_PASSWORD` | Usuarios | `/admin/users` | `UserListComponent` | Restablecer contraseña | Ícono "reset" por fila |
| `USERS.REASSIGN_COMPANY` | Usuarios | `/admin/users` | `UserListComponent` | Cambiar de empresa | Ícono "mover" por fila |
| `NAV_ROUTES.CREATE` | Rutas de navegación | `/admin/routes` | `RouteListComponent` | Crear ruta | Botón "Nueva ruta" / `group-add-btn` |
| `NAV_ROUTES.EDIT` | Rutas de navegación | `/admin/routes` | `RouteListComponent` | Editar ruta | Ícono lápiz por fila |
| `NAV_ROUTES.DEACTIVATE` | Rutas de navegación | `/admin/routes` | `RouteListComponent` | Desactivar ruta | Ícono basura por fila |
| `ROLES.CREATE` | Roles | `/admin/roles` | `RoleListComponent` | Crear rol | Botón "Nuevo rol" / `group-add-btn` |
| `ROLES.EDIT` | Roles | `/admin/roles` | `RoleListComponent` | Editar rol | Ícono lápiz por fila |
| `ROLES.DEACTIVATE` | Roles | `/admin/roles` | `RoleListComponent` | Desactivar rol | Ícono basura por fila |
| `ROLES.ASSIGN_ROUTES` | Roles | `/admin/roles` | `RoleListComponent` | Asignar ventanas al rol | Ícono "sitemap" por fila |
| `COMPANIES.CREATE` | Empresas | `/admin/empresas` | `CompanyListComponent` | Crear empresa | Botón "Nueva empresa" |
| `COMPANIES.EDIT` | Empresas | `/admin/empresas` | `CompanyListComponent` | Editar empresa | Ícono lápiz por fila |
| `COMPANIES.DEACTIVATE` | Empresas | `/admin/empresas` | `CompanyListComponent` | Desactivar empresa | Ícono basura por fila |
| `COMPANIES.UPLOAD_LOGO` | Empresas | `/admin/empresas` | `CompanyListComponent` | Subir/cambiar logo | Input de archivo en modal de edición |
| `BRANCHES.CREATE` | Sucursales | `/catalogo/sucursales` | `BranchListComponent` | Crear sucursal | Botón "Nueva sucursal" / `group-add-btn` |
| `BRANCHES.EDIT` | Sucursales | `/catalogo/sucursales` | `BranchListComponent` | Editar sucursal | Ícono lápiz por fila |
| `BRANCHES.DEACTIVATE` | Sucursales | `/catalogo/sucursales` | `BranchListComponent` | Desactivar sucursal | Ícono basura por fila |
| `WAREHOUSES.CREATE` | Almacenes | `/catalogo/almacenes` | `WarehouseListComponent` | Crear almacén | Botón "Nuevo almacén" / `group-add-btn` |
| `WAREHOUSES.EDIT` | Almacenes | `/catalogo/almacenes` | `WarehouseListComponent` | Editar almacén | Ícono lápiz por fila |
| `WAREHOUSES.DEACTIVATE` | Almacenes | `/catalogo/almacenes` | `WarehouseListComponent` | Desactivar almacén | Ícono basura por fila |
| `WAREHOUSES.ADD_PRODUCT` | Almacenes (detalle) | `/catalogo/almacenes/:id` | `WarehouseDetailComponent` | Agregar producto al almacén | Botón "Agregar producto" |
| `WAREHOUSES.EDIT_MIN_STOCK` | Almacenes (detalle) | `/catalogo/almacenes/:id` | `WarehouseDetailComponent` | Editar stock mínimo | Ícono campana por fila |
| `WAREHOUSES.REGISTER_MOVEMENT` | Almacenes (detalle) | `/catalogo/almacenes/:id` | `WarehouseDetailComponent` | Registrar movimiento de inventario | Ícono flechas por fila |
| `TAX_RATES.CREATE` | Impuestos | `/catalogo/impuestos` | `TaxRateListComponent` | Crear impuesto | Botón "Nuevo impuesto" / `group-add-btn` |
| `TAX_RATES.EDIT` | Impuestos | `/catalogo/impuestos` | `TaxRateListComponent` | Editar impuesto | Ícono lápiz por fila |
| `TAX_RATES.DEACTIVATE` | Impuestos | `/catalogo/impuestos` | `TaxRateListComponent` | Desactivar impuesto | Ícono basura por fila |
| `CATEGORIES.CREATE` | Categorías | `/catalogo/categorias` | `CategoryListComponent` | Crear categoría | Botón "Nueva categoría" / `group-add-btn` |
| `CATEGORIES.EDIT` | Categorías | `/catalogo/categorias` | `CategoryListComponent` | Editar categoría | Ícono lápiz por fila |
| `CATEGORIES.DEACTIVATE` | Categorías | `/catalogo/categorias` | `CategoryListComponent` | Desactivar categoría | Ícono basura por fila |
| `PRODUCTS.CREATE` | Productos | `/catalogo/productos` | `ProductListComponent` | Crear producto | Botón "Nuevo producto" / `group-add-btn` |
| `PRODUCTS.EDIT` | Productos | `/catalogo/productos` | `ProductListComponent` | Editar producto | Ícono lápiz por fila |
| `PRODUCTS.DEACTIVATE` | Productos | `/catalogo/productos` | `ProductListComponent` | Desactivar producto | Ícono basura por fila |
| `CUSTOMERS.CREATE` | Clientes | `/catalogo/clientes` | `CustomerListComponent` | Crear cliente | Botón "Nuevo cliente" / `group-add-btn` |
| `CUSTOMERS.EDIT` | Clientes | `/catalogo/clientes` | `CustomerListComponent` | Editar cliente | Ícono lápiz por fila |
| `CUSTOMERS.DEACTIVATE` | Clientes | `/catalogo/clientes` | `CustomerListComponent` | Desactivar cliente | Ícono basura por fila |
| `CASH_REGISTERS.CREATE` | Cajas | `/caja/cajas` | `CashRegisterListComponent` | Crear caja | Botón "Nueva caja" / `group-add-btn` |
| `CASH_REGISTERS.EDIT` | Cajas | `/caja/cajas` | `CashRegisterListComponent` | Editar caja | Ícono lápiz por fila |
| `CASH_REGISTERS.DEACTIVATE` | Cajas | `/caja/cajas` | `CashRegisterListComponent` | Desactivar caja | Ícono basura por fila |
| `CASH_SESSIONS.OPEN` | Turnos | `/caja/turnos` | `CashSessionListComponent` | Abrir turno | Botón "Abrir caja" |
| `CASH_SESSIONS.CLOSE` | Turnos | `/caja/turnos` | `CashSessionListComponent` | Cerrar turno | Botón "Cerrar turno" |
| `SALES.REGISTER` | Ventas | `/ventas` | `SalePosComponent` | Registrar venta | Botón "Registrar venta" (tab "Nueva venta") |
| `SALES.CANCEL` | Ventas | `/ventas` | `SalePosComponent` | Cancelar venta | Ícono cancelar por fila (tab "Historial") |
| `SALES.SEND_RECEIPT` | Ventas | `/ventas` | `SalePosComponent` | Enviar recibo por correo | Botón "Enviar" en modal de recibo |
| `RETURNS.REGISTER` | Devoluciones | `/devoluciones` | `ReturnListComponent` | Registrar devolución | Botón "Nueva devolución" → "Registrar devolución" |

**Total: 46 acciones** en 15 ventanas con acciones de negocio (2 ventanas — Inventario y Reportes —
son de solo lectura y no aparecen en la tabla).
