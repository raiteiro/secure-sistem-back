# Paginación pendiente — auditoría de endpoints

Inventario de todos los `GET` que devuelven una lista (26 en total), para pedirle al backend que
agregue paginación donde haga falta. Igual que con `PERMISSIONS.md`, esto es una foto tomada del
código en `src/app/features` — si se agregan endpoints nuevos, hay que revisar si aplican aquí.

## Contrato propuesto

Dos query params nuevos, opcionales, con default para no romper nada de golpe:

```
GET /api/sales?page=1&pageSize=25
```

- `page`: 1-based, default `1`.
- `pageSize`: default `25`, tope sugerido `100` (evitar que alguien pida 10,000 de un jalón).

Y la respuesta pasa de ser un array plano a un sobre con metadata:

```json
{
  "items": [ /* lo mismo que antes */ ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 214,
  "totalPages": 9
}
```

**Esto es un breaking change** para cualquier endpoint donde se aplique — hoy el frontend espera
`T[]` directo, no `{ items: T[] }`. No es un cambio que se pueda ir aplicando endpoint por endpoint
sin avisar: en cuanto el backend cambie la forma de la respuesta de alguno de estos, hay que
avisarme para actualizar el `.service.ts` correspondiente en el mismo momento (si no, el frontend
truena intentando iterar un objeto como si fuera arreglo). Recomiendo migrar de a un endpoint a la
vez, no todos junto.

## Por qué la prioridad no es la misma en todos

Los catálogos (sucursales, categorías, roles, etc.) crecen con la operación del negocio — decenas o
cientos de filas, casi nunca miles. Las tablas transaccionales (ventas, movimientos de inventario,
turnos, devoluciones) crecen con el tiempo sin límite — con un año de operación diaria ya estamos
hablando de miles o decenas de miles de filas. Por eso la paginación es indispensable en el segundo
grupo y "bonito tenerla" en el primero.

---

## Prioridad alta — tablas transaccionales (crecen sin límite)

Necesitan paginación **y** filtro de rango de fecha (`from`/`to`) para que además de paginar se
pueda acotar el rango antes de pedir la página. `Sales` ya tiene `from`/`to`; el resto no.

| Endpoint | Filtros actuales | Falta agregar |
|---|---|---|
| `GET /api/Sales` | `branchId`, `customerId`, `cashSessionId`, `status`, `from`, `to` | Solo paginación (`from`/`to` ya existe) |
| `GET /api/Returns` | `saleId`, `cashSessionId` | `from`/`to` (por `createdAt`) + paginación |
| `GET /api/CashSessions` | `cashRegisterId`, `isOpen` | `from`/`to` (por `openedAt`) + paginación |
| `GET /api/InventoryMovements` | `productId`, `warehouseId` | `from`/`to` (por `createdAt`) + paginación — este es el que más rápido va a crecer, cada entrada/salida/venta/devolución genera una fila |
| `GET /api/Consignment/sales` | `supplierId`, `pending` | `from`/`to` (por `createdAt`) + paginación |
| `GET /api/Consignment/settlements` | *(ninguno)* | `from`/`to` (por `createdAt`) + paginación |

## Prioridad media — catálogos que sí pueden crecer bastante

No son un log que crece indefinidamente, pero un negocio con mucho movimiento sí puede acumular
cientos o miles de filas. No necesitan `from`/`to` (no son eventos en el tiempo, son catálogos).

| Endpoint | Nota |
|---|---|
| `GET /api/Products` | El catálogo completo puede crecer mucho en un negocio con muchos SKUs |
| `GET /api/Customers` | Puede crecer mucho en negocios con base de clientes grande |
| `GET /api/Inventory` | Una fila por producto × almacén — crece con el catálogo, no es un log |
| `GET /api/Users` | Crece con la plantilla de la empresa, normalmente no es enorme pero puede acumularse |
| `GET /api/reports/cashier-closeouts` | Ya tiene `from`/`to`; con rangos largos puede devolver muchos turnos cerrados |

## Prioridad baja — catálogos acotados (opcional, "nice to have")

Estos casi nunca van a tener más de unas cuantas decenas de filas por empresa. Paginar aquí es más
por consistencia que por necesidad real — no lo pediría de entrada, solo si sobra tiempo.

`GET /api/Branches`, `GET /api/CashRegisters`, `GET /api/Categories`, `GET /api/Companies` (acotado
al número de empresas del sistema), `GET /api/NavigationRoutes` y `/tree`, `GET /api/Roles`,
`GET /api/Roles/{id}/routes`, `GET /api/Roles/{id}/permissions`, `GET /api/Suppliers`,
`GET /api/TaxRates`, `GET /api/Warehouses`.

## No necesitan paginación

Están acotados por diseño (una fila por producto, por consignador, o ya limitados por un parámetro
propio) y paginar ahí sería sobre-ingeniería:

- `GET /api/Products/{id}/combo-items` — pocos componentes por combo.
- `GET /api/Consignment/balances` — una fila por proveedor consignador activo.
- `GET /api/reports/sales-by-period` — ya acotado por `from`/`to`, el tamaño lo define el rango de fechas, no crece indefinidamente.
- `GET /api/reports/top-products` — ya tiene `limit` (1–100) para esto mismo.

---

## Pendiente del lado del frontend (cuando el backend confirme el contrato)

Por cada endpoint que el backend migre a la respuesta con sobre (`{ items, page, pageSize,
totalCount, totalPages }`):
1. Actualizar el `.service.ts` correspondiente para que el método devuelva el sobre completo, no
   solo `items` — el componente necesita `totalCount` para dibujar el paginador.
2. En las pantallas con `p-table` (casi todas las de catálogo), activar `[paginator]="true"` con
   `[lazy]="true"` y pedir cada página al backend en el evento `(onLazyLoad)`, en vez de traer todo
   y paginar solo del lado del cliente como hace hoy Usuarios (que sí pagina, pero sobre el arreglo
   completo ya cargado — está bien mientras la lista quepa en memoria, pero no sirve una vez que el
   backend empiece a limitar cuántas filas manda por página).
3. En las pantallas de "historial" (Ventas, Devoluciones, Turnos, Consignaciones) que hoy son tablas
   HTML planas sin paginador de PrimeNG, hay que agregarles un paginador propio (mismo patrón que ya
   existe en Usuarios pero conectado al backend) y los inputs de `from`/`to` donde todavía no existen
   (Devoluciones, Turnos, Consignaciones) — Ventas ya los tiene.
